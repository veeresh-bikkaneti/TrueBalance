using Application.Imports;
using Application.Interfaces;
using Application.Ledger;
using Domain.Entities;
using Domain.Enums;
using Domain.Ledger;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using TrueBalance.Tests.Ledger;
using TrueBalance.Tests.Pii;

namespace TrueBalance.Tests.Imports;

public sealed class ImportFlowTests : IDisposable
{
    private const string Csv = "date,description,amount,balance\n" +
        "2026-06-15,\"Fake Vendor 1, \"\"store\"\"\",\"($1,234.50)\",100.00\n" +
        "06/16/2026,Fake Payer 1,$2000,2100.00\n" +
        "06/16/2026,Fake Payer 1,$2000,4100.00\n";

    private readonly string _path = Path.Combine(Path.GetTempPath(), $"tb-{Guid.NewGuid():N}.csv");
    private readonly string _db = Guid.NewGuid().ToString("N");
    private readonly Account _bank = LedgerTestData.Checking();
    private readonly Account _category = LedgerTestData.Expense();

    public ImportFlowTests()
    {
        using var db = NewDb();
        db.AddRange(_bank, _category, LedgerTestData.YearPeriod());
        db.SaveChanges();
        File.WriteAllText(_path, Csv);
    }

    public void Dispose() => File.Delete(_path);

    private OfflineCPADbContext NewDb()
        => new(new DbContextOptionsBuilder<OfflineCPADbContext>().UseInMemoryDatabase(_db).Options);

    private async Task<Guid> ImportAsync(OfflineCPADbContext db)
    {
        var store = new EfImportStore(db);
        var id = await new QueueImport(store, new SystemClock()).HandleAsync(Guid.NewGuid(), ".csv");
        var useCase = new ImportStatement(store, new PathVault(_path),
            new IStatementImporter[] { new CsvStatementImporter() }, new MaskingPiiMasker());
        await useCase.HandleAsync(new ImportStatementCommand(id, _bank.Id, null));
        return id;
    }

    private static PostStagedRow Poster(OfflineCPADbContext db)
    {
        var ledger = new EfLedgerStore(db);
        var clock = new SystemClock();
        return new PostStagedRow(new EfImportStore(db), ledger, new CreateDraftEntry(ledger, clock), new PostEntry(ledger, clock), clock);
    }

    [Fact]
    public async Task Csv_import_stages_signed_decimals_and_leaves_the_ledger_empty()
    {
        using var db = NewDb();
        await ImportAsync(db);

        var rows = db.StagedRows.OrderBy(r => r.RowNumber).ToList();
        Assert.Equal(new[] { -1234.50m, 2000m, 2000m }, rows.Select(r => r.Amount));
        Assert.Equal("Fake Vendor 1, \"store\"", rows[0].Description);
        Assert.Equal(new DateOnly(2026, 6, 16), rows[1].Date);
        // Identical rows inside one file are legitimate, not duplicates.
        Assert.All(rows, r => Assert.Equal(StagedRowStatus.Pending, r.Status));
        Assert.Empty(db.JournalEntries);
        Assert.Empty(db.Transactions);
    }

    [Fact]
    public async Task Importing_the_same_file_twice_flags_duplicates()
    {
        using var db = NewDb();
        await ImportAsync(db);
        var second = await ImportAsync(db);

        var rows = db.StagedRows.Where(r => r.BatchId == second).ToList();
        Assert.Equal(3, rows.Count);
        Assert.All(rows, r => Assert.Equal(StagedRowStatus.Duplicate, r.Status));
        await Assert.ThrowsAsync<LedgerRuleException>(() => Poster(db).HandleAsync(
            new PostStagedRowCommand(rows[0].Id, _category.Id, false, false, "local")));
        Assert.Empty(db.JournalEntries);
        await Poster(db).HandleAsync(new PostStagedRowCommand(rows[0].Id, _category.Id, true, false, "local"));
        Assert.Single(db.JournalEntries);
    }

    [Fact]
    public async Task Posting_a_row_creates_exactly_one_balanced_entry_and_rejects_a_second_post()
    {
        using var db = NewDb();
        await ImportAsync(db);
        var rowId = db.StagedRows.Single(r => r.RowNumber == 3).Id;
        // A second context that read the row before the first post: the concurrency token must stop it.
        using var stale = NewDb();
        await stale.StagedRows.SingleAsync(r => r.Id == rowId);
        var command = new PostStagedRowCommand(rowId, _category.Id, false, false, "local");

        var entryId = await Poster(db).HandleAsync(command);

        await Assert.ThrowsAsync<LedgerRuleException>(() => Poster(db).HandleAsync(command));
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => Poster(stale).HandleAsync(command));
        using var check = NewDb();
        var posted = Assert.Single(check.JournalEntries.Include(e => e.Postings)
            .Where(e => e.Status == JournalEntryStatus.Posted));
        Assert.Equal(entryId, posted.Id);
        Assert.Equal(2000m, posted.Postings.Single(p => p.AccountId == _bank.Id).Amount);
        Assert.Equal(-2000m, posted.Postings.Single(p => p.AccountId == _category.Id).Amount);
        var row = check.StagedRows.Single(r => r.Id == rowId);
        Assert.Equal(StagedRowStatus.Posted, row.Status);
        Assert.Equal(entryId, row.JournalEntryId);
        // Same payee, next time: the last used category is suggested.
        Assert.Equal(_category.Id, await new EfImportStore(check).LastPostedCategoryAsync(row.MatchKey));
    }

    [Theory]
    [InlineData("date,description,amount\n2026-06-15,x,1.005\n", "amount")]
    [InlineData("date,description,amount\n2026-06-15,x,\"1,5\"\n", "amount")]
    [InlineData("date,description,debit,credit\nsecretvalue,x,1,\n", "date")]
    public async Task Parse_errors_name_row_and_column_but_never_the_value(string csv, string column)
    {
        await File.WriteAllTextAsync(_path, csv);
        var ex = await Assert.ThrowsAsync<StatementParseException>(() => new CsvStatementImporter().ParseAsync(_path, null));
        Assert.Equal($"Row 2, column '{column}': the value could not be read.", ex.Message);
    }

    [Fact]
    public async Task Debit_columns_are_money_out_and_ofx_sgml_and_xml_parse()
    {
        await File.WriteAllTextAsync(_path, "date,description,debit,credit\n2026-06-15,a,5.25,\n2026-06-15,b,,7\n");
        var csv = await new CsvStatementImporter().ParseAsync(_path, null);
        Assert.Equal(new[] { -5.25m, 7m }, csv.Rows.Select(r => r.Amount));

        await File.WriteAllTextAsync(_path,
            "<BANKACCTFROM><ACCTID>000</BANKACCTFROM><BANKTRANLIST>\n<STMTTRN>\n<DTPOSTED>20260615120000\n<TRNAMT>-12.30\n<NAME>Fake Vendor 1\n</STMTTRN>\n" +
            "<STMTTRN><DTPOSTED>20260616</DTPOSTED><TRNAMT>4.00</TRNAMT><MEMO>A &amp; B</MEMO></STMTTRN></BANKTRANLIST>");
        var ofx = await new OfxStatementImporter().ParseAsync(_path, null);
        Assert.Equal(new[] { -12.30m, 4.00m }, ofx.Rows.Select(r => r.Amount));
        Assert.Equal(new[] { "Fake Vendor 1", "A & B" }, ofx.Rows.Select(r => r.Description));
        Assert.False(ofx.IsExperimental);
    }

    private sealed class PathVault(string path) : IDocumentVault
    {
        public Task<Application.Uploads.StoredUpload> SaveAsync(Stream content, long length, string originalFileName,
            string? contentType, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public string? FindPath(Guid documentId, string extension) => path;
    }
}

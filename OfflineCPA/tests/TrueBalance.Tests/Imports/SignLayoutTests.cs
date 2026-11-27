using System.Text.Json;
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
using TrueBalance.Tests.Pii.Fixtures;

namespace TrueBalance.Tests.Imports;

public sealed class SignLayoutTests : IDisposable
{
    private const string Bank = "Fake Bank 1";
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"tb-{Guid.NewGuid():N}.csv");
    private readonly string _db = Guid.NewGuid().ToString("N");
    private readonly Account _bank = LedgerTestData.Checking();
    private readonly Account _category = LedgerTestData.Expense();

    public SignLayoutTests()
    {
        using var db = NewDb();
        db.AddRange(_bank, _category, LedgerTestData.YearPeriod());
        db.SaveChanges();
    }

    public void Dispose() => File.Delete(_path);

    private OfflineCPADbContext NewDb()
        => new(new DbContextOptionsBuilder<OfflineCPADbContext>().UseInMemoryDatabase(_db).Options);

    private ImportStatement Importer(OfflineCPADbContext db)
        => new(new EfImportStore(db), new Vault(_path),
            new IStatementImporter[] { new CsvStatementImporter() }, new MaskingPiiMasker());

    private async Task<Guid> QueueAsync(OfflineCPADbContext db, string csv)
    {
        await File.WriteAllTextAsync(_path, csv);
        return await new QueueImport(new EfImportStore(db), new SystemClock()).HandleAsync(Guid.NewGuid(), ".csv");
    }

    private async Task ImportAsync(OfflineCPADbContext db, string csv, ConfirmedLayout? layout = null)
    {
        var id = await QueueAsync(db, csv);
        await Importer(db).HandleAsync(new ImportStatementCommand(
            id, _bank.Id, Bank, layout ?? new ConfirmedLayout(AmountLayout.Signed)));
    }

    private Task<CsvLayoutPreview> PreviewAsync(OfflineCPADbContext db, Guid id)
        => new PreviewCsvLayout(new EfImportStore(db), new Vault(_path),
            new IStatementImporter[] { new CsvStatementImporter() }, new MaskingPiiMasker()).HandleAsync(id, Bank);

    private static PostStagedRow Poster(OfflineCPADbContext db)
        => new(new EfImportStore(db), new EfLedgerStore(db), new SystemClock());

    [Theory]
    [InlineData(AmountLayout.Signed, false, "amount\n2026-06-15,a,-5\n2026-06-16,b,7\n")]
    [InlineData(AmountLayout.SignedReversed, false, "amount\n2026-06-15,a,5\n2026-06-16,b,-7\n")]
    [InlineData(AmountLayout.DebitCredit, false, "debit,credit\n2026-06-15,a,5,\n2026-06-16,b,,7\n")]
    [InlineData(AmountLayout.DebitCredit, true, "debit,credit\n2026-06-15,a,-5,\n2026-06-16,b,,7\n")]
    [InlineData(AmountLayout.Indicator, false, "amount,type\n2026-06-15,a,5,DB\n2026-06-16,b,7,cr\n")]
    public async Task Each_layout_yields_the_right_signs(AmountLayout layout, bool carryMinus, string body)
    {
        using var db = NewDb();
        var indicator = layout == AmountLayout.Indicator;
        await ImportAsync(db, "date,description," + body, new ConfirmedLayout(layout, carryMinus,
            indicator ? "type" : null, indicator ? "CR" : null, indicator ? "DB" : null));

        Assert.Equal(new[] { -5m, 7m }, db.StagedRows.OrderBy(r => r.RowNumber).Select(r => r.Amount).ToArray());
    }

    [Fact]
    public async Task Refund_only_debit_file_is_money_in_when_debits_do_not_carry_minus()
    {
        using var db = NewDb();
        await ImportAsync(db, "date,description,debit,credit\n2026-06-15,a,-5,\n",
            new ConfirmedLayout(AmountLayout.DebitCredit, DebitsCarryMinus: false));

        Assert.Equal(5m, db.StagedRows.Single().Amount);
    }

    [Fact]
    public async Task Preview_proposes_a_layout_and_writes_nothing()
    {
        using var db = NewDb();
        // Signs are reversed here, and only the running balance can show it.
        var id = await QueueAsync(db, "date,description,amount,balance\n" +
            "2026-06-15,a,5,95\n2026-06-16,b,-7,102\n2026-06-17,c,2,100\n2026-06-18,d,1,99\n");

        var preview = await PreviewAsync(db, id);

        Assert.Equal(AmountLayout.SignedReversed, preview.Layout.AmountLayout);
        Assert.True(preview.BalanceBacksIt);
        Assert.Equal(new[] { "MoneyOut", "MoneyIn", "MoneyOut" }, preview.Rows.Select(r => r.Direction));
        Assert.Equal(new[] { 5m, 7m, 2m }, preview.Rows.Select(r => r.Amount));
        using var check = NewDb();
        Assert.Empty(check.StagedRows);
        Assert.Empty(check.CsvMappingProfiles);
        Assert.Equal(ImportBatchStatus.Queued, check.ImportBatches.Single().Status);
    }

    [Fact]
    public async Task Preview_carries_no_raw_description()
    {
        using var db = NewDb();
        var text = FakePii.SampleStatementText().ReplaceLineEndings(" ").Replace("\"", "\"\"");
        var id = await QueueAsync(db, $"date,description,amount\n2026-06-15,\"{text}\",5\n");

        var json = JsonSerializer.Serialize(await PreviewAsync(db, id));

        Assert.All(FakePii.LeakTargets(), raw => Assert.DoesNotContain(raw, json, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Unconfirmed_profile_is_refused_and_a_confirmed_layout_saves_and_imports()
    {
        using var db = NewDb();
        await new SaveCsvProfile(new EfImportStore(db), new MaskingPiiMasker()).HandleAsync(Bank,
            new CsvColumnMap("date", "description", "amount", null, null, null, null));
        var id = await QueueAsync(db, "date,description,amount\n2026-06-15,a,5\n");

        foreach (var name in new[] { Bank, null })
        {
            var ex = await Assert.ThrowsAsync<LedgerRuleException>(
                () => Importer(db).HandleAsync(new ImportStatementCommand(id, _bank.Id, name)));
            Assert.Equal("Confirm how this bank signs amounts first.", ex.Message);
        }
        Assert.Equal(ImportBatchStatus.Queued, db.ImportBatches.Single().Status);

        await Importer(db).HandleAsync(new ImportStatementCommand(
            id, _bank.Id, Bank, new ConfirmedLayout(AmountLayout.SignedReversed)));

        using var check = NewDb();
        var profile = check.CsvMappingProfiles.Single();
        Assert.True(profile.LayoutConfirmed);
        Assert.Equal(AmountLayout.SignedReversed, profile.AmountLayout);
        Assert.Equal(-5m, check.StagedRows.Single().Amount);
        // The saved confirmation is enough next time.
        var next = await QueueAsync(db, "date,description,amount\n2026-06-16,b,3\n");
        Assert.Equal(1, await Importer(db).HandleAsync(new ImportStatementCommand(next, _bank.Id, Bank)));
    }

    [Theory]
    [InlineData("2026-06-15,a,10,110\n2026-06-16,b,-5,105\n", BalanceCheck.Passed)]
    [InlineData("2026-06-16,b,-5,105\n2026-06-15,a,10,110\n", BalanceCheck.Passed)]
    [InlineData("2026-06-15,a,10,110\n2026-06-16,b,-5,999\n", BalanceCheck.Failed)]
    [InlineData("2026-06-15,a,10,\n2026-06-16,b,-5,105\n", BalanceCheck.NotAvailable)]
    public async Task Balance_check_is_stored_on_the_batch(string body, BalanceCheck expected)
    {
        using var db = NewDb();
        await ImportAsync(db, "date,description,amount,balance\n" + body);

        Assert.Equal(expected, db.ImportBatches.Single().BalanceCheck);
    }

    [Fact]
    public async Task Failed_balance_check_blocks_posting_without_review()
    {
        using var db = NewDb();
        await ImportAsync(db, "date,description,amount,balance\n2026-06-15,a,-10,110\n2026-06-16,b,5,105\n");
        var rows = db.StagedRows.ToList();

        Assert.Equal("The running balance does not match these amounts. The opposite sign would match, " +
            "so check this bank's sign layout. Review each row before posting.", db.ImportBatches.Single().BalanceReason);
        foreach (var row in rows)
            Assert.Throws<LedgerRuleException>(() => row.EnsureCanPost(false, reviewed: false));
        await Poster(db).HandleAsync(new PostStagedRowCommand(rows[0].Id, _category.Id, false, true, "local"));
        Assert.Single(db.JournalEntries);
    }

    [Fact]
    public async Task Suggested_category_follows_the_payee_not_the_sign()
    {
        using var db = NewDb();
        await ImportAsync(db, "date,description,amount\n2026-06-15,Fake Vendor 1,-20\n2026-06-16,Fake Vendor 1,8\n");
        var rows = db.StagedRows.OrderBy(r => r.RowNumber).ToList();
        await Poster(db).HandleAsync(new PostStagedRowCommand(rows[0].Id, _category.Id, false, false, "local"));

        Assert.True(rows[1].Amount > 0m);
        Assert.Equal(_category.Id, await new EfImportStore(db).LastPostedCategoryAsync(rows[1].MatchKey));
    }

    private sealed class Vault(string path) : IDocumentVault
    {
        public Task<Application.Uploads.StoredUpload> SaveAsync(Stream content, long length, string originalFileName,
            string? contentType, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public string? FindPath(Guid documentId, string extension) => path;
    }
}

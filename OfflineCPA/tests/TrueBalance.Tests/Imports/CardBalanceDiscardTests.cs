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

public sealed class CardBalanceDiscardTests : IDisposable
{
    private const string Bank = "Fake Bank 1";
    // Two charges and a payment on a card: the amount owed rises as money goes out.
    private const string OwedCsv = "date,description,amount,balance\n" +
        "2026-06-15,a,-10,110\n2026-06-16,b,-25,135\n2026-06-17,c,40,95\n";
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"tb-{Guid.NewGuid():N}.csv");
    private readonly string _db = Guid.NewGuid().ToString("N");
    private readonly Account _category = LedgerTestData.Expense();
    private readonly Account _card = LedgerTestData.Payable();

    public CardBalanceDiscardTests()
    {
        _card.Type = AccountType.Liability;
        using var db = NewDb();
        db.AddRange(_category, _card, LedgerTestData.YearPeriod());
        db.SaveChanges();
    }

    public void Dispose() => File.Delete(_path);

    private OfflineCPADbContext NewDb()
        => new(new DbContextOptionsBuilder<OfflineCPADbContext>().UseInMemoryDatabase(_db).Options);

    private async Task<Guid> QueueAsync(OfflineCPADbContext db, string csv)
    {
        await File.WriteAllTextAsync(_path, csv);
        return await new QueueImport(new EfImportStore(db), new SystemClock()).HandleAsync(Guid.NewGuid(), ".csv");
    }

    private ImportStatement Importer(OfflineCPADbContext db)
        => new(new EfImportStore(db), new Vault(_path),
            new IStatementImporter[] { new CsvStatementImporter() }, new MaskingPiiMasker());

    private Task<int> ImportAsync(OfflineCPADbContext db, Guid id, ConfirmedLayout? layout = null)
        => Importer(db).HandleAsync(new ImportStatementCommand(
            id, _card.Id, Bank, layout ?? new ConfirmedLayout(AmountLayout.Signed)));

    private Task<CsvLayoutPreview> PreviewAsync(OfflineCPADbContext db, Guid id, Guid? account)
        => new PreviewCsvLayout(new EfImportStore(db), new Vault(_path),
            new IStatementImporter[] { new CsvStatementImporter() }, new MaskingPiiMasker()).HandleAsync(id, Bank, account);

    private static Task<int> DiscardAsync(OfflineCPADbContext db, Guid id)
        => new DiscardStagedBatch(new EfImportStore(db), new EfLedgerStore(db), new SystemClock()).HandleAsync(id);

    private Task<Guid> PostAsync(OfflineCPADbContext db, Guid rowId)
        => new PostStagedRow(new EfImportStore(db), new EfLedgerStore(db), new SystemClock())
            .HandleAsync(new PostStagedRowCommand(rowId, _category.Id, true, true, "local"));

    [Fact]
    public async Task Owed_balance_passes_on_a_card_and_fails_with_a_reason_when_read_as_available()
    {
        using var db = NewDb();
        var right = await QueueAsync(db, OwedCsv);
        await ImportAsync(db, right);
        var wrong = await QueueAsync(db, OwedCsv);
        await ImportAsync(db, wrong, new ConfirmedLayout(AmountLayout.Signed, BalanceMeaning: BalanceMeaning.Available));

        Assert.Equal(BalanceCheck.Passed, db.ImportBatches.Single(b => b.Id == right).BalanceCheck);
        var failed = db.ImportBatches.Single(b => b.Id == wrong);
        Assert.Equal(BalanceCheck.Failed, failed.BalanceCheck);
        Assert.Contains("The balance column reads as an amount owed", failed.BalanceReason);
        Assert.DoesNotContain("135", failed.BalanceReason);
    }

    [Fact]
    public async Task Negative_owed_balance_passes_with_available_confirmed_on_the_profile()
    {
        using var db = NewDb();
        const string csv = "date,description,amount,balance\n2026-06-15,a,-10,-110\n2026-06-16,b,-25,-135\n";
        await ImportAsync(db, await QueueAsync(db, csv),
            new ConfirmedLayout(AmountLayout.Signed, BalanceMeaning: BalanceMeaning.Available));
        // The second import sends no layout, so the saved meaning has to be used.
        var next = await QueueAsync(db, csv);
        await Importer(db).HandleAsync(new ImportStatementCommand(next, _card.Id, Bank));

        Assert.All(db.ImportBatches.ToList(), b => Assert.Equal(BalanceCheck.Passed, b.BalanceCheck));
        Assert.Equal(BalanceMeaning.Available, db.CsvMappingProfiles.Single().BalanceMeaning);
    }

    [Fact]
    public async Task Preview_reads_a_card_balance_as_owed_only_when_it_knows_the_account()
    {
        using var db = NewDb();
        var id = await QueueAsync(db, OwedCsv);

        var card = await PreviewAsync(db, id, _card.Id);
        var unknown = await PreviewAsync(db, id, null);

        Assert.Equal(AmountLayout.Signed, card.Layout.AmountLayout);
        Assert.True(card.BalanceBacksIt);
        Assert.Equal(BalanceMeaning.Owed, card.BalanceMeaning);
        Assert.Equal(BalanceMeaning.Available, unknown.BalanceMeaning);
        Assert.Equal("No bank account was given, so the balance was read as money available.", unknown.BalanceMeaningNote);
    }

    [Fact]
    public async Task Discard_requeues_the_batch_and_a_new_layout_stages_fresh_rows()
    {
        using var db = NewDb();
        var id = await QueueAsync(db, OwedCsv);
        await ImportAsync(db, id, new ConfirmedLayout(AmountLayout.SignedReversed));

        Assert.Equal(3, await DiscardAsync(db, id));

        using (var check = NewDb())
        {
            var batch = check.ImportBatches.Single();
            Assert.Equal(ImportBatchStatus.Queued, batch.Status);
            Assert.Equal(BalanceCheck.NotAvailable, batch.BalanceCheck);
            Assert.Null(batch.BalanceReason);
            Assert.Equal(1, batch.ResetCount);
            Assert.NotNull(batch.LastResetAtUtc);
            Assert.All(check.StagedRows.ToList(), r => Assert.Equal(StagedRowStatus.Discarded, r.Status));
        }

        await ImportAsync(db, id);
        var fresh = await new EfImportStore(db).GetRowsAsync(id);
        Assert.Equal(new[] { -10m, -25m, 40m }, fresh.Select(r => r.Amount));
        Assert.Equal(6, db.StagedRows.Count());
    }

    [Fact]
    public async Task Discard_is_refused_while_an_entry_stands_and_allowed_once_reversed()
    {
        using var db = NewDb();
        var id = await QueueAsync(db, OwedCsv);
        await ImportAsync(db, id);
        var entry = await PostAsync(db, db.StagedRows.First().Id);

        var ex = await Assert.ThrowsAsync<LedgerRuleException>(() => DiscardAsync(db, id));
        Assert.Equal("Reverse the posted entries from this batch first. Rows still posted: 1.", ex.Message);
        Assert.Equal(ImportBatchStatus.Staged, db.ImportBatches.Single().Status);

        await new ReverseEntry(new EfLedgerStore(db), new SystemClock())
            .HandleAsync(new ReverseEntryCommand(entry, LedgerTestData.Day, "Fake reversal", "local"));
        Assert.Equal(3, await DiscardAsync(db, id));
    }

    [Fact]
    public async Task Discarded_rows_are_not_duplicates_and_cannot_be_posted()
    {
        using var db = NewDb();
        var id = await QueueAsync(db, OwedCsv);
        await ImportAsync(db, id);
        var old = db.StagedRows.First().Id;
        await DiscardAsync(db, id);
        await ImportAsync(db, id);

        Assert.All(await new EfImportStore(db).GetRowsAsync(id), r => Assert.Equal(StagedRowStatus.Pending, r.Status));
        Assert.Equal(6, (await new EfImportStore(db).GetRowsAsync(id, includeDiscarded: true)).Count);
        var ex = await Assert.ThrowsAsync<LedgerRuleException>(() => PostAsync(db, old));
        Assert.Equal("This row was discarded.", ex.Message);
    }

    [Fact]
    public async Task Preview_and_discard_responses_carry_no_raw_description()
    {
        using var db = NewDb();
        var text = FakePii.SampleStatementText().ReplaceLineEndings(" ").Replace("\"", "\"\"");
        var id = await QueueAsync(db, $"date,description,amount\n2026-06-15,\"{text}\",5\n");
        var preview = await PreviewAsync(db, id, _card.Id);
        await ImportAsync(db, id);

        var json = JsonSerializer.Serialize(new { preview, discarded = await DiscardAsync(db, id) });

        Assert.Contains("\"discarded\":1", json);
        Assert.All(FakePii.LeakTargets(), raw => Assert.DoesNotContain(raw, json, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Confirming_again_without_a_balance_meaning_keeps_the_saved_one()
    {
        using var db = NewDb();
        const string csv = "date,description,amount,balance\n2026-06-15,a,-10,-110\n2026-06-16,b,-25,-135\n";
        await ImportAsync(db, await QueueAsync(db, csv),
            new ConfirmedLayout(AmountLayout.Signed, BalanceMeaning: BalanceMeaning.Available));
        var next = await QueueAsync(db, csv);
        await ImportAsync(db, next, new ConfirmedLayout(AmountLayout.Signed));

        Assert.Equal(BalanceMeaning.Available, db.CsvMappingProfiles.Single().BalanceMeaning);
        Assert.Equal(BalanceCheck.Passed, db.ImportBatches.Single(b => b.Id == next).BalanceCheck);
    }

    private sealed class Vault(string path) : IDocumentVault
    {
        public Task<Application.Uploads.StoredUpload> SaveAsync(Stream content, long length, string originalFileName,
            string? contentType, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public string? FindPath(Guid documentId, string extension) => path;
    }
}

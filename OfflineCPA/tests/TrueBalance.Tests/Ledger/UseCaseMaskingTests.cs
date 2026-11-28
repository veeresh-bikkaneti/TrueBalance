using System.Net.Http.Json;
using Application.Interfaces;
using Application.Ledger;
using Backend.Endpoints;
using Domain.Entities;
using Domain.Pii;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TrueBalance.Tests.Pii;
using TrueBalance.Tests.Pii.Fixtures;
using static TrueBalance.Tests.Ledger.LedgerTestData;

namespace TrueBalance.Tests.Ledger;

/// <summary>Memos are masked inside the use cases, so no caller can store them raw.</summary>
public class UseCaseMaskingTests
{
    private static readonly string Memo = $"Fake memo SSN {FakePii.SsnFormatted}";
    private static readonly string LineMemo = $"Fake line SSN {FakePii.SsnFormatted}";

    [Fact]
    public async Task Create_draft_called_directly_stores_both_memos_masked()
    {
        var period = YearPeriod();
        var store = StoreWith(period);
        var masker = new MaskingPiiMasker();

        var id = await new CreateDraftEntry(store, new FakeClock(), masker)
            .HandleAsync(new CreateDraftEntryCommand(Day, Memo, period.Id,
                [new(Expense().Id, 5m, LineMemo), new(Checking().Id, -5m, null)], "local"));

        var entry = (await store.FindEntryAsync(id))!;
        Assert.Equal(masker.Mask(Memo), entry.Memo);
        Assert.DoesNotContain(FakePii.SsnFormatted, entry.Memo, StringComparison.Ordinal);
        Assert.Equal(masker.Mask(LineMemo), entry.Postings[0].Memo);
        Assert.DoesNotContain(FakePii.SsnFormatted, entry.Postings[0].Memo!, StringComparison.Ordinal);
        Assert.Null(entry.Postings[1].Memo);
    }

    [Fact]
    public async Task Reverse_stores_its_memo_masked()
    {
        var period = YearPeriod();
        var store = StoreWith(period);
        var original = PostedEntry(period);
        await store.AddEntryAsync(original);
        var masker = new MaskingPiiMasker();

        var id = await new ReverseEntry(store, new FakeClock(), masker)
            .HandleAsync(new ReverseEntryCommand(original.Id, Day, Memo, "local"));

        var reversal = (await store.FindEntryAsync(id))!;
        Assert.Equal(masker.Mask(Memo), reversal.Memo);
        Assert.DoesNotContain(FakePii.SsnFormatted, reversal.Memo, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Memos_posted_through_the_endpoint_are_masked_exactly_once()
    {
        var period = YearPeriod();
        var store = StoreWith(period);
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<IPiiMasker, WrappingMasker>();
        builder.Services.AddSingleton<ILedgerStore>(store);
        builder.Services.AddSingleton<IClock, FakeClock>();
        builder.Services.AddScoped<CreateDraftEntry>();
        builder.Services.AddScoped<PostEntry>();
        builder.Services.AddScoped<ReverseEntry>();
        builder.Services.AddScoped<LockPeriod>();
        builder.Services.AddScoped<GetAccountBalances>();
        await using var app = builder.Build();
        app.MapLedgerEndpoints();
        await app.StartAsync();
        var client = app.GetTestClient();

        var response = await client.PostAsJsonAsync("/api/ledger/entries", new CreateEntryRequest(
            Day, "memo", period.Id, [new(Expense().Id, 5m, "line"), new(Checking().Id, -5m, null)]));
        response.EnsureSuccessStatusCode();
        var entry = (await store.FindEntryAsync((await response.Content.ReadFromJsonAsync<Created>())!.Id))!;
        Assert.Equal("masked(memo)", entry.Memo);
        Assert.Equal("masked(line)", entry.Postings[0].Memo);

        (await client.PostAsync($"/api/ledger/entries/{entry.Id}/post", null)).EnsureSuccessStatusCode();
        var reversed = await client.PostAsJsonAsync(
            $"/api/ledger/entries/{entry.Id}/reverse", new ReverseEntryRequest(Day, "undo"));
        reversed.EnsureSuccessStatusCode();
        var reversal = (await store.FindEntryAsync((await reversed.Content.ReadFromJsonAsync<Created>())!.Id))!;
        Assert.Equal("masked(undo)", reversal.Memo);
        Assert.Equal("masked(line)", reversal.Postings[0].Memo);
    }

    [Fact]
    public async Task Posting_a_staged_row_does_not_mask_its_token_a_second_time()
    {
        // A fingerprint can hold a long digit run, which a second pass would read as an account.
        const string Masked = "Fake payment [ACCT ****0123 fp:ab1234567890cdef]";
        await using var db = new OfflineCPADbContext(new DbContextOptionsBuilder<OfflineCPADbContext>()
            .UseInMemoryDatabase($"staged-memo-{Guid.NewGuid():N}").Options);
        var bank = Checking();
        var category = Expense();
        var batch = ImportBatch.Queue(Guid.NewGuid(), ".csv", Now);
        var row = StagedRow.Stage(batch, bank.Id, 1, Day, Masked, -5m, null, false);
        db.AddRange(bank, category, YearPeriod(), batch, row);
        db.SaveChanges();

        var id = await new PostStagedRow(new EfImportStore(db), new EfLedgerStore(db), new SystemClock())
            .HandleAsync(new PostStagedRowCommand(row.Id, category.Id, false, false, "local"));

        Assert.NotEqual(Masked, new MaskingPiiMasker().Mask(Masked));
        Assert.Equal(Masked, db.JournalEntries.Single(e => e.Id == id).Memo);
    }

    private sealed record Created(Guid Id);

    private sealed class WrappingMasker : IPiiMasker
    {
        public string Mask(string text) => $"masked({text})";

        public PiiMaskingResult MaskDetailed(string text) => new(Mask(text), []);
    }
}

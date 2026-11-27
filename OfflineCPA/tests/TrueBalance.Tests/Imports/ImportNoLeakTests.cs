using Application.Imports;
using Application.Interfaces;
using Domain.Ledger;
using Infrastructure.Data;
using Infrastructure.Options;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using TrueBalance.Tests.Ledger;
using TrueBalance.Tests.Pii;
using TrueBalance.Tests.Pii.Fixtures;

namespace TrueBalance.Tests.Imports;

public class ImportNoLeakTests
{
    [Theory]
    [InlineData(".csv")]
    [InlineData(".ofx")]
    public async Task Real_importers_never_store_or_report_raw_values(string extension)
    {
        var root = Path.Combine(Path.GetTempPath(), "tb-leak-" + Guid.NewGuid().ToString("N"));
        try
        {
            var text = FakePii.SampleStatementText().ReplaceLineEndings(" ");
            var vault = new SecureUploadStore(Microsoft.Extensions.Options.Options.Create(new UploadOptions { Directory = root }));
            using var db = new OfflineCPADbContext(new DbContextOptionsBuilder<OfflineCPADbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options);
            var bank = LedgerTestData.Checking();
            db.Add(bank);
            db.SaveChanges();
            var store = new EfImportStore(db);
            var masker = new MaskingPiiMasker();
            var import = new ImportStatement(store, vault,
                new IStatementImporter[] { new CsvStatementImporter(), new OfxStatementImporter() }, masker);

            async Task<Guid> QueueAsync(string content)
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(content);
                var stored = await vault.SaveAsync(new MemoryStream(bytes), bytes.Length, $"{FakePii.AccountNumberA}{extension}", null);
                return await new QueueImport(store, new SystemClock()).HandleAsync(stored.Id, stored.Extension);
            }

            var good = extension == ".csv"
                ? $"date,description,amount\n2026-06-15,\"{text.Replace("\"", "\"\"")}\",-1.00\n"
                : $"<ACCTID>{FakePii.AccountNumberA}\n<STMTTRN><DTPOSTED>20260615<TRNAMT>-1.00<NAME>{text}</STMTTRN>";
            var bad = extension == ".csv"
                ? $"date,description,amount\n2026-06-15,x,{FakePii.AccountNumberA}x\n"
                : $"<STMTTRN><DTPOSTED>20260615<TRNAMT>{FakePii.AccountNumberA}x</STMTTRN>";

            await import.HandleAsync(new ImportStatementCommand(await QueueAsync(good), bank.Id, extension == ".csv" ? "Fake Bank 1" : null, new ConfirmedLayout(Domain.Enums.AmountLayout.Signed)));
            var badId = await QueueAsync(bad);
            var error = await Assert.ThrowsAsync<LedgerRuleException>(
                () => import.HandleAsync(new ImportStatementCommand(badId, bank.Id,
                    extension == ".csv" ? "Fake Bank 1" : null, new ConfirmedLayout(Domain.Enums.AmountLayout.Signed))));

            Assert.Single(db.StagedRows);
            var stored = db.ChangeTracker.Entries()
                .SelectMany(e => e.Properties)
                .Select(p => p.CurrentValue as string)
                .Append(error.Message)
                .Where(v => v is not null)
                .ToList();
            foreach (var raw in FakePii.LeakTargets())
            {
                Assert.All(stored, v => Assert.DoesNotContain(raw, v!, StringComparison.Ordinal));
            }
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }
}

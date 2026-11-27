using System.Text;
using Application.Imports;
using Application.Interfaces;
using Application.Uploads;
using Backend.Services;
using Domain.Enums;
using Infrastructure.Data;
using Infrastructure.Options;
using Infrastructure.Services;
using Infrastructure.Vault;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using TrueBalance.Tests.Ledger;
using TrueBalance.Tests.Pii;
using TrueBalance.Tests.Pii.Fixtures;

namespace TrueBalance.Tests.Vault;

public sealed class VaultTests : IDisposable
{
    private const string Passphrase = "fake passphrase for tests";
    private const string Locked = "Unlock the books first.";

    private static readonly string Csv = "date,description,amount,balance\n" +
        $"2026-06-15,Fake Payer {FakePii.SsnFormatted} acct {FakePii.AccountNumberA},10.00,10.00\n" +
        $"2026-06-16,Fake Vendor card {FakePii.CardNumberSpaced},-4.00,6.00\n";

    private readonly string _root = Directory.CreateTempSubdirectory("tb-vault-").FullName;
    private string Uploads => Path.Combine(_root, "uploads");

    public void Dispose() => Directory.Delete(_root, recursive: true);

    private EncryptedFileVault NewVault() =>
        new(Microsoft.Extensions.Options.Options.Create(new UploadOptions { Directory = Uploads }));

    private async Task<EncryptedFileVault> UnlockedVaultAsync()
    {
        var vault = NewVault();
        await vault.UnlockAsync(Passphrase);
        return vault;
    }

    private static Task<StoredUpload> SaveAsync(IDocumentVault vault, string text) =>
        vault.SaveAsync(new MemoryStream(Encoding.UTF8.GetBytes(text)), Encoding.UTF8.GetByteCount(text), "statement.csv", "text/csv");

    private static async Task<string?> ReadAsync(IDocumentVault vault, StoredUpload stored)
    {
        await using var stream = await vault.OpenReadAsync(stored.Id, stored.Extension);
        return stream is null ? null : await new StreamReader(stream).ReadToEndAsync();
    }

    private void AssertNoPlainContent(string directory)
    {
        foreach (var file in Directory.GetFiles(directory, "*", SearchOption.AllDirectories))
        {
            var text = Encoding.Latin1.GetString(File.ReadAllBytes(file));
            foreach (var raw in FakePii.AllRaw.Append("Fake Payer").Append("Fake Vendor"))
                Assert.False(text.Contains(raw, StringComparison.Ordinal), $"{Path.GetFileName(file)} holds plain text.");
        }
    }

    [Fact]
    public async Task A_stored_document_reads_back_after_a_new_unlock_and_stays_encrypted_on_disk()
    {
        var stored = await SaveAsync(await UnlockedVaultAsync(), Csv);

        Assert.Equal(Csv, await ReadAsync(await UnlockedVaultAsync(), stored));
        AssertNoPlainContent(Uploads);
    }

    [Fact]
    public async Task A_wrong_passphrase_fails_closed()
    {
        await UnlockedVaultAsync();
        var vault = NewVault();

        await Assert.ThrowsAsync<VaultException>(() => vault.UnlockAsync("another fake passphrase"));

        Assert.False(vault.IsUnlocked);
    }

    [Fact]
    public async Task A_short_passphrase_cannot_create_a_vault()
    {
        var vault = NewVault();

        await Assert.ThrowsAsync<VaultException>(() => vault.UnlockAsync("short"));

        Assert.False(vault.IsUnlocked);
        Assert.False(File.Exists(Path.Combine(Uploads, "vault.json")));
    }

    [Theory]
    [InlineData("not json")]
    [InlineData(null)]
    public async Task Damaged_or_missing_settings_never_start_a_new_vault_over_stored_files(string? settings)
    {
        await SaveAsync(await UnlockedVaultAsync(), Csv);
        var path = Path.Combine(Uploads, "vault.json");
        if (settings is null) File.Delete(path);
        else File.WriteAllText(path, settings);
        var vault = NewVault();

        await Assert.ThrowsAsync<VaultException>(() => vault.UnlockAsync(Passphrase));

        Assert.False(vault.IsUnlocked);
    }

    [Fact]
    public async Task A_tampered_or_renamed_file_fails_to_decrypt()
    {
        var vault = await UnlockedVaultAsync();
        var first = await SaveAsync(vault, Csv);
        var second = await SaveAsync(vault, Csv);

        File.Copy(first.FullPath, second.FullPath, overwrite: true);
        await Assert.ThrowsAsync<VaultException>(() => vault.OpenReadAsync(second.Id, second.Extension));

        var bytes = File.ReadAllBytes(first.FullPath);
        bytes[^1] ^= 1;
        File.WriteAllBytes(first.FullPath, bytes);
        await Assert.ThrowsAsync<VaultException>(() => vault.OpenReadAsync(first.Id, first.Extension));
    }

    [Fact]
    public async Task A_locked_vault_refuses_upload_import_preview_and_purge()
    {
        var vault = NewVault();
        using var db = NewDb(Guid.NewGuid().ToString());
        var store = new EfImportStore(db);
        var batchId = await new QueueImport(store, new SystemClock()).HandleAsync(Guid.NewGuid(), ".csv");
        var account = LedgerTestData.Checking();
        db.Add(account);
        db.SaveChanges();
        var importers = new IStatementImporter[] { new CsvStatementImporter() };

        var refusals = new[]
        {
            await Record.ExceptionAsync(() => SaveAsync(vault, Csv)),
            await Record.ExceptionAsync(() => vault.OpenReadAsync(Guid.NewGuid(), ".csv")),
            await Record.ExceptionAsync(() => new ImportStatement(store, vault, importers, new MaskingPiiMasker())
                .HandleAsync(new ImportStatementCommand(batchId, account.Id, "Fake Bank 1", new ConfirmedLayout(AmountLayout.Signed)))),
            await Record.ExceptionAsync(() => new PreviewCsvLayout(store, vault, importers, new MaskingPiiMasker()).HandleAsync(batchId, null)),
            await Record.ExceptionAsync(() => vault.PurgeOriginalsAsync())
        };

        Assert.All(refusals, e => Assert.Equal(Locked, Assert.IsType<VaultLockedException>(e).Message));
        Assert.Equal(ImportBatchStatus.Queued, Assert.Single(db.ImportBatches).Status);
        Assert.Empty(db.CsvMappingProfiles);
    }

    [Fact]
    public async Task Folder_intake_waits_while_locked_then_leaves_no_plain_copy()
    {
        var input = Directory.CreateDirectory(Path.Combine(_root, "input")).FullName;
        var dropped = Path.Combine(input, "statement.csv");
        await File.WriteAllTextAsync(dropped, Csv);
        var vault = NewVault();
        var database = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddSingleton<IDocumentVault>(vault);
        services.AddSingleton<IStatementImporter, CsvStatementImporter>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddDbContext<OfflineCPADbContext>(o => o.UseInMemoryDatabase(database));
        services.AddScoped<IImportStore, EfImportStore>();
        services.AddScoped<QueueImport>();
        using var provider = services.BuildServiceProvider();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Bookkeeping:InputDirectory"] = input }).Build();
        var watcher = new LocalFileProcessorService(NullLogger<LocalFileProcessorService>.Instance, config, provider);

        await watcher.ProcessFilesAsync(CancellationToken.None);
        Assert.True(File.Exists(dropped));
        Assert.Empty(NewDb(database).ImportBatches);

        await vault.UnlockAsync(Passphrase);
        await watcher.ProcessFilesAsync(CancellationToken.None);

        Assert.Single(NewDb(database).ImportBatches);
        Assert.Empty(Directory.GetFiles(input, "*", SearchOption.AllDirectories));
        AssertNoPlainContent(Uploads);
    }

    [Fact]
    public async Task Import_reads_the_decrypted_stream_and_purge_leaves_ledger_and_rows_alone()
    {
        var vault = await UnlockedVaultAsync();
        var stored = await SaveAsync(vault, Csv);
        var database = Guid.NewGuid().ToString();
        using var db = NewDb(database);
        var account = LedgerTestData.Checking();
        db.AddRange(account, LedgerTestData.YearPeriod());
        db.SaveChanges();
        var store = new EfImportStore(db);
        var batchId = await new QueueImport(store, new SystemClock()).HandleAsync(stored.Id, stored.Extension);

        var count = await new ImportStatement(store, vault, new IStatementImporter[] { new CsvStatementImporter() }, new MaskingPiiMasker())
            .HandleAsync(new ImportStatementCommand(batchId, account.Id, "Fake Bank 1", new ConfirmedLayout(AmountLayout.Signed)));

        Assert.Equal(2, count);
        AssertNoPlainContent(Uploads);

        var purge = new PurgeOriginals(vault);
        var refused = await purge.HandleAsync(confirm: false);
        Assert.False(refused.Purged);
        Assert.True(File.Exists(stored.FullPath));
        Assert.Contains("irs.gov", refused.Warning);

        var done = await purge.HandleAsync(confirm: true);
        Assert.Equal((true, 1), (done.Purged, done.Deleted));
        Assert.Null(await ReadAsync(vault, stored));
        Assert.True(File.Exists(Path.Combine(Uploads, "vault.json")));
        Assert.Equal(2, db.StagedRows.Count());
        Assert.Single(db.Accounts);
        Assert.Single(db.ImportBatches);
    }

    [Fact]
    public async Task Existing_plain_uploads_are_encrypted_once_and_running_again_changes_nothing()
    {
        Directory.CreateDirectory(Uploads);
        var id = Guid.NewGuid();
        var plain = Path.Combine(Uploads, id.ToString("N") + ".csv");
        await File.WriteAllTextAsync(plain, Csv);
        var vault = NewVault();

        Assert.Equal(1, await vault.UnlockAsync(Passphrase));
        Assert.Equal(0, await vault.UnlockAsync(Passphrase));

        Assert.False(File.Exists(plain));
        await using var stream = await vault.OpenReadAsync(id, ".csv");
        Assert.Equal(Csv, await new StreamReader(stream!).ReadToEndAsync());
        AssertNoPlainContent(Uploads);
        Assert.Equal(2, Directory.GetFiles(Uploads).Length); // settings and one encrypted file
    }

    private static OfflineCPADbContext NewDb(string name) =>
        new(new DbContextOptionsBuilder<OfflineCPADbContext>().UseInMemoryDatabase(name).Options);
}

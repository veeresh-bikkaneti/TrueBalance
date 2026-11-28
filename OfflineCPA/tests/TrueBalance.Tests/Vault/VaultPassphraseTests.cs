using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Application.Interfaces;
using Application.Uploads;
using Backend.Endpoints;
using Infrastructure.Options;
using Infrastructure.Vault;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace TrueBalance.Tests.Vault;

public sealed class VaultPassphraseTests : IDisposable
{
    private const string Old = "fake passphrase for tests";
    private const string New = "another fake passphrase";
    private const string Text = "fake document text";

    private readonly string _root = Directory.CreateTempSubdirectory("tb-vault-pass-").FullName;
    private string Settings => Path.Combine(_root, "vault.json");

    public void Dispose() => Directory.Delete(_root, recursive: true);

    private EncryptedFileVault NewVault() =>
        new(Microsoft.Extensions.Options.Options.Create(new UploadOptions { Directory = _root }));

    private static Task<StoredUpload> SaveAsync(IDocumentVault vault) =>
        vault.SaveAsync(new MemoryStream(Encoding.UTF8.GetBytes(Text)), Text.Length, "statement.csv", "text/csv");

    private static async Task AssertReadsAsync(IDocumentVault vault, params StoredUpload[] files)
    {
        foreach (var file in files)
        {
            await using var stream = await vault.OpenReadAsync(file.Id, file.Extension);
            Assert.Equal(Text, await new StreamReader(stream!).ReadToEndAsync());
        }
    }

    private int SettingsVersion() =>
        JsonDocument.Parse(File.ReadAllText(Settings)).RootElement.GetProperty("Version").GetInt32();

    private static byte[] Seal(byte[] key, string name, byte[] plain)
    {
        var output = new byte[12 + 16 + plain.Length];
        RandomNumberGenerator.Fill(output.AsSpan(0, 12));
        using var aes = new AesGcm(key, 16);
        aes.Encrypt(output.AsSpan(0, 12), plain, output.AsSpan(28), output.AsSpan(12, 16), Encoding.UTF8.GetBytes(name));
        return output;
    }

    /// <summary>Writes a vault the way version 1 did: the passphrase key seals the files.</summary>
    private StoredUpload[] WriteVersion1Vault()
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var key = Rfc2898DeriveBytes.Pbkdf2(Old, salt, 600_000, HashAlgorithmName.SHA256, 32);
        File.WriteAllText(Settings, JsonSerializer.Serialize(new
        {
            Version = 1,
            Kdf = "PBKDF2-HMAC-SHA256",
            Iterations = 600_000,
            Salt = salt,
            Check = Seal(key, "vault.json", Encoding.UTF8.GetBytes("TrueBalance vault key check"))
        }));
        return Enumerable.Range(0, 2).Select(_ =>
        {
            var id = Guid.NewGuid();
            var name = id.ToString("N") + ".csv";
            var path = Path.Combine(_root, name + ".enc");
            File.WriteAllBytes(path, Seal(key, name, Encoding.UTF8.GetBytes(Text)));
            return new StoredUpload(id, name, "statement.csv", ".csv", Text.Length, path);
        }).ToArray();
    }

    private Dictionary<string, string> SealedFiles() =>
        Directory.GetFiles(_root, "*.enc").ToDictionary(f => f, f => Convert.ToHexString(File.ReadAllBytes(f)));

    [Fact]
    public async Task A_version_1_vault_upgrades_on_unlock_and_every_old_file_still_opens()
    {
        var files = WriteVersion1Vault();
        var before = SealedFiles();

        await NewVault().UnlockAsync(Old);

        Assert.Equal(2, SettingsVersion());
        Assert.Equal(before, SealedFiles());
        var reopened = NewVault();
        await reopened.UnlockAsync(Old);
        await AssertReadsAsync(reopened, files);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task After_a_change_only_the_new_passphrase_unlocks_and_files_are_untouched(bool version1)
    {
        StoredUpload[] files;
        if (version1)
        {
            files = WriteVersion1Vault();
        }
        else
        {
            var first = NewVault();
            await first.UnlockAsync(Old);
            files = new[] { await SaveAsync(first), await SaveAsync(first) };
        }

        var before = SealedFiles();

        await NewVault().ChangePassphraseAsync(Old, New);

        Assert.Equal(before, SealedFiles());
        await Assert.ThrowsAsync<VaultException>(() => NewVault().UnlockAsync(Old));
        var vault = NewVault();
        await vault.UnlockAsync(New);
        await AssertReadsAsync(vault, files);
    }

    [Fact]
    public async Task A_wrong_current_passphrase_or_a_short_new_one_changes_nothing()
    {
        await NewVault().UnlockAsync(Old);
        var before = File.ReadAllBytes(Settings);
        var vault = NewVault();

        var wrong = await Assert.ThrowsAsync<VaultException>(() => vault.ChangePassphraseAsync(New, New));
        await Assert.ThrowsAsync<VaultException>(() => vault.ChangePassphraseAsync(Old, "short"));

        Assert.Equal("That passphrase does not open the books.", wrong.Message);
        Assert.False(vault.IsUnlocked);
        Assert.Equal(before, File.ReadAllBytes(Settings));
    }

    [Fact]
    public async Task The_settings_file_never_holds_the_data_key()
    {
        var vault = NewVault();
        await vault.UnlockAsync(Old);
        await vault.ChangePassphraseAsync(Old, New);
        var key = (byte[])typeof(EncryptedFileVault)
            .GetField("_key", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(vault)!;

        var bytes = File.ReadAllBytes(Settings);
        var text = File.ReadAllText(Settings);

        Assert.Equal(2, SettingsVersion());
        Assert.Equal(-1, bytes.AsSpan().IndexOf(key));
        Assert.DoesNotContain(Convert.ToBase64String(key), text);
        Assert.DoesNotContain(Convert.ToHexString(key), text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Skipping_the_rename_leaves_the_old_settings_readable()
    {
        await NewVault().UnlockAsync(Old);
        var before = File.ReadAllBytes(Settings);
        var vault = NewVault();
        vault.Replace = (_, _, _) => throw new IOException("crash before the rename");

        await Assert.ThrowsAsync<IOException>(() => vault.ChangePassphraseAsync(Old, New));

        Assert.Equal(before, File.ReadAllBytes(Settings));
        await NewVault().UnlockAsync(Old);
    }

    [Fact]
    public async Task Creating_a_vault_never_replaces_settings_that_appeared_meanwhile()
    {
        var vault = NewVault();
        vault.Replace = (from, to, overwrite) =>
        {
            File.WriteAllText(to, "the other vault");
            File.Move(from, to, overwrite);
        };

        await Assert.ThrowsAsync<IOException>(() => vault.UnlockAsync(Old));

        Assert.False(vault.IsUnlocked);
        Assert.Equal("the other vault", File.ReadAllText(Settings));
    }

    [Fact]
    public async Task No_route_logs_or_returns_a_passphrase()
    {
        var log = new CapturingLog();
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders().AddProvider(log).AddFilter<CapturingLog>(null, LogLevel.Trace);
        builder.Services.AddSingleton<IVaultControl>(NewVault());
        builder.Services.AddScoped<PurgeOriginals>();
        new ThrottleModule().Register(builder.Services);
        await using var app = builder.Build();
        app.MapVaultEndpoints();
        await app.StartAsync();
        var client = app.GetTestClient();
        var calls = new (string Route, object Body, bool Succeeds)[]
        {
            ("unlock", new { passphrase = Old }, true),
            ("change-passphrase", new { currentPassphrase = New, newPassphrase = New }, false),
            ("change-passphrase", new { currentPassphrase = Old, newPassphrase = New }, true),
            ("unlock", new { passphrase = Old }, false),
            ("purge-originals", new { confirm = true }, true)
        };

        foreach (var (route, body, succeeds) in calls)
        {
            var response = await client.PostAsJsonAsync("/api/vault/" + route, body);
            Assert.Equal(succeeds, response.IsSuccessStatusCode);
            var reply = await response.Content.ReadAsStringAsync();
            Assert.DoesNotContain(Old, reply);
            Assert.DoesNotContain(New, reply);
        }

        Assert.NotEmpty(log.Lines);
        Assert.All(log.Lines, line =>
        {
            Assert.DoesNotContain(Old, line);
            Assert.DoesNotContain(New, line);
        });
    }

    internal sealed class CapturingLog : ILoggerProvider, ILogger
    {
        public System.Collections.Concurrent.ConcurrentQueue<string> Lines { get; } = new();
        public ILogger CreateLogger(string categoryName) => this;
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Lines.Enqueue(formatter(state, exception) + exception);
        public void Dispose()
        {
        }
    }
}

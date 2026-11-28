using System.Net.Http.Json;
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

public sealed class VaultRecoveryTests : IDisposable
{
    private const string Old = "fake passphrase for tests";
    private const string New = "another fake passphrase";
    private const string Text = "fake document text";
    private const string WrongPassphrase = "That passphrase does not open the books.";

    private readonly string _root = Directory.CreateTempSubdirectory("tb-vault-recovery-").FullName;
    private string Settings => Path.Combine(_root, "vault.json");

    public void Dispose() => Directory.Delete(_root, recursive: true);

    private EncryptedFileVault NewVault() =>
        new(Microsoft.Extensions.Options.Options.Create(new UploadOptions { Directory = _root }));

    private async Task<(EncryptedFileVault Vault, string Code)> VaultWithCodeAsync()
    {
        var vault = NewVault();
        await vault.UnlockAsync(Old);
        return (vault, await vault.CreateRecoveryCodeAsync(Old));
    }

    [Fact]
    public async Task Recovery_sets_a_new_passphrase_and_every_file_still_opens()
    {
        var (first, code) = await VaultWithCodeAsync();
        var files = new List<StoredUpload>();
        for (var i = 0; i < 2; i++)
        {
            files.Add(await first.SaveAsync(new MemoryStream(Encoding.UTF8.GetBytes(Text)), Text.Length, "statement.csv", "text/csv"));
        }

        var before = files.Select(f => Convert.ToHexString(File.ReadAllBytes(f.FullPath))).ToList();
        var recovered = NewVault();

        // Typed by hand: lower case, spaces in place of dashes.
        await recovered.RecoverAsync(code.ToLowerInvariant().Replace('-', ' '), New);

        Assert.True(recovered.IsUnlocked);
        Assert.Equal(before, files.Select(f => Convert.ToHexString(File.ReadAllBytes(f.FullPath))));
        await Assert.ThrowsAsync<VaultException>(() => NewVault().UnlockAsync(Old));
        var reopened = NewVault();
        await reopened.UnlockAsync(New);
        foreach (var file in files)
        {
            await using var stream = await reopened.OpenReadAsync(file.Id, file.Extension);
            Assert.Equal(Text, await new StreamReader(stream!).ReadToEndAsync());
        }
    }

    [Fact]
    public async Task A_code_has_at_least_120_bits_in_groups()
    {
        var (_, code) = await VaultWithCodeAsync();

        Assert.Matches("^([0-9A-F]{4}-){7}[0-9A-F]{4}$", code);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task A_wrong_code_fails_like_a_wrong_passphrase_and_changes_nothing(bool codeWasMade)
    {
        var first = NewVault();
        await first.UnlockAsync(Old);
        if (codeWasMade) await first.CreateRecoveryCodeAsync(Old);
        var before = File.ReadAllText(Settings);
        var vault = NewVault();

        var error = await Assert.ThrowsAsync<VaultException>(() => vault.RecoverAsync("0000-0000-0000-0000-0000-0000-0000-0000", New));

        Assert.Equal(WrongPassphrase, error.Message);
        Assert.False(vault.IsUnlocked);
        Assert.Equal(before, File.ReadAllText(Settings));
    }

    [Fact]
    public async Task A_new_code_stops_the_old_one()
    {
        var (vault, old) = await VaultWithCodeAsync();

        var replacement = await vault.CreateRecoveryCodeAsync(Old);

        Assert.NotEqual(old, replacement);
        await Assert.ThrowsAsync<VaultException>(() => NewVault().RecoverAsync(old, New));
        await NewVault().RecoverAsync(replacement, New);
    }

    [Fact]
    public async Task A_passphrase_change_keeps_the_code_working()
    {
        var (vault, code) = await VaultWithCodeAsync();

        await vault.ChangePassphraseAsync(Old, New);

        await NewVault().RecoverAsync(code, Old);
    }

    [Fact]
    public async Task A_wrong_passphrase_cannot_make_a_code_and_the_old_code_still_works()
    {
        var (vault, code) = await VaultWithCodeAsync();
        var before = File.ReadAllText(Settings);

        var error = await Assert.ThrowsAsync<VaultException>(() => vault.CreateRecoveryCodeAsync(New));

        Assert.Equal(WrongPassphrase, error.Message);
        Assert.Equal(before, File.ReadAllText(Settings));
        await NewVault().RecoverAsync(code, New);
    }

    [Fact]
    public async Task A_code_works_once()
    {
        var (_, code) = await VaultWithCodeAsync();
        await NewVault().RecoverAsync(code, New);
        var before = File.ReadAllText(Settings);

        var error = await Assert.ThrowsAsync<VaultException>(() => NewVault().RecoverAsync(code, Old));

        Assert.Equal(WrongPassphrase, error.Message);
        Assert.Equal(before, File.ReadAllText(Settings));
    }

    [Fact]
    public async Task After_a_recovery_the_new_passphrase_makes_a_new_code()
    {
        var (_, code) = await VaultWithCodeAsync();
        var vault = NewVault();
        await vault.RecoverAsync(code, New);

        await Assert.ThrowsAsync<VaultException>(() => vault.CreateRecoveryCodeAsync(Old));
        var next = await vault.CreateRecoveryCodeAsync(New);

        await NewVault().RecoverAsync(next, Old);
    }

    [Fact]
    public async Task A_locked_vault_refuses_to_make_a_code()
    {
        await NewVault().UnlockAsync(Old);
        var before = File.ReadAllText(Settings);

        await Assert.ThrowsAsync<VaultLockedException>(() => NewVault().CreateRecoveryCodeAsync(Old));

        Assert.Equal(before, File.ReadAllText(Settings));
    }

    [Fact]
    public async Task A_missing_settings_file_gives_a_vault_error()
    {
        var (vault, _) = await VaultWithCodeAsync();
        File.Delete(Settings);

        await Assert.ThrowsAsync<VaultException>(() => vault.CreateRecoveryCodeAsync(Old));
    }

    [Fact]
    public async Task The_code_appears_in_one_response_and_never_in_settings_or_logs()
    {
        var log = new VaultPassphraseTests.CapturingLog();
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders().AddProvider(log).AddFilter<VaultPassphraseTests.CapturingLog>(null, LogLevel.Trace);
        builder.Services.AddSingleton<IVaultControl>(NewVault());
        builder.Services.AddScoped<PurgeOriginals>();
        new ThrottleModule().Register(builder.Services);
        await using var app = builder.Build();
        app.MapVaultEndpoints();
        await app.StartAsync();
        var client = app.GetTestClient();
        var replies = new List<string>();

        async Task<string> PostAsync(string route, object body, bool succeeds)
        {
            var response = await client.PostAsJsonAsync("/api/vault/" + route, body);
            Assert.Equal(succeeds, response.IsSuccessStatusCode);
            var reply = await response.Content.ReadAsStringAsync();
            replies.Add(reply);
            if (route == "recovery-code" && succeeds) Assert.True(response.Headers.CacheControl?.NoStore);
            return reply;
        }

        await PostAsync("recovery-code", new { passphrase = Old }, false);
        await PostAsync("unlock", new { passphrase = Old }, true);
        // A request another web page can send without asking first must not replace the code.
        var before = File.ReadAllText(Settings);
        foreach (var content in new HttpContent?[] { null, new StringContent(JsonSerializer.Serialize(new { passphrase = Old }), Encoding.UTF8, "text/plain") })
        {
            Assert.False((await client.PostAsync("/api/vault/recovery-code", content)).IsSuccessStatusCode);
        }

        await PostAsync("recovery-code", new { passphrase = New }, false);
        Assert.Equal(before, File.ReadAllText(Settings));
        var made = await PostAsync("recovery-code", new { passphrase = Old }, true);
        var code = JsonDocument.Parse(made).RootElement.GetProperty("recoveryCode").GetString()!;
        await PostAsync("recover", new { recoveryCode = "wrong", newPassphrase = New }, false);
        await PostAsync("recover", new { recoveryCode = code, newPassphrase = "short" }, false);
        var recovered = await PostAsync("recover", new { recoveryCode = code, newPassphrase = New }, true);
        Assert.True(JsonDocument.Parse(recovered).RootElement.GetProperty("makeNewCode").GetBoolean());
        await PostAsync("recover", new { recoveryCode = code, newPassphrase = New }, false);

        var forms = new[] { code, code.Replace("-", string.Empty) };
        Assert.Single(replies, r => forms.Any(f => r.Contains(f, StringComparison.OrdinalIgnoreCase)));
        Assert.NotEmpty(log.Lines);
        foreach (var secret in forms.Append(Old).Append(New))
        {
            Assert.DoesNotContain(secret, File.ReadAllText(Settings), StringComparison.OrdinalIgnoreCase);
            Assert.All(log.Lines, line => Assert.DoesNotContain(secret, line, StringComparison.OrdinalIgnoreCase));
        }
    }
}

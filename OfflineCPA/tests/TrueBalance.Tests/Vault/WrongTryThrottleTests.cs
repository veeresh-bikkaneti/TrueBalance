using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Application.Interfaces;
using Application.Uploads;
using Backend.Endpoints;
using Infrastructure;
using Infrastructure.Options;
using Infrastructure.Vault;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace TrueBalance.Tests.Vault;

public sealed class WrongTryThrottleTests : IDisposable
{
    private const string Right = "fake passphrase for tests";
    private const string Wrong = "a wrong fake passphrase";
    private const string WrongMessage = "That passphrase does not open the books.";
    private const string WaitMessage = "Too many tries. Wait a moment, then try again.";

    private readonly string _root = Directory.CreateTempSubdirectory("tb-throttle-").FullName;

    public void Dispose() => Directory.Delete(_root, recursive: true);

    private sealed class Clock : TimeProvider
    {
        private long _ticks = 1;
        public override long TimestampFrequency => TimeSpan.TicksPerSecond;
        public override long GetTimestamp() => Interlocked.Read(ref _ticks);
        public void Advance(double seconds) => Interlocked.Add(ref _ticks, TimeSpan.FromSeconds(seconds).Ticks);
    }

    /// <summary>Counts every secret it is asked to check. It can hold a check open to act out a slow one.</summary>
    private sealed class CountingVault : IVaultControl
    {
        private int _checks;
        public int Checks => _checks;
        public TaskCompletionSource? Hold { get; set; }
        public Exception? Fault { get; set; }
        public Task<int> PurgeOriginalsAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);

        private async Task<int> CheckAsync(string secret)
        {
            Interlocked.Increment(ref _checks);
            if (Hold is not null) await Hold.Task;
            if (Fault is not null) throw Fault;
            if (secret != Right) throw new VaultException(WrongMessage);
            return 0;
        }

        public Task<int> UnlockAsync(string passphrase, CancellationToken cancellationToken = default) => CheckAsync(passphrase);

        public Task ChangePassphraseAsync(string currentPassphrase, string newPassphrase, CancellationToken cancellationToken = default) =>
            CheckAsync(currentPassphrase);

        public async Task<string> CreateRecoveryCodeAsync(string passphrase, CancellationToken cancellationToken = default)
        {
            await CheckAsync(passphrase);
            return "0000-0000";
        }

        public Task<int> RecoverAsync(string recoveryCode, string newPassphrase, CancellationToken cancellationToken = default) =>
            CheckAsync(recoveryCode);
    }

    private EncryptedFileVault RealVault() =>
        new(Microsoft.Extensions.Options.Options.Create(new UploadOptions { Directory = _root }));

    private async Task<WebApplication> StartAsync(IVaultControl vault, Action<IServiceCollection>? throttle = null)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton(vault);
        if (vault is IDocumentVault documents) builder.Services.AddSingleton(documents);
        builder.Services.AddScoped<PurgeOriginals>();
        builder.Services.Configure<UploadOptions>(o => o.Directory = _root);
        if (throttle is null) new ThrottleModule().Register(builder.Services);
        else throttle(builder.Services);
        var app = builder.Build();
        app.MapVaultEndpoints();
        await app.StartAsync();
        return app;
    }

    private Task<WebApplication> StartAsync(IVaultControl vault, TimeProvider clock) =>
        StartAsync(vault, services => services.AddSingleton<IWrongTryThrottle>(new WrongTryThrottle(clock)));

    private static Task<HttpResponseMessage> PostAsync(HttpClient client, string route, string secret)
    {
        object body = route switch
        {
            "change-passphrase" => new { currentPassphrase = secret, newPassphrase = Right },
            "recover" => new { recoveryCode = secret, newPassphrase = Right },
            _ => new { passphrase = secret },
        };
        return client.PostAsJsonAsync("/api/vault/" + route, body);
    }

    private static async Task AssertMustWaitAsync(HttpResponseMessage response, int seconds)
    {
        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.Equal(TimeSpan.FromSeconds(seconds), response.Headers.RetryAfter?.Delta);
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(WaitMessage, body.RootElement.GetProperty("error").GetString());
    }

    private static void Miss(WrongTryThrottle throttle)
    {
        using var attempt = throttle.TryBegin(out _);
        Assert.NotNull(attempt);
        attempt.Wrong();
    }

    [Theory]
    [InlineData("unlock")]
    [InlineData("change-passphrase")]
    [InlineData("recovery-code")]
    [InlineData("recover")]
    public async Task The_4th_wrong_try_waits_unchecked_and_a_right_answer_resets_the_count(string route)
    {
        var vault = new CountingVault();
        var clock = new Clock();
        await using var app = await StartAsync(vault, clock);
        var client = app.GetTestClient();
        for (var i = 0; i < 3; i++)
        {
            Assert.Equal(HttpStatusCode.BadRequest, (await PostAsync(client, route, Wrong)).StatusCode);
        }

        // Even the right answer is not looked at while waiting.
        await AssertMustWaitAsync(await PostAsync(client, route, Right), 1);
        Assert.Equal(3, vault.Checks);

        clock.Advance(1);
        Assert.Equal(HttpStatusCode.OK, (await PostAsync(client, route, Right)).StatusCode);
        for (var i = 0; i < 3; i++)
        {
            Assert.Equal(HttpStatusCode.BadRequest, (await PostAsync(client, route, Wrong)).StatusCode);
        }

        Assert.Equal(7, vault.Checks);
    }

    [Fact]
    public async Task All_four_routes_share_one_count()
    {
        var vault = new CountingVault();
        await using var app = await StartAsync(vault, new Clock());
        var client = app.GetTestClient();
        await PostAsync(client, "unlock", Wrong);
        await PostAsync(client, "change-passphrase", Wrong);
        await PostAsync(client, "recovery-code", Wrong);

        await AssertMustWaitAsync(await PostAsync(client, "recover", Wrong), 1);
        Assert.Equal(3, vault.Checks);
    }

    [Fact]
    public async Task Guesses_sent_together_are_checked_one_at_a_time()
    {
        var vault = new CountingVault { Hold = new TaskCompletionSource() };
        await using var app = await StartAsync(vault, new Clock());
        var client = app.GetTestClient();

        var guesses = Enumerable.Range(0, 20).Select(_ => PostAsync(client, "unlock", Wrong)).ToList();
        // 19 are turned away while the first check is still open.
        for (var i = 0; i < 19; i++)
        {
            var done = await Task.WhenAny(guesses).WaitAsync(TimeSpan.FromSeconds(10));
            guesses.Remove(done);
            await AssertMustWaitAsync(await done, 1);
        }

        vault.Hold.SetResult();
        Assert.Equal(HttpStatusCode.BadRequest, (await guesses.Single()).StatusCode);
        Assert.Equal(1, vault.Checks);
    }

    [Fact]
    public void A_burst_during_a_wait_or_right_after_it_gets_at_most_one_check()
    {
        var clock = new Clock();
        var throttle = new WrongTryThrottle(clock);
        for (var i = 0; i < 3; i++) Miss(throttle);

        int Claimed()
        {
            var claimed = 0;
            Parallel.For(0, 200, _ =>
            {
                if (throttle.TryBegin(out var _) is not null) Interlocked.Increment(ref claimed);
            });
            return claimed;
        }

        Assert.Equal(0, Claimed());
        clock.Advance(1);
        Assert.Equal(1, Claimed());
    }

    [Fact]
    public void The_wait_doubles_from_one_second_to_five_minutes_at_most()
    {
        var clock = new Clock();
        var throttle = new WrongTryThrottle(clock);
        Miss(throttle);
        Miss(throttle);
        Miss(throttle);

        foreach (var seconds in new[] { 1, 2, 4, 8, 16, 32, 64, 128, 256 }.Concat(Enumerable.Repeat(300, 40)))
        {
            Assert.Null(throttle.TryBegin(out var wait));
            Assert.Equal(TimeSpan.FromSeconds(seconds), wait);
            clock.Advance(seconds - 0.5);
            Assert.Null(throttle.TryBegin(out _));
            clock.Advance(0.5);
            Miss(throttle);
        }
    }

    [Fact]
    public async Task A_failure_that_is_not_a_wrong_answer_neither_counts_nor_resets()
    {
        var vault = new CountingVault();
        await using var app = await StartAsync(vault, new Clock());
        var client = app.GetTestClient();
        await PostAsync(client, "unlock", Wrong);
        await PostAsync(client, "unlock", Wrong);

        vault.Fault = new VaultException("Use a passphrase of at least 12 characters.");
        await PostAsync(client, "unlock", Wrong);
        vault.Fault = new IOException("fake disk fault");
        await Assert.ThrowsAnyAsync<Exception>(() => PostAsync(client, "unlock", Wrong));
        vault.Fault = null;
        Assert.Equal(HttpStatusCode.BadRequest, (await PostAsync(client, "unlock", Wrong)).StatusCode);

        await AssertMustWaitAsync(await PostAsync(client, "unlock", Wrong), 1);
        Assert.Equal(5, vault.Checks);
    }

    /// <summary>Real vault, real module: a reworded vault message cannot switch the slowdown off unnoticed.</summary>
    [Theory]
    [InlineData("unlock")]
    [InlineData("change-passphrase")]
    [InlineData("recovery-code")]
    [InlineData("recover")]
    public async Task The_real_vault_is_slowed_down_too(string route)
    {
        var vault = RealVault();
        await vault.UnlockAsync(Right);
        await vault.CreateRecoveryCodeAsync(Right);
        await using var app = await StartAsync(vault);
        var client = app.GetTestClient();
        for (var i = 0; i < 3; i++)
        {
            Assert.Equal(HttpStatusCode.BadRequest, (await PostAsync(client, route, Wrong)).StatusCode);
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, (await PostAsync(client, route, Wrong)).StatusCode);
    }

    [Fact]
    public void The_module_registers_one_shared_throttle()
    {
        var services = new ServiceCollection();
        services.AddInfrastructureAdapterModules();

        var entry = Assert.Single(services, s => s.ServiceType == typeof(IWrongTryThrottle));
        Assert.Equal(ServiceLifetime.Singleton, entry.Lifetime);
    }

    [Fact]
    public async Task Status_says_locked_or_unlocked_and_whether_a_passphrase_is_set()
    {
        await using var app = await StartAsync(RealVault());
        var client = app.GetTestClient();

        Assert.Equal("{\"unlocked\":false,\"passphraseSet\":false}", await client.GetStringAsync("/api/vault/status"));
        await PostAsync(client, "unlock", Right);
        Assert.Equal("{\"unlocked\":true,\"passphraseSet\":true}", await client.GetStringAsync("/api/vault/status"));
    }
}

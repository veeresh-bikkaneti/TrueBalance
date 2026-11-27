using Infrastructure.Pii;
using Microsoft.Extensions.Logging;

namespace TrueBalance.Tests.Pii;

/// <summary>The key store fails closed: it never replaces a key it did not create.</summary>
public sealed class KeyStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"tb-key-{Guid.NewGuid():N}");
    private string KeyPath => Path.Combine(_directory, "pii-fingerprint.key");

    public KeyStoreTests() => Directory.CreateDirectory(_directory);

    public void Dispose() => Directory.Delete(_directory, recursive: true);

    [Fact]
    public void Short_key_file_throws_and_is_left_untouched()
    {
        File.WriteAllBytes(KeyPath, [1, 2, 3]);

        var error = Assert.Throws<InvalidOperationException>(() => LocalFingerprintKeyStore.LoadOrCreate(KeyPath));

        Assert.Contains(KeyPath, error.Message, StringComparison.Ordinal);
        Assert.Contains("delete", error.Message, StringComparison.Ordinal);
        Assert.Equal(new byte[] { 1, 2, 3 }, File.ReadAllBytes(KeyPath));
    }

    [Fact]
    public void Unreadable_key_path_throws_with_the_path()
    {
        Directory.CreateDirectory(KeyPath);

        var error = Assert.Throws<InvalidOperationException>(() => LocalFingerprintKeyStore.LoadOrCreate(KeyPath));

        Assert.Contains(KeyPath, error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Concurrent_starts_agree_on_one_key()
    {
        for (var round = 0; round < 20; round++)
        {
            var path = Path.Combine(_directory, $"{round}.key");
            var keys = new byte[8][];
            var threads = Enumerable.Range(0, keys.Length)
                .Select(i => new Thread(() => keys[i] = Try(path)))
                .ToList();
            threads.ForEach(t => t.Start());
            threads.ForEach(t => t.Join());

            var stored = File.ReadAllBytes(path);
            Assert.All(keys, key => Assert.Equal(stored, key));
        }

        static byte[] Try(string path)
        {
            try { return LocalFingerprintKeyStore.LoadOrCreate(path); }
            catch (Exception) { return []; }
        }
    }

    [UnixFact]
    public void New_key_file_is_owner_read_write_only()
    {
        LocalFingerprintKeyStore.LoadOrCreate(KeyPath);

        if (!OperatingSystem.IsWindows())
        {
            Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite, File.GetUnixFileMode(KeyPath));
        }
    }

    [Fact]
    public void New_key_logs_one_warning_without_the_key_or_full_path()
    {
        var logs = new CapturingLoggerProvider();
        var logger = logs.CreateLogger("t");

        var first = Masker();
        PiiStartup.WarnIfKeyGenerated(first, logger);
        PiiStartup.WarnIfKeyGenerated(Masker(), logger);

        var line = Assert.Single(logs.Lines);
        Assert.Contains("pii-fingerprint.key", line, StringComparison.Ordinal);
        Assert.DoesNotContain(_directory, line, StringComparison.Ordinal);
        Assert.DoesNotContain(Convert.ToBase64String(File.ReadAllBytes(KeyPath)), line, StringComparison.Ordinal);
    }

    private PiiMasker Masker() => new(
        Microsoft.Extensions.Options.Options.Create(
            new Infrastructure.Options.PiiOptions { FingerprintKeyPath = KeyPath }),
        []);

    private sealed class UnixFactAttribute : FactAttribute
    {
        public UnixFactAttribute()
        {
            if (OperatingSystem.IsWindows())
            {
                Skip = "Unix file modes do not exist on Windows.";
            }
        }
    }
}

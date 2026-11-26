using Application.Interfaces;
using Backend.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace TrueBalance.Tests;

public class FolderWatchLogTests : IDisposable
{
    private readonly string _dir;

    public FolderWatchLogTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "tb-watch-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, recursive: true);
        }
    }

    [Fact]
    public async Task Watched_file_log_uses_a_generated_id_not_the_original_filename()
    {
        const string personalName = "person-name-sample.csv";
        await File.WriteAllTextAsync(Path.Combine(_dir, personalName), "a,b\n1,2\n");

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Bookkeeping:InputDirectory"] = _dir
            })
            .Build();

        var logger = new CaptureLogger<LocalFileProcessorService>();
        var services = new ServiceCollection();
        services.AddSingleton<IStatementImporter, RejectingParser>();
        using var provider = services.BuildServiceProvider();

        var service = new LocalFileProcessorService(logger, config, provider);
        await service.ProcessFilesAsync(CancellationToken.None);

        var logged = string.Join("\n", logger.Lines);
        Assert.DoesNotContain(personalName, logged, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Processing file", logged, StringComparison.Ordinal);
        Assert.Matches("[0-9a-f]{32}", logged);
    }

    private sealed class RejectingParser : IStatementImporter
    {
        public bool SupportsExtension(string extension) => false;

        public Task<string> ExtractTextAsync(string filePath, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("This parser does not read files.");
        }
    }

    private sealed class CaptureLogger<T> : ILogger<T>
    {
        public List<string> Lines { get; } = new();

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Lines.Add(formatter(state, exception));
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose()
            {
            }
        }
    }
}

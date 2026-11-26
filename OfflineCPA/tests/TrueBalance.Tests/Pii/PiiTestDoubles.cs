using Application.Interfaces;
using Application.Uploads;
using Domain.Pii;
using Infrastructure.Pii;
using Infrastructure.Pii.Detectors;
using Microsoft.Extensions.Logging;

namespace TrueBalance.Tests.Pii;

/// <summary>Doubles shared by the redaction and no-leak tests.</summary>
public sealed class CapturingLoggerProvider : ILoggerProvider
{
    private readonly List<ILogger> _loggers = [];

    public List<string> Lines { get; } = [];

    public List<Exception> Exceptions { get; } = [];

    public ILogger CreateLogger(string categoryName)
    {
        var logger = new CapturingLogger(this);
        lock (_loggers)
        {
            _loggers.Add(logger);
        }

        return logger;
    }

    public void Dispose()
    {
    }

    private sealed class CapturingLogger(CapturingLoggerProvider owner) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            lock (owner._loggers)
            {
                owner.Lines.Add(formatter(state, exception));
                if (exception is not null)
                {
                    owner.Exceptions.Add(exception);
                }
            }
        }
    }
}

/// <summary>A masker with a fixed key, so no-leak tests are deterministic.</summary>
public sealed class MaskingPiiMasker : IPiiMasker
{
    private readonly PiiMasker _masker;

    public MaskingPiiMasker()
    {
        var options = Microsoft.Extensions.Options.Options.Create(
            new Infrastructure.Options.PiiOptions
            {
                FingerprintKey = Fixtures.FakePii.TestKeyBase64,
            });
        _masker = new PiiMasker(options,
        [
            new AbaRoutingNumberDetector(),
            new SsnDetector(),
            new EinItinDetector(),
            new BankAccountNumberDetector(),
        ]);
    }

    public string Mask(string text) => _masker.Mask(text);

    public Domain.Pii.PiiMaskingResult MaskDetailed(string text) => _masker.MaskDetailed(text);
}

/// <summary>Vault double that keeps the client file name so tests can prove
/// the endpoint masks it before persisting anything.</summary>
public sealed class FakeVault : IDocumentVault
{
    public Task<StoredUpload> SaveAsync(
        Stream content,
        long length,
        string originalFileName,
        string? contentType,
        CancellationToken cancellationToken = default)
    {
        if (contentType == "application/x-invalid")
        {
            throw new InvalidUploadException($"Content rejected for file {originalFileName}.");
        }

        var id = Guid.NewGuid();
        return Task.FromResult(new StoredUpload(
            id,
            $"{id:N}.pdf",
            originalFileName,
            ".pdf",
            length,
            Path.Combine(Path.GetTempPath(), $"{id:N}.pdf")));
    }
}

/// <summary>Importer double returning synthetic statement text; the stub
/// importer path for the no-leak test.</summary>
public sealed class StubImporter(string extractedText) : IStatementImporter
{
    public Task<string> ExtractTextAsync(string filePath, CancellationToken cancellationToken = default)
        => Task.FromResult(extractedText);

    public bool SupportsExtension(string extension)
        => extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase);
}

using Application.Imports;
using Application.Interfaces;
using Application.Uploads;

namespace Backend.Services;

/// <summary>
/// Watches the input folder. Each supported file is stored in the vault and
/// queued for import; it is never parsed or posted here.
/// </summary>
public class LocalFileProcessorService : BackgroundService
{
    private readonly ILogger<LocalFileProcessorService> _logger;
    private readonly string _inputDirectory;
    private readonly IServiceProvider _serviceProvider;
    private readonly HashSet<(string, DateTime)> _queued = new();

    public LocalFileProcessorService(ILogger<LocalFileProcessorService> logger, IConfiguration configuration, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _inputDirectory = configuration.GetValue<string>("Bookkeeping:InputDirectory") ?? "/app/data/input";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Local File Processor Service is starting.");

        if (!Directory.Exists(_inputDirectory))
        {
            _logger.LogWarning("Input directory does not exist. Creating it at: {Directory}", _inputDirectory);
            Directory.CreateDirectory(_inputDirectory);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessFilesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occurred while processing files: {ErrorType}", ex.GetType().Name);
            }

            // Minimum delay before next scan
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }

        _logger.LogInformation("Local File Processor Service is stopping.");
    }

    internal async Task ProcessFilesAsync(CancellationToken stoppingToken)
    {
        var files = Directory.GetFiles(_inputDirectory);

        foreach (var file in files)
        {
            if (stoppingToken.IsCancellationRequested)
                break;

            var fileId = Guid.NewGuid().ToString("N");
            _logger.LogInformation("Processing file {FileId}", fileId);

            using var scope = _serviceProvider.CreateScope();
            var extension = Path.GetExtension(file).ToLowerInvariant();

            if (!scope.ServiceProvider.GetServices<IStatementImporter>().Any(i => i.SupportsExtension(extension)))
            {
                _logger.LogWarning("Unsupported file type {FileId}", fileId);
                continue;
            }

            try
            {
                // Remembered in case the move below fails, so one file is queued once.
                var stamp = (file, File.GetLastWriteTimeUtc(file));
                if (!_queued.Contains(stamp))
                {
                    var vault = scope.ServiceProvider.GetRequiredService<IDocumentVault>();
                    var queue = scope.ServiceProvider.GetRequiredService<QueueImport>();
                    StoredUpload stored;
                    await using (var content = File.OpenRead(file))
                    {
                        stored = await vault.SaveAsync(content, content.Length, Path.GetFileName(file), null, stoppingToken);
                    }

                    await queue.HandleAsync(stored.Id, stored.Extension, stoppingToken);
                    _queued.Add(stamp);
                    _logger.LogInformation("Queued file {FileId} for import", fileId);
                }

                var processedDir = Path.Combine(_inputDirectory, "processed");
                if (!Directory.Exists(processedDir)) Directory.CreateDirectory(processedDir);
                var destPath = Path.Combine(processedDir, Path.GetFileName(file));
                if (File.Exists(destPath)) File.Delete(destPath);
                File.Move(file, destPath);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Type name only: exception messages can quote file names and paths.
                _logger.LogError("Could not queue file {FileId}: {ErrorType}", fileId, ex.GetType().Name);
            }
        }
    }
}

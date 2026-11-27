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

        // A crash mid-queue leaves files here. Say so; a person decides what to do.
        var processing = Path.Combine(_inputDirectory, "processing");
        if (Directory.Exists(processing) && Directory.GetFiles(processing).Length is var left and > 0)
            _logger.LogWarning("{Count} file(s) are left in the processing folder; move them back to retry.", left);

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
                var vault = scope.ServiceProvider.GetRequiredService<IDocumentVault>();
                if (!vault.IsUnlocked)
                {
                    // Left where it is until the owner unlocks.
                    _logger.LogWarning("Unlock the books first. File {FileId} is waiting.", fileId);
                    continue;
                }

                var queue = scope.ServiceProvider.GetRequiredService<QueueImport>();

                // Claimed on disk before queueing: subfolders are never scanned, so a
                // failure later cannot queue the file twice, even after a restart.
                var claimed = Move(file, "processing");
                try
                {
                    StoredUpload stored;
                    await using (var content = File.OpenRead(claimed))
                    {
                        stored = await vault.SaveAsync(content, content.Length, Path.GetFileName(file), null, stoppingToken);
                    }

                    await queue.HandleAsync(stored.Id, stored.Extension, stoppingToken);
                }
                catch
                {
                    // Not queued, so hand it back for the next scan.
                    if (!File.Exists(file)) File.Move(claimed, file);
                    throw;
                }

                _logger.LogInformation("Queued file {FileId} for import", fileId);
                // The vault holds the only copy now, so the plain staging file goes.
                File.Delete(claimed);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Type name only: exception messages can quote file names and paths.
                _logger.LogError("Could not queue file {FileId}: {ErrorType}", fileId, ex.GetType().Name);
            }
        }
    }

    /// <summary>Moves a file into a subfolder of the input folder and never overwrites one.</summary>
    private string Move(string file, string folder)
    {
        var directory = Path.Combine(_inputDirectory, folder);
        Directory.CreateDirectory(directory);
        var name = Path.GetFileName(file);
        var destination = Path.Combine(directory, name);
        if (File.Exists(destination))
            destination = Path.Combine(directory,
                $"{Path.GetFileNameWithoutExtension(name)}-{Guid.NewGuid():N}{Path.GetExtension(name)}");
        File.Move(file, destination);
        return destination;
    }
}

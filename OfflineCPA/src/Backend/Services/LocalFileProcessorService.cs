using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Data;

namespace Backend.Services;

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

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessFilesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing files.");
            }

            // Minimum delay before next scan
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }

        _logger.LogInformation("Local File Processor Service is stopping.");
    }

    private async Task ProcessFilesAsync(CancellationToken stoppingToken)
    {
        var files = Directory.GetFiles(_inputDirectory);
        
        foreach (var file in files)
        {
            if (stoppingToken.IsCancellationRequested)
                break;

            _logger.LogInformation("Processing file: {File}", file);
            
            using var scope = _serviceProvider.CreateScope();
            var parser = scope.ServiceProvider.GetRequiredService<ILocalFileParser>();
            var dbContext = scope.ServiceProvider.GetRequiredService<OfflineCPADbContext>();

            var extension = Path.GetExtension(file);

            if (parser.SupportsExtension(extension))
            {
                var text = await parser.ExtractTextAsync(file, stoppingToken);

                // Create a basic transaction from the file
                var transaction = new Transaction
                {
                    Description = $"Auto-imported from {Path.GetFileName(file)}",
                    Date = DateTime.UtcNow,
                    ReferenceDocumentPath = file
                };

                // TODO: Further basic NLP mapping based on text.

                dbContext.Transactions.Add(transaction);
                await dbContext.SaveChangesAsync(stoppingToken);

                _logger.LogInformation("Successfully processed and mapped {File}", file);
                
                // Move or delete file after processing
                var processedDir = Path.Combine(_inputDirectory, "processed");
                if (!Directory.Exists(processedDir)) Directory.CreateDirectory(processedDir);
                
                var destPath = Path.Combine(processedDir, Path.GetFileName(file));
                if (File.Exists(destPath)) File.Delete(destPath);
                
                File.Move(file, destPath);
            }
            else
            {
                _logger.LogWarning("Unsupported file type: {File}", file);
            }
        }
    }
}

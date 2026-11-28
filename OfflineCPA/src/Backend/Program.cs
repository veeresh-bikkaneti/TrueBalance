using Application.Interfaces;
using Application.Ledger;
using Backend.Configuration;
using Backend.Endpoints;
using Backend.Extensions;
using Backend.Services;
using Domain.Entities;
using Domain.Ledger;
using Infrastructure.Data;
using Infrastructure.Options;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

DotEnv.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSecureErrorHandling();

// Every log line passes the PII masker before it reaches the console, so a
// developer mistake cannot leak a raw account number into the logs.
builder.Logging.ClearProviders();
builder.Logging.Services.AddSingleton<ILoggerProvider>(sp => new PiiRedactionLoggerProvider(
    new ConsoleLoggerProvider(sp.GetRequiredService<IOptionsMonitor<ConsoleLoggerOptions>>()),
    sp.GetRequiredService<IPiiMasker>()));

// Configure EF Core with PostgreSQL. The connection string must come from
// configuration (environment variable or .env); there is no built-in secret.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "ConnectionStrings:DefaultConnection is not configured. Copy OfflineCPA/.env.example to OfflineCPA/.env " +
        "(or run OfflineCPA/scripts/init-env.sh to generate a random password) and set the variable.");
}

builder.Services.AddDbContext<OfflineCPADbContext>(options =>
    options.UseNpgsql(connectionString));

// Configure Services. Adapters register themselves: each adapter folder in
// Infrastructure has one module class, found by convention, so adding an
// adapter needs no edit here.
builder.Services.AddInfrastructureAdapterModules();
// Route groups register themselves the same way, one module class each.
builder.Services.AddEndpointModules();
builder.Services.AddHostedService<LocalFileProcessorService>();
builder.Services.AddScoped<CreateDraftEntry>();
builder.Services.AddScoped<PostEntry>();
builder.Services.AddScoped<ReverseEntry>();
builder.Services.AddScoped<LockPeriod>();
builder.Services.AddScoped<GetAccountBalances>();
builder.Services.AddScoped<Application.Imports.QueueImport>();
builder.Services.AddScoped<Application.Imports.ImportStatement>();
builder.Services.AddScoped<Application.Imports.SaveCsvProfile>();
builder.Services.AddScoped<Application.Imports.PreviewCsvLayout>();
builder.Services.AddScoped<Application.Ledger.DiscardStagedBatch>();
builder.Services.AddScoped<PostStagedRow>();
builder.Services.Configure<UploadOptions>(builder.Configuration.GetSection(UploadOptions.SectionName));
builder.Services.Configure<PiiOptions>(builder.Configuration.GetSection(PiiOptions.SectionName));
// Anchor the fingerprint key to the app content root, not the process
// working directory, so the key lands in one predictable, git-ignored place
// no matter how the backend is started.
builder.Services.PostConfigure<PiiOptions>(options =>
{
    options.FingerprintKeyPath = Path.IsPathRooted(options.FingerprintKeyPath)
        ? options.FingerprintKeyPath
        : Path.Combine(builder.Environment.ContentRootPath, options.FingerprintKeyPath);
});
// Keep uploads in memory: the framework would otherwise spill a large request
// body to a plain temporary file before the vault can encrypt it.
builder.Services.AddOptions<Microsoft.AspNetCore.Http.Features.FormOptions>()
    .Configure<IOptions<UploadOptions>>((form, uploads) =>
    {
        var limit = uploads.Value.MaxBytes + 1024 * 1024;
        form.MultipartBodyLengthLimit = limit;
        form.MemoryBufferThreshold = (int)Math.Min(limit, int.MaxValue);
    });
// No IAiAssist implementation is registered: the app ships offline and AI is
// an opt-in integration; any future implementation receives tokenized text
// from the PII layer, never raw values.

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

Infrastructure.Pii.PiiStartup.WarnIfKeyGenerated(app.Services.GetRequiredService<IPiiMasker>(), app.Logger);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");
app.UseSecureErrorHandling();

// Endpoints
app.MapUploadEndpoints();
app.MapVaultEndpoints();

app.MapEndpointModules();
app.MapLedgerEndpoints();
app.MapImportEndpoints();

// Auto-migrate on startup for ease of deployment
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OfflineCPADbContext>();
    db.Database.Migrate();
}

app.Run();

public record LedgerLineRequest(Guid AccountId, decimal Amount, string? Memo);
public record CreateEntryRequest(DateOnly Date, string Memo, Guid PeriodId, List<LedgerLineRequest> Lines);
public record ReverseEntryRequest(DateOnly ReversalDate, string Memo);

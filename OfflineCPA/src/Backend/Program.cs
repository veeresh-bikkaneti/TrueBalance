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
builder.Services.AddHostedService<LocalFileProcessorService>();
builder.Services.AddScoped<CreateDraftEntry>();
builder.Services.AddScoped<PostEntry>();
builder.Services.AddScoped<ReverseEntry>();
builder.Services.AddScoped<LockPeriod>();
builder.Services.AddScoped<GetAccountBalances>();
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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");
app.UseSecureErrorHandling();

// Endpoints
app.MapUploadEndpoints();

app.MapPost("/api/books/setup", async (SetupBooksRequest request, IChartOfAccountsSeeder seeder, IPiiMasker masker) =>
{
    if (!seeder.SupportedKinds.Contains(request.Kind, StringComparer.OrdinalIgnoreCase))
    {
        return Results.BadRequest(new { error = masker.Mask($"Unknown books kind '{request.Kind}'. Supported kinds: {string.Join(", ", seeder.SupportedKinds)}.") });
    }

    try
    {
        var added = await seeder.SeedAsync(request.Kind);
        return Results.Ok(new { kind = request.Kind.Trim().ToLowerInvariant(), accountsAdded = added });
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = masker.Mask(ex.Message) });
    }
})
.WithName("SetupBooks")
.WithOpenApi();

app.MapGet("/api/transactions", async (OfflineCPADbContext db) =>
{
    return await db.Transactions.OrderByDescending(t => t.Date).ToListAsync();
})
.WithName("GetTransactions")
.WithOpenApi();

app.MapGet("/api/export", async (IReportExporter exporter, CancellationToken cancellationToken) =>
{
    var bytes = await exporter.ExportTransactionLogAsync(cancellationToken);
    return Results.File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"OfflineCPA_Export_{DateTime.Now:yyyyMMdd}.xlsx");
})
.WithName("ExportToExcel")
.WithOpenApi();

// Ledger endpoints: thin. Bind, call one use case, map rule violations to 400.
// No auth until a later prompt, so every ledger action is attributed to the constant "local" user.
const string LocalUserId = "local";

app.MapPost("/api/ledger/entries", async (CreateEntryRequest request, CreateDraftEntry useCase, CancellationToken ct) =>
{
    try
    {
        var lines = request.Lines.Select(l => new DraftPostingLine(l.AccountId, l.Amount, l.Memo)).ToList();
        var id = await useCase.HandleAsync(new CreateDraftEntryCommand(request.Date, request.Memo, request.PeriodId, lines, LocalUserId), ct);
        return Results.Ok(new { id });
    }
    catch (LedgerRuleException ex) { return Results.BadRequest(new { error = ex.Message }); }
})
.WithName("CreateLedgerEntry")
.WithOpenApi();

app.MapPost("/api/ledger/entries/{id:guid}/post", async (Guid id, PostEntry useCase, CancellationToken ct) =>
{
    try
    {
        await useCase.HandleAsync(new PostEntryCommand(id, LocalUserId), ct);
        return Results.Ok(new { id });
    }
    catch (LedgerRuleException ex) { return Results.BadRequest(new { error = ex.Message }); }
})
.WithName("PostLedgerEntry")
.WithOpenApi();

app.MapPost("/api/ledger/entries/{id:guid}/reverse", async (Guid id, ReverseEntryRequest request, ReverseEntry useCase, CancellationToken ct) =>
{
    try
    {
        var reversalId = await useCase.HandleAsync(new ReverseEntryCommand(id, request.ReversalDate, request.Memo, LocalUserId), ct);
        return Results.Ok(new { id = reversalId });
    }
    catch (LedgerRuleException ex) { return Results.BadRequest(new { error = ex.Message }); }
})
.WithName("ReverseLedgerEntry")
.WithOpenApi();

app.MapPost("/api/ledger/periods/{id:guid}/lock", async (Guid id, LockPeriod useCase, CancellationToken ct) =>
{
    try
    {
        await useCase.HandleAsync(new LockPeriodCommand(id, LocalUserId), ct);
        return Results.Ok(new { id });
    }
    catch (LedgerRuleException ex) { return Results.BadRequest(new { error = ex.Message }); }
})
.WithName("LockLedgerPeriod")
.WithOpenApi();

app.MapGet("/api/ledger/balances", async (GetAccountBalances useCase, CancellationToken ct) =>
{
    try { return Results.Ok(await useCase.HandleAsync(ct)); }
    catch (LedgerRuleException ex) { return Results.BadRequest(new { error = ex.Message }); }
})
.WithName("GetLedgerBalances")
.WithOpenApi();

// Auto-migrate on startup for ease of deployment
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OfflineCPADbContext>();
    db.Database.Migrate();
}

app.Run();

public record SetupBooksRequest(string Kind);
public record LedgerLineRequest(Guid AccountId, decimal Amount, string? Memo);
public record CreateEntryRequest(DateOnly Date, string Memo, Guid PeriodId, List<LedgerLineRequest> Lines);
public record ReverseEntryRequest(DateOnly ReversalDate, string Memo);

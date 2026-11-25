using Application.Interfaces;
using Application.Ledger;
using Backend.Services;
using ClosedXML.Excel;
using Domain.Entities;
using Domain.Ledger;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure EF Core with PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Host=localhost;Port=5432;Database=OfflineCPADb;Username=bookkeeping;Password=secure_local_password";

builder.Services.AddDbContext<OfflineCPADbContext>(options =>
    options.UseNpgsql(connectionString));

// Configure Services
builder.Services.AddSingleton<ILocalFileParser, PdfFileParser>();
builder.Services.AddHostedService<LocalFileProcessorService>();
builder.Services.AddScoped<IChartOfAccountsSeeder, JsonChartOfAccountsSeeder>();
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddScoped<ILedgerStore, EfLedgerStore>();
builder.Services.AddScoped<CreateDraftEntry>();
builder.Services.AddScoped<PostEntry>();
builder.Services.AddScoped<ReverseEntry>();
builder.Services.AddScoped<LockPeriod>();
builder.Services.AddScoped<GetAccountBalances>();

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

// Endpoints
app.MapPost("/api/upload", async (IFormFile file, ILocalFileParser parser, OfflineCPADbContext db) =>
{
    if (file == null || file.Length == 0)
        return Results.BadRequest("No file uploaded.");

    var uploadDirectory = "/app/user-assets";
    if (!Directory.Exists(uploadDirectory))
        Directory.CreateDirectory(uploadDirectory);

    var filePath = Path.Combine(uploadDirectory, file.FileName);

    using (var stream = new FileStream(filePath, FileMode.Create))
    {
        await file.CopyToAsync(stream);
    }

    var extension = Path.GetExtension(file.FileName);
    
    // Process offline via PdfPig
    if (parser.SupportsExtension(extension))
    {
        try
        {
            var text = await parser.ExtractTextAsync(filePath);
            
            var transaction = new Transaction
            {
                Description = $"User Uploaded: {file.FileName}",
                Date = DateTime.UtcNow,
                ReferenceDocumentPath = filePath
            };

            db.Transactions.Add(transaction);
            await db.SaveChangesAsync();
            return Results.Ok(new { message = "File uploaded and processed smoothly locally." });
        }
        catch (Exception ex)
        {
             return Results.Problem("File saved locally, but text extraction failed: " + ex.Message);
        }
    }

    return Results.Ok(new { message = "File uploaded securely to local user-assets." });
})
.WithName("UploadFile")
.DisableAntiforgery();

app.MapPost("/api/books/setup", async (SetupBooksRequest request, IChartOfAccountsSeeder seeder) =>
{
    if (!seeder.SupportedKinds.Contains(request.Kind, StringComparer.OrdinalIgnoreCase))
    {
        return Results.BadRequest(new { error = $"Unknown books kind '{request.Kind}'. Supported kinds: {string.Join(", ", seeder.SupportedKinds)}." });
    }

    try
    {
        var added = await seeder.SeedAsync(request.Kind);
        return Results.Ok(new { kind = request.Kind.Trim().ToLowerInvariant(), accountsAdded = added });
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
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

app.MapGet("/api/export", async (OfflineCPADbContext db) =>
{
    var transactions = await db.Transactions.OrderBy(t => t.Date).ToListAsync();

    using var workbook = new XLWorkbook();
    var worksheet = workbook.Worksheets.Add("1120-S Bookkeeping");

    // Headers
    worksheet.Cell(1, 1).Value = "Date";
    worksheet.Cell(1, 2).Value = "Description";
    worksheet.Cell(1, 3).Value = "Document Path";
    
    var row = 2;
    foreach (var txn in transactions)
    {
        worksheet.Cell(row, 1).Value = txn.Date.ToString("yyyy-MM-dd");
        worksheet.Cell(row, 2).Value = txn.Description;
        worksheet.Cell(row, 3).Value = txn.ReferenceDocumentPath ?? "N/A";
        row++;
    }

    worksheet.Columns().AdjustToContents();

    using var stream = new MemoryStream();
    workbook.SaveAs(stream);
    stream.Position = 0;

    return Results.File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"OfflineCPA_Export_{DateTime.Now:yyyyMMdd}.xlsx");
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

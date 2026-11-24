using Application.Interfaces;
using Application.Uploads;
using Backend.Configuration;
using Backend.Extensions;
using Backend.Services;
using ClosedXML.Excel;
using Domain.Entities;
using Infrastructure.Data;
using Infrastructure.Options;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

DotEnv.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSecureErrorHandling();

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

// Configure Services
builder.Services.AddSingleton<ILocalFileParser, PdfFileParser>();
builder.Services.AddHostedService<LocalFileProcessorService>();
builder.Services.AddScoped<IChartOfAccountsSeeder, JsonChartOfAccountsSeeder>();
builder.Services.Configure<UploadOptions>(builder.Configuration.GetSection(UploadOptions.SectionName));
builder.Services.AddScoped<ISecureUploadStore, SecureUploadStore>();

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
app.MapPost("/api/upload", async (IFormFile file, ISecureUploadStore uploadStore, ILocalFileParser parser, OfflineCPADbContext db) =>
{
    if (file == null || file.Length == 0)
        return Results.BadRequest(new { error = "No file uploaded." });

    StoredUpload stored;
    try
    {
        stored = await uploadStore.SaveAsync(
            file.OpenReadStream(),
            file.Length,
            file.FileName,
            file.ContentType,
            HttpContext.RequestAborted);
    }
    catch (InvalidUploadException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }

    // Process offline via PdfPig
    if (parser.SupportsExtension(stored.Extension))
    {
        try
        {
            var text = await parser.ExtractTextAsync(stored.FullPath);

            var transaction = new Transaction
            {
                Description = $"User Uploaded: {stored.OriginalFileName}",
                Date = DateTime.UtcNow,
                ReferenceDocumentPath = stored.FullPath
            };

            db.Transactions.Add(transaction);
            await db.SaveChangesAsync();
            return Results.Ok(new { message = "File uploaded and processed smoothly locally.", id = stored.Id });
        }
        catch (Exception)
        {
             return Results.Problem("File saved locally, but text extraction failed.");
        }
    }

    return Results.Ok(new { message = "File uploaded securely to local user-assets.", id = stored.Id });
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
    return await db.Transactions.Include(t => t.Entries).OrderByDescending(t => t.Date).ToListAsync();
})
.WithName("GetTransactions")
.WithOpenApi();

app.MapGet("/api/export", async (OfflineCPADbContext db) =>
{
    var transactions = await db.Transactions.Include(t => t.Entries).OrderBy(t => t.Date).ToListAsync();

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

// Auto-migrate on startup for ease of deployment
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OfflineCPADbContext>();
    db.Database.Migrate();
}

app.Run();

public record SetupBooksRequest(string Kind);

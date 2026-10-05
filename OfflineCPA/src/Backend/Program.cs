using Application.Interfaces;
using Backend.Services;
using ClosedXML.Excel;
using Domain.Entities;
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

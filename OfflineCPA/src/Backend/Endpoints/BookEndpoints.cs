using Application.Interfaces;

namespace Backend.Endpoints;

public sealed record SetupBooksRequest(string Kind);

public sealed class BookEndpoints : IEndpointModule
{
    public void AddServices(IServiceCollection services) { }

    public void Map(IEndpointRouteBuilder app)
    {
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
    }
}

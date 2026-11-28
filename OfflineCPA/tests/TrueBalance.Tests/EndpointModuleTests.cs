using Application.Charts;
using System.Net;
using System.Net.Http.Json;
using Application.Interfaces;
using Backend.Endpoints;
using Infrastructure.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace TrueBalance.Tests;

public class EndpointModuleTests
{
    [Fact]
    public async Task Each_module_in_an_assembly_is_found_and_mapped_once_in_name_order()
    {
        var assembly = typeof(EndpointModuleTests).Assembly;
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddEndpointModules(assembly);
        await using var app = builder.Build();

        app.MapEndpointModules(assembly);

        Assert.Equal(new[] { "alpha", "zulu" }, app.Services.GetServices<TestModuleMarker>().Select(m => m.Name));
        Assert.Equal(new[] { "/test-modules/alpha", "/test-modules/zulu" }, Routes(app).Select(r => r.RoutePattern.RawText));
    }

    [Fact]
    public async Task Host_modules_map_each_moved_route_once_under_its_old_name()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddSingleton<IChartOfAccountsSeeder>(new TwoKindSeeder());
        builder.Services.AddSingleton<IPiiMasker>(new TrueBalance.Tests.Pii.MaskingPiiMasker());
        builder.Services.AddSingleton<IReportExporter>(_ => throw new NotSupportedException());
        builder.Services.AddDbContext<OfflineCPADbContext>(o => o.UseInMemoryDatabase("endpoint-modules"));
        builder.Services.AddEndpointModules();
        await using var app = builder.Build();

        app.MapEndpointModules();

        var routes = Routes(app).ToList();
        foreach (var (path, method, name) in new[]
                 {
                     ("/api/books/setup", "POST", "SetupBooks"),
                     ("/api/transactions", "GET", "GetTransactions"),
                     ("/api/export", "GET", "ExportToExcel")
                 })
        {
            var route = Assert.Single(routes, r => r.RoutePattern.RawText == path);
            Assert.Equal(method, Assert.Single(route.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods));
            Assert.Equal(name, route.Metadata.GetMetadata<IEndpointNameMetadata>()!.EndpointName);
        }
    }

    [Fact]
    public async Task Setup_with_an_unknown_kind_returns_the_same_error_text()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<IChartOfAccountsSeeder>(new TwoKindSeeder());
        builder.Services.AddSingleton<IPiiMasker>(new TrueBalance.Tests.Pii.MaskingPiiMasker());
        await using var app = builder.Build();
        new BookEndpoints().Map(app);
        await app.StartAsync();

        var response = await app.GetTestClient().PostAsJsonAsync("/api/books/setup", new { kind = "spaceship" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(
            "Unknown books kind 'spaceship'. Supported kinds: alpha, beta.",
            (await response.Content.ReadFromJsonAsync<ErrorBody>())!.Error);
    }

    private static IEnumerable<RouteEndpoint> Routes(IEndpointRouteBuilder app) =>
        app.DataSources.SelectMany(source => source.Endpoints).OfType<RouteEndpoint>();

    private sealed record ErrorBody(string Error);

    private sealed class TwoKindSeeder : IChartOfAccountsSeeder
    {
        public IReadOnlyList<string> SupportedKinds { get; } = new[] { "alpha", "beta" };

        public Task<ChartSeedFile> LoadAsync(string kind, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<int> SeedAsync(string kind, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}

internal sealed record TestModuleMarker(string Name);

internal abstract class TestModuleBase(string name) : IEndpointModule
{
    public void AddServices(IServiceCollection services) => services.AddSingleton(new TestModuleMarker(name));

    public void Map(IEndpointRouteBuilder app) => app.MapGet("/test-modules/" + name, () => name);
}

// Declared out of order on purpose: registration must sort by full name.
internal sealed class ZuluTestModule() : TestModuleBase("zulu");

internal sealed class AlphaTestModule() : TestModuleBase("alpha");

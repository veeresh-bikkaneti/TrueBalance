using System.Net;
using Backend.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace TrueBalance.Tests;

public class SecureErrorHandlingTests
{
    [Fact]
    public async Task Unhandled_exception_returns_problem_details_without_stack_trace_in_production()
    {
        var builder = WebApplication.CreateBuilder(
            new WebApplicationOptions { EnvironmentName = Environments.Production });
        builder.WebHost.UseTestServer();
        builder.Services.AddSecureErrorHandling();

        var app = builder.Build();
        app.UseSecureErrorHandling();
        app.MapGet("/api/test/throw", (HttpContext _) =>
        {
            throw new InvalidOperationException("sensitive-detail-boom");
        });
        await app.StartAsync();

        var client = app.GetTestClient();
        var response = await client.GetAsync("/api/test/throw");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Contains("application/problem+json", response.Content.Headers.ContentType?.ToString());
        Assert.DoesNotContain("sensitive-detail-boom", body, StringComparison.Ordinal);
        Assert.DoesNotContain("StackTrace", body, StringComparison.OrdinalIgnoreCase);

        await app.StopAsync();
    }
}

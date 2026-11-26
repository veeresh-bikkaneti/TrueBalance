using Application.Interfaces;
using Application.Networking;
using Infrastructure;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace TrueBalance.Tests.Networking;

/// <summary>Adapter registration by convention: the module scan registers every adapter folder's module.</summary>
public class AdapterModuleTests
{
    [Fact]
    public void Module_scan_registers_every_port_with_one_adapter()
    {
        var services = new ServiceCollection();
        // EfLedgerStore needs a DbContextOptions instance to resolve. The
        // options are never used to connect; no socket is opened.
        services.AddSingleton(new DbContextOptionsBuilder<OfflineCPADbContext>()
            .UseNpgsql("Host=127.0.0.1;Database=unused;Username=unused;Password=unused")
            .Options);
        services.AddScoped<OfflineCPADbContext>();
        services.AddOptions();
        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(new Infrastructure.Options.UploadOptions()));

        services.AddInfrastructureAdapterModules();

        using var provider = services.BuildServiceProvider();
        Assert.IsType<Infrastructure.Networking.LocalNetworkGate>(provider.GetRequiredService<INetworkGate>());
        Assert.IsType<Infrastructure.Services.PdfFileParser>(provider.GetRequiredService<IStatementImporter>());
        Assert.IsType<Infrastructure.Services.SecureUploadStore>(provider.GetRequiredService<IDocumentVault>());
        Assert.IsType<Infrastructure.Reporting.ClosedXmlReportExporter>(provider.GetRequiredService<IReportExporter>());
    }

    [Fact]
    public void Module_scan_has_one_module_per_adapter_folder()
    {
        var modules = typeof(IAdapterModule).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(IAdapterModule).IsAssignableFrom(t))
            .Select(t => t.Namespace)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            new[] { "Infrastructure.Data", "Infrastructure.Networking", "Infrastructure.Reporting", "Infrastructure.Services" },
            modules);
    }

    [Fact]
    public void No_IAiAssist_implementation_is_registered()
    {
        var services = new ServiceCollection();

        services.AddInfrastructureAdapterModules();

        using var provider = services.BuildServiceProvider();
        Assert.Null(provider.GetService<Application.Ai.IAiAssist>());
    }
}

using System.Reflection;
using Application.Ledger;
using Backend.Services;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using NetArchTest.Rules;

namespace TrueBalance.Tests.Ledger;

/// <summary>
/// Dependency rules from ADR 0011. NetArchTest.Rules 1.3.2 is MIT:
/// https://github.com/BenMorris/NetArchTest/blob/master/LICENSE
/// </summary>
public class LedgerArchitectureTests
{
    private static readonly Assembly DomainAssembly = typeof(JournalEntry).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(CreateDraftEntry).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(OfflineCPADbContext).Assembly;
    private static readonly Assembly BackendAssembly = typeof(LocalFileProcessorService).Assembly;

    // Domain must not reference Infrastructure, EF Core, or ASP.NET Core.
    [Fact]
    public void Domain_does_not_reference_infrastructure_ef_or_aspnet()
    {
        AssertNoDependency(DomainAssembly, "Infrastructure");
        AssertNoDependency(DomainAssembly, "Microsoft.EntityFrameworkCore");
        AssertNoDependency(DomainAssembly, "Microsoft.AspNetCore");
        AssertNoDependency(DomainAssembly, "System.Net.Http");
        AssertNoDependency(DomainAssembly, "System.Net.Sockets");
    }

    // Application must not reference Infrastructure, EF Core, or ASP.NET Core.
    [Fact]
    public void Application_does_not_reference_infrastructure_ef_or_aspnet()
    {
        var names = ApplicationAssembly.GetReferencedAssemblies().Select(a => a.Name).ToArray();
        Assert.DoesNotContain("Infrastructure", names);
        AssertNoDependency(ApplicationAssembly, "Infrastructure");
        AssertNoDependency(ApplicationAssembly, "Microsoft.EntityFrameworkCore");
        AssertNoDependency(ApplicationAssembly, "Microsoft.AspNetCore");
    }

    // Only the networking port (Application.Networking) and the gate adapter
    // (Infrastructure.Networking) may call the network. Every other module
    // has to go through INetworkGate.
    [Fact]
    public void Only_networking_modules_reference_http_or_sockets()
    {
        var result = Types.InAssemblies(new[] { DomainAssembly, ApplicationAssembly, InfrastructureAssembly, BackendAssembly })
            .ShouldNot()
            .HaveDependencyOnAny("System.Net.Http", "System.Net.Sockets")
            .GetResult();

        // NetArchTest 1.3.2 has no namespace allowance, so filter the two
        // allowed namespaces out of the failure list by hand.
        var failures = (result.FailingTypeNames ?? Array.Empty<string>())
            .Where(name => !name.Contains('+', StringComparison.Ordinal))
            .Where(name => !IsInNamespace(name, "Application.Networking"))
            .Where(name => !IsInNamespace(name, "Infrastructure.Networking"))
            .ToArray();
        Assert.True(failures.Length == 0, "Network types used outside the gate module: " + string.Join(", ", failures));
    }

    private static bool IsInNamespace(string typeName, string namespaceName)
        => typeName.StartsWith(namespaceName + ".", StringComparison.Ordinal)
           || typeName == namespaceName;

    // Only ledger namespaces may touch journal entries, postings, periods, or the event log.
    [Fact]
    public void Only_ledger_namespaces_touch_journal_period_and_event_log()
    {
        var assemblies = new[] { DomainAssembly, ApplicationAssembly, InfrastructureAssembly, BackendAssembly };
        var result = Types.InAssemblies(assemblies)
            .That()
            .HaveDependencyOnAny(
                "Domain.Entities.JournalEntry",
                "Domain.Entities.Posting",
                "Domain.Entities.Period",
                "Domain.Entities.LedgerAuditRecord",
                "Domain.Ledger.IDomainEvent")
            .Should()
            .ResideInNamespaceMatching(
                @"^(Domain\.(Entities|Enums|Ledger)|Application\.(Ledger|Interfaces)|Infrastructure\.(Data|Migrations))$")
            .GetResult();

        var failures = (result.FailingTypeNames ?? Array.Empty<string>())
            .Where(name => !name.Contains('+', StringComparison.Ordinal))
            .ToArray();
        Assert.True(failures.Length == 0, "Ledger types used outside ledger namespaces: " + string.Join(", ", failures));
    }

    // The hand-written snapshot must match the model. This does not open a database.
    [Fact]
    public void Ledger_model_matches_the_snapshot()
    {
        var options = new DbContextOptionsBuilder<OfflineCPADbContext>()
            .UseNpgsql("Host=127.0.0.1;Database=unused;Username=unused;Password=unused")
            .Options;
        using var db = new OfflineCPADbContext(options);
        Assert.False(db.Database.HasPendingModelChanges());
    }

    private static void AssertNoDependency(Assembly assembly, string dependency)
    {
        var result = Types.InAssembly(assembly)
            .ShouldNot()
            .HaveDependencyOn(dependency)
            .GetResult();
        Assert.True(result.IsSuccessful,
            assembly.GetName().Name + " -> " + dependency + ": " + string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>()));
    }
}

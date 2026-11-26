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
    // has to go through INetworkGate. Blocked names: NetArchTest matches a
    // dependency name as a dotted segment chain of the referenced type's full
    // name, so a namespace like "System.Net.Http" covers every type in it
    // (source: NetArchTest.Rules/Dependencies/DataStructures/NamespaceTree.cs,
    // GetAllMatchingNames, https://github.com/BenMorris/NetArchTest). [Confidence: High]
    private static readonly string[] NetworkFamilies =
    {
        "System.Net.Http",
        "System.Net.Sockets",
        "System.Net.WebSockets",
        "System.Net.Mail",
        "System.Net.WebClient",
        "System.Net.WebRequest",
        "System.Net.HttpWebRequest",
        "System.Net.Dns"
    };

    [Fact]
    public void Only_networking_modules_reference_http_or_sockets()
    {
        var result = Types.InAssemblies(new[] { DomainAssembly, ApplicationAssembly, InfrastructureAssembly, BackendAssembly })
            .ShouldNot()
            .HaveDependencyOnAny(NetworkFamilies)
            .GetResult();

        // Nested types report as "Outer+Nested": the allowance applies to the
        // outer type's namespace, so nested types can no longer slip through.
        var failures = (result.FailingTypeNames ?? Array.Empty<string>())
            .Select(TypeNameHelper.OuterTypeName)
            .Distinct()
            .Where(name => !IsInNamespace(name, "Application.Networking"))
            .Where(name => !IsInNamespace(name, "Infrastructure.Networking"))
            .ToArray();
        Assert.True(failures.Length == 0, "Network types used outside the gate module: " + string.Join(", ", failures));
    }

    // Empirical check of the two mechanics the rule depends on: the blocked
    // names really match in NetArchTest, and a nested probe type is mapped to
    // its outer type instead of being dropped.
    [Fact]
    public void Dependency_search_finds_a_nested_dns_probe_and_maps_it_to_its_outer_type()
    {
        var matching = Types.InAssembly(GetType().Assembly)
            .That()
            .HaveDependencyOnAny(NetworkFamilies)
            .GetTypes()
            .Select(t => t.FullName ?? t.Name)
            .Select(TypeNameHelper.OuterTypeName)
            .ToArray();

        Assert.Contains("TrueBalance.Tests.Ledger.LedgerArchitectureTests", matching);
    }

    /// <summary>Deliberate probe: the only type in this assembly allowed to touch a blocked family.</summary>
    private static class DnsProbe
    {
        public static object Touch() => System.Net.Dns.GetHostAddresses("localhost");
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

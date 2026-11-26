namespace TrueBalance.Tests.Ledger;

public class TypeNameHelperTests
{
    [Fact]
    public void Keeps_a_plain_type_name()
    {
        Assert.Equal("Infrastructure.Services.Foo", TypeNameHelper.OuterTypeName("Infrastructure.Services.Foo"));
    }

    [Fact]
    public void Maps_a_nested_type_to_its_outer_type()
    {
        // The bug this guards: the old filter DROPPED every nested name, so a
        // nested type outside the allowed namespaces never failed the rule.
        Assert.Equal("Infrastructure.Services.Foo", TypeNameHelper.OuterTypeName("Infrastructure.Services.Foo+Leak"));
    }

    [Fact]
    public void Maps_a_nested_type_in_an_allowed_namespace_to_the_allowed_outer_type()
    {
        Assert.Equal(
            "Infrastructure.Networking.LocalNetworkGate",
            TypeNameHelper.OuterTypeName("Infrastructure.Networking.LocalNetworkGate+X"));
    }

    [Fact]
    public void Maps_a_doubly_nested_type_to_its_outermost_type()
    {
        Assert.Equal("A.B.C", TypeNameHelper.OuterTypeName("A.B.C+D+E"));
    }
}

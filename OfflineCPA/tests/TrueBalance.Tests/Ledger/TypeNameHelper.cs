namespace TrueBalance.Tests.Ledger;

/// <summary>
/// Architecture-rule mapping from a NetArchTest failing type name to its
/// outer type. NetArchTest reports nested types as "Outer+Nested", and the
/// namespace allowance applies to the outer type's namespace.
/// </summary>
public static class TypeNameHelper
{
    /// <summary>Returns the part before the first '+', so the allowance check sees the outer type's namespace.</summary>
    public static string OuterTypeName(string typeName)
    {
        var plus = typeName.IndexOf('+');
        return plus >= 0 ? typeName[..plus] : typeName;
    }
}

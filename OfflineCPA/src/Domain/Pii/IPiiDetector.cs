namespace Domain.Pii;

/// <summary>
/// A detector finds candidate values of one <see cref="PiiType"/> in text.
/// Implementations must be pure local pattern checks: no network, no AI,
/// no state. The infrastructure layer provides them through DI; the policy
/// decides which overlapping match wins.
/// </summary>
public interface IPiiDetector
{
    PiiType Type { get; }

    IEnumerable<PiiMatch> Find(string text);
}

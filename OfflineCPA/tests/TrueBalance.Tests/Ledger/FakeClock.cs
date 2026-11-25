using Application.Interfaces;

namespace TrueBalance.Tests.Ledger;

/// <summary>Deterministic clock. Every read returns the current value; tests advance it explicitly.</summary>
public sealed class FakeClock : IClock
{
    public FakeClock(DateTime? start = null)
    {
        UtcNow = start ?? new DateTime(2026, 11, 25, 12, 0, 0, DateTimeKind.Utc);
    }

    public DateTime UtcNow { get; private set; }

    public void Advance(TimeSpan by) => UtcNow += by;
}

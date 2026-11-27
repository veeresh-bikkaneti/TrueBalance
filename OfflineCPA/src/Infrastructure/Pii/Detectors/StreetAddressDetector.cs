using Domain.Pii;

namespace Infrastructure.Pii.Detectors;

// TEMP STUB for red run.
public sealed class StreetAddressDetector : IPiiDetector
{
    public PiiType Type => PiiType.StreetAddress;

    public IEnumerable<PiiMatch> Find(string text) => [];
}

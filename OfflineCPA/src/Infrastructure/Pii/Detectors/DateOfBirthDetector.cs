using Domain.Pii;

namespace Infrastructure.Pii.Detectors;

// TEMP STUB for red run.
public sealed class DateOfBirthDetector : IPiiDetector
{
    public PiiType Type => PiiType.DateOfBirth;

    public IEnumerable<PiiMatch> Find(string text) => [];
}

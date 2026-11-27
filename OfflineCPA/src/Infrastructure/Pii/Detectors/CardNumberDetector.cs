using Domain.Pii;

namespace Infrastructure.Pii.Detectors;

// TEMP STUB for red run.
public sealed class CardNumberDetector : IPiiDetector
{
    public PiiType Type => PiiType.Card;

    public IEnumerable<PiiMatch> Find(string text) => [];
}

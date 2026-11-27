using Domain.Pii;

namespace Application.Interfaces;

/// <summary>
/// Port for the PII masking core (ADR 0010). The statement importer and every
/// other caller pass text through <see cref="Mask"/> before persisting,
/// logging, or echoing it; detection itself stays behind this port in the
/// infrastructure adapters. Masking is non-destructive: the token keeps the
/// last four digits and a keyed fingerprint so matching still works. Use
/// <see cref="MaskDetailed"/> when storing structured matching keys (for
/// example, the statement importer in prompt 05).
/// </summary>
public interface IPiiMasker
{
    string Mask(string text);

    PiiMaskingResult MaskDetailed(string text);
}

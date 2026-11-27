namespace Domain.Pii;

/// <summary>
/// The masked replacement for one detected value. The full value is never
/// kept: the last four digits stay readable for display and matching, and
/// the fingerprint (HMAC-SHA256 over the digits only, hex) lets two rows
/// holding the same number match without storing it.
/// </summary>
public sealed record MaskedValue(PiiType Type, string Last4, string Fingerprint, string Token);

/// <summary>The output of one masking pass over a text.</summary>
public sealed record PiiMaskingResult(string MaskedText, IReadOnlyList<MaskedValue> Matches);

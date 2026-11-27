namespace Domain.Pii;

/// <summary>
/// One detected value inside a text: what it is, where it starts, and how
/// long the raw span is. The raw value is kept in memory only for the length
/// of the masking pass; it is never returned to callers that persist data.
/// </summary>
public sealed record PiiMatch(PiiType Type, string RawValue, int Start, int Length);

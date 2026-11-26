namespace Application.Ai;

/// <summary>
/// Port for an optional AI assist. No implementation ships with the app and
/// none is registered by default. Any future implementation must receive
/// tokenized text produced by the PII layer, never raw values, file paths, or
/// personal names.
/// </summary>
public interface IAiAssist
{
    Task<AiCategorizationSuggestion> SuggestCategorizationAsync(TokenizedStatementLine line, CancellationToken cancellationToken = default);
}

/// <summary>One statement line with the raw values replaced by PII-layer tokens.</summary>
public sealed record TokenizedStatementLine(IReadOnlyList<string> Tokens, decimal Amount, DateOnly Date);

/// <summary>A suggested account code with a short reason. For review only; the user confirms.</summary>
public sealed record AiCategorizationSuggestion(string AccountCode, string Reason);

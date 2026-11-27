namespace Domain.Pii;

/// <summary>
/// The kinds of personally identifying values the masking core recognizes.
/// Prompt 06 extends this list (cards, dates of birth, addresses).
/// </summary>
public enum PiiType
{
    BankAccountNumber,
    AbaRoutingNumber,
    Ssn,
    EinItin,
    Card,
    DateOfBirth,
    StreetAddress,
}

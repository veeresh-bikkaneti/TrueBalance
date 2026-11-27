namespace Domain.Pii;

/// <summary>
/// The kinds of personally identifying values the masking core recognizes.
/// Cards, dates of birth and street addresses were added in prompt 06; phone
/// numbers and email addresses in prompt 06c.
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
    Phone,
    Email,
}

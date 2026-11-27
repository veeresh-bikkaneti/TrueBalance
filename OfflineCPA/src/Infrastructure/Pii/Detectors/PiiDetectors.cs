using Domain.Pii;

namespace Infrastructure.Pii.Detectors;

/// <summary>
/// The one list of detectors. The app and the test masker both read it, so the
/// no-leak tests can't quietly run fewer detectors than the app does.
/// </summary>
public static class PiiDetectors
{
    // Order does not decide precedence; the domain policy ranks overlapping
    // matches by PiiType priority.
    public static IReadOnlyList<IPiiDetector> All() =>
    [
        new AbaRoutingNumberDetector(),
        new SsnDetector(),
        new EinItinDetector(),
        new CardNumberDetector(),
        new DateOfBirthDetector(),
        new StreetAddressDetector(),
        new PhoneNumberDetector(),
        new EmailDetector(),
        new BankAccountNumberDetector(),
    ];
}

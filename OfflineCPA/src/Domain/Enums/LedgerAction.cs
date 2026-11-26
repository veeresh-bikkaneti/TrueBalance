namespace Domain.Enums;

public enum LedgerAction
{
    DraftCreated = 0,
    Posted = 1,
    Reversed = 2,
    PeriodLocked = 3,
    NetworkGateDenied = 4,
    NetworkGateGranted = 5
}

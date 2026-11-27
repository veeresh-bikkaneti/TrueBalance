using Domain.Ledger;

namespace Application.Uploads;

/// <summary>Carries a fixed sentence only: never a passphrase, a path or file content.</summary>
public class VaultException : LedgerRuleException
{
    public VaultException(string message) : base(message)
    {
    }
}

public sealed class VaultLockedException : VaultException
{
    public VaultLockedException() : base("Unlock the books first.")
    {
    }
}

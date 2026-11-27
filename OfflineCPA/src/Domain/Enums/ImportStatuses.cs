namespace Domain.Enums;

public enum ImportBatchStatus
{
    Queued,
    Staged,
    Failed
}

public enum StagedRowStatus
{
    Pending,
    Duplicate,
    Posted,
    Discarded
}

/// <summary>What the balance column of a statement counts.</summary>
public enum BalanceMeaning
{
    /// <summary>Owed for a liability account (a credit card), available for any other.</summary>
    FromAccountType,
    Available,
    Owed
}

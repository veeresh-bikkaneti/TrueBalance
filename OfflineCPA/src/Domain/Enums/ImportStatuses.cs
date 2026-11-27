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
    Posted
}

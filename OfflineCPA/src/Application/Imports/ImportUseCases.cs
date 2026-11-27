using Application.Interfaces;
using Domain.Entities;
using Domain.Ledger;

namespace Application.Imports;

public sealed class QueueImport(IImportStore store, IClock clock)
{
    public async Task<Guid> HandleAsync(Guid documentId, string extension, CancellationToken ct = default)
    {
        var batch = ImportBatch.Queue(documentId, extension, clock.UtcNow);
        await store.AddBatchAsync(batch, ct);
        await store.SaveChangesAsync(ct);
        return batch.Id;
    }
}

public sealed record ImportStatementCommand(Guid BatchId, Guid BankAccountId, string? BankName);

public sealed class ImportStatement(
    IImportStore store, IDocumentVault vault, IEnumerable<IStatementImporter> importers, IPiiMasker masker)
{
    /// <summary>Parses one queued batch into staged rows. Nothing here touches the ledger.</summary>
    /// <returns>The number of staged rows.</returns>
    public async Task<int> HandleAsync(ImportStatementCommand command, CancellationToken ct = default)
    {
        var batch = await store.FindBatchAsync(command.BatchId, ct)
            ?? throw new LedgerRuleException("The import batch does not exist.");
        batch.EnsureQueued();
        if (!await store.AccountExistsAsync(command.BankAccountId, ct))
            throw new LedgerRuleException("The bank account does not exist.");
        var map = await FindMapAsync(command.BankName, ct);

        ParsedStatement parsed;
        try
        {
            var importer = importers.FirstOrDefault(i => i.SupportsExtension(batch.Extension));
            var path = vault.FindPath(batch.DocumentId, batch.Extension);
            if (importer is null || path is null)
                return await FailAsync(batch, "The stored file is missing or its type is not supported.", ct);
            parsed = await importer.ParseAsync(path, map, ct);
        }
        catch (StatementParseException ex)
        {
            return await FailAsync(batch, ex.Message, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Library messages may quote the file, so none of them is kept.
            return await FailAsync(batch, "The file could not be read.", ct);
        }

        if (parsed.Rows.Count == 0)
            return await FailAsync(batch, "No statement rows were found.", ct);

        var rows = parsed.Rows.Select(r => StagedRow.Stage(batch, command.BankAccountId, r.RowNumber, r.Date,
            masker.Mask(r.Description), r.Amount, r.Balance, parsed.IsExperimental)).ToList();

        // Compare with earlier batches only: identical rows inside one file are real.
        var earlier = (await store.GetAccountRowsAsync(command.BankAccountId, rows.Min(r => r.Date), rows.Max(r => r.Date), ct))
            .Select(r => (r.Date, r.Amount, r.MatchKey))
            .ToHashSet();
        foreach (var row in rows.Where(r => earlier.Contains((r.Date, r.Amount, r.MatchKey))))
            row.FlagDuplicate();

        await store.AddRowsAsync(rows, ct);
        batch.MarkStaged();
        await store.SaveChangesAsync(ct);
        return rows.Count;
    }

    private async Task<CsvColumnMap?> FindMapAsync(string? bankName, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(bankName)) return null;
        var p = await store.FindProfileAsync(masker.Mask(bankName.Trim()), ct)
            ?? throw new LedgerRuleException("No column mapping is saved for that bank.");
        return new CsvColumnMap(p.DateColumn, p.DescriptionColumn, p.AmountColumn, p.DebitColumn, p.CreditColumn,
            p.BalanceColumn, p.DateFormat);
    }

    private async Task<int> FailAsync(ImportBatch batch, string reason, CancellationToken ct)
    {
        batch.MarkFailed(reason);
        await store.SaveChangesAsync(ct);
        throw new LedgerRuleException(reason);
    }
}

public sealed class SaveCsvProfile(IImportStore store, IPiiMasker masker)
{
    public async Task HandleAsync(string bankName, CsvColumnMap map, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(bankName) || string.IsNullOrWhiteSpace(map.Date) || string.IsNullOrWhiteSpace(map.Description))
            throw new LedgerRuleException("A profile needs a bank name, a date column and a description column.");
        if (string.IsNullOrWhiteSpace(map.Amount) && (string.IsNullOrWhiteSpace(map.Debit) || string.IsNullOrWhiteSpace(map.Credit)))
            throw new LedgerRuleException("A profile needs an amount column, or both debit and credit columns.");
        var columns = new[] { map.Date, map.Description, map.Amount, map.Debit, map.Credit, map.Balance, map.DateFormat };
        if (columns.Any(c => c is not null && (c.Length > 100 || masker.Mask(c) != c)))
            throw new LedgerRuleException("A column name is too long or looks like sensitive data.");

        var name = masker.Mask(bankName.Trim());
        if (name.Length > 100)
            throw new LedgerRuleException("The bank name is too long.");
        var profile = await store.FindProfileAsync(name, ct);
        if (profile is null)
        {
            profile = new CsvMappingProfile { BankName = name };
            await store.AddProfileAsync(profile, ct);
        }
        profile.DateColumn = map.Date;
        profile.DescriptionColumn = map.Description;
        profile.AmountColumn = map.Amount;
        profile.DebitColumn = map.Debit;
        profile.CreditColumn = map.Credit;
        profile.BalanceColumn = map.Balance;
        profile.DateFormat = map.DateFormat;
        await store.SaveChangesAsync(ct);
    }
}

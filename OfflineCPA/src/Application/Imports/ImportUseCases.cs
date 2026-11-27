using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
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

/// <summary>The sign layout a person confirmed. Column names stay with the bank profile.</summary>
public sealed record ConfirmedLayout(AmountLayout AmountLayout, bool DebitsCarryMinus = false,
    string? IndicatorColumn = null, string? MoneyInMarker = null, string? MoneyOutMarker = null);

public sealed record ImportStatementCommand(
    Guid BatchId, Guid BankAccountId, string? BankName, ConfirmedLayout? Layout = null);

public sealed class ImportStatement(
    IImportStore store, IDocumentVault vault, IEnumerable<IStatementImporter> importers, IPiiMasker masker)
{
    /// <summary>Parses one queued batch into staged rows. Nothing here touches the ledger.</summary>
    /// <returns>The number of staged rows.</returns>
    public async Task<int> HandleAsync(ImportStatementCommand command, CancellationToken ct = default)
    {
        var batch = await store.FindBatchAsync(command.BatchId, ct)
            ?? throw new LedgerRuleException("The import batch does not exist.");
        batch.EnsureImportable();
        if (!await store.AccountExistsAsync(command.BankAccountId, ct))
            throw new LedgerRuleException("The bank account does not exist.");
        var map = batch.Extension == ".csv"
            ? await ConfirmedCsvMapAsync(command, ct)
            : await FindMapAsync(command.BankName, ct);

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

        var signed = parsed.Rows.Select(r => (r.Amount, r.Balance)).ToList();
        var check = RunningBalance.Check(signed);
        var opposite = check == BalanceCheck.Failed && RunningBalance.Check(
            signed.Select(r => (-r.Amount, r.Balance)).ToList()) == BalanceCheck.Passed;
        batch.RecordBalanceCheck(check, opposite);
        if (check == BalanceCheck.Failed) rows.ForEach(r => r.FlagBalanceFailed());

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
        return CsvProfiles.ToMap(p);
    }

    /// <summary>A CSV file never decides its own signs: a person confirmed them, now or earlier.</summary>
    private async Task<CsvColumnMap> ConfirmedCsvMapAsync(ImportStatementCommand command, CancellationToken ct)
    {
        const string refusal = "Confirm how this bank signs amounts first.";
        if (string.IsNullOrWhiteSpace(command.BankName)) throw new LedgerRuleException(refusal);
        var profile = await store.FindProfileAsync(masker.Mask(command.BankName.Trim()), ct);
        if (command.Layout is not { } layout)
            return profile is { LayoutConfirmed: true } ? CsvProfiles.ToMap(profile) : throw new LedgerRuleException(refusal);

        var debitCredit = layout.AmountLayout == AmountLayout.DebitCredit;
        var columns = profile is not null ? CsvProfiles.ToMap(profile) : new CsvColumnMap("date", "description",
            debitCredit ? null : "amount", debitCredit ? "debit" : null, debitCredit ? "credit" : null, "balance", null);
        var map = columns with
        {
            AmountLayout = layout.AmountLayout,
            DebitsCarryMinus = layout.DebitsCarryMinus,
            IndicatorColumn = layout.IndicatorColumn,
            MoneyInMarker = layout.MoneyInMarker,
            MoneyOutMarker = layout.MoneyOutMarker
        };
        await CsvProfiles.ApplyAsync(store, masker, command.BankName, map, confirmed: true, ct);
        return map;
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
    /// <summary>Saving columns never confirms a layout; changing them takes the confirmation back.</summary>
    public async Task HandleAsync(string bankName, CsvColumnMap map, CancellationToken ct = default)
    {
        await CsvProfiles.ApplyAsync(store, masker, bankName, map, confirmed: false, ct);
        await store.SaveChangesAsync(ct);
    }
}

internal static class CsvProfiles
{
    public static CsvColumnMap ToMap(CsvMappingProfile p)
        => new(p.DateColumn, p.DescriptionColumn, p.AmountColumn, p.DebitColumn, p.CreditColumn, p.BalanceColumn,
            p.DateFormat, p.AmountLayout, p.DebitsCarryMinus, p.IndicatorColumn, p.MoneyInMarker, p.MoneyOutMarker);

    /// <summary>Validates, masks and sets the profile. The caller saves.</summary>
    public static async Task ApplyAsync(IImportStore store, IPiiMasker masker, string bankName, CsvColumnMap map,
        bool confirmed, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(bankName) || string.IsNullOrWhiteSpace(map.Date) || string.IsNullOrWhiteSpace(map.Description))
            throw new LedgerRuleException("A profile needs a bank name, a date column and a description column.");
        var layout = map.AmountLayout
            ?? (string.IsNullOrWhiteSpace(map.Amount) ? AmountLayout.DebitCredit : AmountLayout.Signed);
        if (!Enum.IsDefined(layout))
            throw new LedgerRuleException("That amount layout is not supported.");
        if (layout == AmountLayout.DebitCredit
                ? string.IsNullOrWhiteSpace(map.Debit) || string.IsNullOrWhiteSpace(map.Credit)
                : string.IsNullOrWhiteSpace(map.Amount))
            throw new LedgerRuleException("A profile needs an amount column, or both debit and credit columns.");
        if (layout == AmountLayout.Indicator && (string.IsNullOrWhiteSpace(map.IndicatorColumn)
                || string.IsNullOrWhiteSpace(map.MoneyInMarker) || string.IsNullOrWhiteSpace(map.MoneyOutMarker)
                || map.MoneyInMarker.Trim().Equals(map.MoneyOutMarker.Trim(), StringComparison.OrdinalIgnoreCase)))
            throw new LedgerRuleException("An indicator layout needs an indicator column and two different markers.");
        var values = new[]
        {
            map.Date, map.Description, map.Amount, map.Debit, map.Credit, map.Balance, map.DateFormat,
            map.IndicatorColumn, map.MoneyInMarker, map.MoneyOutMarker
        };
        if (values.Any(c => c is not null && (c.Length > 100 || masker.Mask(c) != c)))
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
        profile.AmountLayout = layout;
        profile.DebitsCarryMinus = map.DebitsCarryMinus;
        profile.IndicatorColumn = map.IndicatorColumn;
        profile.MoneyInMarker = map.MoneyInMarker;
        profile.MoneyOutMarker = map.MoneyOutMarker;
        profile.LayoutConfirmed = confirmed;
    }
}

public sealed record PreviewRow(DateOnly Date, string Description, string Direction, decimal Amount);

public sealed record CsvLayoutPreview(
    ConfirmedLayout Layout, BalanceCheck BalanceCheck, bool BalanceBacksIt, IReadOnlyList<PreviewRow> Rows);

/// <summary>Proposes a sign layout for one queued CSV batch. Writes nothing.</summary>
public sealed class PreviewCsvLayout(
    IImportStore store, IDocumentVault vault, IEnumerable<IStatementImporter> importers, IPiiMasker masker)
{
    public async Task<CsvLayoutPreview> HandleAsync(Guid batchId, string? bankName, CancellationToken ct = default)
    {
        var batch = await store.FindBatchAsync(batchId, ct)
            ?? throw new LedgerRuleException("The import batch does not exist.");
        if (batch.Extension != ".csv")
            throw new LedgerRuleException("Only a CSV batch has a sign layout to preview.");
        var profile = string.IsNullOrWhiteSpace(bankName)
            ? null
            : await store.FindProfileAsync(masker.Mask(bankName.Trim()), ct);
        var map = profile is null ? null : CsvProfiles.ToMap(profile);
        // An unconfirmed profile gives only column names, except for markers nobody can guess.
        var confirmed = profile is { LayoutConfirmed: true };
        if (!confirmed && map is { AmountLayout: not AmountLayout.Indicator })
            map = map with { AmountLayout = null };

        var parsed = await ParseAsync(batch, map, ct);
        var check = Check(parsed);
        var applied = parsed.AppliedMap!;
        if (!confirmed && check == BalanceCheck.Failed && applied.AmountLayout != AmountLayout.Indicator)
        {
            var other = await ParseAsync(batch, applied.AmountLayout switch
            {
                AmountLayout.DebitCredit => applied with { DebitsCarryMinus = !applied.DebitsCarryMinus },
                AmountLayout.Signed => applied with { AmountLayout = AmountLayout.SignedReversed },
                _ => applied with { AmountLayout = AmountLayout.Signed }
            }, ct);
            if (Check(other) == BalanceCheck.Passed)
                (parsed, check, applied) = (other, BalanceCheck.Passed, other.AppliedMap!);
        }

        return new CsvLayoutPreview(
            new ConfirmedLayout(applied.AmountLayout!.Value, applied.DebitsCarryMinus, applied.IndicatorColumn,
                applied.MoneyInMarker, applied.MoneyOutMarker),
            check,
            check == BalanceCheck.Passed,
            parsed.Rows.Take(3).Select(r => new PreviewRow(r.Date, masker.Mask(r.Description),
                r.Amount < 0m ? "MoneyOut" : "MoneyIn", Math.Abs(r.Amount))).ToList());
    }

    private static BalanceCheck Check(ParsedStatement parsed)
        => RunningBalance.Check(parsed.Rows.Select(r => (r.Amount, r.Balance)).ToList());

    private async Task<ParsedStatement> ParseAsync(ImportBatch batch, CsvColumnMap? map, CancellationToken ct)
    {
        try
        {
            var importer = importers.FirstOrDefault(i => i.SupportsExtension(batch.Extension));
            var path = vault.FindPath(batch.DocumentId, batch.Extension);
            if (importer is null || path is null)
                throw new LedgerRuleException("The stored file is missing or its type is not supported.");
            var parsed = await importer.ParseAsync(path, map, ct);
            return parsed.Rows.Count > 0 ? parsed : throw new LedgerRuleException("No statement rows were found.");
        }
        catch (StatementParseException ex)
        {
            throw new LedgerRuleException(ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not LedgerRuleException)
        {
            // Library messages may quote the file, so none of them is kept.
            throw new LedgerRuleException("The file could not be read.");
        }
    }
}

import { useState } from 'react';
import { AccountSelect } from './AccountSelect';
import { ActionButton } from './ActionButton';
import { importBatch, previewLayout, saveProfile, type Account, type Batch, type Layout, type Preview } from './api';
import { ColumnFields } from './ColumnFields';
import { startingFields, toColumns } from './columns';
import { SignChoice } from './SignChoice';

interface Props {
  batch: Batch;
  accounts: Account[];
  onImported: () => unknown;
}

export function ColumnsStep({ batch, accounts, onImported }: Props) {
  const [accountId, setAccountId] = useState('');
  const [bankName, setBankName] = useState('');
  const [fields, setFields] = useState(startingFields);
  const [shown, setShown] = useState<{ inputs: string; preview: Preview; layout: Layout }>();
  const isCsv = batch.extension === '.csv';
  const bank = bankName.trim();
  const ready = accountId !== '' && (!isCsv || bank !== '');
  // A preview belongs to the inputs it was made from. Change one and it goes away.
  const inputs = JSON.stringify([accountId, bank, fields]);
  const current = shown?.inputs === inputs ? shown : undefined;

  const runImport = async (layout?: Layout) => {
    await importBatch(batch.id, { bankAccountId: accountId, bankName: isCsv ? bank : null, layout });
    await onImported();
  };

  const runPreview = async () => {
    await saveProfile(bank, toColumns(fields));
    const preview = await previewLayout(batch.id, bank, accountId);
    setShown({ inputs, preview, layout: preview.layout });
  };

  return (
    <>
      <h2>Set up this import</h2>
      {batch.failureReason && <p>Last try failed: {batch.failureReason}</p>}
      <AccountSelect label="Bank account" accounts={accounts} value={accountId} onChange={setAccountId} />
      {accounts.length === 0 && (
        <p>No accounts to pick yet. This list shows only accounts that already have a posted entry.</p>
      )}
      {!isCsv && <ActionButton label="Import" disabled={!ready} onRun={() => runImport()} />}
      {isCsv && (
        <>
          <label>
            Bank name
            <input value={bankName} onChange={(e) => setBankName(e.target.value)} />
          </label>
          <p>Confirmed this bank before? Use its saved settings. If not, match the columns below.</p>
          <ActionButton label="Import with saved bank settings" disabled={!ready} onRun={() => runImport()} />
          <ColumnFields value={fields} onChange={setFields} />
          <ActionButton label="Save columns and preview" disabled={!ready} onRun={runPreview} />
          {current && (
            <>
              <SignChoice preview={current.preview} layout={current.layout}
                onChange={(layout) => setShown({ ...current, layout })} />
              <ActionButton label="Confirm and import" onRun={() => runImport(current.layout)} />
            </>
          )}
        </>
      )}
    </>
  );
}

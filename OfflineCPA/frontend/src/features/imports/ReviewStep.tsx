import { useState } from 'react';
import { ActionButton } from './ActionButton';
import { discardBatch, listRows, RowStatus, type Account, type Batch, type RowItem } from './api';
import { ReviewRow } from './ReviewRow';

interface Props {
  batch: Batch;
  accounts: Account[];
  onDiscarded: () => unknown;
}

export function ReviewStep({ batch, accounts, onDiscarded }: Props) {
  const [items, setItems] = useState<RowItem[]>();
  const load = async () => setItems(await listRows(batch.id));
  // Unknown until the rows arrive, so the discard button waits for them.
  const nothingPosted = items?.every((item) => item.row.status !== RowStatus.Posted);

  const discard = async () => {
    if (!window.confirm('Discard this import? Nothing from it is posted. You can import the stored file again.')) return;
    await discardBatch(batch.id);
    await onDiscarded();
  };

  return (
    <>
      <h2>Review rows</h2>
      {batch.balanceReason && <p>{batch.balanceReason}</p>}
      {accounts.length === 0 && (
        <p>No categories to pick yet. This list shows only accounts that already have a posted entry.</p>
      )}
      <ActionButton label="Reload rows" runOnMount onRun={load} />
      <ul className="rows">
        {items?.map((item) => <ReviewRow key={item.row.id} item={item} accounts={accounts} onPosted={load} />)}
      </ul>
      {nothingPosted && <ActionButton label="Discard this import" onRun={discard} />}
    </>
  );
}

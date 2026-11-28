import { useState } from 'react';
import { BatchStatus, listAccounts, listBatches, type Account, type Batch } from './api';
import { ColumnsStep } from './ColumnsStep';
import { PickStatement } from './PickStatement';
import { ReviewStep } from './ReviewStep';
import './imports.css';

/** Step switcher: pick a statement, then set it up or review it, depending on its status. */
export function ImportsScreen() {
  const [batches, setBatches] = useState<Batch[]>([]);
  const [accounts, setAccounts] = useState<Account[]>([]);
  const [openId, setOpenId] = useState<string>();
  const open = batches.find((batch) => batch.id === openId);

  const load = async () => {
    const [nextBatches, nextAccounts] = await Promise.all([listBatches(), listAccounts()]);
    setBatches(nextBatches);
    setAccounts(nextAccounts);
  };

  return (
    <div className="imports">
      {!open ? <PickStatement batches={batches} onLoad={load} onOpen={setOpenId} /> : (
        <>
          <button type="button" onClick={() => setOpenId(undefined)}>Back to statements</button>
          {open.status === BatchStatus.Staged
            ? <ReviewStep key={open.id} batch={open} accounts={accounts} onDiscarded={load} />
            : <ColumnsStep key={open.id} batch={open} accounts={accounts} onImported={load} />}
        </>
      )}
    </div>
  );
}

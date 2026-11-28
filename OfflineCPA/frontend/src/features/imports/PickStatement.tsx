import { ActionButton } from './ActionButton';
import { BatchStatus, type Batch } from './api';

const statusText: Record<number, string> = {
  [BatchStatus.Queued]: 'waiting',
  [BatchStatus.Staged]: 'ready to review',
  [BatchStatus.Failed]: 'failed',
};

interface Props {
  batches: Batch[];
  onLoad: () => unknown;
  onOpen: (batchId: string) => void;
}

export function PickStatement({ batches, onLoad, onOpen }: Props) {
  return (
    <>
      <h2>Import a statement</h2>
      <p>
        Uploading from this screen is not built yet. Put a CSV, OFX, QFX or PDF statement in the watched folder,
        unlock the books, then refresh this list.
      </p>
      <ActionButton label="Refresh list" runOnMount onRun={onLoad} />
      <ul className="rows">
        {batches.map((batch) => {
          const name = `${batch.extension.slice(1).toUpperCase()} statement`;
          const added = batch.createdAtUtc.slice(0, 10);
          return (
            <li key={batch.id}>
              <p>{name}, added {added}, {statusText[batch.status]}</p>
              {batch.failureReason && <p>{batch.failureReason}</p>}
              <button type="button" aria-label={`Open ${name} added ${added}`} onClick={() => onOpen(batch.id)}>
                Open
              </button>
            </li>
          );
        })}
      </ul>
    </>
  );
}

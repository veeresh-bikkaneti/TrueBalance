import { useCallback, useEffect, useRef, useState } from 'react';

interface Props {
  label: string;
  onRun: () => unknown;
  disabled?: boolean;
  runOnMount?: boolean;
}

/** A button that keeps its own failure message right beside it. Busy buttons can't be double-clicked. */
export function ActionButton({ label, onRun, disabled, runOnMount }: Props) {
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  const run = useCallback(async (action: () => unknown) => {
    setBusy(true);
    setError('');
    try {
      await action();
    } catch (failure) {
      setError(failure instanceof Error ? failure.message : String(failure));
    } finally {
      setBusy(false);
    }
  }, []);
  const onMount = useRef(runOnMount ? onRun : undefined);
  useEffect(() => {
    if (onMount.current) void run(onMount.current);
  }, [run]);

  return (
    <div className="action">
      <button type="button" disabled={disabled || busy} onClick={() => void run(onRun)}>{label}</button>
      {error && <p role="alert">{error}</p>}
    </div>
  );
}

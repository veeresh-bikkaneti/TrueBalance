import type { BalanceMeaning, Layout, Preview } from './api';

const checkText = { Passed: 'passed', Failed: 'failed', NotAvailable: 'not available' };

interface Props {
  preview: Preview;
  layout: Layout;
  onChange: (layout: Layout) => void;
}

/** Shows how the first rows read, then lets a person confirm the bank's sign. */
export function SignChoice({ preview, layout, onChange }: Props) {
  return (
    <>
      <h3>Check the first rows</h3>
      <p>These rows use the suggested sign.</p>
      <ul>
        {preview.rows.map((row, index) => (
          <li key={index}>
            {row.date}, {row.description}, {row.direction === 'MoneyIn' ? 'money in' : 'money out'},{' '}
            {row.amount.toFixed(2)}
          </li>
        ))}
      </ul>
      <p>Balance check: {checkText[preview.balanceCheck]}.</p>
      {preview.balanceMeaningNote && <p>{preview.balanceMeaningNote}</p>}
      {layout.amountLayout === 'Indicator' && <p>The in-or-out column decides the direction.</p>}
      {(layout.amountLayout === 'Signed' || layout.amountLayout === 'SignedReversed') && (
        <label>
          Bank&apos;s sign
          <select value={layout.amountLayout}
            onChange={(e) => onChange({ ...layout, amountLayout: e.target.value as Layout['amountLayout'] })}>
            <option value="Signed">A minus amount is money out</option>
            <option value="SignedReversed">A minus amount is money in</option>
          </select>
        </label>
      )}
      {layout.amountLayout === 'DebitCredit' && (
        <label>
          Bank&apos;s sign
          <select value={String(layout.debitsCarryMinus)}
            onChange={(e) => onChange({ ...layout, debitsCarryMinus: e.target.value === 'true' })}>
            <option value="false">Debits are written as plain numbers</option>
            <option value="true">Debits are written with a minus</option>
          </select>
        </label>
      )}
      <label>
        The balance column counts
        <select value={String(layout.balanceMeaning ?? 0)}
          onChange={(e) => onChange({ ...layout, balanceMeaning: Number(e.target.value) as BalanceMeaning })}>
          <option value="0">Follow the account type</option>
          <option value="1">Money available</option>
          <option value="2">An amount owed</option>
        </select>
      </label>
    </>
  );
}

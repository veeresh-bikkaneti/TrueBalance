import type { Fields } from './columns';

interface Props {
  value: Fields;
  onChange: (fields: Fields) => void;
}

export function ColumnFields({ value, onChange }: Props) {
  const input = (label: string, key: Exclude<keyof Fields, 'kind'>) => (
    <label>
      {label}
      <input value={value[key]} onChange={(e) => onChange({ ...value, [key]: e.target.value })} />
    </label>
  );

  return (
    <fieldset>
      <legend>Match the columns (type the header names from your CSV file)</legend>
      {input('Date column', 'date')}
      {input('Description column', 'description')}
      <label>
        Amounts are in
        <select value={value.kind} onChange={(e) => onChange({ ...value, kind: e.target.value as Fields['kind'] })}>
          <option value="Signed">One amount column</option>
          <option value="DebitCredit">Separate debit and credit columns</option>
          <option value="Indicator">An amount column and an in-or-out column</option>
        </select>
      </label>
      {value.kind === 'DebitCredit' ? (
        <>
          {input('Debit column', 'debit')}
          {input('Credit column', 'credit')}
        </>
      ) : input('Amount column', 'amount')}
      {value.kind === 'Indicator' && (
        <>
          {input('In-or-out column', 'indicator')}
          {input('Word for money in', 'moneyIn')}
          {input('Word for money out', 'moneyOut')}
        </>
      )}
      {input('Balance column (optional)', 'balance')}
      {input('Date format (optional, such as MM/dd/yyyy)', 'dateFormat')}
    </fieldset>
  );
}

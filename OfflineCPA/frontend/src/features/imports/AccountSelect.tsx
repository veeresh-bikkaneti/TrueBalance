import type { Account } from './api';

interface Props {
  label: string;
  accounts: Account[];
  value: string;
  onChange: (accountId: string) => void;
}

export function AccountSelect({ label, accounts, value, onChange }: Props) {
  return (
    <label>
      {label}
      <select value={value} onChange={(e) => onChange(e.target.value)}>
        <option value="">Pick an account</option>
        {accounts.map((a) => (
          <option key={a.accountId} value={a.accountId}>{`${a.code} ${a.name}`}</option>
        ))}
      </select>
    </label>
  );
}

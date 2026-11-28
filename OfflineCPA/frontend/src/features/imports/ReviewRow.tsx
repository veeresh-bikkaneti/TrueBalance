import { useState } from 'react';
import { AccountSelect } from './AccountSelect';
import { ActionButton } from './ActionButton';
import { postRow, RowStatus, type Account, type RowItem } from './api';

interface Props {
  item: RowItem;
  accounts: Account[];
  onPosted: () => unknown;
}

export function ReviewRow({ item: { row, suggestedCategoryAccountId }, accounts, onPosted }: Props) {
  const categories = accounts.filter((a) => a.accountId !== row.bankAccountId);
  const [picked, setPicked] = useState<string>();
  const [allowDuplicate, setAllowDuplicate] = useState(false);
  const [reviewed, setReviewed] = useState(false);
  // The account list may arrive after the row, so the suggestion is read on every render.
  const category = picked ?? categories.find((a) => a.accountId === suggestedCategoryAccountId)?.accountId ?? '';
  const duplicate = row.status === RowStatus.Duplicate;
  const needsCheck = row.isExperimental || row.balanceCheckFailed;
  const blocked = !category || (duplicate && !allowDuplicate) || (needsCheck && !reviewed);

  const post = async () => {
    await postRow(row.id, { categoryAccountId: category, allowDuplicate, reviewed });
    await onPosted();
  };
  const tick = (label: string, checked: boolean, set: (value: boolean) => void) => (
    <label className="tick">
      <input type="checkbox" checked={checked} onChange={(e) => set(e.target.checked)} />
      {label}
    </label>
  );

  return (
    <li>
      <p>
        {row.date} | {row.description} | {row.amount.toFixed(2)} | balance{' '}
        {row.balance?.toFixed(2) ?? 'not given'}
      </p>
      {duplicate && <p><strong>Looks like a duplicate.</strong></p>}
      {row.status === RowStatus.Posted ? <p>Posted.</p> : (
        <>
          <AccountSelect label="Category" accounts={categories} value={category} onChange={setPicked} />
          {duplicate && tick('Post it anyway, I know it looks like a duplicate', allowDuplicate, setAllowDuplicate)}
          {needsCheck && tick('I checked this row against the statement', reviewed, setReviewed)}
          <ActionButton label="Post" disabled={blocked} onRun={post} />
        </>
      )}
    </li>
  );
}

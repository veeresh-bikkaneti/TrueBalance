import type { AmountLayout, CsvColumns } from './api';

/** What the column form starts with: the header names the importer expects without a saved profile. */
export const startingFields = {
  kind: 'Signed' as Exclude<AmountLayout, 'SignedReversed'>,
  date: 'date',
  description: 'description',
  amount: 'amount',
  debit: 'debit',
  credit: 'credit',
  indicator: '',
  moneyIn: '',
  moneyOut: '',
  balance: 'balance',
  dateFormat: '',
};

export type Fields = typeof startingFields;

/** Only the columns that belong to the chosen layout are sent. Blank means "no such column". */
export function toColumns(f: Fields): CsvColumns {
  const text = (value: string, used = true) => (used && value.trim()) || null;
  const split = f.kind === 'DebitCredit';
  const marked = f.kind === 'Indicator';
  return {
    date: f.date.trim(),
    description: f.description.trim(),
    amount: text(f.amount, !split),
    debit: text(f.debit, split),
    credit: text(f.credit, split),
    balance: text(f.balance),
    dateFormat: text(f.dateFormat),
    amountLayout: f.kind,
    debitsCarryMinus: false,
    indicatorColumn: text(f.indicator, marked),
    moneyInMarker: text(f.moneyIn, marked),
    moneyOutMarker: text(f.moneyOut, marked),
  };
}

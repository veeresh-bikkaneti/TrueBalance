// `newPassphrase` is handed in: a folder inside a feature may not import from the one above.
type Values = Record<string, string>;

export const setForm = <F>(newPassphrase: F[], send: (passphrase: string) => Promise<unknown>) => ({
  title: 'Set your passphrase',
  hint: 'It locks your documents. If you lose it and have no recovery code, nobody can reset it.',
  button: 'Set passphrase',
  fields: newPassphrase,
  send: (values: Values) => send(values.newPassphrase),
});

export const changeForm = <F>(newPassphrase: F[], send: (current: string, next: string) => Promise<unknown>) => ({
  title: 'Change your passphrase',
  button: 'Change passphrase',
  fields: [{ name: 'current', label: 'Current passphrase' }, ...newPassphrase],
  send: (values: Values) => send(values.current, values.newPassphrase),
});

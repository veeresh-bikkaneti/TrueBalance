// `newPassphrase` is handed in: a folder inside a feature may not import from the one above.
type Values = Record<string, string>;

export const makeCodeForm = (send: (passphrase: string) => Promise<unknown>) => ({
  title: 'Make a recovery code',
  hint: 'Your spare key if you forget the passphrase. A new code switches the old one off.',
  button: 'Make a code',
  fields: [{ name: 'passphrase', label: 'Passphrase' }],
  send: (values: Values) => send(values.passphrase),
});

export const recoverForm = <F>(newPassphrase: F[], send: (code: string, next: string) => Promise<unknown>) => ({
  title: 'Forgot your passphrase?',
  hint: 'Use your recovery code to set a new one. Capital letters, dashes and spaces do not matter.',
  button: 'Recover',
  fields: [{ name: 'code', label: 'Recovery code', visible: true }, ...newPassphrase],
  send: (values: Values) => send(values.code, values.newPassphrase),
});

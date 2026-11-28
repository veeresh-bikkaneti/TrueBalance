export const unlockForm = (send: (passphrase: string) => Promise<unknown>) => ({
  title: 'Unlock the books',
  button: 'Unlock',
  fields: [{ name: 'passphrase', label: 'Passphrase' }],
  send: (values: Record<string, string>) => send(values.passphrase),
});

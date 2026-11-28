import { api } from '../../shared/api';

export interface VaultStatus {
  unlocked: boolean;
  passphraseSet: boolean;
}

const post = <T>(action: string, body: object) => api<T>(`/vault/${action}`, { method: 'POST', body });

// Secrets go in the JSON body only, never in the address.
export const getStatus = () => api<VaultStatus>('/vault/status');
export const unlock = (passphrase: string) => post('unlock', { passphrase });
export const changePassphrase = (currentPassphrase: string, newPassphrase: string) =>
  post('change-passphrase', { currentPassphrase, newPassphrase });
export const createRecoveryCode = (passphrase: string) => post<{ recoveryCode: string }>('recovery-code', { passphrase });
export const recover = (recoveryCode: string, newPassphrase: string) => post('recover', { recoveryCode, newPassphrase });

import { useCallback, useEffect, useState } from 'react';
import { changePassphrase, createRecoveryCode, getStatus, recover, unlock, type VaultStatus } from './api';
import { changeForm, setForm } from './passphrase/forms';
import { CodeOnce } from './recovery/CodeOnce';
import { makeCodeForm, recoverForm } from './recovery/forms';
import { SecretForm, type Field } from './SecretForm';
import { unlockForm } from './unlock/forms';
import './vault.css';

// Typed twice. The vault enforces the same 12 on its side.
const newPassphrase: Field[] = [
  { name: 'newPassphrase', label: 'New passphrase (at least 12 characters)', minLength: 12 },
  { name: 'again', label: 'Type the new passphrase again', sameAs: 'newPassphrase' },
];

export function VaultScreen() {
  const [status, setStatus] = useState<VaultStatus>();
  const [unreadable, setUnreadable] = useState(false);
  const [notice, setNotice] = useState('');
  // The one place a recovery code is held, and only until it is written down.
  const [code, setCode] = useState('');

  const refresh = useCallback(async () => {
    try {
      setStatus(await getStatus());
      setUnreadable(false);
    } catch {
      setUnreadable(true);
    }
  }, []);

  useEffect(() => {
    void refresh();
  }, [refresh]);

  const open = async (passphrase: string) => {
    await unlock(passphrase);
    setNotice('');
    await refresh();
  };

  return (
    <>
      <h2>Vault</h2>
      {unreadable && (
        <div role="alert">
          <p>Could not check whether the books are locked.</p>
          <button type="button" onClick={() => void refresh()}>
            Try again
          </button>
        </div>
      )}
      {status && (
        <>
          <p>
            {!status.passphraseSet && 'No passphrase yet. '}
            <strong>{status.unlocked ? 'The books are unlocked.' : 'The books are locked.'}</strong>
          </p>
          {notice && <p role="status">{notice}</p>}

          {!status.passphraseSet && <SecretForm key="set" {...setForm(newPassphrase, open)} />}

          {status.passphraseSet && !status.unlocked && (
            <>
              <SecretForm key="unlock" {...unlockForm(open)} />
              <SecretForm
                key="recover"
                {...recoverForm(newPassphrase, async (recoveryCode, next) => {
                  await recover(recoveryCode, next);
                  setNotice('Your passphrase is reset. That code is used up, so make a new one now.');
                  await refresh();
                })}
              />
            </>
          )}

          {status.unlocked &&
            (code ? (
              <CodeOnce code={code} onWritten={() => setCode('')} />
            ) : (
              <SecretForm
                key="code"
                {...makeCodeForm(async (passphrase) => {
                  setCode((await createRecoveryCode(passphrase)).recoveryCode);
                  setNotice('');
                })}
              />
            ))}

          {status.passphraseSet && (
            <SecretForm
              key="change"
              {...changeForm(newPassphrase, async (current, next) => {
                setNotice('');
                await changePassphrase(current, next);
                setNotice('Passphrase changed.');
              })}
            />
          )}
        </>
      )}
    </>
  );
}

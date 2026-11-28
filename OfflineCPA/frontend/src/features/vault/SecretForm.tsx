import { useId, useState, type FormEvent } from 'react';
import { ApiError } from '../../shared/api';

export interface Field {
  name: string;
  label: string;
  /** Shown as typed. Everything else is dotted out. */
  visible?: boolean;
  minLength?: number;
  /** The name of the field this one has to match. */
  sameAs?: string;
}

type Values = Record<string, string>;

export interface SecretFormProps {
  title: string;
  hint?: string;
  button: string;
  fields: Field[];
  send: (values: Values) => Promise<unknown>;
}

function problems(fields: Field[], values: Values): Values {
  const found: Values = {};
  for (const { name, minLength, sameAs } of fields) {
    const value = values[name] ?? '';
    if (!value) found[name] = 'Fill in this field.';
    else if (minLength && value.length < minLength) found[name] = `Use at least ${minLength} characters.`;
    else if (sameAs && value !== values[sameAs]) found[name] = 'The two passphrases do not match.';
  }
  return found;
}

/**
 * The one form behind every vault action. Secrets live in its state only, and
 * leave it the moment they are sent. No input has a `name`, so even a stray
 * page submit has nothing to put in the address.
 */
export function SecretForm({ title, hint, button, fields, send }: SecretFormProps) {
  const id = useId();
  const [values, setValues] = useState<Values>({});
  const [errors, setErrors] = useState<Values>({});
  const [busy, setBusy] = useState(false);

  async function submit(event: FormEvent) {
    event.preventDefault();
    if (busy) return;
    const found = problems(fields, values);
    setErrors(found);
    if (Object.keys(found).length > 0) return;

    setBusy(true);
    // Cleared now, so the fields are empty whether the call works or not.
    setValues({});
    try {
      await send(values);
    } catch (error) {
      const message = error instanceof ApiError ? error.message : 'Could not reach the app. Try again.';
      // The first field holds the current passphrase or the code, so the answer sits there.
      setErrors({ [fields[0].name]: message });
    } finally {
      setBusy(false);
    }
  }

  return (
    <form className="vault-form" method="post" noValidate onSubmit={submit} aria-labelledby={`${id}-title`}>
      <h3 id={`${id}-title`}>{title}</h3>
      {hint && <p>{hint}</p>}
      {fields.map(({ name, label, visible }) => {
        const error = errors[name];
        const errorId = `${id}-${name}-error`;
        return (
          <div key={name}>
            <label>
              {label}
              <input
                type={visible ? 'text' : 'password'}
                autoComplete="off"
                autoCapitalize="off"
                spellCheck={false}
                readOnly={busy}
                value={values[name] ?? ''}
                onChange={(event) => {
                  const { value } = event.target;
                  setValues((current) => ({ ...current, [name]: value }));
                }}
                aria-invalid={error ? true : undefined}
                aria-describedby={error ? errorId : undefined}
              />
            </label>
            {error && (
              <p className="vault-error" id={errorId} role="alert">
                {error}
              </p>
            )}
          </div>
        );
      })}
      <button type="submit" disabled={busy}>
        {busy ? 'Working...' : button}
      </button>
    </form>
  );
}

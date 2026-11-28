# The recovery code: your spare key

Everyone forgets a passphrase sooner or later. The recovery code is the spare key for that day.

## What it does

Your documents are locked with a random data key. Your passphrase locks that key. A recovery code is a second lock on the same key, so either one can open your books.

- Make a code while the books are unlocked: `POST /api/vault/recovery-code` with the body `{"confirm": true}`.
- The code looks like eight groups of four characters. The app shows it once. It does not save it, so it cannot show it again.
- Forgot the passphrase? Send the code and a new passphrase to `POST /api/vault/recover`. The books unlock and your files stay as they are. Capital letters, dashes and spaces don't matter.
- Making a new code switches the old one off.
- A code stays valid after you use it, and after you change your passphrase. If someone else may have seen it, make a new one.

## Where to keep it

Write it on paper and put it somewhere safe, away from this computer: a locked drawer or a safe works well. Don't save it in the same folder as your books, and don't email it to yourself. Anyone who has the code and your files can open them.

## If you lose both

If you lose the passphrase **and** the recovery code, the books are gone. There is no back door and nobody can reset it for you. So make the code today, while everything still opens.

## One limit

A new passphrase or a new code does not change the data key underneath. Someone who kept an older copy of `vault.json`, plus the passphrase or code that opened that copy, can still get the key.

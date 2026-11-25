# TrueBalance prompt C1: one PR for D2 (plan sync) and 02 (security hardening)

Phase: P0 + Chore | Branch: `chore/sync-and-security-2026-11-25` (new) | Token cap: 90k | Estimate: 62k | Seats: main session on a cheaper model, Seat A builder (02 code), Seat B writer (D2 docs), 1 mid-tier reviewer
Depends on: PR 28 and PR 29 merged (done 2026-11-25, main `18db3df` or later).

You are working in the TrueBalance repo. Veeresh wants D2 and 02 done in **one session and one draft PR**. This file is the only launch prompt. It tells you how to run the two detailed prompts together. Where this file and D2 or 02 disagree, this file wins.

## Inputs
Veeresh works in a web interface and runs no commands himself. You run every command. He attaches `truebalance-prompts-2026-11-25.zip` to this message. Unzip it into a new empty folder outside the repo (for example `/tmp/tb-prompts`). It holds `D2-sync-plan-and-agents.md`, `02-security-hardening.md`, this file, the other prompts and `MANIFEST.sha256`. D2's "Inputs" section means this folder. If you cannot see the attachment, stop and say so in one line.

## Overrides (read these before D2 and 02)
1. **One branch:** `chore/sync-and-security-2026-11-25` from the latest `origin/main`. Ignore the branch names in D2 and 02.
2. **One draft PR** at the very end. Ignore "open a draft PR" inside D2 and 02. Its first line is "Replaces #26".
3. **PR 26:** do not edit, update, comment on or close it, and do not ask for "Update branch" on it. Its conflict is expected. Veeresh closes it.
4. **One budget:** cap 90k, estimate 62k for the whole run. State both before your first edit or subagent call. Warn at 72k. Stop and ask Veeresh before passing 90k.
5. **Copy list:** D2's copy step also copies this file (`C1-one-pr-sync-and-security.md`) into `docs/specs/prompts/`.
6. **Never name a vendor or model.** Say cheaper model, mid-tier model or frontier model.

## Step 0 (paste the output, stop on any failure)
```
mkdir -p /tmp/tb-prompts && unzip -o <attached zip> -d /tmp/tb-prompts && cd /tmp/tb-prompts && sha256sum -c MANIFEST.sha256
cd <repo root> && git fetch origin && git status -sb && git log --oneline -3 origin/main
git log origin/main --oneline | grep -i ledger           # PR 28 must be on main
dotnet build OfflineCPA/OfflineCPA.sln -c Release
```
Stop if a checksum fails, the tree is dirty, PR 28 is not on main, or main does not build. Then read `AGENTS.md`, ADR 0010, ADR 0011, then D2 and 02 in full.

## Run order (one commit per stage, Conventional Commits)
1. **Rebuild PR 26 (from 02, Step 0):** `git switch -c chore/sync-and-security-2026-11-25 origin/main`, `git cherry-pick e4fb349`, resolve as 02 says (keep main's seeder file; in `Program.cs` keep PR 28's ledger DI and `/api/ledger/` routes and add PR 26's connection-string throw, upload options and rewritten `/api/upload`). `git diff --name-only --diff-filter=U` must be empty. Build must pass. Commit: keep the cherry-picked message.
2. **02 scope items 1 to 4** (Seat A): the CS0120 fix, the GitGuardian check, the missing tests, and the "Left for 04b" note for the PR body. Commit `fix(security): ...`.
3. **D2 change 1** (Seat B): copy the prompts verbatim. Commit `docs(specs): sync prompts 2026-11-25`.
4. **D2 changes 2, 3, 4, 5, 7, 8** (Seat B): `AGENTS.md`, `.agent/rules/TRUEBALANCE.md`, `.agent/agents/orchestrator.md`, `.agent/agents/product-owner.md`, `.agent/skills/llm-wiki/SKILL.md`, `.gitignore`. Commit `chore(agent): ...`.
5. **D2 change 6** (Seat B): `wiki_lint.py` PII check and both fixtures. Commit `chore(agent): ...`.
6. **D2 change 9** (Seat B): `docs/backlog/product-backlog.md`, with a row for C1. Commit `docs(backlog): ...`.
7. **Review** (mid-tier reviewer): read the whole diff against D2 and 02 and this file. Re-vote only on disputed items.
8. **Push and open one draft PR.**

Seats A and B touch different files, so they can work at the same time after stage 1. Neither may edit `Program.cs` except Seat A.

## Gates (paste every output)
- All gates in 02 (build, `dotnet test` counts, `docker compose config` with a sample `.env`, GitGuardian result or the history-only explanation).
- All gates in D2 (prompt diffs empty, `wiki_lint.py` pass on the tree and fail on each fixture, `git check-ignore raw/private/x.md`, the grep checks).
- `grep -n "/api/ledger" OfflineCPA/src/Backend/Program.cs` still shows the ledger routes.
- `git log --format=%B origin/main..HEAD | grep -i -E "co-authored-by|generated with"` prints nothing.

## Git rules
Conventional Commits. No `Co-Authored-By` or AI trailer. No "Generated with" line in the PR body. No rebase, no force push, no `git merge`; cherry-pick is allowed. Draft PR only. Never merge, approve or enable auto-merge; Veeresh merges.

PR body: "Replaces #26" first, then "Before:" and "After:" paragraphs, one sentence on what the change does, a short "How" with two parts (Security, Plan sync), "Left for 04b", and the gate outputs.

## Report back (pstack:teach + i-have-adhd)
Line 1: the draft PR link, or what blocked you. Then 3 to 5 bullets: conflict resolution in `Program.cs`, test counts, AGENTS.md changes (role table, tiers, budget footer), gate results, full head SHA. End with the budget footer:
`Budget: uncached <n> | cached <n> | output <n> | turns <n> | $<n> of $<cap> (<p>%)`
Write "Data not available." for any number you cannot measure.

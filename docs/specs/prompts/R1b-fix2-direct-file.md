# TrueBalance prompt R1b-fix2: correct Direct File and Free File on PR 27

Phase: Research | Branch: `docs/r1-competitor-research` (PR 27, draft) | Token cap: 15k | Estimate: 8k | Seats: main session on a cheaper model + 1 mid-tier reviewer
Depends on: commit `988402fde89761f05215b60ee4bf3320af2fdddb` on PR 27. PR 27 merges after this lands.

Docs only. Touch only `docs/research/r1-competitor-research.md` and `docs/research/r1-evidence.md`. Write no app code. Do not open a new PR.

## Step 0
Run `git fetch origin && git status -sb && git log --oneline -3 origin/docs/r1-competitor-research` and paste it. Stop if the head is not `988402f` or a later commit on top of it, or the tree is dirty.

## What the architect review found on 988402f (2026-11-25). Re-check each at its source before you edit.

1. **Direct File is still wrong.** Section 9.3, the sweet-spot summary in 9.6 and the evidence row say it is a "Permanent option", "25+ states in 2025, expanding", citing `p6036.pdf`. That PDF is the Direct File Outreach Guide, Rev. 2-2025, written for the 2025 filing season. Later sources say the program ended: Wikipedia's IRS Direct File article says it was suspended in November 2025 and discontinued in 2026, and Kiplinger (15 Jan 2026) says it was not available for the 2026 filing season. Search irs.gov for a current statement. Then:
   - Set the row's Status to "Discontinued (not offered for the 2026 filing season)" and tag it by the owner's rules: irs.gov statement found = cite it; none found = name the secondary sources, tag `[Moderate 2/2]` or lower, and add a line "irs.gov statement: Data not available."
   - Keep p6036 only as history ("made permanent in 2025, later discontinued"), never as current status.
   - Rewrite the Direct File sentence in the sweet-spot summary to match.
2. **Free File limit is stale.** The current irs.gov page (canonical https://www.irs.gov/e-file-do-your-taxes-for-free, updated 21 Oct 2026) says "$95,000 adjusted gross income (AGI) or less", "IRS Free File has closed. Please check back January 2027." and "you can only file your current year tax return". Show both: $89,000 for the 2026 season (2025 returns) only if a primary source says so, and $95,000 as the page's current figure for the season opening January 2027. Use the canonical URL. Fix the sweet-spot summary to match.
3. **Housekeeping:** the heading `### 9.3 IRS Free Filing Options (2026 Filing Season)` appears twice; keep one. Any `[Moderate 2/2]` tag must name both sources (inline or in an evidence row); if only one exists, make it `[Moderate 1/1]`.

## Source and evidence rules
Apply the owner's rules exactly as written in `R1b-fix-irs-part2.md` (Grounding, Zero Fabrication, Confidence levels, Source Attribution, Statistical Rigor). Tag analysis `[Inferred]`, direct checks `[Tested]`.

## Gates (paste output)
- `grep -n -i 'direct file' docs/research/*.md` shows no "Permanent option" as current status and no "expanding".
- `grep -c '### 9.3' docs/research/r1-competitor-research.md` is 1.
- `grep -c 'google.com/search\|High 2/2\|Level n/m' docs/research/*.md` is 0.
- The reviewer accepts or rejects fixes 1 to 3 in a table.

## Budget, git and report
- Cap 15k, estimate 8k; say both first; stop and ask before passing the cap; report measured tokens or "Data not available."
- One commit: `docs: correct Direct File status and Free File limit`. No `Co-Authored-By` or AI trailer. No "Generated with" line. No rebase, force push or `git merge`. Push to the PR 27 branch only; Veeresh merges.
- Report (pstack:teach + i-have-adhd): answer first, then the new Direct File and Free File rows, gate output, reviewer table, full commit SHA, tokens vs cap.

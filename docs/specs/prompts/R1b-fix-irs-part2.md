# TrueBalance prompt R1b-fix: correct Part 2 (IRS) and the tags on PR 27

Phase: Research | Branch: `docs/r1-competitor-research` (exists, PR 27, draft) | Token cap: 60k | Estimate: 35k | Helper seats: 2 researchers on cheaper models (Seat F: IRS forms and retention; Seat G: free filing options and 1099) + 1 mid-tier reviewer who accepts or rejects each fix
Depends on: R1b commit `54fe5e5` on PR 27. PR 27 should not merge until this lands.

You are working in the TrueBalance repo. Docs only, on the existing PR 27 branch. Do not open a new PR. Write no app code. Touch only `docs/research/r1-competitor-research.md` and `docs/research/r1-evidence.md`.

## Step 0: repo check

Run `git fetch origin && git status -sb && git log --oneline -3 origin/docs/r1-competitor-research` and paste it. Stop if the tree is dirty, you are not on the PR 27 branch, or the head is not `54fe5e5` or a later commit on top of it.

## What the architect review found on 54fe5e5 (2026-11-25)

Part 1 (the merge, statecharts, 24 ideas, debate log) is usable. Part 2 has wrong facts with High tags on them, and the tags break the owner's rules. Each item below is a claim from the review: re-check it at the primary source before you change anything, and say if the source disagrees with the review.

1. **Free File (section 9.3):** the report says "$79,000 AGI" and "All taxpayers". The irs.gov Free File page (https://www.irs.gov/filing/irs-free-file-do-your-taxes-for-free, last updated 21 Oct 2026 when the review read it) says "$95,000 adjusted gross income (AGI) or less" and "IRS Free File has closed. Please check back January 2027." Re-read it, state the tax year the limit applies to if irs.gov gives one (otherwise write "tax year not stated on the page"), and fix "Who qualifies".
2. **Direct File (9.3 and 9.4):** the report says "Pilot expanded", "No limit", "All 50 + DC" with `[High 3/3]`. The cited page https://www.irs.gov/newsroom/irs-direct-file-pilot-expansion did not load for the review. Secondary reports (for example Kiplinger, 15 Jan 2026) say Treasury and the IRS ended Direct File and it was not available for the 2026 filing season. Search irs.gov for its current status. If irs.gov confirms the end, mark the row Discontinued with that link. If irs.gov says nothing, mark it Discontinued per secondary sources with a Low or Moderate tag per the rules, and name the sources. Remove Direct File from the hand-off table in 9.4.
3. **Schedule C lines (9.2):** the table puts gross receipts on line 8 and returns on line 9. On the form, lines 8 to 27 are Part II expenses. Re-read https://www.irs.gov/pub/irs-pdf/f1040sc.pdf (and its instructions) and rebuild the table with the form's own line numbers and names for Part I income, Part II expenses, total expenses and net profit. Say which form year you read.
4. **Form 1120-S lines (9.2):** "Lines 2-19 deductions" does not match the form as the review knows it (COGS and deductions sit on different lines). Re-read https://www.irs.gov/pub/irs-pdf/f1120s.pdf and rebuild the table the same way.
5. **Form 1040 schedules (9.2):** the table says Schedule C is capital gains and Schedule D is capital gains distributions. Schedule C is business profit or loss. Re-read the 1040 instructions and fix every row.
6. **Retention (9.1):** "3-7 years ... 7 years if loss claim" is too loose. Re-read https://www.irs.gov/businesses/small-businesses-self-employed/how-long-should-i-keep-records and list each period with its exact trigger in the page's own words (for example the employment tax period and the bad-debt or worthless-securities period).
7. **VITA and Military OneSource (9.3):** "Typically <$64,000" and "Yes (all forms)" are unverified. Check the current VITA income limit and what MilTax supports (Schedule C? 1120-S?) on irs.gov and militaryonesource.mil. Use "Data not available." where the page does not say.
8. **Tags:** `[High 2/2]` appears 14 times. Under the owner's rules 2 agreeing sources is Moderate, never High. Many `[High 3/3]` tags sit on one irs.gov link. Recount every tag in Part 2: list the agreeing sources for each in the evidence pack, and set the level by the rules below. A claim one irs.gov page supports is `[Moderate 1/1]` (one primary). Spot-check ten `[High 3/3]` tags in Part 1 the same way and fix any that fail.
9. **Wrong sources in the Must/Should/Nice table (9.6):** the IRS recordkeeping page does not say "required by all CPAs", and security.intuit.com does not support an encrypted ZIP packet. Where a row is our own reasoning, tag it `[Inferred]` and drop the link. Keep a link only where the page says what the row claims.
10. **Sweet spot:** after fixes 1 and 2, rewrite the two or three sentences on where the free IRS options leave TrueBalance users: households under the Free File limit, Schedule C filers, and S-corps (no free IRS option for 1120-S per the report; verify). Tag it `[Inferred]`.
11. **Housekeeping:** the evidence pack legend still says "[Level n/m]"; replace it with the real format. Add an evidence row (tool, topic, finding, URL, source type, date read) for every Part 2 source you use. Fix the heading numbers (section 10 holds subsections 9.1 to 9.6). Leave the word "simulated" only where the debate log quotes Agent A.

## Source and evidence rules (owner's standard, apply to every claim you touch)


### 1. Grounding & Verification
- **Search First:** Actively use web search to ground responses in current information. Prioritize official documentation, primary datasets, source code, or user-provided files before secondary sources.
- **Deduplicate Sources:** Multiple articles or copies republishing the same underlying origin count as a single source.
- **Fact-Check Everything:** Independently verify every claim, number, version number, setting name, and reference.

### 2. Zero Fabrication
- Base all analysis strictly on verified, provided, or cited real-world data.
- Never invent, estimate, or extrapolate missing figures.
- If required data cannot be verified or found, explicitly state: **"Data not available."** Drop unverified claims rather than guessing.

### 3. Confidence Scoring & Phrasing
Evaluate each verified claim using the following scale and match your phrasing to the confidence level:
- **High (3+ agreeing primary sources):** Use definitive phrasing (e.g., *"confirms," "establishes"*).
- **Moderate (2 agreeing sources, or 1 primary source):** Use measured phrasing (e.g., *"indicates," "suggests"*).
- **Low (1 secondary source such as a blog/forum, or conflicting sources):** Explicitly flag the uncertainty (e.g., *"sources conflict," "preliminary reports suggest"*).
- **None (0 verifiable sources):** Omit the claim entirely.

### 4. Source Attribution & Formatting
- **Inline Citations:** End every sourced claim or data point with a direct source link and a confidence tag showing agreeing sources over total sources checked—for example: `[High 3/3]` or `[Low 1/2]`.
- **Label Original Work:** Clearly separate verified facts from your own work by explicitly labeling analytical interpretations as **[Inferred]** and direct executions/checks as **[Tested]**.

### 5. Statistical Rigor
When presenting quantitative data or analysis:
- Show all calculations step-by-step.
- Name the exact statistical method used (e.g., mean, median, standard deviation, linear regression).
- State the sample size ($n$) and exact timeframe for every metric.
- Cite the specific dataset or source for every input number.

How these rules apply here:
- The tag format is `[High 3/3]` (agreeing sources / sources checked), replacing the older `[Confidence: High | Vote: 3/3]` format. Convert every existing tag in both reports.
- Council votes on ideas stay separate from source tags: write votes as "Council: Adopt 3/3".
- Link checks are [Tested]. Rankings, rulings, module placement and "changes to our plan" are [Inferred].
- Effort sizes (S/M/L) and token figures are estimates: label them [Inferred], or drop them.


## How (pstack: sharpen, swarm, tdd, unslop)

- Orchestrator (main session) splits the 11 fixes: Seat F takes 3, 4, 5, 6; Seat G takes 1, 2, 7, 9's links; the orchestrator does 8, 10, 11 from the seats' evidence rows.
- Seats return evidence rows only (direct URL, quoted line, date read). No seat writes the report.
- Gates before commit, paste the output of each:
  - `grep -c 'google.com/search' docs/research/*.md` is 0.
  - `grep -c '\[High 2/2\]' docs/research/*.md` is 0.
  - `grep -n 'Direct File' docs/research/r1-competitor-research.md` shows no "Pilot" or "expanded".
  - `grep -c 'Level n/m' docs/research/*.md` is 0.
  - Every Part 2 link: `curl -sIL -o /dev/null -w '%{http_code} %{url_effective}\n'` result listed; any non-200 is fixed or its claim downgraded.
- The mid-tier reviewer reads the diff against this list and accepts or rejects each of the 11 fixes in a table.

## Budget, git and report

- Cap 60k, estimate 35k. Say both first, with a per-seat budget (about 12k per seat, 8k for the reviewer). Stop and ask before passing the cap. Report measured tokens (main plus seats) and cost; if you cannot measure, say "Data not available." rather than an estimate.
- One commit: `docs: fix R1 Part 2 IRS facts and confidence tags`. No `Co-Authored-By` or AI trailer. No "Generated with" line. No rebase, force push or `git merge`. Push to the PR 27 branch only; Veeresh merges.
- Do not say prompt 03 can start. Its gate is 02 merged (PR 26) and PR 27 merged.
- Report back (pstack:teach + i-have-adhd): answer first in one line, then 3 to 5 bullets: the reviewer's accept/reject table counts, any review claim the sources contradicted, the corrected Free File and Direct File rows, tag counts by level, gate output, tokens vs cap.

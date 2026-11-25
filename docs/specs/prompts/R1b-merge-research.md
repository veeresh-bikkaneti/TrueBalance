# TrueBalance prompt R1b: Merge both R1 research results and add IRS recordkeeping and filing readiness (PR 27)

Phase: Research | Branch: `docs/r1-competitor-research` (exists, PR 27, draft) | Token cap: 220k | Estimate: 155k | Max helper seats: master orchestrator (main session) + 1 merge builder + 3 IRS researchers + 3 council seats that debate the merge and review Part 2 (cheaper models) + 1 mid-tier reviewer
Depends on: nothing. Prompt 03 stays blocked until this PR merges. Finish Part 1 first and push it, so Veeresh can review the ledger findings while Part 2 runs.

You are working in the TrueBalance repo. Docs only, on the existing PR 27 branch. Do not open a new PR. Write no app code.

## Inputs

- A: PR 27 as it stands (`docs/research/r1-competitor-research.md`, `docs/research/r1-evidence.md`).
- B: a second agent's report, which Veeresh gives you as `r1-agent-b.md`. The architect already made two mechanical fixes to it (see the note at its top): every Google search redirect is replaced by the direct URL it wrapped, and its tags are converted to the `[Level n/m]` format. Nothing in it was re-checked. Its original is `r1-agent-b.orig.md`; you do not need it unless a link looks wrong.

## Step 0: repo check

Run `git fetch origin && git status -sb && git log --oneline -3 origin/docs/r1-competitor-research` and paste it. Stop if the tree is dirty or you are not on the PR 27 branch.

## What the architect review found (2026-11-25)

- A has direct links and an evidence pack (about 67 unique sources), but no confidence or vote tags anywhere, its council says research seats were "simulated", and it leans on some secondary blogs and vendor sites for claims about other products.
- B is the stronger analysis: 24 ranked ideas with 3-seat votes, statecharts, an arena, module placement and per-prompt deltas. But all 195 of its links were wrapped in google.com search redirects (now unwrapped), it had only 23 confidence tags, and the evidence pack it cites does not exist.
- They agree on the core: signed postings, immutable posted entries with reversals, staged import with dedupe, $0.00 reconciliation gate, versioned tax maps, no new ADR needed. One difference: A wants GnuCash-style base-36 account codes; B wants account numbers optional and off by default. Arena it (one reason, one pick).
- Links could not be opened from the review environment, so link health is not yet checked.

## Source and evidence rules (owner's standard, apply to every claim in both parts)

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

## Part 1: merge the two reports (pstack: sharpen, swarm, arena, how, unslop)

1. Make B's structure the main report (`r1-competitor-research.md`): answer first, matrix, 24-item ranked list, statecharts, module placement, arena, per-prompt changes, licenses.
2. B's links are already unwrapped. Confirm no redirect remains (`grep -c "google.com/search"` prints 0) and that each link points at the page the claim is about, not a site's home page.
3. Merge both evidence sets into `r1-evidence.md`: one row per claim with tool, topic, finding, direct URL, source type, date read.
4. Open each cited URL. Drop dead links; replace with a primary source or drop the claim.
5. Re-check and tag every factual claim per the source and evidence rules above, format `[High 3/3]`. B's existing tags are its own and unverified: recount the sources yourself; never copy a tag forward. Claims about a product backed only by a blog or a different vendor are Low at best. Missing data reads "Data not available."
6. Remove the "simulated" wording: say plainly how many real seats ran.
7. Resolve the account-code difference with a two-option arena.

### Part 1 merge debate (council + master orchestrator)

- The main session is the master orchestrator. It chairs the merge; it does not write the report alone.
- Before merging, the orchestrator lists every point where A and B differ or where only one of them makes a claim: a different recommendation, a different rank, a tool covered by only one report, or a claim with conflicting sources. Number them D1, D2, and so on.
- Council of 3 seats on cheaper models, clean contexts, each given only the two reports, the evidence pack and the debate list: CPA/bookkeeper seat (does it help a CPA file?), architecture seat (does it fit ADR 0003 and 0011?), privacy seat (does it fit ADR 0005 and 0010?).
- Round 1: each seat states a position on every debate point with one reason and its source. Round 2: only on points without a 3/3 or 2/1 majority, each seat answers the other seats' strongest argument once. Then the orchestrator rules with one reason, and the seat vote is recorded beside the ruling.
- A ruling can never upgrade a claim's confidence beyond what its sources support. A point the sources cannot settle is marked "open, needs owner" instead.
- Add a "Merge debate log" section to the report: point, A says, B says, seat votes, ruling, reason. Keep each row to one line.
- The same council later reviews Part 2 (IRS), so keep the seats' context small: pass them the debate list and evidence rows, not the full web pages.

Acceptance:
- `grep -c "google.com/search" docs/research/*.md` prints 0 for each file.
- Every row of the ranked list and every matrix cell with a fact carries a direct link and a `[Level n/m]` tag; every analysis line is labeled [Inferred] or [Tested].
- No `[Confidence:` tags remain.
- Paste link-check counts: checked, OK, replaced, dropped.


## Part 2: extend with IRS recordkeeping rules and filing readiness

### Part 2 goal

Find the sweet spot: the smallest set of records and reports TrueBalance must produce so that a CPA, or a person using a free or low-cost filing tool, can complete the return without retyping or guessing. Compare three levels: (1) what the IRS requires a taxpayer to keep, (2) what free IRS-provided or IRS-partnered filing options accept as input and who can use them, (3) what premium tools (QuickBooks Online and Desktop, plus one premium tax product the researchers judge common, named with a reason) hand to a preparer.

TrueBalance prepares records for filing. It does not file returns and does not give tax advice. Say so in the report.

### Part 2 questions (one section each)

1. IRS recordkeeping: what records a small business, a sole proprietor, an S-corp and a household must keep, for how long, and in what form. Primary sources: IRS publications and pages (for example the small-business recordkeeping guidance and "How long should I keep records").
2. Inputs per return: for Schedule C, Form 1120-S (with K-1s) and a household (Form 1040 with common schedules), list the numbers and supporting details the preparer needs, grouped by form line, from the current-year instructions.
3. IRS free filing options: which free options exist for the current and next filing season, who qualifies (income limits, return types, states), and whether each accepts business income (Schedule C) or business returns (1120-S). Check each program's current status on irs.gov; programs have changed between seasons, so do not rely on news articles or memory. If a program was discontinued or changed, say so with the date and source.
4. Data hand-off: which file formats or exports each option and premium tool accepts or produces (for example TXF, CSV, PDF reports, accountant copies), and which of these a desktop app can produce offline.
5. 1099 and information returns: what a small business must issue (1099-NEC, 1099-MISC), what it needs to collect (W-9 data), and which IRS system handles filing them; what TrueBalance should only prepare versus never do.
6. Sweet spot: a table of packet items, marked Must (filing cannot happen without it), Should (a CPA asks for it every time) or Nice (premium-only convenience), each with the source behind the mark.

### Part 2 how to run it

- Researchers on cheaper models, parallel, clean contexts: seat A IRS rules and forms (questions 1, 2, 5); seat B free filing options (question 3); seat C premium tools and hand-off formats (question 4). Their rows go into the same merged `r1-evidence.md`, marked topic "IRS".
- Council on cheaper models, reading only the evidence pack: CPA seat (can a CPA file from this?), privacy seat (ADR 0005 and 0010: packet masked by default, full TINs only on explicit opt-in), product seat (is this the smallest packet that works for an underwater small business?). Re-vote only on disagreements. The mid-tier reviewer accepts or rejects the whole PR.
- Add a year-end statechart (close books -> build packet -> hand to CPA or filing tool), an arena for the 2 riskiest IRS choices, and for each Must item the acceptance test prompt 07 or 08 should add.
- For IRS facts, irs.gov is the only primary source. Record the tax year each fact applies to. No accounts, sign-ups or e-file attempts anywhere. Frame everything as recordkeeping for a preparer; add "not tax advice".
- Put Part 2 in the main report as its own section, "IRS recordkeeping and filing readiness", with the Must/Should/Nice table, and add its rows to "Changes to our plan" (07, 08, 10, 12).

## Budget, git and report

- Cap 220k, estimate 155k. Say both first, with a per-seat budget (about 25k for the merge builder, 30k per IRS researcher, 17k per council seat across the debate and the Part 2 review). Stop and ask before passing the cap. Report tokens (main plus seats) and cost.
- Conventional Commits (`docs: ...`). No `Co-Authored-By` or AI trailer. No "Generated with" line. No rebase, force push or `git merge`. Draft PR only; Veeresh merges.
- Report back (pstack:teach + i-have-adhd): answer first in one line, then 3 to 5 bullets (top 3 ideas for 03, the debate points that were close or left open, the IRS Must items, link-check counts and tag counts by level, tokens vs cap and cost, one decision if any).

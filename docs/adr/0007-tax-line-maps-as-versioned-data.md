# ADR 0007: Tax line maps are versioned data, not enums

- Status: Accepted
- Date: 2026-11-21

## Context
`TaxLineItem` is a C# enum tied to Form 1120-S only, and it mislabels Travel and Meals as lines 20/21. Current 1120-S instructions list line 19 as Energy Efficient Commercial Buildings Deduction and line 20 as Other Deductions ([IRS i1120s](https://irs.gov/instructions/i1120s)) [Confidence: Moderate | Vote: 1/1 primary]. Schedule C Part II runs from line 8 (Advertising) to 27b (Other expenses) ([Schedule C](https://www.irs.gov/pub/irs-pdf/f1040sc.pdf)) [Confidence: Moderate | Vote: 1/1 primary]. Households and sole proprietors have no fit today.

## Decision
- Store one map file per form and tax year (household, Schedule C, 1120-S), mapping accounts to form lines.
- Thresholds that change by law (for example the 1099-NEC/MISC threshold) live in data. Two sources report it rising from $600 to $2,000 for payments in 2026 ([CPA Practice Advisor](https://www.cpapracticeadvisor.com/2026/09/01/what-the-new-1099-reporting-rules-mean/189451/), [Barnes Dennig](https://www.barnesdennig.com/obbba-brings-relief-form-1099-reporting/)) [Confidence: Moderate | Vote: 2/2 secondary].
- A new tax year is a data change plus tests, never a code change.

## Consequences
- The app keeps records aligned for a CPA; it does not give tax advice or file returns.
- Spec: `docs/specs/prompts/07-tax-maps.md`.

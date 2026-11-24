---
name: llm-wiki
description: Use when ingesting sources into raw/, querying or synthesizing knowledge from wiki/, linting wiki citations and wikilinks, or maintaining the local Markdown knowledge base.
---

# Local LLM Wiki (Karpathy Pattern + Strict Source Citations)

Maintain a compounding, local Markdown knowledge base (`raw/` + `wiki/`) without losing exact figures, quotes, or code references.

## 1. Directory Contract
- `raw/` — Immutable source files (docs, papers, transcripts, specs). **NEVER modify, rename, or delete files inside `raw/`.**
- `wiki/index.md` — Content catalog organized by category with `[[wikilinks]]` and 1-line summaries.
- `wiki/log.md` — Append-only chronological timeline (`## [YYYY-MM-DD] <ingest|query|lint> | <Title>`).
- `wiki/overview.md` — High-level synthesis of the workspace knowledge.
- `wiki/sources/` — Per-source summaries with full inline citations.
- `wiki/entities/` — Pages for concrete people, systems, modules, or tools.
- `wiki/concepts/` — Pages for domain concepts, invariants, and patterns.
- `wiki/analyses/` — Valuable query syntheses and comparisons filed back into the wiki.

## 2. Strict Citation Rule (Mandatory)
Every factual claim, number, quote, or code reference written in `wiki/` MUST include an inline source pointer to the exact raw file and location:
- Markdown/text/code: `(source: raw/filename.md#section-or-line)`
- PDFs/paginated docs: `(source: raw/doc.pdf, p. 4)`
Never summarize away exact figures without linking back to where they live.

## 3. Workflows (Delegate Drafting/Extraction to Cheaper Models)
### Ingest
1. Read the new file in `raw/`.
2. Write a summary in `wiki/sources/<slug>.md` with full inline `(source: raw/...)` citations.
3. Create or update relevant pages in `wiki/entities/` and `wiki/concepts/` using `[[wikilinks]]` and inline source citations (extend existing pages rather than creating near-duplicates).
4. Update `wiki/index.md` (and `wiki/overview.md` if top-level understanding changed).
5. Append `## [YYYY-MM-DD] ingest | <Title>` to `wiki/log.md`.

### Query
1. Read `wiki/index.md` first to locate relevant pages.
2. Read the matched `wiki/` pages.
3. Follow `(source: raw/...)` citations back into `raw/` whenever exact numbers, quotes, code, or fine-grained details are needed.
4. Cite BOTH the wiki page (`[[page]]`) and the raw file (`raw/...`) in the final answer.
5. If the answer produces a reusable comparison or synthesis, file it in `wiki/analyses/` and log it in `wiki/index.md` and `wiki/log.md`.

### Lint
Check and report:
1. Uncited factual claims, numbers, quotes, or code refs in `wiki/`.
2. Broken `(source: raw/...)` paths (missing files or moved anchors).
3. Broken `[[wikilinks]]` (links with no target page).
4. Orphan pages (no inbound links from `wiki/index.md` or other pages).
5. Contradictions or superseded claims across pages.
Append `## [YYYY-MM-DD] lint | <Summary>` to `wiki/log.md`.

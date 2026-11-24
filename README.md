# TrueBalance

TrueBalance is a free, open-source bookkeeping app for small businesses and households, with a privacy-first design: balanced double-entry books, PII masking and encryption, offline by default, and a yearly packet a CPA can use to prepare tax returns. TrueBalance is not legal or tax advice.

**Current state:** the plan is written and agreed; the code is an early prototype. The product goals above are not working features yet. Read the plan before the code:

- [AGENTS.md](AGENTS.md) — working rules, start here
- [docs/README.md](docs/README.md) — index of the whole plan
- [docs/specs/prompts/INDEX.md](docs/specs/prompts/INDEX.md) — the 17 build prompts in order
- [docs/adr/](docs/adr/) — accepted architecture decisions (0001–0010)
- [docs/backlog/product-backlog.md](docs/backlog/product-backlog.md) — ordered backlog with acceptance criteria
- [OfflineCPA/walkthrough.md](OfflineCPA/walkthrough.md) — what the prototype actually does today (and does not)

## Project structure
- `docs/` — research, roadmap, specs, ADRs, backlog, mockups.
- `OfflineCPA/` — the prototype: .NET 8 backend, React frontend, docker-compose.

## Running the prototype

The prototype is a .NET 8 API plus a Vite/React frontend. It builds and a fresh database is migrated automatically at startup, but it is **not** a working ledger yet. See [OfflineCPA/walkthrough.md](OfflineCPA/walkthrough.md) for exact commands and known gaps.

## Contributing

One spec, one draft PR, built in the order in `docs/specs/prompts/INDEX.md`. See [AGENTS.md](AGENTS.md) for the working rules.

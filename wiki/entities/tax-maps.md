---
checked:
  OfflineCPA/src/Infrastructure/TaxMaps/JsonTaxMapCatalog.cs: 9dd32867b5d590e799ce9800ef487081be8ff8ec
  OfflineCPA/src/Infrastructure/TaxMaps/TaxMapValidator.cs: 7450f184bae0286fba30d0860b155721c8875f4b
  OfflineCPA/src/Application/Charts/GetActiveTaxMap.cs: 30a4827bbe9cbbd4ca07f15b84e9ced489c34109
  OfflineCPA/src/Domain/Entities/MileageLogEntry.cs: 0f48e65e1dbf929e5659bf503d07c0880f698d3b
  tax-maps/1120s-2026.json: cc1a37a7f8c2b194550780ee8ecf9e60421c1ae5
  tax-maps/schedule-c-2026.json: 9f4269db303deaaf3b7bbc9c2515577730b3fbb1
  tax-maps/years/2026.json: d40653e44de6195f6189ef1d0ea496ab26c34e7b
  .github/workflows/ci.yml: 4bd09a68d11bbb723d9ea073ae422b5e2c427300
  OfflineCPA/docker-compose.yml: bf787f349e19e2ea266c10a9dc5f17cf1f114962
  OfflineCPA/src/Backend/Dockerfile: 4291a2bdceb5a78cc6b42bf0379ff01ab31da483
---
# Tax maps

A tax map says which form line each seeded account belongs to, for one chart and one tax year. It is recordkeeping help for a CPA, not tax advice.

## Files

Each `tax-maps/*.json` file holds a form, chart, tax year, sources, lines with a confidence tag, and accounts keyed by four-digit code (repo: tax-maps/1120s-2026.json#L1-L8). The 1120-S numbers were checked against the 2025 form (repo: tax-maps/1120s-2026.json#L1-L8); the Schedule C numbers against the 2026 form (repo: tax-maps/schedule-c-2026.json#L1-L8). Yearly figures sit in `tax-maps/years/`: the 2026 1099 threshold is 2000 (repo: tax-maps/years/2026.json#L1-L7).

## Code

`JsonTaxMapCatalog` finds a map by chart and year and reads the 1099 threshold (repo: OfflineCPA/src/Infrastructure/TaxMaps/JsonTaxMapCatalog.cs#L11-L30). `GetActiveTaxMap` picks the map named by the book settings (repo: OfflineCPA/src/Application/Charts/GetActiveTaxMap.cs#L6-L11). `TaxMapValidator` returns one message per problem in a file (repo: OfflineCPA/src/Infrastructure/TaxMaps/TaxMapValidator.cs#L17-L67). `MileageLogEntry` requires a purpose, and miles that fit between the odometer readings (repo: OfflineCPA/src/Domain/Entities/MileageLogEntry.cs#L17-L29). No route or screen uses the mileage log yet.

## Checks and packaging

CI runs the schema check as its own step (repo: .github/workflows/ci.yml#L17-L18). The container build reads the repo-root maps through a second build context (repo: OfflineCPA/docker-compose.yml#L24-L25) (repo: OfflineCPA/src/Backend/Dockerfile#L9).

## Change it safely

For a new year, add a map file per chart with the new `taxYear` and a file in `years/`. No code changes. Keep each account code in the seed and the map pointing at the same line. See [[charts-of-accounts]].

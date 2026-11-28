---
checked:
  OfflineCPA/src/Infrastructure/Seed/household.json: 603fc448b8da598e21a8e99942e64fe4013f49e1
  OfflineCPA/src/Infrastructure/Services/JsonChartOfAccountsSeeder.cs: dfc92b704f4cd8693ce88715c6d40fdf2884d201
  OfflineCPA/src/Infrastructure/Data/OfflineCPADbContext.cs: 8382eb3de930dd971227f05a0cabefa92b192d8a
  OfflineCPA/src/Backend/Program.cs: 742bc67ef2e99c040cdbdaba2b50a1fcda30bf82
  OfflineCPA/tests/TrueBalance.Tests/ChartSeedTests.cs: b7688286f2722f0bcad02c6a22c5e4ad1fc918af
  OfflineCPA/src/Domain/Entities/Account.cs: 5b21ac6013a9220715e09ff0d27c2f1d48372295
  OfflineCPA/src/Backend/Endpoints/BookEndpoints.cs: a6197c0c20fbe76149e530d6bb394b98e8c8c09e
---
# Charts of accounts

Three seed files ship with the app: `household.json`, `sole-proprietor.json`, and `s-corporation.json`. Each file has `kind`, `displayName`, and an `accounts` array of code, name, type, tax line ID, an optional meals flag, and an optional description (repo: OfflineCPA/src/Infrastructure/Seed/household.json#L1-L5) (repo: OfflineCPA/src/Infrastructure/Services/JsonChartOfAccountsSeeder.cs#L13).

## Seeder and route

`JsonChartOfAccountsSeeder` loads `Seed/<kind>.json` from the app base directory with a case-insensitive JSON reader (repo: OfflineCPA/src/Infrastructure/Services/JsonChartOfAccountsSeeder.cs#L30-L42). `SeedAsync` takes the tax year from the book settings, or the newest map for a new set of books. It skips codes that already exist, refuses a line ID that the map does not have, and writes the book settings once (repo: OfflineCPA/src/Infrastructure/Services/JsonChartOfAccountsSeeder.cs#L50-L99). Account codes are unique in the database (repo: OfflineCPA/src/Infrastructure/Data/OfflineCPADbContext.cs#L31-L36).

`POST /api/books/setup` rejects an unknown kind and otherwise returns the kind plus `accountsAdded` (repo: OfflineCPA/src/Backend/Endpoints/BookEndpoints.cs#L13-L31). The seeder is registered as `IChartOfAccountsSeeder` (repo: OfflineCPA/src/Backend/Program.cs#L47).

## Tests

`ChartSeedTests` checks that each seed file exists, codes are unique, types parse, and every account has a line ID (repo: OfflineCPA/tests/TrueBalance.Tests/ChartSeedTests.cs#L7-L58).

## Change it safely

Add an account in the JSON file and extend `ChartSeedTests` if the shape changes. Do not rename a code that may already be stored; the seeder will skip it and leave the old row. A new kind needs a file, a name in the `Kinds` array, and a test. `TaxLineId` and `IsMeals` are properties on `Account` (repo: OfflineCPA/src/Domain/Entities/Account.cs#L15-L18). Books seeded before this change keep their accounts as they are: sole-proprietor accounts stay without a line. A customer refund posts to account 4900, Sales Returns and Allowances. See [[tax-maps]] and [[data-and-migrations]].

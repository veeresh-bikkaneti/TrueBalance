---
checked:
  OfflineCPA/src/Infrastructure/Seed/household.json: a3f3ab0e7952d8470fcf20b5c548399a1bcf1192
  OfflineCPA/src/Infrastructure/Services/JsonChartOfAccountsSeeder.cs: 068b242b1c9b1ab4ad4773cf925766b1c6e6c717
  OfflineCPA/src/Infrastructure/Data/OfflineCPADbContext.cs: 79af2ba8029ac2126a5a2367c395c30dc210b3ab
  OfflineCPA/src/Backend/Program.cs: e02af47f1e75ea8f35e7446d8b8315be41b80bc8
  OfflineCPA/tests/TrueBalance.Tests/ChartSeedTests.cs: b86f643b3ca3a10024595e2ea30a02584d58ac4e
  OfflineCPA/src/Domain/Entities/Account.cs: a1cf6b39dc2c2488604e542adf232732cbe2a353
---
# Charts of accounts

Three seed files ship with the app: `household.json`, `sole-proprietor.json`, and `s-corporation.json`. Each file has `kind`, `displayName`, and an `accounts` array of code, name, type, tax form line, and an optional description (repo: OfflineCPA/src/Infrastructure/Seed/household.json#L1-L5) (repo: OfflineCPA/src/Infrastructure/Services/JsonChartOfAccountsSeeder.cs#L13).

## Seeder and route

`JsonChartOfAccountsSeeder` loads `Seed/<kind>.json` from the app base directory with a case-insensitive JSON reader (repo: OfflineCPA/src/Infrastructure/Services/JsonChartOfAccountsSeeder.cs#L15-L31). `SeedAsync` skips codes that already exist, parses `AccountType` and `TaxLineItem`, and inserts the rest (repo: OfflineCPA/src/Infrastructure/Services/JsonChartOfAccountsSeeder.cs#L42-L80). Account codes are unique in the database (repo: OfflineCPA/src/Infrastructure/Data/OfflineCPADbContext.cs#L23-L28).

`POST /api/books/setup` rejects an unknown kind and otherwise returns the kind plus `accountsAdded` (repo: OfflineCPA/src/Backend/Program.cs#L127-L144). The seeder is registered as `IChartOfAccountsSeeder` (repo: OfflineCPA/src/Backend/Program.cs#L39).

## Tests

`ChartSeedTests` checks that each seed file exists, codes are unique, and the enums parse (repo: OfflineCPA/tests/TrueBalance.Tests/ChartSeedTests.cs#L7-L33).

## Change it safely

Add an account in the JSON file and extend `ChartSeedTests` if the shape changes. Do not rename a code that may already be stored; the seeder will skip it and leave the old row. A new kind needs a file, a name in the `Kinds` array, and a test. TaxFormLine is a property on `Account` (repo: OfflineCPA/src/Domain/Entities/Account.cs#L14). See [[data-and-migrations]].

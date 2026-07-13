# OMIE Day-Ahead Price Ingestion

Technical assessment for CONVEX Energy. A .NET 10 system that periodically downloads Day-Ahead auction results from the Spanish Spot Power Exchange (OMIE), persists them, exposes them as a CET time series over a REST API, and displays them in a Blazor Server frontend.

Built with AI assistance throughout, per the assignment. The complete, unedited AI conversation transcripts are in `docs/ai-transcripts/`.

## Architecture

```
ConvexEnergy.sln
  ConvexEnergy.Shared    Domain model, EF Core DbContext, migrations, file parser
  ConvexEnergy.Worker    Quartz.NET host: downloads, parses, imports (owns the DB schema)
  ConvexEnergy.Tests     xunit tests for the file parser, run against a committed real sample file
  ConvexEnergy.Api       Read-only REST API over the stored data (port 5080)
  ConvexEnergy.Web       Blazor Server frontend, consumes the API over HTTP only (port 5090)
```

Data flow: `OMIE public files -> Worker -> SQLite (data/convexenergy.db) -> Api -> Web`.

The Web project has no reference to Shared, EF Core, or the database. It only knows the API's JSON contract, which is why its DTO is deliberately duplicated instead of shared. This enforces the assignment rule that the frontend must consume the REST API and never access the database or application services directly.

## Prerequisites

- .NET 10 SDK
  - Ubuntu 24.04: `sudo apt update && sudo apt install -y dotnet-sdk-10.0`
  - Other platforms: https://dotnet.microsoft.com/download/dotnet/10.0
- No accounts, keys, or paid services. The OMIE data source requires no registration.

## Running

From the repository root, in three terminals:

```bash
# 1. Worker: creates/migrates the database, then downloads and imports.
#    Wait for "Run finished" in the logs (first run imports about 8 days).
dotnet run --project ConvexEnergy.Worker

# 2. API (listens on http://localhost:5080)
dotnet run --project ConvexEnergy.Api

# 3. Frontend (listens on http://localhost:5090)
dotnet run --project ConvexEnergy.Web
```

Open http://localhost:5090, pick a delivery date within the imported range (roughly the last week), and the per-period price table loads through the API. The Worker keeps running on an hourly schedule; it can be stopped and restarted at any time without creating duplicates.

### API examples

```bash
# JSON (default)
curl "http://localhost:5080/api/day-ahead-prices?from=2026-07-12&to=2026-07-12" -H "Accept: application/json"

# Semicolon-separated CSV
curl "http://localhost:5080/api/day-ahead-prices?from=2026-07-12&to=2026-07-12" -H "Accept: text/plain"
```

Both `from` and `to` are optional `yyyy-MM-dd` delivery-date filters. Each point carries `startCet` (the period's start as a CET/CEST timestamp), `deliveryDate`, `period`, `priceEsEurMwh`, and `pricePtEurMwh`.

### Tests

```bash
dotnet test
```

Five xunit tests cover the marginalpdbc parser: period count and known values verified against the real sample file committed at `docs/samples/marginalpdbc_20260712.1`, CRLF tolerance, culture-invariant parsing under a non-English system locale, and rejection of files without price rows.

### Configuration

All configuration lives in each project's `appsettings.json`. Worker settings (section `Omie`):

| Key                   | Default                | Meaning                                                 |
| --------------------- | ---------------------- | ------------------------------------------------------- |
| `DownloadUrlTemplate` | OMIE file-download URL | `{0}` = yyyyMMdd, `{1}` = file version                  |
| `CronSchedule`        | `0 10 * * * ?`         | Quartz cron, evaluated in Europe/Madrid (hourly at :10) |
| `BackfillDays`        | 7                      | How many past delivery days each run re-checks          |
| `MaxVersionProbe`     | 5                      | Highest file version suffix probed per day              |

The SQLite file is `data/convexenergy.db` at the repository root (`../data` relative to each project, since `dotnet run` uses the project directory as the working directory). It is runtime state and is gitignored.

## Research summary

**What the Spanish Spot Power Exchange is.** OMIE (OMI, Polo Espanol S.A.) is the nominated electricity market operator (NEMO) and the only NEMO for Spain and Portugal, operating the day-ahead and intraday markets of the Iberian electricity market (MIBEL). The day-ahead auction is part of the European Single Day-Ahead Coupling (SDAC); the market session runs every day at 12:00 CET and sets prices and energies for the following delivery day. Source: https://www.omie.es/en/mercado-de-electricidad

**Where the results are published.** Two places: interactive market-results pages, and a public file repository at https://www.omie.es/en/file-access-list covering the last 6 rolling years. Day-ahead marginal prices are published as `marginalpdbc_YYYYMMDD.V` files, downloadable without registration via `https://www.omie.es/en/file-download?parents=marginalpdbc&filename=marginalpdbc_YYYYMMDD.V`. Observed publication time for day D+1 is early afternoon CET of day D (roughly 13:00 to 16:00), consistent with the 12:00 CET auction.

**Chosen data source and why.** The `marginalpdbc` flat files: they are the official primary source, machine-readable, versioned, and require no account or API key (the assignment allows only a free GitHub registration, which rules out token-based alternatives such as REE's ESIOS API or the ENTSO-E Transparency Platform).

**How the data is structured.** Defined in OMIE's "Modelo de Ficheros para la distribucion publica de Informacion del mercado de electricidad", v1.36 (2025-03-18), section 6.18 (https://www.omie.es/sites/default/files/2025-03/formato_ficheros_inf_pub_136.pdf): header line `MARGINALPDBC;`, then one row per period `Year;Month;Day;Period;MarginalPT;MarginalES;` in EUR/MWh with `.` as decimal separator, terminated by a `*` line. Column 5 is the Portuguese zone price and column 6 the Spanish zone price, per the spec; this was verified against the document because the two columns are identical on most days (market coupling produces a single Iberian price whenever the ES-PT interconnection is not congested), which makes the order impossible to infer from typical data alone.

**The 15-minute change.** On 2025-10-01 the SDAC day-ahead market moved from hourly to 15-minute Market Time Units. A normal delivery day now has 96 periods, and period 1 is 00:00 to 00:15 CET. The transition is visible in the file archive as a jump in file sizes exactly at that date. This system stores per-period rows and never assumes a fixed period count.

**File versioning.** The file extension is a version counter. Most days publish as `.1`, but corrections are republished with incremented versions, and some days exist only as `.2` or `.3` (observed: 2025-11-27 only as `.2`, 2025-10-30 as `.3`). The importer probes versions upward and treats a higher version as a correction that replaces the stored day.

## Design decisions

- **Poll-and-idempotent instead of publish-time scheduling.** OMIE's publication time varies, so the Worker polls hourly and each run is a no-op unless something new or corrected is available. One mechanism covers periodic download, dedup, restart recovery, and corrections.
- **Dedup enforced by the database.** A unique index on `(DeliveryDate, Period)` makes duplicate imports structurally impossible, independent of application logic. Imports of a corrected day delete and reinsert inside one transaction, so readers never observe a half-imported day.
- **Restart safety.** All state lives in SQLite. On startup the Worker runs immediately and re-checks a `BackfillDays` window, so any days missed while the process was down are picked up. A second run logs "Skipping <day>: stored version N >= available version N" for every already-imported day, which is the observable proof of both properties.
- **SQLite via EF Core.** File-based storage keeps clone-and-run friction at zero and survives restarts by nature. The Worker applies EF migrations at startup and is the only schema owner; the API opens the database read-only in behavior (`AsNoTracking`, no migrate call).
- **CET semantics.** "CET time series" is interpreted as Spanish market local time (Europe/Madrid), which is CET in winter and CEST in summer, matching how OMIE itself labels periods. Timestamps are computed by converting Madrid-local midnight to an absolute instant and adding `(period - 1) * 15` minutes of absolute time, which stays correct on DST transition days (92 or 100 periods).
- **Culture-invariant number handling** on both parse (file uses `.` decimals) and CSV output, so a server locale like es-ES or de-DE cannot corrupt data.
- **Fixed ports via appsettings, template `launchSettings.json` removed**, so the documented URLs are deterministic for anyone cloning the repo.

## Assumptions and limitations

- Single Worker instance. Quartz uses its in-memory store; running multiple Worker replicas against one database is out of scope.
- The Spanish price is the primary subject per the assignment; the Portuguese price is stored and exposed as well since the file provides it at no cost.
- EF's SQLite provider stores `decimal` as TEXT. Fine here, as no SQL-side arithmetic is performed on prices; all aggregation happens in .NET.
- The API has no authentication and binds to localhost; it is a local demo, not a hardened service.
- OMIE occasionally answers HTTP 200 with a non-file body for nonexistent files; the downloader validates content by the `MARGINALPDBC` header instead of trusting status codes.
- Historical data is limited by OMIE's 6-year rolling window, and this system only backfills `BackfillDays` into the past by default (configurable).

## Notable issues hit and resolved during the AI-assisted build

Kept here deliberately, since evaluating the AI workflow is part of the assignment. Full detail in the transcripts.

1. **Vulnerable transitive packages.** Fresh templates shipped `SQLitePCLRaw.lib.e_sqlite3` 2.1.11 and `Microsoft.OpenApi` 2.0.0, both with high-severity advisories. SQLitePCLRaw was cleared with a direct reference to 3.0.3. For Microsoft.OpenApi the advisory covers the whole 2.x line while 3.x breaks ASP.NET's OpenAPI source generator (`IOpenApiMediaType.Example` became read-only), so the optional OpenAPI feature was removed instead of shipping a flagged dependency.
2. **Working-directory database drift.** `dotnet run --project X` uses the project directory as the working directory, so a relative `data/` path gave each process its own database. Fixed by anchoring the connection string to `../data`, landing all processes on the same repo-root file.
3. **launchSettings port override.** The template's `launchSettings.json` overrode the configured URLs with random ports; deleted so appsettings is the single source of truth.
4. **Production static assets.** Removing launchSettings also removed `ASPNETCORE_ENVIRONMENT=Development`, and Blazor's static web assets manifest only auto-loads in Development, which broke `blazor.web.js` and left the UI prerendered but dead. Fixed with `builder.WebHost.UseStaticWebAssets()`.
5. **`AddHttpClient` missing in the Worker.** Web SDK projects get `Microsoft.Extensions.Http` implicitly; the Worker SDK does not. Added the package explicitly.

## AI usage

AI assistants were used for research, design, implementation, and debugging, as required by the assignment. The complete conversations, verbatim and unedited, including wrong turns and corrections, are in `docs/ai-transcripts/`. Every architectural decision above can be traced to its origin and discussion there.

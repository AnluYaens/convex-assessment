# Roadmap

Time budget: roughly 10 focused hours. Statuses: DONE / IN PROGRESS / NEXT / TODO.

## Phase 0: Environment and scaffold (DONE)

- .NET 10 SDK installed via apt (dotnet-sdk-10.0, SDK 10.0.109)
- Solution with 4 projects: Shared (classlib), Worker (Quartz host), Api (webapi), Web (Blazor Server)
- Git repo with .NET .gitignore, docs/ai-transcripts and docs/samples folders

## Phase 1: Research (DONE)

- OMIE identified as the exchange, marginalpdbc files as the data source
- Real file downloaded and format confirmed against a live sample
- 15-minute MTU change (2025-10-01) identified and reflected in the schema
- File version suffix behavior (.1/.2/.3 corrections) identified
- DONE: confirm which price column is Spain vs Portugal from OMIE format doc page 67

Definition of done: README research section can cite a source for every claim.

## Phase 2: Data layer (DONE)

- Entity DayAheadPeriodPrice with unique index on (DeliveryDate, Period)
- ConvexDbContext + design-time factory, SQLite provider
- InitialCreate migration generated
- Parser MarginalPdbcParser with culture-invariant decimal parsing
- Vulnerable transitive packages bumped (SQLitePCLRaw, Microsoft.OpenApi)

## Phase 3: Worker (DONE)

- Quartz job: download latest available file(s), parse, upsert
- Version-aware upsert: skip same-or-lower version, overwrite on higher version, log both
- Catch-up on startup: backfill missed days since last import
- appsettings.json config: cron schedule, download URL template, DB path
- Structured logging of every meaningful event and failure
- Database.Migrate() on startup

Definition of done: run worker, see rows in SQLite; stop it, restart it, see zero duplicates and a log line explaining why nothing was re-imported.

## Phase 4: REST API (DONE)

- GET endpoint returning stored data as CET time series (timestamps computed via Europe/Madrid, DST-safe)
- Content negotiation: application/json and text/plain (semicolon-separated CSV)
- Date range filtering
- Database.Migrate() NOT here; worker owns the schema, API is read-only

Definition of done: curl with Accept: application/json and Accept: text/plain both return correct data for a real day.

## Phase 5: Blazor frontend (DONE)

- One page: date picker, table of periods and prices
- Data fetched via HttpClient from the REST API only, no DB access, no project reference to Shared
- Chart only if time remains (it is optional per the assignment)

Definition of done: browser shows real auction data that traveled through the API.

## Phase 6: Polish and submission (DONE)

- README: setup, architecture, research summary with sources, assumptions and limitations
- Export ALL AI transcripts verbatim into docs/ai-transcripts/
- Delete local clone, re-clone fresh, follow README start to finish, fix anything that breaks
- Make repo public, submit URL

Definition of done: a stranger with the README and nothing else gets the full system running.

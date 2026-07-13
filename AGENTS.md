# AGENTS.md

Guidelines for any AI agent (Claude Code, Cursor, Copilot, etc.) working in this repository. Read docs/PROJECT_CONTEXT.md and docs/ROADMAP.md first.

## The single most important rule

Every AI conversation about this project is a graded deliverable. Transcripts are exported verbatim to docs/ai-transcripts/ with nothing edited, cleaned up, or summarized. Never suggest trimming them. Dead ends and corrections stay in.

## Architecture invariants (do not violate, do not "improve")

1. Four projects: ConvexEnergy.Shared, ConvexEnergy.Worker, ConvexEnergy.Api, ConvexEnergy.Web.
2. ConvexEnergy.Web must NEVER reference ConvexEnergy.Shared, EF Core, or the database. It talks to the API over HTTP only. This is an explicit assessment requirement.
3. Dedup is enforced by the unique index on (DeliveryDate, Period), not only by application logic.
4. The Worker owns schema migrations (Database.Migrate() at startup). The Api reads only.
5. Storage is SQLite via EF Core. Do not introduce another database.
6. Scheduling is Quartz.NET. Do not replace it with Timer, BackgroundService loops, or Hangfire; Quartz is an explicit requirement.

## Domain rules

1. A delivery day has a variable number of 15-minute periods: 96 normally, 92 or 100 on DST transition days. Never hardcode 96.
2. CET timestamps are computed from Europe/Madrid start-of-day as an absolute instant plus (period - 1) * 15 minutes of absolute time. Never add offsets to local wall-clock times.
3. Prices are decimal, never double or float. Parsing uses CultureInfo.InvariantCulture.
4. marginalpdbc file extensions are version counters. Ingestion must handle .2/.3 corrections: same or lower version is skipped and logged, higher version overwrites and logs.
5. Parser tolerates CRLF line endings.

## Coding conventions

1. Target framework net10.0 for all projects.
2. Configuration lives in appsettings.json, strongly typed via the options pattern. No secrets exist in this project; the DB is a local SQLite file.
3. Log meaningful events with ILogger: file downloaded, rows imported, rows skipped and why, download failures, parse failures. No silent catches.
4. Prefer small, explainable code over clever code. The author must be able to defend every line in an interview.

## Workflow rules

1. All changes build cleanly: dotnet build with zero errors before any commit.
2. Run dotnet list package --vulnerable --include-transitive after adding packages; resolve advisories immediately.
3. Commit messages: short subject line plus bullet points. No attribution footers, no Co-Authored-By lines.
4. No em dashes in any written deliverable (README, docs, commit messages).
5. Keep a sample marginalpdbc file in docs/samples/ and use it for parser tests.

## Prohibited

1. Adding paid services, API keys, or anything requiring registration beyond GitHub.
2. Editing files in docs/ai-transcripts/.
3. Making the Web project aware of the database in any way.
4. Rewriting working code for style without being asked.

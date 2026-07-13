# Project Context: CONVEX Energy Technical Assessment

## What this is

Technical assessment for a recruitment process at CONVEX Energy. The task: research the Spanish Spot Power Exchange with the help of AI, then build a .NET 10 system that periodically ingests Day-Ahead auction results, stores them, exposes them via a REST API as a CET time series, and displays them in a Blazor Server frontend.

## What is actually being evaluated

Not primarily the code. The stated evaluation criteria are:

1. How effectively the candidate works with AI (prompt quality, validation of AI output, iteration).
2. Whether the candidate understands the final solution and can defend every decision in a follow-up discussion.
3. The complete, unedited AI conversation transcripts, which are a first-class deliverable.

Blindly copied code that the candidate cannot explain results in a failed assessment.

## Constraints

- Deadline: less than one day from start of work.
- Solo developer, first .NET project (background is TypeScript/Node, so concepts are familiar under different names).
- Heavy AI assistance is not a shortcut here, it is the assignment.
- No paid services allowed. Only a free GitHub registration.
- Public GitHub repository, so zero secrets anywhere in the repo, ever.

## Key domain facts (verified during research, see README for sources)

- The Spanish Spot Power Exchange is OMIE, the single NEMO for Spain and Portugal (MIBEL market).
- The day-ahead auction (SDAC) runs daily at 12:00 CET and sets prices for the next delivery day.
- Since 2025-10-01 the day-ahead market uses 15-minute Market Time Units: 96 periods on a normal day, 92 on the March DST day, 100 on the October DST day. Never hardcode 96.
- Results are published as public flat files, no registration: `marginalpdbc_YYYYMMDD.V` at omie.es file access. `V` is a version counter; corrections are republished as `.2`, `.3`, etc.
- File format: header line `MARGINALPDBC;`, then rows `YYYY;MM;DD;period;price;price;` in EUR/MWh with `.` decimals, terminated by a line containing `*`.
- The two price columns are the Portuguese and Spanish system prices. On most days they are identical (market coupling). Column order is verified against OMIE's official file model document (see README).
- Files are published early afternoon CET on the day before delivery (roughly 13:00 to 14:30 based on observed publication timestamps).

## Deliverables checklist

- [x] Working Quartz.NET worker: periodic download, persistence, dedup, restart safety, logging
- [x] REST API: CET time series, application/json and text/plain (semicolon CSV)
- [x] Blazor Server frontend consuming ONLY the REST API
- [x] README with setup instructions and research summary
- [x] Complete unedited AI transcripts in docs/ai-transcripts/
- [x] Fresh-clone test: clone, follow README, everything runs
- [ ] Repo public, URL submitted

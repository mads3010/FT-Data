# Folketingets afstemninger

A transparency platform for the Danish parliament: **who voted for and against what, how often members show up, and how each vote split across parties** — for every roll-call vote in the chamber since October 2004, straight from Folketinget's open data.

The site is a Danish-language, server-rendered web app (installable as a PWA) with a small read-only JSON/CSV API. It takes no political position: every number is a count of official records, and the method behind each figure is documented on the site's *Om og metode* page and in [docs/features.md](docs/features.md).

## Stack

| Layer | Choice |
|---|---|
| Language / runtime | C# on .NET 10 (LTS) |
| Web | Blazor Web App, static server-side rendering, no client framework, charts as HTML/CSS marks |
| Database | PostgreSQL 17, EF Core 10 with migrations, statistics as materialized views |
| Data source | [oda.ft.dk](https://oda.ft.dk/) OData v3 API (votes, ballots, cases, actors), [folketingstidende.dk](https://www.folketingstidende.dk/) for bill texts |
| Ingestion | `FolketingetVotes.Ingest` console: full backfill + incremental sync by `opdateringsdato` |
| Tests | xUnit; unit tests, Testcontainers (Postgres) integration test, page smoke tests with fakes |

See [docs/architecture.md](docs/architecture.md) for the reasoning and [docs/adr](docs/adr) for the decisions.

## Quick start

Prerequisites: .NET 10 SDK, Docker (for PostgreSQL).

```bash
docker compose up -d db                                            # PostgreSQL on localhost:5432
dotnet run --project src/FolketingetVotes.Ingest -- migrate       # create schema + empty statistics views
dotnet run --project src/FolketingetVotes.Ingest -- sync           # first run = full backfill (~1.8M ballots, 30–60 min)
dotnet run --project src/FolketingetVotes.Web                      # https://localhost:5001 (see launchSettings)
```

Later runs of `sync` are incremental (seconds to minutes). Run it on a schedule, e.g. hourly on sitting days.

No Docker? Any PostgreSQL 17 works. With Homebrew on macOS:

```bash
brew install postgresql@17
LC_ALL=en_US.UTF-8 /opt/homebrew/opt/postgresql@17/bin/pg_ctl -D /opt/homebrew/var/postgresql@17 -l /tmp/pg.log start
/opt/homebrew/opt/postgresql@17/bin/psql -h localhost -d postgres -c "CREATE ROLE folketinget LOGIN PASSWORD 'folketinget';" -c "CREATE DATABASE folketinget OWNER folketinget;"
```

The database integration test uses Docker (Testcontainers) by default; point it at a scratch database instead with
`FOLKETINGET_TEST_CONNECTION="Host=localhost;Database=folketinget_test;Username=folketinget;Password=folketinget"` (the test truncates that database).

Useful commands:

```bash
dotnet run --project src/FolketingetVotes.Ingest -- status                     # sync bookkeeping and row counts
dotnet run --project src/FolketingetVotes.Ingest -- sync --only Afstemning,Stemme
dotnet run --project src/FolketingetVotes.Ingest -- refresh-stats              # rebuild materialized views only
dotnet run --project src/FolketingetVotes.Ingest -- import-party-accounts data/partiregnskaber/partiregnskaber_2023.pdf
dotnet test                                                                     # all tests (DB test skips without Docker)
```

Configuration lives in `appsettings.json` of the two apps (`ConnectionStrings:Folketinget`, `Oda:*`). Override with environment variables, e.g. `ConnectionStrings__Folketinget`.

## Repository layout

```
src/FolketingetVotes.Core      entities, enums, read models, query interfaces, ISummaryProvider (no dependencies)
src/FolketingetVotes.Data      EF Core context + migrations, oda.ft.dk client, sync, stats SQL, query services, party-account parser
src/FolketingetVotes.Ingest    CLI: migrate | sync | refresh-stats | import-party-accounts | status
src/FolketingetVotes.Web       Blazor pages, chart components, JSON/CSV API, PWA assets
tests/                         Core.Tests (pure), Data.Tests (parser, client, DB pipeline), Web.Tests (page smoke via fakes)
docs/                          architecture, data model, data sources, features, roadmap, progress, ADRs
```

## Documentation

- [docs/features.md](docs/features.md) — what is implemented and exactly how each figure is computed
- [docs/roadmap.md](docs/roadmap.md) — planned work, including the AI summariser and party-finance pipeline
- [docs/progress.md](docs/progress.md) — current status log
- [docs/data-sources.md](docs/data-sources.md) — everything learned about oda.ft.dk and the other sources
- [docs/data-model.md](docs/data-model.md) — tables, views and how API entities map to them
- [docs/architecture.md](docs/architecture.md) — layers, sync design, rendering approach

## Principles

1. **Objective by construction.** Only official records are shown; derived figures are simple counts with the method stated next to them. No scoring, ranking or editorialising.
2. **Traceable.** Every page links back to the source (oda.ft.dk, Folketingstidende, the party's own accounts and page number).
3. **Boring technology, one language.** A single C# solution, SQL you can read, no build pipeline for the frontend.
4. **Extensible where it matters.** AI summaries plug in behind `ISummaryProvider`; new statistics are a SQL file; new data sources are a sync class.

## License

MIT (see `LICENSE`). Data from Folketinget is public and free to reuse; cite oda.ft.dk.

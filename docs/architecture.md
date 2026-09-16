# Architecture

## Layers

```
┌──────────────────────────┐   ┌──────────────────────────┐
│  FolketingetVotes.Web    │   │ FolketingetVotes.Ingest  │
│  Blazor SSR pages, API   │   │ CLI: migrate/sync/stats  │
└────────────┬─────────────┘   └────────────┬─────────────┘
             │ IVoteQueries, IPoliticianQueries, …          SyncOrchestrator, StatsRefresher
┌────────────▼─────────────────────────────▼─────────────┐
│  FolketingetVotes.Data                                  │
│  EF Core DbContext + migrations · OdaClient · EntitySync │
│  SQL for derived tables/materialized views · queries     │
└────────────┬────────────────────────────────────────────┘
             │ entities, enums, read models, ISummaryProvider
┌────────────▼─────────────┐
│  FolketingetVotes.Core   │   (no package references)
└──────────────────────────┘
```

- **Core** holds the domain vocabulary and the *contracts* the web app needs (query interfaces returning read-model records). Nothing in Core knows about EF Core, HTTP or Blazor, so the web layer can be tested with in-memory fakes and the data layer can change freely.
- **Data** owns everything stateful: the PostgreSQL schema (EF Core migrations), the oda.ft.dk client, synchronisation, the derived statistics (plain SQL files embedded in the assembly) and the LINQ query services.
- **Ingest** is a thin command dispatcher over Data services. It runs on a schedule (cron, systemd timer, GitHub Actions…) and is the only writer.
- **Web** renders pages from the query interfaces. It never writes to the database.

## Synchronisation design

oda.ft.dk is an OData v3 service with two hard constraints: **every page is capped at 100 rows** (`$top` above 100 is ignored, expanded collections are capped too) and there is no bulk export. Every entity carries `opdateringsdato`, which makes incremental sync cheap.

`EntitySync<TDto, TEntity>` implements both modes:

- **Full load** walks the entity set by id (`$filter=id gt {last}&$orderby=id&$top=100`). Keyset paging keeps every request cheap regardless of depth. Large sets (ballots, case actors, case steps, relations, cases) are split into id ranges processed concurrently, bounded by `Oda:MaxConcurrentRequests` (default 4) so the public API is not hammered. Progress (`LastId`) is checkpointed so an interrupted backfill resumes.
- **Incremental load** fetches `opdateringsdato gt {checkpoint - overlap}` ordered by `opdateringsdato,id` and follows `odata.nextLink`. The overlap (default 10 minutes) plus idempotent upserts make the sync robust to clock skew and rows updated mid-run.

Upserts are EF Core `SetValues`/`Add` batches for normal entities and a `COPY` into a temp table followed by `INSERT … ON CONFLICT DO UPDATE` for ballots (the 1.8M-row set). `sync_states` records per entity: full-load completion, last id, last `opdateringsdato`, last run times.

Order matters only loosely (there are no foreign-key constraints between synced tables, by design — the API occasionally references rows that arrive later). Lookups → periods → actors → relations → meetings → cases → steps → case actors → votes → ballots.

## Statistics

All derived numbers live in SQL under `src/FolketingetVotes.Data/Sql/`, run by `StatsRefresher` after each sync (and by `refresh-stats`):

1. `010_parties.sql` — one row per group short name (S, V, …) from the per-session group actors.
2. `020_party_memberships.sql` — person × group-per-session with dates (relation dates, falling back to the group's session dates).
3. `030_materialized_views.sql` — `mv_ballots` (ballot + party on the day), `mv_vote_totals`, `mv_vote_party_breakdown` (with the party majority), `mv_politician_stats`, `mv_party_stats`.

The views are dropped and recreated on every refresh, so changing a definition is a file edit, not a migration. Queries map them as keyless EF entities.

## Rendering approach

Blazor **static server-side rendering** only: every page is plain HTML produced on the server, indexable and linkable, with `blazor.web.js` providing enhanced navigation. There is no WebAssembly payload and no SignalR circuit, which keeps hosting simple and the pages fast on mobile. Filters are ordinary `GET` forms; state lives in the URL.

Charts are HTML/CSS marks (stacked bars, seat grids, share bars), designed with the data-visualisation method in `docs/features.md` § Charts: thin marks, 2 px surface gaps, direct labels, a legend for multi-series, and an accompanying table for every chart. The ballot palette (blue = for, orange = against, aqua = neither, hatched gray = absent) was validated for colour-vision deficiency in light and dark mode; party colours are identity swatches only.

The site is installable as a PWA (`manifest.webmanifest`, `sw.js` network-first). Native shells (e.g. .NET MAUI Blazor Hybrid) can reuse the components later; see the roadmap.

## Extension points

- **AI summaries**: implement `ISummaryProvider` (Core) and register it in `AddFolketingetData`; persist results in `bill_summaries`. The case page already renders the result when present and links the source PDF on folketingstidende.dk.
- **New statistic**: add a view to `030_materialized_views.sql`, a keyless entity in `Persistence/Views`, and a read model + query.
- **New data source**: add an `IEntitySync` (or a dedicated importer like `PartyAccountImporter`) and register it.

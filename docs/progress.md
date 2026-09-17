# Progress log

Newest first. Keep this short; details belong in features.md / roadmap.md.

## 2026-09-17 (v0.4) — explorer and visuals

- Shared SVG chart components (bar, line, donut, timeline band, hemicycle), no JavaScript.
- Data explorer at `/udforsk`: metric × parties/members × period × grouping, four chart types, shareable links, CSV/JSON.
- Vote pages: hemicycle and a plain-language summary; home page: votes per month and attendance per group this session; profiles: career timeline and attendance trend; party and session pages: trend and attendance charts.
- Done on branch `feature/explorer-and-visuals` in stage commits; v0.3.0 tagged before, v0.4.0 after the merge.

## 2026-09-16 (v0.3) — issues #1–#16 filed, ten implemented

- Filed 16 feature issues on GitHub with priorities; implemented the P1 set: questions to ministers, leave periods, legislation outcomes, party switchers, party-difference page, topic trends, session dissent list, composition, global search with trigram indexes, per-member/topic feeds and OpenAPI, data-quality page.
- Open (P2/P3): appropriations (#5), EU dimension (#10), public party support (#11), income statements from the accounts (#12), pre-2004 votes from transcripts (#15).

## 2026-09-16 (v0.2) — audit fixes and new views

- Fixed "current members" (233 → 179) by defining it from the latest sitting day's roster; politician list defaults to current members; birth years shown for namesakes.
- Added ministerial periods and attendance excluding ministerial time, committee memberships and proposals on profiles; transcript links on vote pages.
- Added topics (Emneord sync), sessions with agreement matrix and closest votes, donor index, case search, comparison, Atom feed, sitemap, Open Graph tags.
- Local Postgres now runs as a Homebrew service; `deploy/macos/install-sync-agent.sh` schedules an hourly sync via launchd.

## 2026-09-16 (later) — party accounts, OCR, sharing

- Found the published party-account PDFs are scans; built `tools/ocr-pdf` (Apple Vision, Danish) and OCR sidecars; fixed a memory leak that killed it after ~100 pages.
- Rewrote the party-account parser against the real 2023 text and verified across 2020–2024 (see features.md); imported six years (2019–2024, 721 rows).
- Pushed the repository to GitHub (mads3010/FT-Data); added Dockerfiles, a production compose file, `deploy/share.sh` and `docs/deployment.md`; shared a temporary Cloudflare quick-tunnel link.

## 2026-09-16 — v0.1 scaffold to working site

- Researched oda.ft.dk: entity model, 100-row page cap, keyset paging, `opdateringsdato`, per-session group actors, ballot coverage since Oct 2004; documented in `docs/data-sources.md`.
- Chose .NET 10 + Blazor static SSR + PostgreSQL (ADRs 0001–0005).
- Implemented Core (entities, enums, read models, `ISummaryProvider`), Data (EF Core schema, OdaClient, resumable full/incremental sync, COPY-based ballot upsert, SQL statistics, query services, party-account parser), Ingest CLI, Web (all pages, charts, API, PWA), tests (unit, Testcontainers pipeline test, page smoke tests), docs.
- Validated the ballot colour palette for colour-vision deficiency in both modes.
- First full backfill run against oda.ft.dk from the development machine: lookups, periods, 18k actors, 164k relations (37 s), 14k meetings, cases, steps, case actors, 10.5k votes, ballots. Keyset paging at roughly 25 pages/s per worker.
- Development machine note: Docker Desktop's image pulls hung, so a local Homebrew PostgreSQL 17 was used (see README); `docker compose up -d db` remains the documented path and works elsewhere.
- Verified against ft.dk: vote 10604 (3 Sep 2026) totals 106/6/0 match Folketinget's conclusion text; party histories (e.g. Venstre → UFG → Moderaterne) match biographies.
- Found that the API lacks group relations for ~96 voting members (20 % of ballots unattributed); added biography-derived party terms (`BiographyParser`, `PartyNames`) and the biography's stated party as fallbacks. Unattributed ballots: 0.44 %.
- Full backfill timing on a laptop: ~7 minutes end to end (ballots 3.5 min via COPY), statistics 16 s; incremental sync afterwards runs in seconds.
- Test suite: 68 tests (unit, parser, client, database pipeline, page smoke) all green.
- Open: spot-check more figures against ft.dk; calibrate the party-accounts parser on real PDFs; AI summaries not enabled (by decision).

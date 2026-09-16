# Roadmap

Ordered by value. Each item names the extension point it uses.

## 1. Verify and tune the party-accounts parser (bonus feature, partly done)

- Done: PDFs for 2019–2024 OCR'd and imported; parser calibrated on the real text (24 tests from OCR excerpts).
- Remaining: spot-check a sample of rows against the PDFs by eye each year; add the 2018 file if wanted; consider `--no-correction` OCR for names.
- Add an "all donors" page (`/bidrag`) listing donors across parties and years, with donor search — a strong transparency feature once data is reliable.
- Consider public party support (partistøtte) amounts from valg.im.dk for context.

## 2. AI summaries of bills (extension point: `ISummaryProvider`)

Design already in place: `bill_summaries` table, `BillSummaryContent` record (what it is about / what it does / why / proposed by, plus provider, model, prompt version, generated at, source URL), the case page renders it as "Kort fortalt — maskingenereret" with a link to the source text.

Implementation sketch (Claude via the Anthropic .NET/HTTP API):
1. `ClaudeSummaryProvider : ISummaryProvider` in Data: on a cache miss, fetch the bill as introduced from folketingstidende.dk (`CaseDetail.BillTextPdfUrl`), extract the "Bemærkninger til lovforslaget → Almindelige bemærkninger" section (PdfPig, same as the party parser), and call the model with a fixed, versioned system prompt demanding neutral, non-evaluative language, Danish output, and a JSON response matching `BillSummaryContent`. Persist to `bill_summaries`; serve from cache afterwards.
2. Prefer generating in a nightly `summarize` command in Ingest (Batch API, cheaper) rather than on page view, so pages never wait and cost is bounded.
3. Show provider, model and date; never render a summary without the source link; add an "report a problem" mailto.
4. Register with `services.AddSingleton<ISummaryProvider, ClaudeSummaryProvider>()` in `AddFolketingetData` (behind a config flag) and keep `NullSummaryProvider` as the default when no API key is configured.

## 3. More views on the same data

Done in v0.2–v0.3: topics (with trends), sessions with agreement matrix, closest votes, legislation outcomes and dissent lists, party differences, party switchers, composition, donor index, case search, member comparison, questions to ministers, proposals, ministerial and leave periods on profiles, global search, feeds, sitemap, OpenAPI, status page. Remaining ideas are tracked as GitHub issues (#5, #10, #11, #12, #15).

Remaining ideas:
- Party-vs-party comparison page (the matrix gives the number; a page could list the votes where two parties differed).
- Topic trends over time (votes per topic per session).
- Substitute (stedfortræder) periods, if a data source turns up (the biographies do not state them; the API's role 10 relations only cover committees).

## 4. Operations

- Scheduled sync (GitHub Actions cron or a systemd timer on the host) with alerting when `sync_states.last_run_completed_at` gets stale.
- Output caching for the heaviest pages (vote detail, politician profile) with invalidation after sync.
- Deployment recipe: Docker image for Web, managed Postgres, `dotnet ef database update` on release.

## 5. Native apps

The components are static-SSR today. For app-store distribution, host the site in a .NET MAUI Blazor Hybrid shell or wrap the PWA with Capacitor. Interactive components would need render modes (`@rendermode InteractiveServer`) — keep them few.

## 6. Data quality

- Cross-check `mv_vote_totals` against `Afstemning.konklusion` counts and log discrepancies.
- Sync `Dokument`/`Fil` for cases with votes to list documents directly instead of deep links.
- Handle members with multiple simultaneous memberships (should not occur; the LATERAL picks the latest start).

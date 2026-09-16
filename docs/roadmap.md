# Roadmap

Ordered by value. Each item names the extension point it uses.

## 1. Verify and tune the party-accounts parser (bonus feature, partly done)

- Done: PDFs for 2019–2024 are in `data/partiregnskaber/` (ignored by git) and OCR'd with `tools/ocr-pdf`.
- Run `import-party-accounts`, inspect `party_donations.raw_text`, tune the heading list, `DonorHeaderRegex`, `BlockEndRegex`, `AmountRegex` against the OCR text; add real-text fixtures to `PartyAccountParserTests`.
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

- Vote comparison: how often two parties (or two members) vote the same way per session (`mv_ballots` self-join).
- Topic browsing via `Emneord`/`EmneordSag` (subject keywords) — needs two more sync classes.
- Minister accountability: cases per minister and their outcomes (`case_actors` role 14).
- Per-session "most contested votes" (smallest margins) and "unanimous" lists.

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

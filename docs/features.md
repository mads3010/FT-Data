# Features — what is implemented and how it works

Status legend: ✅ implemented · 🧪 implemented, needs real-data calibration · 🔜 planned (see roadmap.md)

## ✅ Data ingestion (`FolketingetVotes.Ingest sync`)

- Full backfill and incremental sync of periods, actors, actor relations, meetings, cases, case steps, case actors, votes, ballots and the 12 code tables from oda.ft.dk. See `docs/architecture.md` § Synchronisation.
- Resumable: `sync_states` remembers the last id (full load) and the last `opdateringsdato` (incremental). `status` prints it.
- Polite: at most `Oda:MaxConcurrentRequests` (4) requests in flight, standard resilience (retries with backoff, timeouts, circuit breaker) via `Microsoft.Extensions.Http.Resilience`.
- After every sync the statistics are rebuilt (`refresh-stats` does it alone).

## ✅ Vote pages (`/afstemninger`, `/afstemninger/{id}`)

- Search and filter by text (case title, short title, number, step title), session, vote type, case type and outcome; paged, newest first. State is in the URL so results are shareable.
- Vote detail: outcome (Folketinget's own `vedtaget`), type, step, date; four count tiles; **party breakdown** (stacked bar per party plus the same counts as text); **seat grid** (every member as a square, grouped by the group they belonged to on the day, linking to the member); list of members who **voted differently from their group's majority**; Folketinget's official conclusion text; full ballot table.

**How "group on the day" works:** `party_memberships` is looked up for each ballot at the sitting date. It has two sources, in priority order: (1) the API's group→person relations (AktørAktør role 15 to the per-session group actors); (2) the terms stated in the member's own biography ("Folketingsmedlem for Venstre i …, 13. november 2007 – 18. juni 2015"), parsed by `BiographyParser` and mapped to group short names by `PartyNames`. Source 2 exists because the API has no group relation at all for about one in eight voting members (e.g. long-serving MPs whose relations list only committees). A third, undated last resort is the party the biography names (`<partyShortname>`), used only for vote attribution for the few members with neither; it is never shown as a membership span or counted as a current member. Members outside any group appear as UFG; a ballot with no match in any source is shown as "Uden gruppe". Result on the September 2026 backfill (1,839,500 ballots): 80 % attributed by source 1, 19 % by source 2, 0.2 % by source 3, 0.44 % unattributed.

**How "group majority" works:** among the group's *present* ballots (for/against/abstain) the most common one; NULL on a tie or when nobody was present. A member "dissents" when present and their ballot differs from that majority. The member's own ballot is part of the majority count (standard party-unity definition).

## ✅ Politician pages (`/politikere`, `/politikere/{id}`)

- List with name search, party filter (any time or current only), attendance and ballot counts.
- Profile: current group and membership history (contiguous sessions in the same group are merged into one span), overall tiles (ballots, attendance, "voted like the group", for/against/abstain), stacked bar per session, filterable ballot history (session, ballot, dissent-only, text) with a link to the vote and the group's majority in that vote. CSV export of the history.

**Attendance** = (ballots that are for, against or abstain) ÷ (all registered ballots). The site states next to the figure that absence has many legitimate causes (ministerial travel, illness, leave, "clearing" pairs) and is not a performance score.

**Voted like the group** = with_party ÷ (with_party + against_party), counting only present ballots in votes where the group had a majority.

## ✅ Party pages (`/partier`, `/partier/{short}`)

- All groups ever seen with current member count and years covered.
- Per session: members, ballots, attendance, **cohesion** (share of present ballots matching the group majority); current members; disclosed private contributions (when accounts have been imported).

## ✅ Case pages (`/sager/{id}`)

- Case number, title, status, Folketinget's official summary (`resume`), who is behind it (proposers, minister, ministry with their official role names), all votes in the case, the official voting conclusion, deterministic links to Folketingstidende (case page and the bill as introduced PDF), law number/date and Retsinformation when available, the case's step history.
- A slot for a machine-generated summary renders only when an `ISummaryProvider` returns one (v1: never).

## ✅ Home and About

- Home: headline counts, latest votes, last sync time.
- About (`/om`): data sources, every formula in plain Danish, limitations, the party-finance method, the AI-summary policy.

## ✅ Read-only API (`/api/v1`)

`status`, `periods`, `votes` (same filters as the page), `votes/{id}`, `politicians`, `politicians/{id}`, `politicians/{id}/ballots` (+ `?format=csv`), `parties`, `parties/{short}`, `cases/{id}`. Enums are serialised as strings. The API is the same query layer the pages use, so numbers always agree.

## ✅ PWA

`manifest.webmanifest`, icons, and a network-first service worker so the site can be installed on phones and desktops. Content is never served stale: the network is tried first and the cache only answers when offline.

## ✅ Charts

HTML/CSS marks following the data-visualisation method used in this project: bars ≤ 14 px thick with 2 px surface gaps between stacked segments and a 4 px rounded data-end, a legend for every multi-series chart, direct labels only where they help (counts at the end of each row), and a table with the same numbers under every chart. Colours: for = blue `#2a78d6`, against = orange `#eb6834`, neither = aqua `#1baf7a`, absent = hatched neutral gray; dark-mode steps `#3987e5 / #d95926 / #199e70`. The three-hue set was validated for protan/deutan separation (worst pair ΔE 9.2 light / 9.4 dark) and for normal vision (≥ 20). The light-mode aqua sits at 2.7:1 contrast, which is why counts are always printed next to the bars.

## ✅ Party accounts (`import-party-accounts`, `inspect-party-accounts`)

`PartyAccountPdfReader` reads an OCR sidecar (`file.ocr.txt`, produced by `tools/ocr-pdf` with Apple Vision because the published PDFs are scans) or, for PDFs with a real text layer, extracts text with PdfPig; `PartyAccountParser` splits the combined PDF into party sections by known headings, finds the "contributions above the threshold" blocks and parses `Name, Address … amount kr.` lines, keeping the raw line and page number. `PartyAccountImporter` upserts by (year, party) and replaces that party's donations. `inspect-party-accounts <file>` prints what the parser sees without importing.

**What the source looks like.** Folketinget's combined PDF concatenates each party's own annual report, so every party has its own layout. Observed 2019–2024 variants, all handled and covered by tests built from the real OCR text: comma-separated `Name, Street, Postal Town` rows; two-space column rows; bulleted rows (`-`, `•`, `●`); rows with a trailing amount (Enhedslisten voluntarily lists everything above 5,000 kr with amounts; most parties list name and address only, as the law requires); vertical entries (name / address / `Bidrag: 600.585 kr.`); "Modtager / Beløb / Modtaget fra" entries; wrapped names; headers that span lines and state the indexed threshold ("over 22.800 kr.", "større end 22,8 t.kr.", "stillet faciliteter til rådighed"); running headers and footers per page; e-signature widget text glued onto lines; income-statement lines that mention the threshold but are not lists; the same donors listed twice (management report and notes). Rows are only accepted when they look like a donor (postal code, street number or amount) and never when they read like statutory prose.

**Result (September 2026):** 2019: 134 rows, 2020: 77, 2021: 104, 2022: 194, 2023: 101, 2024: 111 disclosed contributions (721 in total) across 9–15 organisations per year; about a third carry amounts. Known limits: a few rows lose the donor name to OCR (kept as "(navn ikke læsbart i kilden)" with the address), OCR misspells names occasionally ("Arbejderes Landsbank"), and the party page shows the raw line on hover so every figure can be checked against the stated page.

## Not features (by design)

- No scores, rankings or "grades" of politicians.
- No inferred positions: only recorded ballots.
- No tracking, accounts or cookies.

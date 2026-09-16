# Data sources

## oda.ft.dk — Folketingets åbne data

- Base URL `https://oda.ft.dk/api/`, **OData v3**, JSON with `$format=json`, no authentication, no documented rate limit (we cap ourselves at 4 concurrent requests and a descriptive User-Agent).
- **Page size is hard-capped at 100** rows. `$skip` works but keyset paging on `id` is used for large sets. `$expand` works (nested too, e.g. `Sagstrin/Sag`) but expanded collections are also capped at 100 with a nested `nextLink`, so ballots are fetched from the `Stemme` set directly.
- `$inlinecount=allpages` returns `odata.count` (as a string). `$filter` on navigation properties works (`Møde/dato lt datetime'2009-01-01'`, `FraAktør/typeid eq 4`).
- Use `%24` for `$` in hand-built URLs; entity names with æ/ø/å must be percent-encoded (`Akt%C3%B8r`).
- Timestamps have no offset and are Danish local time; stored as `timestamp without time zone`.
- Ids are not dense: `AktørAktør` ids reach 57 million for 164k rows, so a full load walks many empty id ranges (cheap: one request each). `Oda:RangeSize` controls the range width.
- Text fields have no length guarantees (an actor short name exceeded 32 characters), so all API-sourced strings are stored as `text`.

### Entities used (counts, September 2026)

| Entity set | Rows | Notes |
|---|---|---|
| `Afstemning` | 10.5k | one roll-call vote; `konklusion` is the official text with party positions; `vedtaget` is the outcome |
| `Stemme` | 1.84M | one row per member per vote; `typeid` 1 For, 2 Imod, 3 Fravær, 4 Hverken for eller imod |
| `Aktør` | 18k | persons (typeid 5), parliamentary groups (4, **one per session** with dates), committees (3), ministries… `biografi` is XML (party, constituency, portrait URL) |
| `AktørAktør` | 164k | relations; `rolleid` 15 = medlem. Group→person links carry start/end dates back to 1953 (2.1k have null dates; the group's session dates are used then) |
| `Sag` | 99k | cases; `typeid` 3 lovforslag, 5 beslutningsforslag; `resume` is the official summary; `nummer` like "L 41" |
| `Sagstrin` | 224k | steps (1./2./3. behandling etc.); votes hang off steps |
| `SagAktør` | 424k | case ↔ actor with role (19 forslagsstiller (reg.), 16 (priv.), 14 minister, 6 ministerområde) |
| `Møde` | 14k | sittings; votes reference the chamber sitting and its date and `periodeid` |
| `Periode` | 162 | sessions back to 1952; `kode` like `20231` for 2023-24 |
| code tables | small | Aktørtype, AktørAktørRolle, Sagstype, Sagsstatus, Sagskategori, Sagstrinstype, Sagstrinsstatus, SagAktørRolle, Afstemningstype, Stemmetype, Mødetype, Mødestatus → single `lookups` table |

Vote records with individual ballots exist from **7 October 2004** onwards (session 2004-05, about 179 ballots per vote from the start). Older cases exist but without votes.

Afstemningstype: 1 Endelig vedtagelse, 2 Udvalgsindstilling, 3 Forslag til vedtagelse, 4 Ændringsforslag.

### Historical party affiliation

Parliamentary groups are separate `Aktør` rows per session (e.g. Radikale Venstre has ids 405, 406, 415, 269 for 2011-12 … 2013-14), each with `periodeid`, `startdato`, `slutdato` and `gruppenavnkort` (S, V, RV, …). Membership is the `AktørAktør` relation role 15 from the group to the person. **That relation is missing entirely for ~96 of ~776 voting members** (their relations only cover committees; `gruppenavnkort` on persons is empty too), which left ~20 % of ballots unattributed. The member biography XML (`<constituencies><constituency>Folketingsmedlem for {party} i {constituency}, {date} – {date}.</constituency>`) is complete and is used as the fallback (`biography_memberships`, source 2 in `party_memberships`). A ballot is attributed to the group the member belonged to on the sitting date, preferring the relation source. Members outside any group appear as `UFG` (Uden for folketingsgrupperne).

## folketingstidende.dk

Bill texts, committee reports and transcripts. Unlike ft.dk it serves PDFs and HTML to scripts. URLs are deterministic from the session code and case number, e.g.
`https://www.folketingstidende.dk/samling/20231/lovforslag/L41/index.htm` and
`https://www.folketingstidende.dk/ripdf/samling/20231/lovforslag/l41/20231_l41_som_fremsat.pdf`.
The `Fil.filurl` values from the API point at `www.ft.dk/ripdf/...`, which is the same path on a host that blocks scripts (Cloudflare challenge). The app therefore builds folketingstidende links itself (`ExternalLinks` in Core).

## Party accounts (partiregnskaber)

- Legal basis: partiregnskabsloven. Parties that stood at the latest election must submit annual accounts to Folketinget within 12 months; donors giving more than 20,000 DKK (2017 level, indexed yearly) in a year must be listed with **name, address and total amount**. Anonymous donations above the threshold are banned since 1 July 2017.
- Publication: Folketinget publishes one combined PDF per year (e.g. `…/folketingets-regnskaber/partierne/partiregnskaber_2018.ashx`). **ft.dk is behind a Cloudflare bot challenge**, so the PDFs must be downloaded by a person and imported with `import-party-accounts`. No structured (non-PDF) source exists; valg.im.dk only links to ft.dk and to the 98 municipalities' own declarations.
- The parser is heuristic (each party lays out its accounts differently) and keeps the raw line and page number for every row so the site can show the exact source.

## Not used (yet)

- `Dokument`/`Fil`: document metadata (hundreds of thousands of rows). Links are built deterministically instead; the sets can be synced later if per-document listings are wanted.
- EU cases (`EUsag`), `Aktstykke`, `Dagsordenspunkt`, `Debat`.

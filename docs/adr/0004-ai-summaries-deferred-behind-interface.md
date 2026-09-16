# ADR 0004: AI summaries deferred but designed in

**Status:** accepted · **Date:** 2026-09-16

## Context
The owner wants a short neutral summary per bill eventually, but not in v1.

## Decision
Ship `ISummaryProvider` + `BillSummaryContent` in Core, a `bill_summaries` cache table, a `NullSummaryProvider` default, and a case-page slot that renders a labelled summary when one exists. The bill text source is already resolvable (`CaseDetail.BillTextPdfUrl` on folketingstidende.dk, which serves PDFs to scripts).

## Consequences
Enabling summaries is a new class plus a DI registration and a config flag; no schema or UI changes. See roadmap.md § 2 for the implementation sketch and the neutrality requirements.

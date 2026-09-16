# ADR 0002: PostgreSQL with EF Core migrations and SQL-defined statistics

**Status:** accepted · **Date:** 2026-09-16

## Context
~2M ballot rows, read-heavy public site, statistics that aggregate across the whole set, ingestion running concurrently with reads.

## Decision
PostgreSQL 17. Tables are EF Core entities with migrations. Derived data (parties, memberships, five `mv_*` materialized views) is plain SQL embedded in the Data assembly and rebuilt after each sync — not part of migrations.

## Consequences
+ Aggregations are computed once per sync, pages read pre-aggregated rows.
+ Changing a statistic is a SQL edit reviewed in a PR; no migration churn.
− Views are unavailable for a few seconds during a refresh (drop+create in one transaction); acceptable for a nightly/hourly job.
− No FK constraints between synced tables (the API can reference rows arriving later); integrity is by construction of the sync order.

# ADR 0005: Party accounts are imported from manually downloaded PDFs

**Status:** accepted · **Date:** 2026-09-16

## Context
Folketinget publishes party accounts (with disclosed donors above the legal threshold) only as combined PDFs on ft.dk, which sits behind a Cloudflare bot challenge. No structured source exists.

## Decision
Do not attempt to bypass the challenge. Provide `import-party-accounts` that parses PDFs a person has downloaded into `data/partiregnskaber/`, keep the raw text and page for every parsed row, and show it on the party page. Treat the parser as best-effort until calibrated on real files.

## Consequences
The finance feature needs a small yearly manual step. Every displayed figure remains traceable to a page in the party's own statement.

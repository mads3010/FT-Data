# ADR 0001: .NET 10 with Blazor static server-side rendering

**Status:** accepted · **Date:** 2026-09-16

## Context
The owner asked for a .NET backend and left the frontend open. Requirements: cross-platform reach, maintainable single-language code, pages that are linkable and indexable (transparency content must be shareable), modest hosting.

## Decision
One C# solution. The web app is a Blazor Web App in static SSR mode: no WebAssembly, no SignalR circuit, enhanced navigation only. Charts are HTML/CSS. The site is a PWA.

## Consequences
+ One language, one test framework, trivial hosting (a single ASP.NET Core process).
+ First paint is HTML; works without JavaScript; SEO-friendly.
− No rich client interactivity; anything interactive later needs explicit render modes (kept to a minimum by design).
− Native app-store presence needs a wrapper (MAUI Blazor Hybrid / Capacitor) — see roadmap.

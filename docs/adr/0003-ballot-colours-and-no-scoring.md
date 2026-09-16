# ADR 0003: Objectivity rules — no scores, validated neutral colours

**Status:** accepted · **Date:** 2026-09-16

## Context
The goal is legislative transparency, not persuasion. Visual encodings carry meaning: green/red for for/against would imply good/bad.

## Decision
- Show only official records and simple counts; every formula is stated on `/om` and next to the figure.
- No rankings, grades or composite indices. Absence is shown with the caveat that it has many legitimate causes.
- Ballot colours are a colour-vision-safe categorical trio (blue/orange/aqua) plus hatched gray for absent; validated with the data-viz palette checker in light and dark mode. Party colours (media convention) are identity swatches only and never encode outcomes.
- Machine-generated summaries, when enabled, are labelled, versioned and linked to the source text.

## Consequences
Some "engaging" features (leaderboards, rebel rankings) are deliberately out of scope. Dissent lists are per vote and per member, never aggregated into a score.

# deploy/

- `docker-compose.prod.yml` — single-host stack (Postgres + web + one-shot ingest). See `docs/deployment.md`.
- `share.sh` — expose the locally running app through a temporary Cloudflare quick tunnel (no account needed).

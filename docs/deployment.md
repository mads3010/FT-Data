# Deployment options

The app is one ASP.NET Core process plus PostgreSQL (about 600 MB with the full history; roughly half of that is the
`mv_ballots` materialized view and indexes). Ingestion is a CLI run on a schedule. Nothing else is required.

## Now: temporary share link from a laptop (free, no account)

`deploy/share.sh` starts the site and a Cloudflare *quick tunnel* (`cloudflared tunnel --url`). You get a random
`https://….trycloudflare.com` URL that works while the script runs and the machine is awake. Good for showing a friend;
not a public deployment (the URL changes on every start and there is no uptime guarantee).

## Permanent, low-traffic, cheapest honest option: one small VPS

A €4–5/month VPS (Hetzner CX22, DigitalOcean, etc.) with Docker runs `deploy/docker-compose.prod.yml`
(Postgres + web + ingest), an hourly cron for `ingest sync`, and Caddy in front for automatic HTTPS.
Full control, no free-tier expiry, the whole database fits with room to spare. Steps:

```bash
git clone https://github.com/mads3010/FT-Data.git /srv/folketinget && cd /srv/folketinget
export POSTGRES_PASSWORD=$(openssl rand -hex 16)
docker compose -f deploy/docker-compose.prod.yml up -d --build db web
docker compose -f deploy/docker-compose.prod.yml run --rm ingest migrate
docker compose -f deploy/docker-compose.prod.yml run --rm ingest sync        # first full load, ~10 minutes
# cron: 0 * * * *  cd /srv/folketinget && docker compose -f deploy/docker-compose.prod.yml run --rm ingest sync
```

## Free tiers (each has a real caveat)

| Provider | Free offer (2026) | Caveat for this app |
|---|---|---|
| **Render** | 750 free web-service hours/month, native .NET or Docker; one free Postgres, 1 GB, no backups | Free Postgres **expires after 30 days** (deleted after a further 14); web service sleeps when idle (cold start ~30 s). Fine for a demo month. |
| **Neon / Supabase** (Postgres only) | 0.5 GB storage on the free plan | Database is 600 MB; would need `mv_ballots` turned into a plain view (feasible, see roadmap) to fit. Pair with Render/Fly for the web app. |
| **Fly.io** | Small allowances / trial credit for new orgs; Docker-first | No longer a guaranteed free tier; Postgres is unmanaged ("Fly Postgres" is a VM you run). |
| **Railway** | Trial credit, then usage-based (~$5/month) | Not free after the trial. |
| **Azure App Service F1 + Azure Database for PostgreSQL free offer** | F1 is free (60 CPU-min/day); Postgres flexible server has a 12-month free offer for new accounts | Needs an Azure account with a card; F1 is slow and sleeps; DB offer time-limited. |
| **GitHub Pages (static export)** | Free | Would require pre-rendering ~15k pages and dropping search filters; not built. |

All of these need an account created by you (and usually a card on file). Push-to-deploy works with the Dockerfiles in
`src/*/Dockerfile`; set `ConnectionStrings__Folketinget` as an environment variable and run `ingest migrate` then
`ingest sync` once against the hosted database (from your laptop is fine: the sync only needs the connection string).

## Checklist for any host

- Set `ConnectionStrings__Folketinget`; nothing else is required.
- Run `ingest migrate` once, then `ingest sync` (first run is the full backfill), then schedule `ingest sync` hourly.
- Put TLS in front (platform-provided or Caddy). The app sends HSTS when not in Development.
- Health endpoint: `/health`. Data freshness: `/api/v1/status`.

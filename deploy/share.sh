#!/usr/bin/env bash
# Share the local site with someone for a while: runs the web app in Production mode on port 5080
# and opens a temporary Cloudflare quick tunnel (https://<random>.trycloudflare.com, no account).
# The link works only while this script runs and this machine is awake. Requires: cloudflared (brew install cloudflared),
# a running PostgreSQL with data (see README).
set -euo pipefail
cd "$(dirname "$0")/.."
# publish (not build) so static assets are bundled with a correct manifest
dotnet publish src/FolketingetVotes.Web -c Release -o /tmp/folketinget-web -v q --nologo
ASPNETCORE_ENVIRONMENT=Production ASPNETCORE_URLS=http://localhost:5080 dotnet /tmp/folketinget-web/FolketingetVotes.Web.dll &
WEB=$!
trap 'kill $WEB 2>/dev/null || true' EXIT
sleep 3
cloudflared tunnel --url http://localhost:5080

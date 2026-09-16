#!/usr/bin/env bash
# Share the local site with someone for a while: runs the web app in Production mode on port 5080
# and opens a temporary Cloudflare quick tunnel (https://<random>.trycloudflare.com, no account).
# The link works only while this script runs and this machine is awake. Requires: cloudflared (brew install cloudflared),
# a running PostgreSQL with data (see README).
set -euo pipefail
cd "$(dirname "$0")/.."
dotnet build src/FolketingetVotes.Web -c Release -v q --nologo
ASPNETCORE_ENVIRONMENT=Production ASPNETCORE_URLS=http://localhost:5080 dotnet src/FolketingetVotes.Web/bin/Release/net10.0/FolketingetVotes.Web.dll &
WEB=$!
trap 'kill $WEB 2>/dev/null || true' EXIT
sleep 3
cloudflared tunnel --url http://localhost:5080

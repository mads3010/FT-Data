#!/usr/bin/env bash
# Installs a launchd user agent that runs `ingest sync` every hour on this Mac (logs in ~/Library/Logs/folketinget-votes).
# Publishes the ingest tool to ~/.local/share/folketinget-votes/ingest first. Re-run after code changes.
# Uninstall: launchctl bootout gui/$(id -u)/dk.folketingetvotes.sync && rm ~/Library/LaunchAgents/dk.folketingetvotes.sync.plist
set -euo pipefail
REPO="$(cd "$(dirname "$0")/../.." && pwd)"
TARGET="$HOME/.local/share/folketinget-votes/ingest"
LOGS="$HOME/Library/Logs/folketinget-votes"
PLIST="$HOME/Library/LaunchAgents/dk.folketingetvotes.sync.plist"
DOTNET="$(command -v dotnet)"
mkdir -p "$TARGET" "$LOGS" "$HOME/Library/LaunchAgents"
dotnet publish "$REPO/src/FolketingetVotes.Ingest" -c Release -o "$TARGET" -v q --nologo
cat > "$PLIST" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>Label</key><string>dk.folketingetvotes.sync</string>
    <key>ProgramArguments</key>
    <array>
        <string>$DOTNET</string>
        <string>$TARGET/FolketingetVotes.Ingest.dll</string>
        <string>sync</string>
    </array>
    <key>WorkingDirectory</key><string>$TARGET</string>
    <key>StartInterval</key><integer>3600</integer>
    <key>RunAtLoad</key><true/>
    <key>StandardOutPath</key><string>$LOGS/sync.log</string>
    <key>StandardErrorPath</key><string>$LOGS/sync.err.log</string>
    <key>EnvironmentVariables</key>
    <dict>
        <key>DOTNET_CLI_TELEMETRY_OPTOUT</key><string>1</string>
        <key>PATH</key><string>/usr/local/bin:/opt/homebrew/bin:/usr/bin:/bin</string>
    </dict>
</dict>
</plist>
PLIST
launchctl bootout "gui/$(id -u)/dk.folketingetvotes.sync" 2>/dev/null || true
launchctl bootstrap "gui/$(id -u)" "$PLIST"
echo "Installed: hourly sync via launchd (dk.folketingetvotes.sync). Logs: $LOGS"

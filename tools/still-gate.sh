#!/bin/zsh
# Write an Exhibition still request for the open Unity editor, then Play.
# Personal Unity cannot -batchmode. PNGs land in unity/Temp/gs-stills/.
set -euo pipefail
root="$(cd "$(dirname "$0")/.." && pwd)"
temp="$root/unity/Temp"
mkdir -p "$temp/gs-stills"
rm -f "$temp/gs-still-done.json"
cat > "$temp/gs-still-request.json" <<'JSON'
{"shots":["title","select","plate","mound","diamond-grounder","smash"],"home":"rio","away":"ashlord","hudOff":true,"charge01":1,"width":1920,"height":1080}
JSON
echo "wrote $temp/gs-still-request.json"
if ! pgrep -x Unity >/dev/null; then
  echo "Unity editor is not running. Open grand-sluggers/unity, HarborDiamond, then Play (or menu Grand Sluggers → Capture Still Gate)."
  exit 0
fi
osascript <<'APPLESCRIPT' || true
tell application "Unity" to activate
delay 0.4
tell application "System Events" to keystroke "p" using command down
APPLESCRIPT
echo "Play sent. Wait for $temp/gs-still-done.json"

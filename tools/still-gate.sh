#!/bin/zsh
# Write an Exhibition still request for the open Unity editor, then Play.
# Personal Unity cannot -batchmode. PNGs land in unity/Temp/gs-stills/.
# Cmd+P is stolen (Grok / Safari). Click Grand Sluggers → Capture Still Gate.
set -euo pipefail
root="$(cd "$(dirname "$0")/.." && pwd)"
temp="$root/unity/Temp"
mkdir -p "$temp/gs-stills"
rm -f "$temp/gs-still-done.json"
cat > "$temp/gs-still-request.json" <<'JSON'
{"shots":["title","select","lineup","plate","pitch","mound","diamond-grounder","smash"],"home":"rio","away":"ashlord","hudOff":true,"charge01":1,"width":1920,"height":1080}
JSON
echo "wrote $temp/gs-still-request.json"
if ! pgrep -x Unity >/dev/null; then
  echo "Unity editor is not running. Open grand-sluggers/unity, HarborDiamond, then menu Grand Sluggers → Capture Still Gate."
  exit 0
fi
click_gate() {
  osascript <<'APPLESCRIPT'
tell application "Unity" to activate
delay 0.4
tell application "System Events"
  tell process "Unity"
    set frontmost to true
    delay 0.15
    click menu item "Capture Still Gate" of menu "Grand Sluggers" of menu bar 1
  end tell
end tell
APPLESCRIPT
}
# First click while Play is on only stops Play (delayCall can be lost).
click_gate || true
sleep 2
if [[ ! -f "$temp/gs-still-done.json" ]]; then
  click_gate || true
fi
echo "Capture Still Gate clicked. Wait for $temp/gs-still-done.json"

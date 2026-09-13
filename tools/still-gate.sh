#!/bin/zsh
# Exhibition stills — in-game half of the dual-still rail for Harbor kit (#651).
# Personal Unity cannot -batchmode. PR stills: scratchpad/stills/{shot}.png.
# DCC pair: tools/dcc-still.sh harbor. Cmd+P is stolen (Grok / Safari).
set -euo pipefail
root="$(cd "$(dirname "$0")/.." && pwd)"
temp="$root/unity/Temp"
dest="$root/scratchpad/stills"
mkdir -p "$temp/gs-stills" "$dest"
rm -f "$temp/gs-still-done.json"
cat > "$temp/gs-still-request.json" <<'JSON'
{"shots":["title","select","lineup","plate","pitch","mound","diamond-grounder","smash"],"home":"rio","away":"ashlord","hudOff":true,"charge01":1,"width":1920,"height":1080}
JSON
echo "wrote $temp/gs-still-request.json"
copy_named() {
  local copied=0
  if ls "$temp/gs-stills"/*.png >/dev/null 2>&1; then
    cp "$temp/gs-stills"/*.png "$dest/"
    copied=1
  fi
  echo "PR stills: $dest/plate.png $dest/mound.png $dest/diamond-grounder.png"
  if [[ $copied -eq 0 ]]; then
    echo "Copy those from $temp/gs-stills/ when capture finishes. DCC pair: tools/dcc-still.sh harbor"
  fi
}

if ! pgrep -x Unity >/dev/null; then
  echo "Unity editor is not running. Open grand-sluggers/unity, HarborDiamond, then menu Grand Sluggers → Capture Still Gate."
  copy_named
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
copy_named

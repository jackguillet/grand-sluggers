#!/bin/zsh
# Character turntable stills. PNGs: unity/Temp/gs-stills/char-{id}-rest.png
# and char-{id}-pose.png. Humans pass look. Agents do not.
set -euo pipefail
root="$(cd "$(dirname "$0")/.." && pwd)"
temp="$root/unity/Temp"
id="${1:-fenn}"
mkdir -p "$temp/gs-stills"
rm -f "$temp/gs-still-done.json"
cat > "$temp/gs-still-request.json" <<JSON
{"shots":["char-rest","char-pose"],"home":"${id}","away":"rio","hudOff":true,"width":1920,"height":1080}
JSON
echo "wrote $temp/gs-still-request.json home=$id"
if ! pgrep -x Unity >/dev/null; then
  echo "Unity editor is not running. Open grand-sluggers/unity, HarborDiamond, then menu Grand Sluggers → Capture Character Stills."
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
    click menu item "Capture Character Stills" of menu "Grand Sluggers" of menu bar 1
  end tell
end tell
APPLESCRIPT
}
click_gate || true
sleep 2
if [[ ! -f "$temp/gs-still-done.json" ]]; then
  click_gate || true
fi
echo "Capture Character Stills clicked. Wait for $temp/gs-still-done.json"

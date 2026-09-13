#!/bin/zsh
# Character turntable stills — in-game half of the dual-still rail (#651).
# PR stills: scratchpad/stills/char-{id}-rest.png and char-{id}-pose.png.
# DCC pair: tools/dcc-still.sh. Humans pass look. Agents file and stop.
set -euo pipefail
root="$(cd "$(dirname "$0")/.." && pwd)"
temp="$root/unity/Temp"
dest="$root/scratchpad/stills"
id="fenn"
copy_only=0
for arg in "$@"; do
  case "$arg" in
    --copy) copy_only=1 ;;
    --help|-h)
      echo "usage: tools/still-gate-character.sh [id] [--copy]"
      echo "PR stills: $dest/char-{id}-rest.png $dest/char-{id}-pose.png"
      echo "DCC pair: tools/dcc-still.sh"
      exit 0
      ;;
    *) id="$arg" ;;
  esac
done
mkdir -p "$temp/gs-stills" "$dest"

copy_named() {
  local copied=0
  for f in "$temp/gs-stills/char-${id}-rest.png" "$temp/gs-stills/char-${id}-pose.png"; do
    if [[ -f "$f" ]]; then
      cp "$f" "$dest/"
      copied=1
    fi
  done
  echo "PR stills: $dest/char-${id}-rest.png $dest/char-${id}-pose.png"
  if [[ $copied -eq 0 ]]; then
    echo "Copy those from $temp/gs-stills/ when capture finishes. DCC pair: tools/dcc-still.sh"
  fi
}

if [[ $copy_only -eq 1 ]]; then
  copy_named
  exit 0
fi

rm -f "$temp/gs-still-done.json"
cat > "$temp/gs-still-request.json" <<JSON
{"shots":["char-rest","char-pose"],"home":"${id}","away":"rio","hudOff":true,"width":1920,"height":1080}
JSON
echo "wrote $temp/gs-still-request.json home=$id"
if ! pgrep -x Unity >/dev/null; then
  echo "Unity editor is not running. Open grand-sluggers/unity, HarborDiamond, then menu Grand Sluggers → Capture Character Stills."
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
copy_named

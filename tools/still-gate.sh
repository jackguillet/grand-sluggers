#!/bin/zsh
# Exhibition stills — in-game half of the dual-still rail for Harbor kit (#651).
# Personal Unity cannot -batchmode. PR stills: scratchpad/stills/{shot}.png;
# --park / --night name themselves in the file ({shot}-{park}-night.png).
# DCC pair: tools/dcc-still.sh harbor. Cmd+P is stolen (Grok / Safari).
set -euo pipefail
root="$(cd "$(dirname "$0")/.." && pwd)"
temp="$root/unity/Temp"
dest="$root/scratchpad/stills"
park=""
night=""
while [[ $# -gt 0 ]]; do
  case "$1" in
    --park)
      if [[ $# -lt 2 || "$2" == --* ]]; then
        echo "--park needs a park id (see --help)" >&2
        exit 2
      fi
      park="$2"; shift 2 ;;
    --park=*) park="${1#*=}"; shift ;;
    --night) night=1; shift ;;
    --help|-h)
      echo "usage: tools/still-gate.sh [--park <id>] [--night]"
      echo "park ids: the catalog in ExhibitionPick.Parks. No --park is the default park."
      echo "PR stills: $dest/{shot}.png ({shot}-{park}.png, {shot}-{park}-night.png away from the default park)"
      exit 0
      ;;
    *) echo "unknown argument: $1 (see --help)" >&2; exit 2 ;;
  esac
done
mkdir -p "$temp/gs-stills" "$dest"
rm -f "$temp/gs-still-done.json"
# No flag must write today's request byte for byte, so the character gate and the
# existing stills do not move (StillRequestTests pins this line).
request='{"shots":["title","select","lineup","plate","pitch","mound","diamond-grounder","smash"],"home":"rio","away":"ashlord","hudOff":true,"charge01":1,"width":1920,"height":1080}'
extra=""
if [[ -n "$park" ]]; then
  extra="$extra,\"park\":\"$park\""
fi
if [[ -n "$night" ]]; then
  extra="$extra,\"night\":true"
fi
if [[ -n "$extra" ]]; then
  request="${request%\}}$extra}"
fi
printf '%s\n' "$request" > "$temp/gs-still-request.json"
echo "wrote $temp/gs-still-request.json${park:+ park=$park}${night:+ night=on}"
copy_named() {
  local copied=0
  if ls "$temp/gs-stills"/*.png >/dev/null 2>&1; then
    cp "$temp/gs-stills"/*.png "$dest/"
    copied=1
  fi
  echo "PR stills: $dest/plate.png $dest/mound.png $dest/diamond-grounder.png (a park or night run names itself in the file)"
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

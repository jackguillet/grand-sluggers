#!/bin/zsh
# Exhibition stills — in-game half of the dual-still rail for Harbor kit (#651).
# Personal Unity cannot -batchmode. PR stills: scratchpad/stills/{shot}.png;
# --park / --night name themselves in the file ({shot}-{park}-night.png).
# DCC pair: tools/dcc-still.sh harbor. Cmd+P is stolen (Grok / Safari).
# One GUI Unity user on this Mac at a time: the capture holds tools/unity_gui.py's lock until the
# editor open on this worktree writes gs-still-done.json, and fronts and clicks that editor by PID.
set -euo pipefail
root="$(cd "$(dirname "$0")/.." && pwd)"
temp="$root/unity/Temp"
dest="$root/scratchpad/stills"
park=""
night=""
timeout=600
player_ok=()
gui() { python3 "$root/tools/unity_gui.py" "$@"; }
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
    --timeout|--timeout=*)
      if [[ "$1" == --timeout ]]; then timeout="${2:-}"; shift; else timeout="${1#*=}"; fi
      shift
      if [[ ! "$timeout" =~ ^[0-9]+$ ]]; then
        echo "--timeout needs whole seconds (see --help)" >&2
        exit 2
      fi ;;
    --player-open-ok) player_ok=(--player-open-ok); shift ;;
    --help|-h)
      echo "usage: tools/still-gate.sh [--park <id>] [--night] [--timeout <seconds>] [--player-open-ok]"
      echo "park ids: the park files in data/parks (the catalog). No --park is the default park."
      echo "PR stills: $dest/{shot}.png ({shot}-{park}.png, {shot}-{park}-night.png away from the default park)"
      echo "Clicks the GUI Unity editor open on $root/unity and holds the machine-wide GUI Unity lock"
      echo "until the capture is done (default ${timeout}s). A live holder refuses the run by name."
      echo "--player-open-ok: Jack said the machine is free, so fronting the editor may take focus from his game window."
      exit 0
      ;;
    *) echo "unknown argument: $1 (see --help)" >&2; exit 2 ;;
  esac
done
# Take the lock before anything touches Unity or the request (docs/editor-startup.md).
gui acquire --pid $$ --worktree "$root" --focus "${player_ok[@]}" \
  --purpose "tools/still-gate.sh${park:+ --park $park}${night:+ --night}" || exit $?
trap 'gui release --pid $$' EXIT
trap 'exit 129' HUP
trap 'exit 130' INT
trap 'exit 143' TERM
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

editor="$(gui editor "$root/unity")" || editor=""
if [[ -z "$editor" ]]; then
  echo "No Unity editor is open on $root/unity. Open it on HarborDiamond, then re-run, or use menu Grand Sluggers → Capture Still Gate."
  copy_named
  exit 0
fi
click_gate() {
  gui menu --owner $$ "$editor" "Grand Sluggers" "Capture Still Gate"
}
wait_done() {
  local waited=0
  while [[ ! -f "$temp/gs-still-done.json" ]]; do
    if ! kill -0 "$editor" 2>/dev/null; then
      echo "Editor $editor quit before the capture finished." >&2
      return 1
    fi
    if (( waited >= timeout )); then
      echo "The capture did not finish in ${timeout}s." >&2
      return 1
    fi
    sleep 2
    (( waited += 2 ))
  done
}
# First click while Play is on only stops Play (delayCall can be lost).
click_gate || true
sleep 2
if [[ ! -f "$temp/gs-still-done.json" ]]; then
  click_gate || true
fi
echo "Capture Still Gate clicked in editor $editor. Holding the GUI Unity lock until $temp/gs-still-done.json (up to ${timeout}s)."
if ! wait_done; then
  copy_named
  exit 1
fi
copy_named

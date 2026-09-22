#!/bin/zsh
# Character turntable stills — in-game half of the dual-still rail (#651).
# PR stills: scratchpad/stills/char-{id}-rest.png and char-{id}-pose.png.
# DCC pair: tools/dcc-still.sh. Humans pass look. Agents file and stop.
# One GUI Unity user on this Mac at a time: the capture holds tools/unity_gui.py's lock until the
# editor open on this worktree writes gs-still-done.json, and fronts and clicks that editor by PID.
set -euo pipefail
root="$(cd "$(dirname "$0")/.." && pwd)"
temp="$root/unity/Temp"
dest="$root/scratchpad/stills"
id="fenn"
copy_only=0
timeout=600
player_ok=()
gui() { python3 "$root/tools/unity_gui.py" "$@"; }
while [[ $# -gt 0 ]]; do
  case "$1" in
    --copy) copy_only=1; shift ;;
    --timeout|--timeout=*)
      if [[ "$1" == --timeout ]]; then timeout="${2:-}"; shift; else timeout="${1#*=}"; fi
      shift
      if [[ ! "$timeout" =~ ^[0-9]+$ ]]; then
        echo "--timeout needs whole seconds (see --help)" >&2
        exit 2
      fi ;;
    --player-open-ok) player_ok=(--player-open-ok); shift ;;
    --help|-h)
      echo "usage: tools/still-gate-character.sh [id] [--copy] [--timeout <seconds>] [--player-open-ok]"
      echo "PR stills: $dest/char-{id}-rest.png $dest/char-{id}-pose.png"
      echo "DCC pair: tools/dcc-still.sh"
      echo "Clicks the GUI Unity editor open on $root/unity and holds the machine-wide GUI Unity lock"
      echo "until the capture is done (default ${timeout}s). A live holder refuses the run by name."
      echo "--player-open-ok: Jack said the machine is free, so fronting the editor may take focus from his game window."
      exit 0
      ;;
    *) id="$1"; shift ;;
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

# Take the lock before anything touches Unity or the request (docs/editor-startup.md).
gui acquire --pid $$ --worktree "$root" --focus "${player_ok[@]}" \
  --purpose "tools/still-gate-character.sh $id" || exit $?
trap 'gui release --pid $$' EXIT
trap 'exit 129' HUP
trap 'exit 130' INT
trap 'exit 143' TERM
rm -f "$temp/gs-still-done.json"
cat > "$temp/gs-still-request.json" <<JSON
{"shots":["char-rest","char-pose"],"home":"${id}","away":"rio","hudOff":true,"width":1920,"height":1080}
JSON
echo "wrote $temp/gs-still-request.json home=$id"
editor="$(gui editor "$root/unity")" || editor=""
if [[ -z "$editor" ]]; then
  echo "No Unity editor is open on $root/unity. Open it on HarborDiamond, then re-run, or use menu Grand Sluggers → Capture Character Stills."
  copy_named
  exit 0
fi
click_gate() {
  gui menu --owner $$ "$editor" "Grand Sluggers" "Capture Character Stills"
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
click_gate || true
sleep 2
if [[ ! -f "$temp/gs-still-done.json" ]]; then
  click_gate || true
fi
echo "Capture Character Stills clicked in editor $editor. Holding the GUI Unity lock until $temp/gs-still-done.json (up to ${timeout}s)."
if ! wait_done; then
  copy_named
  exit 1
fi
copy_named

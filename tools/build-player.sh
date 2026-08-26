#!/bin/zsh
# Drop a Linux player request for the already-open Unity editor, then pack
# linux/ + data/ into one tarball the agent copies to its computer.
# Personal Unity cannot -batchmode. PlayerBuildGate watches
# unity/Temp/gs-player-request.json in edit mode.
set -euo pipefail
root="$(cd "$(dirname "$0")/.." && pwd)"
temp="$root/unity/Temp"
builds="$root/unity/Builds"
mkdir -p "$temp" "$builds"

pack_player() {
  local exe="$builds/linux/GrandSluggers.x86_64"
  if [[ ! -x "$exe" ]]; then
    echo "no player at $exe" >&2
    return 1
  fi
  rm -rf "$builds/data"
  cp -R "$root/data" "$builds/data"
  # macOS AppleDouble + Unity debug junk the Linux loader chokes on
  find "$builds/linux" "$builds/data" -name '._*' -delete
  find "$builds/linux" -name '*_s.debug' -delete
  find "$builds/linux" -name '*.pdb' -delete
  find "$builds/linux" -type d -name 'BurstDebugInformation_DoNotShip' -prune -exec rm -rf {} +
  COPYFILE_DISABLE=1 tar -C "$builds" -czf "$builds/gs-linux-player.tar.gz" linux data
  ls -lh "$builds/gs-linux-player.tar.gz"
  echo "packed $builds/gs-linux-player.tar.gz (linux/ + data/ as siblings)"
}

if [[ "${1:-}" == "pack" ]]; then
  pack_player
  exit 0
fi

rm -f "$temp/gs-player-done.json"
cat > "$temp/gs-player-request.json" <<'JSON'
{"target":"linux","width":1280,"height":800,"development":true}
JSON
echo "wrote $temp/gs-player-request.json"
if ! pgrep -x Unity >/dev/null; then
  echo "Unity editor is not running. Open grand-sluggers/unity, then re-run."
  exit 0
fi
echo "Waiting for $temp/gs-player-done.json (editor builds on the next tick)."
for i in {1..180}; do
  if [[ -f "$temp/gs-player-done.json" ]]; then
    cat "$temp/gs-player-done.json"
    echo
    if grep -q '"ok":true' "$temp/gs-player-done.json"; then
      pack_player
    else
      echo "build failed, not packing" >&2
      exit 1
    fi
    exit 0
  fi
  sleep 2
done
echo "timed out waiting for the Linux player. Is the editor in edit mode on HarborDiamond?"
exit 1

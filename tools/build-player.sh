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
revision="$(git -C "$root" rev-parse HEAD)"

if [[ -n "$(git -C "$root" status --porcelain --untracked-files=no)" ]]; then
  echo "tracked changes make revision evidence ambiguous; commit them before building" >&2
  exit 1
fi

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
printf '{"target":"linux","width":1280,"height":800,"development":true,"revision":"%s"}\n' "$revision" > "$temp/gs-player-request.json"
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
    if python3 -c 'import json,sys; d=json.load(open(sys.argv[1])); assert d.get("ok") is True; assert d.get("revision")==sys.argv[2]; assert d.get("scene")=="Assets/Scenes/HarborDiamond.unity"' "$temp/gs-player-done.json" "$revision"; then
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

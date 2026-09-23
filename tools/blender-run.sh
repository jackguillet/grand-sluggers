#!/bin/zsh
# Fail before Blender's Metal startup when the calling process cannot see a GPU.
set -euo pipefail
root="$(cd "$(dirname "$0")/.." && pwd)"
blender_bin="${BLENDER:-/opt/homebrew/bin/blender}"
if [[ "$(uname -s)" == Darwin ]]; then
  probe_dir="$(mktemp -d "${TMPDIR:-/tmp}/gs-blender-metal.XXXXXX")"
  trap 'rm -rf "$probe_dir"' EXIT
  /usr/bin/clang -fobjc-arc -framework Foundation -framework Metal \
    "$root/tools/native/metal-device-probe.m" -o "$probe_dir/probe"
  if ! "$probe_dir/probe"; then
    echo "blender-run: Blender was not started. Run with approved GPU access outside the sandbox; do not retry the same restricted launch." >&2
    exit 78
  fi
  rm -rf "$probe_dir"
  trap - EXIT
fi
[[ "${1:-}" == --check ]] && exit 0
[[ -x "$blender_bin" ]] || { echo "blender-run: executable not found: $blender_bin (set BLENDER)" >&2; exit 127; }
exec "$blender_bin" "$@"

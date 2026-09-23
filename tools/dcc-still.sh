#!/bin/zsh
# Named DCC stills into scratchpad/stills/. Pair with still-gate-character.sh
# (or still-gate.sh for Harbor kit). Humans pass look. Agents file and stop.
set -euo pipefail
root="$(cd "$(dirname "$0")/.." && pwd)"
drop="$root/scratchpad/stills"
takes="$root/scratchpad/takes"
B="$root/tools/blender-run.sh"
kind=""
clip="swing"
print_only=0

usage() {
  echo "usage: tools/dcc-still.sh body|extras|takes [clip]|harbor [--print]"
  echo "PR still: $drop/dcc-body.png (etc). In-game pair: tools/still-gate-character.sh."
}

for arg in "$@"; do
  case "$arg" in
    --print) print_only=1 ;;
    --help|-h) usage; exit 0 ;;
    body|extras|takes|harbor|harbor-kit)
      if [[ -z "$kind" ]]; then kind="$arg"; else clip="$arg"; fi
      ;;
    *)
      if [[ -z "$kind" ]]; then kind="$arg"; else clip="$arg"; fi
      ;;
  esac
done

[[ "$kind" == "harbor" ]] && kind="harbor-kit"

named=""
case "$kind" in
  body) named="dcc-body.png" ;;
  extras) named="dcc-extras.png" ;;
  takes) named="dcc-${clip}.png" ;;
  harbor-kit) named="dcc-harbor-kit.png" ;;
  *) usage; exit 1 ;;
esac

mkdir -p "$drop" "$takes"
echo "PR still: $drop/$named"
if [[ $print_only -eq 1 ]]; then
  exit 0
fi

if [[ ! -x "$B" ]]; then
  echo "Blender not found at $B. Restore tools/blender-run.sh; set BLENDER to override its Blender executable."
  echo "Then re-run this script so $drop/$named exists before the in-game pair."
  exit 1
fi

case "$kind" in
  body)
    "$B" -b --python "$root/tools/blender/hero_shared_blockout.py" -- \
      --out "$root/unity/Assets/Art/Characters/SharedRig/hero-shared.fbx" \
      --resources "$root/unity/Assets/Resources/Art/Characters/SharedRig" \
      --clay "$takes"
    cp "$takes/body.png" "$drop/$named"
    ;;
  extras)
    "$B" -b --python "$root/tools/blender/hero_shared_extras.py" -- \
      --out "$root/unity/Assets/Art/Characters/SharedRig/extras.fbx" \
      --resources "$root/unity/Assets/Resources/Art/Characters/SharedRig" \
      --clay "$takes"
    cp "$takes/extras.png" "$drop/$named"
    ;;
  takes)
    "$B" -b --python "$root/tools/blender/hero_shared_takes.py" -- \
      --out "$root/unity/Assets/Art/Animation/Clips" \
      --resources "$root/unity/Assets/Resources/Art/Animation/Clips" \
      --sheets "$takes" --only "$clip"
    cp "$takes/${clip}.png" "$drop/$named"
    ;;
  harbor-kit)
    "$B" -b --python "$root/tools/blender/harbor_kit.py" -- \
      --out "$root/unity/Assets/Art/Parks/harbor-diamond/harbor-kit.fbx" \
      --resources "$root/unity/Assets/Resources/Art/Parks/harbor-diamond" \
      --clay "$takes"
    cp "$takes/harbor-kit.png" "$drop/$named"
    ;;
esac

echo "wrote $drop/$named"

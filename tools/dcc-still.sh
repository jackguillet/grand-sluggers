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
  echo "usage: tools/dcc-still.sh body|extras|takes [clip]|harbor|park <park-id>|lineup [--print]"
  echo "PR still: $drop/dcc-body.png (etc). In-game pair: tools/still-gate-character.sh."
}

for arg in "$@"; do
  case "$arg" in
    --print) print_only=1 ;;
    --help|-h) usage; exit 0 ;;
    body|extras|takes|harbor|harbor-kit|lineup)
      if [[ -z "$kind" ]]; then kind="$arg"; else clip="$arg"; fi
      ;;
    *)
      if [[ -z "$kind" ]]; then kind="$arg"; else clip="$arg"; fi
      ;;
  esac
done

[[ "$kind" == "harbor" ]] && kind="harbor-kit"
# A park's kit (F7-b): Harbor's is harbor_kit.py; any other park has no DCC kit until its art stage (F9-c, after
# its greybox sitting), so its lane names the still and refuses to invent one.
if [[ "$kind" == "park" ]]; then
  if [[ "$clip" == "harbor-diamond" ]]; then kind="harbor-kit"
  elif [[ ! -f "$root/tools/blender/park_kit_${clip}.py" && $print_only -eq 0 ]]; then
    echo "park $clip has no DCC kit yet (tools/blender/park_kit_${clip}.py): its greybox is drawn from data in Unity;"
    echo "the DCC half of its dual still arrives with its art stage, after Jack's greybox sitting (F9-c)."
    exit 2
  fi
fi

named=""
case "$kind" in
  body) named="dcc-body.png" ;;
  lineup) named="dcc-lineup-turnaround.png" ;;
  extras) named="dcc-extras.png" ;;
  takes) named="dcc-${clip}.png" ;;
  harbor-kit) named="dcc-harbor-kit.png" ;;
  park) named="dcc-park-${clip}.png" ;;
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
  lineup)
    # Every captain side by side: dcc-lineup-turnaround.png, dcc-lineup-gameplay.png, dcc-lineup-gameplay-black.png.
    "$B" -b --python "$root/tools/blender/hero_lineup.py" -- --out "$drop" --prefix dcc-lineup
    ;;
  harbor-kit)
    "$B" -b --python "$root/tools/blender/harbor_kit.py" -- \
      --out "$root/unity/Assets/Art/Parks/harbor-diamond/harbor-kit.fbx" \
      --resources "$root/unity/Assets/Resources/Art/Parks/harbor-diamond" \
      --clay "$takes"
    cp "$takes/harbor-kit.png" "$drop/$named"
    ;;
  park)
    "$B" -b --python "$root/tools/blender/park_kit_${clip}.py" -- \
      --out "$root/unity/Assets/Art/Parks/${clip}/park-kit.fbx" \
      --resources "$root/unity/Assets/Resources/Art/Parks/${clip}" \
      --clay "$takes"
    cp "$takes/park-kit-${clip}.png" "$drop/$named"
    ;;
esac

echo "wrote $drop/$named"

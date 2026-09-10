#!/bin/zsh
# Licensed/configured runner gate: import the real project, compile Unity assemblies,
# validate content/assets, and open HarborDiamond. This is separate from player smoke.
set -euo pipefail

root="$(cd "$(dirname "$0")/.." && pwd)"
editor="${UNITY_EDITOR_BIN:-}"
evidence="${1:-$root/unity/Temp/validation/unity-evidence.json}"
log="${2:-$root/unity/Temp/validation/unity.log}"

die() { echo "unity-project-validate: $*" >&2; exit 1; }
[[ -n "$editor" ]] || die "set UNITY_EDITOR_BIN to the configured runner's Unity executable"
[[ -x "$editor" ]] || die "Unity executable not found: $editor"
[[ "${GS_UNITY_BATCH_LICENSED:-}" == "1" ]] ||
  die "set GS_UNITY_BATCH_LICENSED=1 only on a runner licensed and configured for Unity batch mode"
[[ -z "$(git -C "$root" status --porcelain --untracked-files=no)" ]] ||
  die "tracked changes make revision evidence ambiguous; commit them first"
revision="$(git -C "$root" rev-parse HEAD)"
mkdir -p "$(dirname "$evidence")" "$(dirname "$log")"
rm -f "$evidence"

GS_VALIDATION_REVISION="$revision" GS_VALIDATION_EVIDENCE="$evidence" \
  "$editor" -batchmode -nographics -quit -projectPath "$root/unity" \
  -executeMethod GrandSluggers.EditorTools.ValidationGate.Run -logFile "$log"

[[ -f "$evidence" ]] || die "Unity exited without evidence; see $log"
[[ -z "$(git -C "$root" status --porcelain --untracked-files=no)" ]] ||
  die "Unity import changed tracked files; evidence no longer represents a clean $revision checkout"
python3 -c 'import json,sys; p,rev=sys.argv[1:]; d=json.load(open(p)); assert d.get("ok") is True, d; assert d.get("revision")==rev, d' \
  "$evidence" "$revision" || die "evidence does not pass or match $revision; see $evidence"
echo "OK     Unity import + assemblies + art + Harbor scene at $revision"
echo "evidence $evidence"

#!/bin/zsh
# Compile Sim + Runtime + Editor the way Unity does: no implicit usings,
# Unity 6000.5.9f1 refs, langversion latest. Personal Unity cannot -batchmode;
# this is the pre-merge compile gate.
set -euo pipefail
root="$(cd "$(dirname "$0")/.." && pwd)"
unity="${UNITY_EDITOR:-/Applications/Unity/Hub/Editor/6000.5.9f1}"
engine="$unity/Unity.app/Contents/Resources/Scripting/Managed/UnityEngine"
ns="$unity/Unity.app/Contents/Resources/Scripting/NetStandard/ref/2.1.0/netstandard.dll"
bcl="$unity/Unity.app/Contents/Resources/Scripting/BCLExtensions/TargetingPacks/netstandard2.1/ref"
dotnet="$unity/Unity.app/Contents/Resources/Scripting/DotNetSdk/dotnet"
csc="$unity/Unity.app/Contents/Resources/Scripting/DotNetSdk/sdk/8.0.318/Roslyn/bincore/csc.dll"
out="$root/unity/Temp/unity-compile"
lib="$root/unity/Library"
if [[ ! -d "$lib/ScriptAssemblies" && -d /Users/jack/repos/grand-sluggers/unity/Library/ScriptAssemblies ]]; then
  lib=/Users/jack/repos/grand-sluggers/unity/Library
fi
asm="$lib/ScriptAssemblies"

die() { echo "unity-compile: $*" >&2; exit 1; }
[[ -x "$dotnet" ]] || die "Unity editor not found at $unity"
[[ -f "$csc" ]] || die "csc.dll missing"
[[ -f "$ns" ]] || die "netstandard ref missing"
[[ -d "$engine" ]] || die "UnityEngine modules missing"
mkdir -p "$out"

csc() {
  "$dotnet" exec "$csc" /nologo /nostdlib /noconfig /t:library \
    /langversion:latest /deterministic /optimize+ \
    /nowarn:0169 /nowarn:0649 /nowarn:0282 /nowarn:1701 /nowarn:1702 /nowarn:0436 /nowarn:0618 /nowarn:8632 \
    "$@"
}

refs=()
for dll in "$engine"/UnityEngine*.dll "$engine"/UnityEditor*.dll "$engine"/Unity.Scripting.dll; do
  [[ -f "$dll" ]] && refs+=(-r:"$dll")
done
refs+=(-r:"$ns")
if [[ -d "$bcl" ]]; then
  for dll in "$bcl"/*.dll; do
    [[ -f "$dll" ]] && refs+=(-r:"$dll")
  done
fi
compat="$unity/Unity.app/Contents/Resources/Scripting/NetStandard"
if [[ -d "$compat" ]]; then
  while IFS= read -r dll; do
    refs+=(-r:"$dll")
  done < <(find "$compat" -name '*.dll' | sort)
fi
for extra in Unity.InputSystem.dll Unity.RenderPipelines.Universal.Runtime.dll Unity.RenderPipelines.Core.Runtime.dll Unity.Mathematics.dll; do
  if [[ -f "$asm/$extra" ]]; then
    refs+=(-r:"$asm/$extra")
  fi
done

sim_cs=("$root"/src/GrandSluggers.Sim/*.cs)
runtime_cs=("$root"/unity/Assets/Scripts/Runtime/*.cs)
editor_cs=("$root"/unity/Assets/Editor/*.cs)

echo "unity-compile  Sim"
csc /out:"$out/GrandSluggers.Sim.dll" "${refs[@]}" "${sim_cs[@]}"
echo "unity-compile  Runtime"
csc /out:"$out/GrandSluggers.Runtime.dll" "${refs[@]}" -r:"$out/GrandSluggers.Sim.dll" "${runtime_cs[@]}"
echo "unity-compile  Editor"
csc /out:"$out/GrandSluggers.Editor.dll" "${refs[@]}" \
  -r:"$out/GrandSluggers.Sim.dll" -r:"$out/GrandSluggers.Runtime.dll" "${editor_cs[@]}"
echo "OK     Unity Sim + Runtime + Editor compile"

#!/bin/zsh
# Fast compiler emulation for the tracked Unity C# assemblies. This does not import
# the project, resolve packages, build a player, or replace tools/unity-project-validate.sh.
set -euo pipefail

root="$(cd "$(dirname "$0")/.." && pwd)"
unity="${UNITY_EDITOR:-/Applications/Unity/Hub/Editor/6000.5.9f1}"
engine="$unity/Unity.app/Contents/Resources/Scripting/Managed/UnityEngine"
ns="$unity/Unity.app/Contents/Resources/Scripting/NetStandard/ref/2.1.0/netstandard.dll"
bcl="$unity/Unity.app/Contents/Resources/Scripting/BCLExtensions/TargetingPacks/netstandard2.1/ref"
dotnet="$unity/Unity.app/Contents/Resources/Scripting/DotNetSdk/dotnet"
csc="$unity/Unity.app/Contents/Resources/Scripting/DotNetSdk/sdk/8.0.318/Roslyn/bincore/csc.dll"
out="$root/unity/Temp/unity-compile"
package_assemblies="${UNITY_PACKAGE_ASSEMBLIES:-$root/unity/Library/ScriptAssemblies}"

die() { echo "unity-compile: $*" >&2; exit 1; }

collect_cs() {
  local source_root="$1"
  shift
  reply=()
  while IFS= read -r -d '' file; do
    reply+=("$file")
  done < <(find "$source_root" "$@" -type f -name '*.cs' -print0 | sort -z)
  (( ${#reply[@]} > 0 )) || die "no C# sources under $source_root"
}

collect_cs "$root/src/GrandSluggers.Sim" ! -path '*/bin/*' ! -path '*/obj/*'
sim_cs=("${reply[@]}")
collect_cs "$root/unity/Assets/Scripts/Runtime"
runtime_cs=("${reply[@]}")
collect_cs "$root/unity/Assets/Scripts" -maxdepth 1
project_cs=("${reply[@]}")
collect_cs "$root/unity/Assets/Editor"
editor_cs=("${reply[@]}")

if [[ "${1:-}" == "--list-sources" ]]; then
  print -rl -- "${sim_cs[@]}" "${runtime_cs[@]}" "${project_cs[@]}" "${editor_cs[@]}" |
    sed "s|$root/||"
  exit 0
fi

[[ -x "$dotnet" ]] || die "Unity editor not found at $unity"
[[ -f "$csc" ]] || die "csc.dll missing under $unity"
[[ -f "$ns" ]] || die "netstandard ref missing under $unity"
[[ -d "$engine" ]] || die "UnityEngine modules missing under $unity"
[[ -d "$package_assemblies" ]] || die "package assemblies missing at $package_assemblies; import this checkout or set UNITY_PACKAGE_ASSEMBLIES to a configured runner cache"

required_packages=(
  Unity.InputSystem.dll
  Unity.RenderPipelines.Universal.Runtime.dll
  Unity.RenderPipelines.Core.Runtime.dll
)
for dll in "${required_packages[@]}"; do
  [[ -f "$package_assemblies/$dll" ]] || die "required package assembly missing: $package_assemblies/$dll"
done

mkdir -p "$out"

csc_compile() {
  "$dotnet" exec "$csc" /nologo /nostdlib /noconfig /t:library \
    /langversion:latest /deterministic /optimize+ \
    /nowarn:0169 /nowarn:0649 /nowarn:0282 /nowarn:1701 /nowarn:1702 /nowarn:0436 /nowarn:0618 /nowarn:8632 \
    "$@"
}

framework_refs=(-r:"$ns")
if [[ -d "$bcl" ]]; then
  for dll in "$bcl"/*.dll; do
    [[ -f "$dll" ]] && framework_refs+=(-r:"$dll")
  done
fi
compat="$unity/Unity.app/Contents/Resources/Scripting/NetStandard"
if [[ -d "$compat" ]]; then
  while IFS= read -r dll; do
    framework_refs+=(-r:"$dll")
  done < <(find "$compat" -type f -name '*.dll' | sort)
fi

engine_refs=()
for dll in "$engine"/UnityEngine*.dll "$engine"/Unity.Scripting.dll; do
  [[ -f "$dll" ]] && engine_refs+=(-r:"$dll")
done
package_refs=()
for dll in "${required_packages[@]}" Unity.Mathematics.dll; do
  [[ -f "$package_assemblies/$dll" ]] && package_refs+=(-r:"$package_assemblies/$dll")
done
editor_refs=()
for dll in "$engine"/UnityEditor*.dll; do
  [[ -f "$dll" ]] && editor_refs+=(-r:"$dll")
done

echo "unity-compile  GrandSluggers.Sim (${#sim_cs[@]} sources)"
csc_compile /out:"$out/GrandSluggers.Sim.dll" "${framework_refs[@]}" "${engine_refs[@]}" "${sim_cs[@]}"
echo "unity-compile  GrandSluggers.Runtime (${#runtime_cs[@]} sources)"
csc_compile /out:"$out/GrandSluggers.Runtime.dll" "${framework_refs[@]}" "${engine_refs[@]}" \
  "${package_refs[@]}" -r:"$out/GrandSluggers.Sim.dll" "${runtime_cs[@]}"
echo "unity-compile  Assembly-CSharp (${#project_cs[@]} sources)"
csc_compile /out:"$out/Assembly-CSharp.dll" "${framework_refs[@]}" "${engine_refs[@]}" \
  "${package_refs[@]}" -r:"$out/GrandSluggers.Sim.dll" -r:"$out/GrandSluggers.Runtime.dll" "${project_cs[@]}"
echo "unity-compile  GrandSluggers.Editor (${#editor_cs[@]} sources)"
csc_compile /out:"$out/GrandSluggers.Editor.dll" "${framework_refs[@]}" "${engine_refs[@]}" \
  "${editor_refs[@]}" "${package_refs[@]}" -r:"$out/GrandSluggers.Sim.dll" \
  -r:"$out/GrandSluggers.Runtime.dll" "${editor_cs[@]}"
echo "OK     narrow C# compile (Unity import/build not run)"

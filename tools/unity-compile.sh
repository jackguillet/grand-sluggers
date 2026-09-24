#!/bin/zsh
# Fast compiler emulation for the tracked Unity C# assemblies. This does not import
# the project, resolve packages, build a player, or replace tools/unity-project-validate.sh.
set -euo pipefail

root="$(cd "$(dirname "$0")/.." && pwd)"
die() { echo "unity-compile: $*" >&2; exit 1; }

# The editor is the one the project pins (unity/ProjectSettings/ProjectVersion.txt), so an upgrade moves this
# gate with it. UNITY_EDITOR names an editor folder outright; UNITY_HUB_EDITORS moves the Hub folder
# (tools/unity_gui.py reads the same two).
version_file="$root/unity/ProjectSettings/ProjectVersion.txt"
version="$(sed -n 's/^m_EditorVersion:[[:space:]]*//p' "$version_file" 2>/dev/null | head -n 1)"
hub="${UNITY_HUB_EDITORS:-/Applications/Unity/Hub/Editor}"
unity="${UNITY_EDITOR:-$hub/$version}"
engine="$unity/Unity.app/Contents/Resources/Scripting/Managed/UnityEngine"
ns="$unity/Unity.app/Contents/Resources/Scripting/NetStandard/ref/2.1.0/netstandard.dll"
bcl="$unity/Unity.app/Contents/Resources/Scripting/BCLExtensions/TargetingPacks/netstandard2.1/ref"
dotnet="$unity/Unity.app/Contents/Resources/Scripting/DotNetSdk/dotnet"
out="$root/unity/Temp/unity-compile"
package_assemblies="${UNITY_PACKAGE_ASSEMBLIES:-$root/unity/Library/ScriptAssemblies}"

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

[[ -n "$version" ]] || die "$version_file does not pin m_EditorVersion"
if [[ ! -d "$unity/Unity.app" ]]; then
  installed=("$hub"/*/Unity.app(N:h:t))
  die "Unity $version (ProjectVersion.txt) is not installed at $unity; installed: ${${(j:, :)installed}:-none}. Install it with Unity Hub, or set UNITY_EDITOR or UNITY_HUB_EDITORS."
fi
[[ -x "$dotnet" ]] || die "Unity $version has no bundled dotnet at $dotnet"
# The editor bundles one .NET SDK; its version moves with the editor, so take the newest one it ships.
compilers=("$unity"/Unity.app/Contents/Resources/Scripting/DotNetSdk/sdk/*/Roslyn/bincore/csc.dll(Nn))
(( ${#compilers[@]} > 0 )) || die "Unity $version bundles no C# compiler under $unity/Unity.app/Contents/Resources/Scripting/DotNetSdk/sdk"
csc="${compilers[-1]}"
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

# A warning fails the gate. Muted: 0649 (a [SerializeField] field Unity assigns) and 1701/1702 (netstandard
# reference unification, which MSBuild also mutes). Each assembly reads the csc.rsp Unity reads for it.
csc_compile() {
  "$dotnet" exec "$csc" /nologo /nostdlib /noconfig /t:library \
    /langversion:latest /deterministic /optimize+ /warnaserror+ \
    /nowarn:0649 /nowarn:1701 /nowarn:1702 \
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
# The Sim asmdef sets noEngineReferences: it compiles against the framework alone.
csc_compile @"$root/src/GrandSluggers.Sim/csc.rsp" /out:"$out/GrandSluggers.Sim.dll" "${framework_refs[@]}" "${sim_cs[@]}"
echo "unity-compile  GrandSluggers.Runtime (${#runtime_cs[@]} sources)"
csc_compile @"$root/unity/Assets/Scripts/Runtime/csc.rsp" /out:"$out/GrandSluggers.Runtime.dll" "${framework_refs[@]}" "${engine_refs[@]}" \
  "${package_refs[@]}" -r:"$out/GrandSluggers.Sim.dll" "${runtime_cs[@]}"
echo "unity-compile  Assembly-CSharp (${#project_cs[@]} sources)"
csc_compile /out:"$out/Assembly-CSharp.dll" "${framework_refs[@]}" "${engine_refs[@]}" \
  "${package_refs[@]}" -r:"$out/GrandSluggers.Sim.dll" -r:"$out/GrandSluggers.Runtime.dll" "${project_cs[@]}"
echo "unity-compile  GrandSluggers.Editor (${#editor_cs[@]} sources)"
csc_compile @"$root/unity/Assets/Editor/csc.rsp" /out:"$out/GrandSluggers.Editor.dll" "${framework_refs[@]}" "${engine_refs[@]}" \
  "${editor_refs[@]}" "${package_refs[@]}" -r:"$out/GrandSluggers.Sim.dll" \
  -r:"$out/GrandSluggers.Runtime.dll" "${editor_cs[@]}"
echo "OK     narrow C# compile (Unity import/build not run)"

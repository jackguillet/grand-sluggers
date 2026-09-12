#!/bin/zsh
# The falsify pass from AGENTS.md, in one command, on this worktree.
# Portable checks always run. The narrow Unity compile runs when a licensed
# editor is present and is reported as skipped, not passed, when it is not.
set -euo pipefail

root="$(cd "$(dirname "$0")/.." && pwd)"
unity="${UNITY_EDITOR:-/Applications/Unity/Hub/Editor/6000.5.9f1}"

results=()
failed=0

step() {
  local name="$1"
  shift
  echo "\n=== $name ==="
  if "$@"; then
    results+=("PASS  $name")
  else
    results+=("FAIL  $name")
    failed=1
  fi
}

skip() {
  echo "\n=== $1 ==="
  echo "skipped: $2"
  results+=("SKIP  $1 — $2")
}

# A worktree that has never been imported has no ScriptAssemblies of its own.
# Borrow a sibling worktree's rather than making the caller remember the path.
find_package_assemblies() {
  local own="$root/unity/Library/ScriptAssemblies"
  if [[ -d "$own" ]]; then
    print -r -- "$own"
    return 0
  fi
  # Loop in this shell, not down a pipeline: a subshell cannot return a miss.
  local dir
  local -a worktrees
  worktrees=(${(f)"$(git -C "$root" worktree list --porcelain 2>/dev/null |
    awk '/^worktree /{print substr($0, 10)}')"})
  for dir in $worktrees; do
    if [[ -d "$dir/unity/Library/ScriptAssemblies" ]]; then
      print -r -- "$dir/unity/Library/ScriptAssemblies"
      return 0
    fi
  done
  return 1
}

step "sim tests" dotnet test "$root/src/GrandSluggers.Sim.Tests/GrandSluggers.Sim.Tests.csproj"
step "art catalog" dotnet run --project "$root/src/GrandSluggers.Cli" -- art
step "tool tests" python3 -m unittest discover -s "$root/tools/tests" -p 'test_*.py'

packages="${UNITY_PACKAGE_ASSEMBLIES:-$(find_package_assemblies || true)}"

if [[ ! -d "$unity" ]]; then
  skip "unity compile" "no editor at $unity (set UNITY_EDITOR)"
elif [[ -z "$packages" ]]; then
  skip "unity compile" "no imported unity/Library/ScriptAssemblies in any worktree"
else
  echo "\nusing package assemblies: $packages"
  step "unity compile" env UNITY_PACKAGE_ASSEMBLIES="$packages" "$root/tools/unity-compile.sh"
fi

echo "\n=== summary ==="
for line in "${results[@]}"; do echo "$line"; done
if (( failed )); then
  echo "\nverify: something failed above"
  exit 1
fi
if [[ "${results[*]}" == *SKIP* ]]; then
  echo "\nverify: portable checks passed; skipped steps above are NOT verified"
  exit 0
fi
echo "\nverify: all checks passed"

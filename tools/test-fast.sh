#!/usr/bin/env bash
# The breakage suite: every Sim test not tagged [Trait("Kind", "Balance")].
#
#   tools/test-fast.sh                     every breakage test
#   tools/test-fast.sh AtBatTests RulesTests   only those classes (still Kind!=Balance)
#   tools/test-fast.sh --balance [Class ...]   the balance and calibration tests instead
#
# Parallelism is capped at half the machine by src/GrandSluggers.Sim.Tests/xunit.runner.json.
set -euo pipefail

cd "$(dirname "$0")/.."

kind='Kind!=Balance'
classes=()
for arg in "$@"; do
  case "$arg" in
    --balance) kind='Kind=Balance' ;;
    -h|--help)
      sed -n '2,8p' "$0" | sed 's/^# \{0,1\}//'
      exit 0
      ;;
    -*) echo "test-fast: unknown flag $arg" >&2; exit 2 ;;
    *) classes+=("$arg") ;;
  esac
done

filter="$kind"
if ((${#classes[@]} > 0)); then
  names=""
  for c in "${classes[@]}"; do
    names+="${names:+|}FullyQualifiedName~$c"
  done
  filter="($names)&$kind"
fi

echo "test-fast: --filter \"$filter\""
exec dotnet test src/GrandSluggers.Sim.Tests/GrandSluggers.Sim.Tests.csproj -c Release --filter "$filter"

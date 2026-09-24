#!/usr/bin/env bash
# Rewrite one checked-in test fixture from the current code by running its writer ([WriterFact] in
# src/GrandSluggers.Sim.Tests). A writer is skipped in every other run, so it never counts as a pass.
#
#   tools/regenerate.sh pitch-golden   fixtures/pitch-family-golden.json (PitchFamilyGoldenTests)
#   tools/regenerate.sh night-games    fixtures/night-games.json (NightBlockTests, SF-25)
#
# A regeneration that changes a byte is a behaviour change: review the diff and say in the PR why it moved.
# Never run in CI.
set -euo pipefail

case "${1:-}" in
  pitch-golden) variable=GRAND_SLUGGERS_WRITE_PITCH_GOLDEN; test=PitchFamilyGoldenTests.Regenerate ;;
  night-games) variable=GRAND_SLUGGERS_WRITE_NIGHT_GAMES; test=NightBlockTests.RegenerateNightGames ;;
  *)
    echo "usage: tools/regenerate.sh pitch-golden|night-games" >&2
    exit 2
    ;;
esac

root="$(cd "$(dirname "$0")/.." && pwd)"
env "$variable=1" dotnet test "$root/src/GrandSluggers.Sim.Tests/GrandSluggers.Sim.Tests.csproj" -c Release \
  --filter "FullyQualifiedName=GrandSluggers.Sim.Tests.$test"
git -C "$root" status --short -- src/GrandSluggers.Sim.Tests/fixtures

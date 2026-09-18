using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// Which diamond this test process plays on. The geometry is process-wide (<see cref="Diamond"/> reads the one default table), so a
/// scenario row is only honest on the C80 copy in a process whose data root is the copy: <c>GRAND_SLUGGERS_TRIAL=trials/c80 dotnet
/// test --filter "Rows=compact"</c>. A root-aware row names both fixtures — the shipped one it always had, untouched, and the compact
/// one that makes the same claim on the 80-ft diamond (#715, ahead of 3e) — and <see cref="Pick{T}"/> takes the one this process plays.
/// Classes whose every row holds on both roots carry <c>[Trait("Rows", "compact")]</c>; CI runs those a second time on the copy.
/// </summary>
public static class TestRoot
{
    /// <summary>True when the process-wide table is the compact profile: 80-ft basepaths.</summary>
    public static readonly bool Compact = Rules.Default.Infield.BaselineFt < 85;

    public static T Pick<T>(T shipped, T compact) => Compact ? compact : shipped;
}

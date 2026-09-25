using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

/// <summary>A team whose bodies play no body class (§8.1): the table's shared ramp and reach, the full knockback, the plain barrel.</summary>
static class UnclassedTeam
{
    public static Team Unclassed(this Team team)
    {
        static Character Strip(Character c) => c with { BodyClass = "" };
        return team with
        {
            Captain = Strip(team.Captain),
            Roster = team.Roster.Select(Strip).ToList(),
            Order = team.Order?.Select(Strip).ToList(),
            Starter = team.Starter is { } s ? Strip(s) : null,
            Gloves = team.Gloves?.ToDictionary(kv => kv.Key, kv => Strip(kv.Value))
        };
    }
}

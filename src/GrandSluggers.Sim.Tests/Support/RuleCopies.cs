using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// A changed rules table in a test is a <c>with</c> copy of a loaded one. The tables carry no code
/// defaults, so a table built with <c>new</c> holds zeros, not the shipped numbers.
/// </summary>
static class RuleCopies
{
    /// <summary>
    /// The table with only the two pitch families every table must author (fastball and changeup):
    /// the off path for a library id with no row (#818). The shipped root authors all five.
    /// </summary>
    public static RulesTable TwoFamilies(RulesTable? from = null)
    {
        var t = from ?? Rules.Default;
        return t with
        {
            Pitching = t.Pitching with
            {
                Families = t.Pitching.Families with { Curveball = null, Slider = null, Sinker = null }
            }
        };
    }
}

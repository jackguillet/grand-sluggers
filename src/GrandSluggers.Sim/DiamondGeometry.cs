namespace GrandSluggers.Sim;

/// <summary>
/// One rules table's diamond (§8, §16): the bags, the rubber and where the fielders stand with nobody on, in feet (home at the
/// origin, +Z toward second and center, +X toward first). The numbers are the table's <c>infield</c> and <c>fielders</c>
/// sections, so a match, a trial or a test that builds its own table plays its own bags. Built once per table and cached
/// (<see cref="Of"/>), never per tick; it holds nothing that changes.
/// </summary>
public sealed class DiamondGeometry
{
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<RulesTable, DiamondGeometry> Cache = new();

    /// <summary>The diamond that <paramref name="rules"/> names.</summary>
    public static DiamondGeometry Of(RulesTable rules) => Cache.GetValue(rules, r => new DiamondGeometry(r.Infield, r.Fielders));

    DiamondGeometry(InfieldRules infield, FielderRules starts)
    {
        Baseline = infield.BaselineFt;
        InnerHalfFt = infield.InnerHalfFt;
        BackArcFt = infield.BackArcFt;
        Mound = infield.MoundFt;
        First = (infield.CornerFt, infield.CornerFt);
        Second = (0, infield.SecondFt);
        Third = (-infield.CornerFt, infield.CornerFt);
        Rubber = (0, infield.MoundFt);
        // Seven of the nine come from the fielders table (#725); the pitcher is the rubber the infield table names and the
        // catcher is HomeSet.CatcherZ, so neither is repeated in the fielders file. The outfield is one set for every park
        // here; a park's own outfield starts are OutfieldStarts.
        Positions = new Dictionary<string, (double X, double Z)>
        {
            ["P"] = Rubber,
            ["C"] = (0, HomeSet.CatcherZ),
            ["1B"] = starts.Spot("1B"),
            ["2B"] = starts.Spot("2B"),
            ["3B"] = starts.Spot("3B"),
            ["SS"] = starts.Spot("SS"),
            ["LF"] = starts.Spot("LF"),
            ["CF"] = starts.Spot("CF"),
            ["RF"] = starts.Spot("RF")
        };
    }

    public double Baseline { get; }

    /// <summary>Half the infield grass diamond (L1, around second and the mound), measured from the bags (§16 infield).</summary>
    public double InnerHalfFt { get; }

    /// <summary>The 1B–2B–3B dirt's outer arc, from the mound (§16 infield).</summary>
    public double BackArcFt { get; }
    public double Mound { get; }
    public (double X, double Z) Home => (0, 0);
    public (double X, double Z) First { get; }
    public (double X, double Z) Second { get; }
    public (double X, double Z) Third { get; }
    public (double X, double Z) Rubber { get; }

    /// <summary>Where each fielder stands with nobody on. Read every tick; it does not allocate.</summary>
    public IReadOnlyDictionary<string, (double X, double Z)> Positions { get; }

    public (double X, double Z) Bag(int bag) => bag switch
    {
        1 => First,
        2 => Second,
        3 => Third,
        _ => Home
    };
}

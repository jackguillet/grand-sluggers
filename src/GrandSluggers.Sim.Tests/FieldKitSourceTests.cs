using System.Globalization;
using System.Text.RegularExpressions;
using GrandSluggers.Sim;
using Xunit;
using RxMatch = System.Text.RegularExpressions.Match;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// F6-a (#859; FD-16, FR-13, FR-04): one field kit draws the diamond, rail and wall at every park.
/// Unity has no EditMode test assembly and <c>unity/</c> is not in the solution, so these read the
/// source the way <see cref="HarborKitPaintTests"/> does. The geometry the kit draws is pinned where it
/// is read from: <see cref="HarborWallTests"/> holds the loop to the flight polygon (<c>SF-05</c>).
/// </summary>
public sealed class FieldKitSourceTests
{
    static readonly ContentCatalog Content = Shipped.Content;
    readonly string _repo = Path.GetFullPath(Path.Combine(Content.Root.Shipped, ".."));

    string Runtime(string file) =>
        File.ReadAllText(Path.Combine(_repo, "unity/Assets/Scripts/Runtime", file));

    /// <summary>The kit is park-neutral: no park id in it, so no park can be special-cased inside it.</summary>
    [Fact]
    public void TheFieldKitNamesNoPark()
    {
        var src = Runtime("FieldKit.cs");
        foreach (var id in Content.Parks.Keys)
            Assert.DoesNotContain("\"" + id + "\"", src, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// The other five parks draw the kit, not the retired primitive diamond and fence, and Harbor's
    /// wall goes up through the kit rather than a second wall builder in <c>HarborKit.cs</c>.
    /// </summary>
    [Fact]
    public void EveryParkDrawsTheOneKit()
    {
        var view = Runtime("ParkView.cs");
        Assert.Contains("new FieldKit(", view, StringComparison.Ordinal);
        foreach (var retired in new[] { "void Infield(", "void Bags(", "void FoulLines(", "void Fence(", "void WarningTrack(", "void HarborDiamondSkin(" })
            Assert.DoesNotContain(retired, view, StringComparison.Ordinal);

        var harbor = Runtime("HarborKit.cs");
        Assert.Contains("Field.Wall(", harbor, StringComparison.Ordinal);
        Assert.DoesNotContain("RampPrism(", harbor, StringComparison.Ordinal);
    }

    // ---- F6-a2 (#881): no dress piece stands in the backstop around the plate or in a dugout ----

    /// <summary>
    /// F6-a2 (#881; FD-16 B, FD-01, FR-13, FR-05): the kit owns the backstop around the plate, so no
    /// piece of a park's dress stands inside it. Until F6-a2 every park but Harbor still built its
    /// pre-kit backstop at z −24, two side panels beside the batter, and — three of them — a ledge
    /// along z −36, inside or on the wall the kit now draws there (implementation map finding 29).
    ///
    /// <para>
    /// The backstop is read from the geometry owner, not written here: the radius of the wrap the
    /// drawn loop makes around the plate (<see cref="HarborWall.Loop(Park)"/>, built from
    /// <see cref="ParkBoundary"/>), the largest over the catalog parks, plus half the wall the kit
    /// stands on it (<see cref="HarborPostcard.WallThickFt"/>, the thickness <c>FieldKit.Wall</c>
    /// draws). So a spot inside the wall's outer face fails — a ledge laid on the wall as well as a
    /// panel inside it — and a park that moves its backstop moves the line with it.
    /// </para>
    ///
    /// <para>
    /// A piece's spot is where the dress places it, read from <c>ParkView.cs</c> by
    /// <see cref="Spots"/>. The scan is proved on the retired pieces themselves in
    /// <see cref="TheBackstopScanCatchesThePiecesF6a2Retired"/>, so it cannot pass by reading nothing.
    /// </para>
    /// </summary>
    [Fact]
    public void NoDressPieceStandsInsideTheKitsBackstop()
    {
        var spots = Spots(Runtime("ParkView.cs"));
        Assert.NotEmpty(spots);

        var line = BackstopOuterFaceFt();
        var inside = spots.Where(s => s.FromPlateFt < line).ToList();
        Assert.True(inside.Count == 0,
            $"the kit's backstop wall stands {line:0.0} ft from the plate at every park; these dress pieces stand inside it or on it:\n"
            + string.Join("\n", inside.Select(s => "  " + s)));
    }

    /// <summary>
    /// The scan reads each route a dress piece can be placed by, and catches every piece F6-a2
    /// removed, written as it was. The fixture is those lines verbatim (<c>ParkView.cs</c> at
    /// <c>4b47ad4e</c>), plus one piece per route the live dress does not use today, and the two
    /// lights it must not count.
    /// </summary>
    [Fact]
    public void TheBackstopScanCatchesThePiecesF6a2Retired()
    {
        const string retired = """
            void CrystalGarden(Park park)
            {
                Cube("IceLip", new Vector3(0, 2.0f, -36), new Vector3(70, 1.4f, 1.4f), pink);
            }

            void CrystalBoards(Park park, Material glass, Material kick)
            {
                Cube("GlassBack", new Vector3(0, 8f, -24f), new Vector3(38, 16, 0.45f), glass);
                Cube("GlassL", new Vector3(-20, 7f, -12f), new Vector3(0.45f, 14, 16f), glass);
                Cube("GlassR", new Vector3(20, 7f, -12f), new Vector3(0.45f, 14, 16f), glass);
            }

            void FunfairBackstop(Material cream, Material red, Material wood)
            {
                Cube("BuntHome", new Vector3(0, 8f, -24f), new Vector3(38, 14, 0.5f), cream);
                Cube("BuntStripe", new Vector3(0, 8f, -23.6f), new Vector3(38, 2.2f, 0.2f), red);
                Cube("BuntL", new Vector3(-20, 7f, -12f), new Vector3(0.5f, 12, 16f), cream);
                Cube("BuntR", new Vector3(20, 7f, -12f), new Vector3(0.5f, 12, 16f), cream);
                Cylinder("PostL", new Vector3(-19, 0, -24), 0.5f, 16f, wood);
                Cylinder("PostR", new Vector3(19, 0, -24), 0.5f, 16f, wood);
            }

            void RooftopDeck(Park park)
            {
                Cube("ChainBack", new Vector3(0, 8f, -24f), new Vector3(38, 16, 0.45f), steel);
                Cube("ChainL", new Vector3(-20, 7f, -12f), new Vector3(0.45f, 14, 16f), steel);
                Cube("ChainR", new Vector3(20, 7f, -12f), new Vector3(0.45f, 14, 16f), steel);
                Cube("ParapetLip", new Vector3(0, 2.0f, -36), new Vector3(70, 1.2f, 1.2f), steel);
            }

            void CanopyGrounds(Park park)
            {
                Cube("VineBack", new Vector3(0, 8f, -24f), new Vector3(38, 16, 0.7f), vine);
                Cube("VineL", new Vector3(-20, 7f, -12f), new Vector3(0.7f, 14, 16f), vine);
                Cube("VineR", new Vector3(20, 7f, -12f), new Vector3(0.7f, 14, 16f), vine);
            }

            void EmberCourtyard(Park park)
            {
                Cube("IronBack", new Vector3(0, 8f, -24f), new Vector3(38, 16, 0.55f), iron);
                Cube("IronL", new Vector3(-20, 7f, -12f), new Vector3(0.55f, 14, 16f), iron);
                Cube("IronR", new Vector3(20, 7f, -12f), new Vector3(0.55f, 14, 16f), iron);
                Cube("AshLip", new Vector3(0, 2.0f, -36), new Vector3(70, 1.4f, 1.4f), gold);
            }

            void NextPark(Park park)
            {
                var root = new GameObject("GroupRoot").transform;
                root.SetParent(_root, false);
                root.position = new Vector3(6, 0, -30);
                Look.Prim(PrimitiveType.Cube, "Box", root, new Vector3(0, 2f, 0), new Vector3(4, 4, 4), steel);
                Look.Prim(PrimitiveType.Cube, "UnderTheParkRoot", _root, new Vector3(-10, 1, -10), new Vector3(2, 2, 2), steel);
                var spots = new[] { new Vector3(-5, 0, -30), new Vector3(5, 0, 300) };
                AcUnit(new Hazard("ac_unit", 3, -20, 6, null));
                Glow("NearGlow", new Vector3(0, 10, -20), Colors.Gold, 1f, 20f);
                var go = new GameObject("NearSpot");
                go.transform.SetParent(_root, false);
                go.transform.position = new Vector3(0, 52, 20);
                var light = go.AddComponent<Light>();
            }

            void Glow(string name, Vector3 pos, Color color, float intensity, float range)
            {
                var go = new GameObject(name);
                go.transform.SetParent(_root, false);
                go.transform.position = pos;
                var light = go.AddComponent<Light>();
            }
            """;

        var spots = Spots(retired);
        var line = BackstopOuterFaceFt();
        var inside = spots.Where(s => s.FromPlateFt < line).Select(s => s.Name).OrderBy(n => n, StringComparer.Ordinal).ToArray();
        var expected = new[]
        {
            "IceLip", "GlassBack", "GlassL", "GlassR",
            "BuntHome", "BuntStripe", "BuntL", "BuntR", "PostL", "PostR",
            "ChainBack", "ChainL", "ChainR", "ParapetLip",
            "VineBack", "VineL", "VineR",
            "IronBack", "IronL", "IronR", "AshLip",
            "GroupRoot", "UnderTheParkRoot", "spots", "ac_unit"
        }.OrderBy(n => n, StringComparer.Ordinal).ToArray();
        Assert.Equal(expected, inside);

        // The far spot in the list is read, and outside; the two lights are not pieces.
        Assert.Contains(spots, s => s.Name == "spots" && s.FromPlateFt > line);
        Assert.DoesNotContain(spots, s => s.Name is "NearGlow" or "NearSpot");
    }

    /// <summary>
    /// F6-a2 (#881, extended after Jack's look at the sheets: "looks like we also need to get rid of
    /// those old dugouts"): no park's dress builds a dugout. Until then every park but Harbor stood a
    /// bench (and, at three of them, an awning over it) at (±42, 22), in foul ground in front of the
    /// span Harbor's dugouts fill. Harbor's dugouts are its own dress in <c>HarborKit</c>, which this
    /// scan does not read, and they do not change.
    ///
    /// <para>
    /// The region is the one the geometry owner already pins at every park (<see cref="InADugout"/>):
    /// along each foul line from <see cref="HarborDugout.AlongHome"/> to <see cref="HarborDugout.AlongBag"/>
    /// — the span the star meter stands on the rail over at every park — and from the chalk into foul
    /// ground as far as the pit's back wall, the rail's offset (<see cref="HarborWall.FoulOffset"/>, from
    /// <see cref="ParkBoundary"/>) plus the pit's depth. A dress piece placed there is a dugout, whether
    /// it sits in front of the rail or behind it.
    /// </para>
    /// </summary>
    [Fact]
    public void NoDressPieceStandsInADugout()
    {
        var spots = Spots(Runtime("ParkView.cs"));
        Assert.NotEmpty(spots);

        var inside = spots.Where(InADugout).ToList();
        Assert.True(inside.Count == 0,
            $"the dugouts stand {HarborDugout.AlongHome:0.0}-{HarborDugout.AlongBag:0.0} ft along each foul line, from the chalk to "
            + $"{DugoutBackFt:0.0} ft into foul ground; these dress pieces stand in one:\n"
            + string.Join("\n", inside.Select(s => "  " + s)));
    }

    /// <summary>
    /// The dugout region catches every bench and awning F6-a2 removed, written as they were
    /// (<c>ParkView.cs</c> at <c>0f93da5a</c>), and is bounded where the geometry owner bounds it: a
    /// piece in the pit behind the rail is in it; the same spot mirrored into fair ground, one past the
    /// bag end, one short of the home end and one behind the pit's back wall are not.
    /// </summary>
    [Fact]
    public void TheDugoutScanCatchesTheBenchesF6a2Retired()
    {
        const string retired = """
            void CrystalGarden(Park park)
            {
                Cube("IceBench1B", new Vector3(42, 1.1f, 22), new Vector3(20, 1.0f, 6), ice);
                Cube("IceBench3B", new Vector3(-42, 1.1f, 22), new Vector3(20, 1.0f, 6), ice);
            }

            void FunfairBenches(Material wood, Material canvas)
            {
                Cube("Bench1B", new Vector3(42, 1.0f, 22), new Vector3(20, 1.0f, 6), wood);
                Cube("Awning1B", new Vector3(42, 5.2f, 22), new Vector3(22, 0.5f, 8), canvas);
                Cube("Bench3B", new Vector3(-42, 1.0f, 22), new Vector3(20, 1.0f, 6), wood);
                Cube("Awning3B", new Vector3(-42, 5.2f, 22), new Vector3(22, 0.5f, 8), canvas);
            }

            void RooftopDeck(Park park)
            {
                Cube("Bench1B", new Vector3(42, 1.0f, 22), new Vector3(20, 1.0f, 6), tar);
                Cube("Awning1B", new Vector3(42, 5.4f, 22), new Vector3(22, 0.35f, 8), neon);
                Cube("Bench3B", new Vector3(-42, 1.0f, 22), new Vector3(20, 1.0f, 6), tar);
                Cube("Awning3B", new Vector3(-42, 5.4f, 22), new Vector3(22, 0.35f, 8), magenta);
            }

            void CanopyGrounds(Park park)
            {
                Cube("LogBench1B", new Vector3(42, 1.0f, 22), new Vector3(20, 1.0f, 6), wood);
                Cube("LeafAwning1B", new Vector3(42, 5.4f, 22), new Vector3(22, 0.6f, 8), leaf);
                Cube("LogBench3B", new Vector3(-42, 1.0f, 22), new Vector3(20, 1.0f, 6), wood);
                Cube("LeafAwning3B", new Vector3(-42, 5.4f, 22), new Vector3(22, 0.6f, 8), leaf);
            }

            void EmberCourtyard(Park park)
            {
                Cube("StoneBench1B", new Vector3(42, 1.0f, 22), new Vector3(20, 1.0f, 6), stone);
                Cube("StoneBench3B", new Vector3(-42, 1.0f, 22), new Vector3(20, 1.0f, 6), stone);
            }

            void Bounds(Park park)
            {
                Cube("InThePit", new Vector3(-64.7f, 1, 8.84f), new Vector3(4, 2, 4), stone);
                Cube("FairMirror", new Vector3(22, 1, 42), new Vector3(4, 2, 4), stone);
                Cube("PastTheBagEnd", new Vector3(66.5f, 1, 46.7f), new Vector3(4, 2, 4), stone);
                Cube("ShortOfTheHomeEnd", new Vector3(41, 1, -1.4f), new Vector3(4, 2, 4), stone);
                Cube("BehindThePit", new Vector3(62, 1, -2), new Vector3(4, 2, 4), stone);
            }
            """;

        var spots = Spots(retired);
        var inside = spots.Where(InADugout).Select(s => s.Method + "." + s.Name).OrderBy(n => n, StringComparer.Ordinal).ToArray();
        var expected = new[]
        {
            "CrystalGarden.IceBench1B", "CrystalGarden.IceBench3B",
            "FunfairBenches.Bench1B", "FunfairBenches.Awning1B", "FunfairBenches.Bench3B", "FunfairBenches.Awning3B",
            "RooftopDeck.Bench1B", "RooftopDeck.Awning1B", "RooftopDeck.Bench3B", "RooftopDeck.Awning3B",
            "CanopyGrounds.LogBench1B", "CanopyGrounds.LeafAwning1B", "CanopyGrounds.LogBench3B", "CanopyGrounds.LeafAwning3B",
            "EmberCourtyard.StoneBench1B", "EmberCourtyard.StoneBench3B",
            "Bounds.InThePit"
        }.OrderBy(n => n, StringComparer.Ordinal).ToArray();
        Assert.Equal(expected, inside);

        // The benches stood 47 ft out, past the backstop line: only the dugout region names them.
        Assert.DoesNotContain(spots, s => s.FromPlateFt < BackstopOuterFaceFt());
    }

    /// <summary>
    /// A spot in a dugout, on either side, as the geometry owner pins it at every park: between
    /// <see cref="HarborDugout.AlongHome"/> and <see cref="HarborDugout.AlongBag"/> along the foul line
    /// (the span <see cref="HarborDugout.WallOpensHere"/> opens and the star meter reads), and from the
    /// chalk into foul ground out to the pit's back wall (<see cref="DugoutBackFt"/>). The rail is still
    /// parallel to the line over the span (it flares from <see cref="ParkBoundary.FlareStartFt"/>), so
    /// the offset is the rail's at every park.
    /// </summary>
    static bool InADugout(Spot s)
    {
        const double inv = 0.7071067811865476;
        var ax = Math.Abs(s.X - Diamond.Home.X);
        var z = s.Z - Diamond.Home.Z;
        var along = (ax + z) * inv;
        var into = (ax - z) * inv;
        return along >= HarborDugout.AlongHome && along <= HarborDugout.AlongBag
            && into > 0 && into <= DugoutBackFt;
    }

    /// <summary>The pit's back wall, measured into foul ground from the chalk: the rail, then the pit's depth behind it.</summary>
    static double DugoutBackFt => HarborWall.FoulOffset + 2.0 * HarborDugout.HalfDeep;

    /// <summary>
    /// Where the kit's backstop wall stops, measured from the plate: the radius of the loop's wrap —
    /// its nearest vertex to <see cref="Diamond.Home"/>, which is the rail's home end on the
    /// <see cref="ParkBoundary"/> — at the park where it is widest, plus half the wall.
    /// </summary>
    static double BackstopOuterFaceFt() =>
        Content.Parks.Values.Max(park => HarborWall.Loop(park).Min(p => FromPlate(p.X, p.Z)))
        + HarborPostcard.WallThickFt * 0.5;

    static double FromPlate(double x, double z) =>
        Math.Sqrt((x - Diamond.Home.X) * (x - Diamond.Home.X) + (z - Diamond.Home.Z) * (z - Diamond.Home.Z));

    /// <summary>A dress piece placed at a literal spot: the method that places it, its name and its spot.</summary>
    public readonly record struct Spot(string Method, string Name, double X, double Z)
    {
        public double FromPlateFt => FromPlate(X, Z);
        public override string ToString() => $"{Method}: {Name} at ({X:0.##}, {Z:0.##}), {FromPlateFt:0.0} ft from the plate";
    }

    const string Num = @"-?\d+(?:\.\d+)?f?";
    const string Vec = @"new Vector3\(\s*(?<x>" + Num + @")\s*,\s*(?<y>" + Num + @")\s*,\s*(?<z>" + Num + @")\s*\)";
    /// <summary>A name argument: a string literal, optionally with an index appended (<c>"Pole" + p + i</c>).</summary>
    const string NameArg = @"""(?<name>[^""]*)""(?:\s*\+\s*[\w\.]+)*";

    /// <summary>A member of the class, up to its body: <c>void CrystalGarden(Park park) {</c>, or an expression body.</summary>
    static readonly Regex Member = new(
        @"^[ \t]*(?:(?:public|private|internal|static|override|sealed)\s+)*(?!(?:else|return|new|var)\b)[\w\.<>\[\]]+\s+(?<method>\w+)\s*\([^;{}]*\)\s*(?<open>\{|=>)",
        RegexOptions.Multiline);
    /// <summary>A call to one of the view's builders with a literal first spot: <c>Cube("Name", new Vector3(…), …)</c>, <c>Brazier(new Vector3(…), …)</c>.</summary>
    static readonly Regex BuilderAt = new(@"(?<![\w\.])(?<call>[A-Z]\w*)\(\s*(?:" + NameArg + @"\s*,\s*)?" + Vec);
    /// <summary>A primitive straight under the park root, whose local spot is its world spot.</summary>
    static readonly Regex UnderParkRoot = new(@"Look\.Prim\(\s*PrimitiveType\.\w+\s*,\s*" + NameArg + @"\s*,\s*_root\s*,\s*" + Vec);
    /// <summary>A transform set to a literal spot (a group root when it parents primitives).</summary>
    static readonly Regex TransformAt = new(@"(?<obj>\w+(?:\.transform)?)\.position\s*=\s*" + Vec);
    /// <summary>A list of literal spots a builder loops over (<c>var spots = new[] { … }</c>).</summary>
    static readonly Regex SpotList = new(@"(?<list>\w+)\s*=\s*new\[\]\s*\{(?<items>[^}]*)\}");
    static readonly Regex AnyVec = new(Vec);
    /// <summary>A hazard actor the view writes itself rather than reading from the park.</summary>
    static readonly Regex HazardAt = new(@"new Hazard\(\s*" + NameArg + @"\s*,\s*(?<x>" + Num + @")\s*,\s*(?<z>" + Num + @")");
    static readonly Regex Named = new(@"new GameObject\(\s*""(?<name>[^""]*)""\s*\)");

    /// <summary>
    /// Every spot the source places a piece at, by five routes: a builder handed a literal spot
    /// (<c>Cube</c>, <c>Quad</c>, <c>Cylinder</c>, <c>CrowdCard</c>, <c>Tent</c>, <c>Building</c>,
    /// <c>VineWall</c>, <c>Tower</c>, <c>Brazier</c>, …); <c>Look.Prim</c> straight under the park root;
    /// a transform set to a literal spot that parents primitives; a literal list of spots; a
    /// <c>new Hazard</c> written in the view. A builder that adds a <c>Light</c> and draws no primitive
    /// (<c>Glow</c>) places a light, not a piece, and so does a transform that parents none (the follow
    /// spot). A spot computed from an expression is not a literal and is not read.
    /// </summary>
    static List<Spot> Spots(string src)
    {
        var methods = Methods(src).ToList();
        var lights = methods
            .Where(m => m.Body.Contains("AddComponent<Light>", StringComparison.Ordinal)
                && !m.Body.Contains("Look.Prim(", StringComparison.Ordinal)
                && !m.Body.Contains("CreatePrimitive(", StringComparison.Ordinal))
            .Select(m => m.Name)
            .ToHashSet(StringComparer.Ordinal);

        var spots = new List<Spot>();
        foreach (var (method, body) in methods)
        {
            foreach (RxMatch m in BuilderAt.Matches(body))
            {
                var call = m.Groups["call"].Value;
                if (lights.Contains(call)) continue;
                var name = m.Groups["name"].Success ? m.Groups["name"].Value : call;
                spots.Add(At(method, name, m));
            }
            foreach (RxMatch m in UnderParkRoot.Matches(body))
                spots.Add(At(method, m.Groups["name"].Value, m));
            foreach (RxMatch m in TransformAt.Matches(body))
            {
                var obj = m.Groups["obj"].Value;
                var parents = new Regex(@"Look\.Prim\(\s*PrimitiveType\.\w+\s*,[^,]+,\s*" + Regex.Escape(obj) + @"\s*,");
                if (!parents.IsMatch(body)) continue;
                var named = Named.Matches(body[..m.Index]).LastOrDefault();
                spots.Add(At(method, named?.Groups["name"].Value ?? obj, m));
            }
            foreach (RxMatch list in SpotList.Matches(body))
                foreach (RxMatch m in AnyVec.Matches(list.Groups["items"].Value))
                    spots.Add(At(method, list.Groups["list"].Value, m));
            foreach (RxMatch m in HazardAt.Matches(body))
                spots.Add(At(method, m.Groups["name"].Value, m));
        }
        return spots;
    }

    static Spot At(string method, string name, RxMatch m) =>
        new(method, name, Feet(m.Groups["x"].Value), Feet(m.Groups["z"].Value));

    static double Feet(string literal) =>
        double.Parse(literal.TrimEnd('f'), NumberStyles.Float, CultureInfo.InvariantCulture);

    /// <summary>Each member and its body: braces matched from the opening one, or an expression body to its semicolon.</summary>
    static IEnumerable<(string Name, string Body)> Methods(string src)
    {
        foreach (RxMatch m in Member.Matches(src))
        {
            var open = m.Groups["open"];
            if (open.Value == "=>")
            {
                var end = src.IndexOf(';', open.Index);
                yield return (m.Groups["method"].Value, src[open.Index..(end + 1)]);
                continue;
            }
            var depth = 0;
            for (var i = open.Index; i < src.Length; i++)
            {
                if (src[i] == '{') depth++;
                else if (src[i] == '}' && --depth == 0)
                {
                    yield return (m.Groups["method"].Value, src[open.Index..(i + 1)]);
                    break;
                }
            }
        }
    }
}

using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// <c>SF-24</c>: hazards off (§0.3 D21, §14; FD-10 B, FD-09; F4-h, #858). A match option, default on,
/// that plays a park with its hazard instances removed and <b>nothing else changed</b>: the park keeps
/// its size, fence, walls, air, wind, ground zones, foul territory and depth, the match plays the same
/// resolved table, and both seats and the CPU read the one park the match holds. Since F4-d (FD-11) the
/// switch is the last step of the one resolution (<see cref="PlayedPark.Of"/>), so a night block's
/// instances go with the day's; <see cref="NightBlockTests"/> holds that half.
///
/// <para>
/// <b>Which instances go</b> is a property of the pattern set, <see cref="HazardPattern.Hazards"/>:
/// every instance whose type's pattern is a status volume, a ball redirect, a reward target or a catch
/// stealer. A <c>wallTrait</c> (a climbable span is a wall property, FD-06) and a <c>decoration</c>
/// (it does nothing in play; the kit still draws it) stay. <see cref="TheSwitchRemovesTheFourActingPatternsAndKeepsTheWallAndTheScenery"/>
/// pins that choice, which Jack confirmed (4. a, September 22, 2026).
/// </para>
///
/// </summary>
public sealed class HazardsOffTests
{
    static readonly ContentCatalog Catalog = ContentCatalog.Load();
    const double Frame = 1.0 / 60.0;

    static bool ActsInPlay(ContentCatalog content, Hazard h) =>
        HazardPattern.IsHazard(content.Rules.Hazards.Of(h.Type).Pattern);

    /// <summary>The parks the switch changes on this catalog: those that list at least one instance of a hazard pattern.</summary>
    static IReadOnlyList<string> ParksWithAHazard(ContentCatalog content) =>
        content.ParkPickOrder.Where(id => content.Parks[id].Hazards.Any(h => ActsInPlay(content, h))).ToArray();

    // ---------------------------------------------------------------------------------
    // The choice (FD-10 contract work): which patterns count as a hazard
    // ---------------------------------------------------------------------------------

    [Fact]
    public void TheSwitchRemovesTheFourActingPatternsAndKeepsTheWallAndTheScenery()
    {
        Assert.Equal(
            new[] { HazardPattern.StatusVolume, HazardPattern.BallRedirect, HazardPattern.RewardTarget, HazardPattern.CatchStealer },
            HazardPattern.Hazards);
        // Every pattern in the closed set is on one side or the other, and the other side is exactly these two.
        Assert.Equal(new[] { HazardPattern.WallTrait, HazardPattern.Decoration },
            HazardPattern.All.Where(p => !HazardPattern.IsHazard(p)));
        Assert.All(HazardPattern.Hazards, p => Assert.Contains(p, HazardPattern.All));
        Assert.False(HazardPattern.IsHazard(null));
        Assert.False(HazardPattern.IsHazard("tilt"));
    }

    // ---------------------------------------------------------------------------------
    // SF-24: the park with hazards off
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// <c>SF-24</c>, every park in the pick order. With hazards off the match's park has no
    /// instance of a hazard pattern, keeps every <c>wallTrait</c> and <c>decoration</c> instance in its
    /// authored order, and is the catalog's park in every other member (record equality once the list
    /// is put back). With hazards on it is the catalog's park itself. The table is the same reference
    /// either way, so the switch cannot have reached a rule (<c>SF-01</c>).
    ///
    /// <para>
    /// Re-authored to FD-11 (F4-d, #895) for a park with a night block: the match holds the park as it
    /// plays by day, the block resolved away (<c>Night</c> null) and every other member the catalog's, so
    /// "the catalog's park itself" is that park with no night block, by record equality. Every park with no
    /// night block is still the catalog's own object.
    /// </para>
    /// </summary>
    [Fact]
    public void SF24_AHazardsOffMatchHasNoHazardInstanceAndEveryOtherParkMemberUnchanged()
    {
        var content = Catalog;
        var removed = 0;
        var kept = 0;
        foreach (var id in content.ParkPickOrder)
        {
            var park = content.Parks[id];
            var on = Match.Exhibition(content, "rio", "ashlord", innings: 3, seed: 7, parkId: id);
            var off = Match.Exhibition(content, "rio", "ashlord", innings: 3, seed: 7, parkId: id, hazards: false);
            Assert.True(on.Hazards);
            Assert.False(off.Hazards);
            if (park.Night is null) Assert.Same(park, on.Park);
            else Assert.Equal(park with { Night = null }, on.Park);

            var patterns = content.Rules.Hazards;
            Assert.DoesNotContain(off.Park.Hazards, h => HazardPattern.IsHazard(patterns.Of(h.Type).Pattern));
            // Written against the two kept patterns by name, not through IsHazard, so this row is a
            // second statement of the choice rather than the same statement read back.
            var scenery = park.Hazards
                .Where(h => patterns.Of(h.Type).Pattern is HazardPattern.WallTrait or HazardPattern.Decoration)
                .ToArray();
            Assert.Equal(scenery, off.Park.Hazards);
            Assert.All(off.Park.Hazards, h => Assert.Contains(park.Hazards, p => ReferenceEquals(p, h)));

            Assert.Equal(park with { Night = null }, off.Park with { Hazards = park.Hazards });
            Assert.Equal(park.Id, off.Park.Id);
            Assert.Same(on.Rules, off.Rules);
            Assert.Equal(on.Night, off.Night);

            removed += park.Hazards.Count - off.Park.Hazards.Count;
            kept += off.Park.Hazards.Count;
            // A park with nothing to remove and no night block to resolve plays the catalog's own
            // object: nothing about it moved.
            if (off.Park.Hazards.Count == park.Hazards.Count && park.Night is null) Assert.Same(park, off.Park);
        }
        // Not vacuous: the switch removes instances, and the scenery it keeps is there to keep.
        Assert.True(removed > 0, content.Root.Provenance);
        Assert.True(kept > 0, content.Root.Provenance);
    }

    /// <summary>
    /// The trace says which park was played (§0.3 rails: <c>PlayTraceIdentity</c> serialises the whole
    /// <c>Park</c>). A hazards-off match at a park that lost instances carries the kept list and a
    /// different identity; at a park that lost none the identity is the hazards-on identity, because the
    /// park is the same object. Every stored identity is a hazards-on one, so none of them moves.
    /// </summary>
    [Fact]
    public void SF24_AHazardsOffTraceIdentityCarriesTheKeptList()
    {
        foreach (var id in Catalog.ParkPickOrder)
        {
            var on = PlayTraceIdentity.Capture(Match.Exhibition(Catalog, "rio", "ashlord", innings: 3, seed: 7, parkId: id));
            var off = PlayTraceIdentity.Capture(Match.Exhibition(Catalog, "rio", "ashlord", innings: 3, seed: 7, parkId: id, hazards: false));
            if (ParksWithAHazard(Catalog).Contains(id))
                Assert.NotEqual(on.Sha256, off.Sha256);
            else
                Assert.Equal(on.Sha256, off.Sha256);
        }
    }

    // ---------------------------------------------------------------------------------
    // SF-24: no hazard event
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// The seed set, found by a scan at every park with a hazard, day and night, with the
    /// first matchup and the same detector as <see cref="HazardOutcomes"/> (#858). Seeds 1–12 first; where
    /// they held no outcome the scan ran on (a chomp to 16). Rescanned after F4-e (#862) moved
    /// Crystal's and Ember's volumes, and again after each promotion that reseeded every game (#871, #886). Each row is a game in which hazards on plays at least one hazard
    /// outcome, so the hazards-off twin of the same game proves something. Chompers bite only at night
    /// (they are Funfair's night block since F4-d, FD-11), so Funfair's row is a night game with a chomp in it.
    /// </summary>
    static IReadOnlyList<(string Park, bool Night, int Seed)> NoHazardEventSeeds =>
        [
            ("crystal-rink", false, 1),   // a freeze volume slows the chase (a Crystal night is its day since FD-11-R2)
            ("ember-keep", true, 1),      // a lava pit or the breath slows the chase
            ("funfair-park", true, 16),   // a chomper eats a fly
            ("canopy-yard", false, 4),    // a barrel warps a grounder
            ("rooftop-city", false, 2)    // a billboard pays the batting team
        ];

    /// <summary>A park, a condition and a seed at which the switch changes the whole game.</summary>
    static (string Park, bool Night, int Seed) DefaultOnGame => ("canopy-yard", false, 4);

    /// <summary>
    /// <c>SF-24</c>, the event half. Over the fixed seed set, hazards on plays at least one hazard outcome
    /// (the premise: a chase frozen by a volume, a ball redirected by a can or a barrel, a fly chomped,
    /// a billboard paid) and the same game with hazards off plays none. The seed set covers every park
    /// that has a hazard, so a park added with a hazard and no row here fails.
    /// </summary>
    [Fact]
    [Trait("Kind", "Balance")]
    [Trait("Cost", "Heavy")]
    public void SF24_OverAFixedSeedSetHazardsOnPlaysAHazardOutcomeAndHazardsOffPlaysNone()
    {
        var (home, away) = ParkFactorCohort.Matchups[0];
        Assert.Equal(
            ParksWithAHazard(Catalog).Order(StringComparer.Ordinal),
            NoHazardEventSeeds.Select(r => r.Park).Distinct().Order(StringComparer.Ordinal));
        foreach (var (park, night, seed) in NoHazardEventSeeds)
        {
            // The premise needs one outcome, so the hazards-on game stops at its first; the hazards-off
            // game is played to the end, because the claim is that none of it happens.
            var on = HazardOutcomes(Catalog, home, away, park, night, seed, hazards: true, stopAtFirst: true);
            var off = HazardOutcomes(Catalog, home, away, park, night, seed, hazards: false, stopAtFirst: false);
            Assert.True(on.Count > 0, $"the premise: {park} {(night ? "night" : "day")} seed {seed} plays a hazard with hazards on");
            Assert.True(off.Count == 0, $"{park} {(night ? "night" : "day")} seed {seed} with hazards off played [{string.Join(", ", off)}]");
        }
    }

    /// <summary>
    /// What a game's hazards did, play by play, from the sim's own facts. A status volume's is the live touch
    /// (F4-b, #896): a <see cref="PlayTraceMarkKind.BodySlowed"/> mark in the play's trace, since the preview
    /// no longer reads a volume at all — the heart swing's slow, a special, never makes one. The redirect and
    /// the catch stealer are still the preview the live ball played (<c>Warped</c>, <c>Chomped</c>), read off
    /// the trace's <c>BeginLive</c> command; the star swings that warp without a hazard (the shell and cask
    /// swings) are not counted, because the switch does not touch the specials. The billboard's payment has
    /// no typed fact of its own; its only record is the line the match appends where it pays. The game is
    /// <see cref="Match.AutoPlayGame"/>'s loop, one <see cref="Match.AutoPlay"/> at a time, so each play's
    /// trace is read and dropped as it lands (a whole traced game held at once is the slow part).
    /// </summary>
    static List<string> HazardOutcomes(ContentCatalog content, string home, string away, string park, bool night, int seed,
        bool hazards, bool stopAtFirst)
    {
        var match = Match.Exhibition(content, home, away, innings: 3, seed: seed, parkId: park, night: night, hazards: hazards);
        Assert.Equal(hazards, match.Hazards);
        match.Tracing = true;
        var found = new List<string>();
        var guard = 0;
        while (!match.Over && guard++ < 2000)
        {
            var logged = match.Log.Count;
            match.AutoPlay();
            foreach (var trace in match.Traces)
            {
                if (trace.Marks?.Any(m => m.Kind == PlayTraceMarkKind.BodySlowed) == true) found.Add("slowed");
                var begin = trace.Commands?.FirstOrDefault(c => c.Input.Kind == LivePlayCommandKind.BeginLive)?.Input;
                if (begin?.Preview is not { } pre) continue;
                Assert.Same(match.Park, trace.Context!.Park);
                var star = begin.Hit?.StarSwingUsed;
                // A park's volume never freezes the preview (FD-08-R1): only the heart swing does.
                Assert.True(!pre.Frozen || star == "heart-swing", "a frozen preview without the heart swing");
                if (pre.Warped && star is not ("shell-swing" or "cask-swing")) found.Add("warped");
                if (pre.Chomped) found.Add("chomped");
            }
            found.AddRange(match.Log.Skip(logged).Where(e => e.Caption.Contains("Billboard STAR!")).Select(_ => "billboard"));
            match.Tracing = true; // drops the traces just read; tracing is observation and moves no play
            if (stopAtFirst && found.Count > 0) return found;
        }
        Assert.True(match.Over);
        return found;
    }

    // ---------------------------------------------------------------------------------
    // SF-24: both seats and the CPU read the same park
    // ---------------------------------------------------------------------------------

    static readonly LiveSeats HumanGlove = new(HumanBats: false, HumanPitches: true, PlayerMustField: true, Versus: false);

    /// <summary>
    /// A fly aimed at the centre of the Rink's deepest status volume, read from the park as the catalog
    /// authored it — the ball <see cref="ParkSlowRowsTests"/> slows, wherever a placement child puts the disc.
    /// </summary>
    static (double Carry, double Spray) RinkFly()
    {
        var rink = Catalog.MustPark("crystal-rink");
        var deep = rink.Hazards
            .Where(h => Catalog.Rules.Hazards.Of(h.Type).Pattern == HazardPattern.StatusVolume)
            .OrderByDescending(h => Diamond.Dist(0, 0, h.X, h.Z))
            .First();
        return (Diamond.Dist(0, 0, deep.X, deep.Z), Math.Atan2(deep.X, deep.Z) * 180 / Math.PI);
    }

    /// <summary>
    /// A human-seat fixture and an <c>AutoPlay</c> fixture see the same list (FD-10: both seats and the
    /// CPU get the same state). The human glove plays a fly into a freeze volume: with hazards on the live
    /// ball tests its bodies against the Rink's three volumes, with hazards off against none, and the
    /// preview is not frozen either way (F4-b: a volume slows the body that touches it, never a play from
    /// its landing). The live ball the pad drives reads the match's one park. The CPU's live plays at the
    /// same park read that same object, play after play.
    /// </summary>
    [Fact]
    public void SF24_AHumanSeatAndTheCpuPlayTheSameHazardsOffPark()
    {
        var (carry, spray) = RinkFly();
        foreach (var hazards in new[] { true, false })
        {
            // Match.Slice's teams, so the glove that plays the fly is the one ParkSlowRowsTests slows.
            var match = new Match(Catalog, PresetTeams.EmberCourt(Catalog), PresetTeams.SparkAllStars(Catalog),
                Catalog.MustPark("crystal-rink"), seed: 1, hazards: hazards);
            var hit = FlightFixtures.Landing(match.Park, carry, 30, spray);
            var preview = match.PreviewHit(hit);
            Assert.False(preview.Frozen);
            var live = match.LivePlay;
            live.Recording = true;
            Assert.True(live.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, preview, null, HumanGlove, 0,
                LivePlayCommandSource.Human)).Snapshot.Active);
            Assert.Equal(hazards ? 3 : 0, live.StatusVolumes.Count);
            // A dead pad for a second of the chase: the trace's context is written at BeginLive, and the
            // ticks are the live ball running on that park.
            PlayEvent? done = null;
            for (var i = 0; i < 60 && done is null; i++)
                done = live.Apply(LivePlayCommand.Tick(Frame, LivePadInput.Dead, LivePadInput.Dead, false, LivePlayCommandSource.Human)).CompletedPlay;
            var human = live.TakeTrace(done);
            Assert.NotEmpty(human.Ticks);
            Assert.Equal(HumanGlove, human.Context!.Seats);
            Assert.Same(match.Park, human.Context.Park);

            var cpu = Match.Slice(Catalog, seed: 1, parkId: "crystal-rink");
            if (!hazards) cpu = new Match(Catalog, PresetTeams.EmberCourt(Catalog), PresetTeams.SparkAllStars(Catalog),
                Catalog.MustPark("crystal-rink"), seed: 1, hazards: false);
            Assert.Equal(hazards, cpu.Hazards);
            // The CPU's first half-dozen live plays: each one's trace names the park the live ball ran on.
            cpu.Tracing = true;
            var played = new List<PlayTrace>();
            for (var guard = 0; played.Count < 6 && !cpu.Over && guard < 2000; guard++)
            {
                cpu.AutoPlay();
                played.AddRange(cpu.Traces.Where(t => t.Context is not null));
                cpu.Tracing = true;
            }
            Assert.Equal(6, played.Count);
            Assert.All(played, t => Assert.Same(cpu.Park, t.Context!.Park));
            Assert.All(played, t => Assert.Equal(LiveSeats.CpuOnly, t.Context!.Seats));

            // The two fixtures, one list: the human's glove and the CPU's read the same instances.
            Assert.Equal(match.Park.Hazards, cpu.Park.Hazards);
            Assert.Equal(hazards ? 3 : 0, cpu.Park.Hazards.Count);
        }
    }

    // ---------------------------------------------------------------------------------
    // Default on
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// The default is on: a match built without the argument is the match built with
    /// <c>hazards: true</c>, seed for seed. The whole game is played through the captains' overload; the
    /// constructor and the teams' overload are held to the same inputs — <c>Hazards</c> true and the
    /// catalog's own park object — which is everything the switch can reach. The last assertion is the
    /// premise that makes the first mean something: at a park and seed where the switch changes the game,
    /// the default game is the hazards-on one, not the hazards-off one.
    /// </summary>
    [Fact]
    [Trait("Kind", "Balance")]
    [Trait("Cost", "Heavy")]
    public void SF24_AMatchBuiltWithoutTheArgumentIsTheHazardsOnMatchSeedForSeed()
    {
        var (park, night, seed) = DefaultOnGame;
        var catalogPark = Catalog.MustPark(park);
        // Fresh teams for every match, so no match can inherit another's state through a shared roster.
        static (Team Home, Team Away) Teams() => PresetTeams.Pair(Catalog, "rio", "ashlord");
        foreach (var match in new[]
                 {
                     Match.Exhibition(Catalog, "rio", "ashlord", innings: 3, seed: seed, parkId: park, night: night),
                     Match.Exhibition(Catalog, Teams().Home, Teams().Away, innings: 3, seed: seed, parkId: park, night: night),
                     new Match(Catalog, Teams().Away, Teams().Home, catalogPark, 3, seed, night)
                 })
        {
            Assert.True(match.Hazards);
            Assert.Same(catalogPark, match.Park);
        }

        var byDefault = Game(Match.Exhibition(Catalog, "rio", "ashlord", innings: 3, seed: seed, parkId: park, night: night));
        var on = Game(Match.Exhibition(Catalog, "rio", "ashlord", innings: 3, seed: seed, parkId: park, night: night, hazards: true));
        var off = Game(Match.Exhibition(Catalog, "rio", "ashlord", innings: 3, seed: seed, parkId: park, night: night, hazards: false));
        Assert.Equal(on, byDefault);
        Assert.NotEqual(off, byDefault);

        static string Game(Match match)
        {
            match.AutoPlayGame();
            Assert.True(match.Over);
            return $"{match.AwayScore}-{match.HomeScore} " + string.Join("|", match.Log.Select(e => $"{e.Kind} {e.Caption}"));
        }
    }
}

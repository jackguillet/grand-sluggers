using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>3d (#715): export the #702 race fixtures, the #719 gap liner, the §10.4 double-play rows and the S-60 steal as version-2 traces on whichever root the environment names. Asserts nothing; records what would not run.</summary>
[Trait("Kind", "Balance")]
public sealed class ThreeDExportTests
{
    static readonly ContentCatalog Content = ContentCatalog.Load();
    const double Frame = 1.0 / 60;

    [Fact]
    public void Export()
    {
        var folder = Environment.GetEnvironmentVariable("GS_RACE_TRACE_OUTPUT");
        if (string.IsNullOrEmpty(folder)) return;
        Directory.CreateDirectory(folder);
        var errors = new List<string>();
        void Try(string id, Func<PlayTrace> make)
        {
            try { File.WriteAllText(Path.Combine(folder, id + ".json"), make().ToJson()); }
            catch (Exception e) { errors.Add(id + ": " + e.GetType().Name + " " + e.Message); }
        }
        AtBatResult Landing(Match m, double carry, double launch, double spray) => FlightFixtures.Landing(m.Park, carry, launch, spray, rules: m.Rules);
        AtBatResult Hit(Match m, double exit, double launch, double spray) => FlightFixtures.Hit(m.Park, exit, launch, spray, rules: m.Rules);

        // The #702 fixtures.
        Try("S31", () => { var m = Defense702("cinder"); return Run(m, Landing(m, 118, 4, -18)); });
        Try("S32", () => { var m = Defense702("dart"); return Run(m, Landing(m, 118, 4, -18)); });
        Try("harbor-fixed-grounder", () => { var m = Defense702("cinder", "harbor-diamond"); return Run(m, Hit(m, 84, 8, -12)); });
        Try("harbor-tactical-grounder", () => { var m = Defense702("cinder", "harbor-diamond"); return Run(m, Landing(m, 118, 4, -18)); });
        Try("harbor-fly", () => { var m = Defense702("cinder", "harbor-diamond"); return Run(m, Hit(m, 100, 30, -18)); });
        Try("relay", () =>
        {
            var m = Defense702("cinder");
            m.StationRunner(1, m.AwayOrder[2]);
            return Run(m, Landing(m, 118, 4, -18), new LiveSeats(false, true, true, false), LivePlayCommandSource.Cpu,
                pad: live => live.HoldsBall && !live.Throwing ? new LivePadInput(KeysBag: live.Throws == 0 ? 2 : 1, SouthDown: true) : LivePadInput.Dead);
        });
        // The #719 gap liner (the DiveTests fixture) and a routine fly to centre.
        Try("gap-liner-719", () => { var m = DefenseDive(); return Run(m, Landing(m, 280, 12, -8)); });
        Try("routine-fly-cf", () => { var m = DefenseDive(); return Run(m, Landing(m, 245, 34, 0)); });
        Try("hot-liner-lf", () => { var m = DefenseDive(); return Run(m, Hit(m, 120, 12, -25)); });
        // The §10.4 double-play rows on the CPU seat.
        foreach (var (id, carry, launch, spray, runners, outs, leadoff) in new (string, double, double, double, int[], int, string)[]
        {
            ("S40-6-4-3", 118, 4, -18, [1], 0, "cinder"), ("S41-4-6-3", 120, 4, 5, [1], 0, "cinder"), ("S42-5-4-3", 100, 4, -40, [1], 0, "cinder"),
            ("S43-3-tag", 92, 4, 41, [1], 0, "cinder"), ("S44-3-6-3", 110, 4, 38, [1], 0, "cinder"), ("S45-1-6-3", 62, 3, 1, [1], 0, "cinder"),
            ("S46-5u-3", 92, 4, -44, [1, 2], 0, "cinder"), ("S47-1-2-3", 38, 3, -6, [1, 2, 3], 0, "cinder"), ("S48-3-plate", 92, 4, 41, [1, 2, 3], 0, "cinder"),
            ("S50-two-outs", 118, 4, -18, [1], 2, "cinder"),
        })
        {
            Try(id, () =>
            {
                var m = DefenseOuts(leadoff);
                var roster = m.Away.Roster.ToList();
                foreach (var bag in runners) m.StationRunner(bag, roster[bag + 1]);
                m.SetOuts(outs);
                return Run(m, Landing(m, carry, launch, spray));
            });
        }
        // S-60: the straight steal of second on a take.
        Try("S60-steal", () =>
        {
            var m = DefenseOuts("cinder");
            m.StationRunner(1, m.AwayOrder[2]);
            if (!m.StartSteal()) throw new InvalidOperationException("no steal to arm");
            m.BeginAtBat(Scenario.Paint, Scenario.Take, out _, out var finished);
            if (finished is null || !m.StealThrowPending) throw new InvalidOperationException("no steal throw pending: " + finished?.Kind);
            var live = m.LivePlay;
            live.Recording = true;
            live.Apply(LivePlayCommand.BeginSteal(finished, LiveSeats.CpuOnly, LivePlayCommandSource.Cpu));
            PlayEvent? done = null;
            for (var i = 0; i < 60 * 30 && done is null; i++)
                done = live.Apply(LivePlayCommand.Tick(Frame, LivePadInput.Dead, LivePadInput.Dead, false, LivePlayCommandSource.Cpu)).CompletedPlay;
            if (done is null) throw new InvalidOperationException("the steal never completed");
            return live.TakeTrace(done);
        });
        File.WriteAllLines(Path.Combine(folder, "_errors.txt"), errors);
    }

    Match Defense702(string leadoff, string? park = null)
    {
        var home = Content.Team("Defense", "vale", "pewter", "lace", "frost", "basil", "ashlord", "vine", "moss", "hex");
        var rest = new[] { "jester", "soot", "grit", "nugget", "boom", "marlow", "gull", "pip" }.Where(id => id != leadoff).Take(7).ToArray();
        var away = Content.Team("Offense", "zig", new[] { leadoff }.Concat(rest).ToArray());
        var match = Match.Exhibition(Content, home, away, innings: 3, seed: 1, parkId: park);
        match.BeginAtBat(Scenario.Paint, Scenario.Swing, out _, out _);
        return match;
    }

    static Match DefenseDive()
    {
        var home = Content.Team("Defense", "vale", "pewter", "lace", "frost", "basil", "grit", "vine", "moss", "hex");
        var away = Content.Team("Offense", "rio", "boom", "cinder", "soot", "nugget", "nico", "gull", "marlow", "ashlord");
        return Match.Exhibition(Content, home, away, 3, 1, parkId: "harbor-diamond");
    }

    static Match DefenseOuts(string leadoff)
    {
        var home = Content.Team("Defense", "vale", "pewter", "lace", "frost", "basil", "ashlord", "vine", "moss", "hex");
        var away = Content.Team("Offense", "zig", leadoff, "dart", "jester", "cinder", "grit", "soot", "boom", "nugget");
        return Match.Exhibition(Content, home, away, 3, 1);
    }

    static PlayTrace Run(Match match, AtBatResult hit, LiveSeats? seats = null, LivePlayCommandSource source = LivePlayCommandSource.Cpu, Func<LivePlaySystem, LivePadInput>? pad = null)
    {
        var live = match.LivePlay;
        live.Recording = true;
        live.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, match.PreviewHit(hit), null, seats ?? LiveSeats.CpuOnly, source: source));
        PlayEvent? done = null;
        for (var i = 0; i < 60 * 30 && done is null; i++)
            done = live.Apply(LivePlayCommand.Tick(Frame, pad?.Invoke(live), source: source)).CompletedPlay;
        if (done is null) throw new InvalidOperationException("the play never completed");
        return live.TakeTrace(done);
    }
}

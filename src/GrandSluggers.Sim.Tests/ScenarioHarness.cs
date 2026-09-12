using System.Text;
using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// Headless scenario runner (spec Appendix B). Set the match state, script the seat's
/// commands (human) or let the CPU fill the seat, run <see cref="LivePlaySystem"/> to
/// Complete, and assert the outcome <em>and the reason</em> from the typed
/// <see cref="PlayOutcome"/>. No Unity scene object is ever involved (S-91). Later epics
/// add rows by spec id; this file is the rail they land on.
/// </summary>
public sealed class Scenario
{
    public static readonly PitchCommand Paint = new("fastball", 0, 0, false);
    public static readonly SwingCommand Swing = new(true, 0, 0, false);
    public static readonly SwingCommand Take = new(false, 0, 0, false);

    /// <summary>
    /// A normal fastball whose plate crossing is exactly (<paramref name="worldX"/>, <paramref name="worldY"/>)
    /// in world feet — the point the umpire, the body, and the cursor read (spec §3, §4.4).
    /// </summary>
    public static PitchCommand PitchAt(double worldX, double worldY, double charge = 0, bool changeup = false) =>
        PitchFlight.AimForCrossing(
            new PitchCommand("fastball", charge, 0, false, Changeup: changeup),
            worldX / PitchFlight.PlateScaleX,
            (worldY - PitchFlight.PlateY) / PitchFlight.PlateScaleY);

    /// <summary>A swing whose bat reaches the plane <paramref name="errFrames"/> after the ball (negative = early).</summary>
    public static SwingCommand SwingAt(double errFrames, double charge = 0, bool bunt = false, double stickX = 0, double launchAim = 0) =>
        new(true, charge, errFrames, false, AtBatResolver.SprayAimDeg(stickX), bunt, launchAim);

    public Scenario(ContentCatalog content, int seed = 1)
    {
        Content = content;
        Match = Match.Slice(content, innings: 3, seed: seed);
    }

    public ContentCatalog Content { get; }
    public Match Match { get; }

    /// <summary>Station the away order's n-th hitter on a bag (1–3).</summary>
    public Scenario Runner(int bag, int orderIndex)
    {
        Assert.True(Match.StationRunner(bag, Match.AwayOrder[orderIndex]), $"station bag {bag}");
        return this;
    }

    public Scenario Outs(int outs)
    {
        Assert.True(Match.SetOuts(outs), $"set outs {outs}");
        return this;
    }

    /// <summary>Put the ball in play with a fixed pitch and swing; the seed decides the contact.</summary>
    public AtBatResult Contact()
    {
        Assert.True(Match.BeginAtBat(Paint, Swing, out var hit, out _), "the scripted swing must put the ball in play");
        return hit;
    }

    /// <summary>
    /// Replay a recorded command script for one seat. Every command is re-stamped with the
    /// seat so the same script serves the CPU seat and the human seat (S-90).
    /// </summary>
    public LivePlayCommandResult Replay(LivePlayCommandSource seat, IEnumerable<LivePlayCommand> script)
    {
        LivePlayCommandResult? last = null;
        foreach (var command in script)
            last = Match.LivePlay.Apply(command with { Source = seat });
        Assert.NotNull(last);
        return last!;
    }

    /// <summary>The whole log as one comparable string: kind, caption, count, score, and the typed outcome.</summary>
    public string Stream() => Fingerprint(Match.Log);

    public static string Fingerprint(IEnumerable<PlayEvent> log)
    {
        var sb = new StringBuilder();
        foreach (var ev in log) sb.Append(Fingerprint(ev)).Append('\n');
        return sb.ToString();
    }

    public static string Fingerprint(PlayEvent ev)
    {
        var o = ev.Outcome ?? PlayOutcome.Empty;
        var outs = string.Join(",", o.OutsMade.Select(x => $"{x.Type}@{x.Bag}<{x.FromBag}:{x.Runner.Id}/{x.Fielder?.Id}"));
        var moves = string.Join(",", o.Moves.Select(m => $"{m.Runner.Id}:{m.FromBag}>{m.ToBag}"));
        return $"{ev.Kind}|{ev.Caption}|o{ev.OutsAfter}|{ev.AwayScoreAfter}-{ev.HomeScoreAfter}|op{ev.OutsOnPlay}"
               + $"|outs[{outs}]|moves[{moves}]|b{o.BatterToBag}|e{o.Error}|fc{o.FieldersChoice}"
               + $"|{o.DefensiveFeat}|{o.RunnerResult}{o.RunnerFromBag}>{o.RunnerToBag}|{o.ThrowEndpoint}"
               + $"|{ev.NextState}";
    }
}

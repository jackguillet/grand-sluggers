using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// The Unity pass of #719, #720 and #721: what each fielding body owes after the ball met it, as the client draws it
/// (<see cref="FielderTells"/>). Every play-level check drives a real live ball and reads it frame by frame — the stun on the
/// body that fumbled and on no other, the dive's recovery on the diver down then up, the brace on a hard ball and on no routine
/// one, the jump's root rise on the sim's own arc — and carries the debt past the play's completing frame the way the result
/// beat does. The shipped table owes nothing new; its one visible change is that its fumble shows the stun, not the batter's miss.
/// </summary>
public sealed class FielderTellsTests
{
    static readonly ContentCatalog Control = ContentCatalog.Load();
    static readonly string TrialDir = Path.GetFullPath(Path.Combine(Control.Root.Shipped, "..", "trials", "c80"));
    static readonly ContentCatalog Trial = ContentCatalog.Load(new DataRoot(Control.Root.Shipped, TrialDir));
    const double Frame = 1.0 / 60.0;
    static readonly LiveSeats HumanGlove = new(HumanBats: false, HumanPitches: true, PlayerMustField: true, Versus: false);
    static FieldTellsFeel Feel => Control.Feel.FieldTells;

    [Fact]
    public void TheFeelTableNamesTheGetUpAndTheBraceForBothTables()
    {
        Assert.Equal((0.20, 0.16), (Control.Feel.FieldTells.DiveGetUpSec, Control.Feel.FieldTells.BraceSquash));
        // The trial overlays no feel file: the client draws both tables with the same two numbers.
        Assert.Equal((0.20, 0.16), (Trial.Feel.FieldTells.DiveGetUpSec, Trial.Feel.FieldTells.BraceSquash));
        Assert.Throws<InvalidDataException>(() => new FieldTellsFeel { DiveGetUpSec = -0.1 }.Validate());
        Assert.Throws<InvalidDataException>(() => new FieldTellsFeel { BraceSquash = 0.5 }.Validate());
        Assert.Throws<InvalidDataException>(() => new FieldTellsFeel { BraceSquash = double.NaN }.Validate());
    }

    /// <summary>A debt is the body's, not the ring's: the fumbler and the diver show theirs while another body holds the ring.</summary>
    [Fact]
    public void ADebtIsTheBodysNotTheRings()
    {
        var o = FielderTells.Owed.None with { GlovePos = "LF", StunPos = "SS", StunSec = 0.3, DivingPos = "CF", DiveRecoverySec = 0.5 };
        Assert.Equal(Motion.Verb.Spin, FielderTells.Verb(o, "SS", Feel));
        Assert.Equal(Motion.Verb.Dive, FielderTells.Verb(o, "CF", Feel));
        Assert.Null(FielderTells.Verb(o, "LF", Feel));
        Assert.Equal(Motion.Verb.Crouch, FielderTells.Verb(o with { DiveRecoverySec = 0.20 }, "CF", Feel));
        // The ring's own debts: the jump and the brace ride the glove.
        var j = FielderTells.Owed.None with { GlovePos = "LF", Airborne = true, AirSec = 0.30, AirTotalSec = 0.60, RiseTopFt = 2 };
        Assert.Equal(2.0, FielderTells.RiseFt(j, "LF"), 9);
        Assert.Equal(0, FielderTells.RiseFt(j, "CF"));
        Assert.Equal(Motion.Verb.Catch, FielderTells.Verb(j, "LF", Feel));
        var b = FielderTells.Owed.None with { GlovePos = "P", Bracing = true, BraceSec = 0.10, BraceDurSec = 0.20 };
        Assert.Equal((1.04, 0.92, 1.04), Round(FielderTells.Brace(b, "P", 0.20, Feel)));
        Assert.Equal((1.0, 1.0, 1.0), FielderTells.Brace(b, "C", 0.20, Feel));
        Assert.Equal(FielderTells.Owed.None, FielderTells.Owed.None.Aged(1));
    }

    /// <summary>
    /// On a copy where every rising take fails: the 120-mph liner gets past left field, the 90-mph grounder drops at the
    /// shortstop's feet. Every frame of the 0.40-s stun shows the fumbler the stun and no other body; nobody ever shows the
    /// batter's miss, which carries the bat.
    /// </summary>
    [Theory]
    [InlineData(120, 16, -18, "LF", true)]
    [InlineData(90, 4, -25, "SS", false)]
    public void TheFumblerShowsTheStunAndNoOtherBodyDoes(double exit, double launch, double spray, string fumbler, bool deflects)
    {
        using var certain = new PatchedTrial(text => text
            .Replace("\"chanceCap\": 0.10", "\"chanceCap\": 1.0").Replace("\"handsCut\": 0.80", "\"handsCut\": 0")
            .Replace("\"hopMinApexFt\": 0.5", "\"hopMinApexFt\": 0").Replace("\"hopFullApexFt\": 1.5", "\"hopFullApexFt\": 0").Replace("\"hopPhaseHalfWidth\": 0.35", "\"hopPhaseHalfWidth\": 100"));
        var (match, live) = BeginCpu(certain.Content, exit, launch, spray, seed: 1, quality: ContactQuality.Perfect);
        var stunFrames = 0;
        var deflected = false;
        PlayEvent? play = null;
        for (var i = 0; i < 60 * 25 && play is null; i++)
        {
            play = Tick(live);
            if (play is not null) break;
            var o = FielderTells.Owed.Of(live, match.Rules);
            foreach (var pos in Diamond.Order)
            {
                var verb = FielderTells.Verb(o, pos, Feel);
                Assert.NotEqual(Motion.Verb.Miss, verb);
                if (live.StunT > 0 && pos == live.StunPos) Assert.Equal(Motion.Verb.Spin, verb);
                else Assert.NotEqual(Motion.Verb.Spin, verb);
            }
            if (live.StunT > 0)
            {
                Assert.Equal(fumbler, live.StunPos);
                stunFrames++;
                deflected |= live.Deflected;
            }
        }
        Assert.Equal(24, stunFrames);
        Assert.Equal(deflects, deflected);
    }

    /// <summary>
    /// The shipped fumble — the energy roll's whole-tick bobble on the glove — shows the stun too, for as long as the fumble
    /// holds, and never the batter's miss. (Before this pass the fielder took the batting miss, and the bat appeared in the glove hand.)
    /// </summary>
    [Fact]
    public void TheShippedFumbleShowsTheStunNotTheBattersMiss()
    {
        for (var seed = 1; seed <= 400; seed++)
        {
            var (match, live) = BeginCpu(Control, 110, 3, -25, seed, quality: ContactQuality.Perfect);
            var fumbleFrames = 0;
            PlayEvent? play = null;
            for (var i = 0; i < 60 * 8 && play is null; i++)
            {
                play = Tick(live);
                if (play is not null) break;
                var o = FielderTells.Owed.Of(live, match.Rules);
                Assert.False(o.Airborne || o.Bracing || o.DiveRecoverySec > 0 || o.StunSec > 0, "the shipped table owes nothing new");
                if (!live.Bobbling) continue;
                fumbleFrames++;
                Assert.Equal(Motion.Verb.Spin, FielderTells.Verb(o, live.GlovePos, Feel));
                foreach (var pos in Diamond.Order.Where(p => p != live.GlovePos))
                    Assert.Null(FielderTells.Verb(o, pos, Feel));
            }
            if (fumbleFrames == 0) continue;
            // The fumble's clock is rules.fielding.bobble.fumbleSec, counted down a frame at a time from the take.
            Assert.InRange(fumbleFrames, (int)(Control.Rules.Fielding.Bobble.FumbleSec * 60) - 1, (int)(Control.Rules.Fielding.Bobble.FumbleSec * 60) + 1);
            return;
        }
        Assert.Fail("no shipped fumble in 400 seeds of a hot grounder to short");
    }

    /// <summary>
    /// The trial's centre fielder commits on the gap liner and pays 0.555 s. While the debt lasts the diver stays laid out, and
    /// gets up in the last 0.20 s. The catch ends the play on the same frame, which resets the live field — the last live frame's
    /// debt, aged through the result beat, keeps the diver down for the rest of the recovery and up at its end.
    /// </summary>
    [Fact]
    public void TheDiverStaysDownThenGetsUpThroughTheResultBeat()
    {
        var (match, live) = BeginCpu(Trial, 130, 18, -6, seed: 1);
        var last = FielderTells.Owed.None;
        var commits = 0;
        PlayEvent? play = null;
        for (var i = 0; i < 60 * 12 && play is null; i++)
        {
            play = Tick(live);
            if (play is not null) break;
            if (live.Events.Contains(LiveEvent.DiveCommit)) commits++;
            last = FielderTells.Owed.Of(live, match.Rules);
            if (live.DiveRecoveryT > 0) AssertDown(last, live.DivingPos);
        }
        Assert.NotNull(play);
        Assert.Equal(1, commits);
        Assert.Equal(PlayKind.FlyOut, play.Kind);
        Assert.Equal("CF", last.DivingPos);
        var left = last.DiveRecoverySec;
        Assert.InRange(left, 0.50, 0.555 + 1e-9);

        var aged = last;
        var t = 0.0;
        var down = 0;
        var up = 0;
        while (aged.DiveRecoverySec > 0)
        {
            aged = aged.Aged(Frame);
            t += Frame;
            var verb = FielderTells.Verb(aged, "CF", Feel);
            if (aged.DiveRecoverySec <= 0) Assert.Null(verb);
            else if (verb == Motion.Verb.Dive) down++;
            else { Assert.Equal(Motion.Verb.Crouch, verb); up++; }
        }
        Assert.InRange(t, left - 1e-9, left + Frame);
        Assert.InRange(up, 11, 12);   // the last 0.20 s at 60 Hz
        Assert.True(down > 15, $"laid out for {down} frames");
    }

    /// <summary>
    /// The same liner moved 16 ft into the gap the frame after the commitment: the dive misses and the ball stays live, and the
    /// diver lies down and gets up on the dive's own clock, whoever the ring goes to meanwhile.
    /// </summary>
    [Fact]
    public void AMissedDiveStillLiesTheDiverDownWhoeverHoldsTheRing()
    {
        var (match, live) = BeginCpu(Trial, 130, 18, -6, seed: 1);
        var owedFrames = 0;
        var nudged = false;
        PlayEvent? play = null;
        for (var i = 0; i < 60 * 12 && play is null; i++)
        {
            play = Tick(live);
            if (play is not null) break;
            if (!nudged && live.Events.Contains(LiveEvent.DiveCommit))
            {
                live.NudgeBall(-16, 0);
                nudged = true;
            }
            if (live.DiveRecoveryT <= 0) continue;
            owedFrames++;
            var o = FielderTells.Owed.Of(live, match.Rules);
            AssertDown(o, live.DivingPos);
            foreach (var pos in Diamond.Order.Where(p => p != live.DivingPos))
                Assert.NotEqual(Motion.Verb.Dive, FielderTells.Verb(o, pos, Feel));
        }
        Assert.True(nudged, "the centre fielder never committed");
        Assert.InRange(owedFrames, 32, 34);   // 0.555 s
    }

    /// <summary>
    /// West on the trial: every airborne frame the glove's root rises exactly what the sim's arc says, two feet at the apex, and
    /// no other body rises; the jumper reaches up. From the apex, the debt aged on its own lands on the same arc at 0.60 s.
    /// </summary>
    [Fact]
    public void TheJumpersRootRisesOnTheSimsArcAndLands()
    {
        var (match, live) = BeginJump(Trial);
        Tick(live, 60, _ => LivePadInput.Dead);
        var rises = new List<double>();
        FielderTells.Owed? apex = null;
        Tick(live, 40, i => i == 0 ? new LivePadInput(WestDown: true) : LivePadInput.Dead, () =>
        {
            var o = FielderTells.Owed.Of(live, match.Rules);
            var rise = FielderTells.RiseFt(o, live.GlovePos);
            Assert.Equal(live.JumpHeightFt, rise, 9);
            foreach (var pos in Diamond.Order.Where(p => p != live.GlovePos))
                Assert.Equal(0, FielderTells.RiseFt(o, pos));
            Assert.Equal(live.Airborne ? Motion.Verb.Catch : (Motion.Verb?)null, FielderTells.Verb(o, live.GlovePos, Feel));
            rises.Add(rise);
            if (rises.Count == 19) apex = o;
        });
        Assert.Equal(2.0, rises.Max(), 9);
        Assert.Equal(2.0, rises[18], 9);
        Assert.Equal(0, rises[^1]);

        var aged = apex!.Value;
        var glove = aged.GlovePos;
        for (var k = 1; k <= 18; k++)
        {
            aged = aged.Aged(Frame);
            var u = Math.Min(1, (0.30 + k * Frame) / 0.60);
            Assert.Equal(k < 18 ? 4 * 2.0 * u * (1 - u) : 0, FielderTells.RiseFt(aged, glove), 9);
        }
        Assert.False(aged.Airborne, "landed at the airtime");
    }

    /// <summary>West on the shipped table arms the old window: nothing rises, the debt names no jump, the jump take keeps its own lift.</summary>
    [Fact]
    public void TheShippedJumpRisesNothing()
    {
        var (match, live) = BeginJump(Control);
        Tick(live, 60, _ => LivePadInput.Dead);
        var armed = false;
        Tick(live, 40, i => i == 0 ? new LivePadInput(WestDown: true) : LivePadInput.Dead, () =>
        {
            armed |= live.JumpT > 0;
            var o = FielderTells.Owed.Of(live, match.Rules);
            Assert.False(o.Airborne);
            Assert.Equal(0, FielderTells.RiseFt(o, live.GlovePos));
            Assert.Null(FielderTells.Verb(o, live.GlovePos, Feel));
        });
        Assert.True(armed, "West armed the shipped window");
    }

    /// <summary>
    /// The trial's comebacker costs vale 0.13 s (weight 0.65): the frame it lands the pitcher squashes 0.16 × 0.65 and eases back
    /// to full shape as the recovery runs out; no other body braces. The routine grounder braces nobody on any frame, and the
    /// shipped knockback on the same comebacker is not a brace.
    /// </summary>
    [Fact]
    public void AHardBallBracesTheGloveAndARoutineOneBracesNobody()
    {
        var cap = Trial.Rules.Fielding.Recoil.CapSec;
        var (match, live) = BeginCpu(Trial, 125, 2, 0, seed: 1, quality: ContactQuality.Perfect);
        var braced = new List<double>();
        var dur = 0.0;
        PlayEvent? play = null;
        for (var i = 0; i < 60 * 8 && play is null; i++)
        {
            play = Tick(live);
            if (play is not null) break;
            var o = FielderTells.Owed.Of(live, match.Rules);
            foreach (var pos in Diamond.Order.Where(p => p != "P"))
                Assert.Equal((1.0, 1.0, 1.0), FielderTells.Brace(o, pos, cap, Feel));
            var brace = FielderTells.Brace(o, "P", cap, Feel);
            if (!o.Bracing) { Assert.Equal((1.0, 1.0, 1.0), brace); continue; }
            Assert.Equal("P", live.GlovePos);
            Assert.Equal(1 - 0.16 * (live.RecoilDur / cap) * (live.RecoilT / live.RecoilDur), brace.Y, 9);
            Assert.Equal(1 + (1 - brace.Y) * 0.5, brace.X, 9);
            braced.Add(brace.Y);
            dur = live.RecoilDur;
        }
        Assert.Equal(0.13, dur, 9);
        Assert.InRange(braced.Count, 7, 9);
        Assert.Equal(1 - 0.16 * 0.65, braced[0], 9);   // the take: the full squash at this ball's weight
        Assert.True(braced.Zip(braced.Skip(1)).All(p => p.Second > p.First), "the brace eases out, never deepens");

        // The routine grounder on both tables, and the shipped knockback on the same comebacker: nobody braces on any frame.
        foreach (var (content, exit2, launch2, quality) in new[]
                 {
                     (Trial, 70.0, 8.0, ContactQuality.Nice), (Control, 70.0, 8.0, ContactQuality.Nice), (Control, 125.0, 2.0, ContactQuality.Perfect)
                 })
        {
            var (m, l) = BeginCpu(content, exit2, launch2, 0, seed: 1, quality: quality);
            PlayEvent? done = null;
            for (var i = 0; i < 60 * 8 && done is null; i++)
            {
                done = Tick(l);
                if (done is not null) break;
                var o = FielderTells.Owed.Of(l, m.Rules);
                Assert.False(o.Bracing);
                foreach (var pos in Diamond.Order)
                    Assert.Equal((1.0, 1.0, 1.0), FielderTells.Brace(o, pos, cap, Feel));
            }
        }
    }

    // ---------------------------------------------------------------------------------
    // Harness
    // ---------------------------------------------------------------------------------

    /// <summary>The defence of DiveTests, DeflectionTests and RecoilTests: vale on the mound, vine in left, moss (Field 4) in centre.</summary>
    static readonly string[] Defense = ["vale", "pewter", "lace", "frost", "basil", "grit", "vine", "moss", "hex"];

    static void AssertDown(FielderTells.Owed o, string diver)
    {
        var verb = FielderTells.Verb(o, diver, Control.Feel.FieldTells);
        Assert.Equal(o.DiveRecoverySec > Control.Feel.FieldTells.DiveGetUpSec + 1e-9 ? Motion.Verb.Dive : Motion.Verb.Crouch, verb);
    }

    /// <summary>Harbor, the CPU on both sides of the ball; the contact judged by the flight (Nice unless the source test named Perfect).</summary>
    static (Match Match, LivePlaySystem Live) BeginCpu(ContentCatalog content, double exit, double launch, double spray, int seed,
        ContactQuality quality = ContactQuality.Nice)
    {
        var d = Defense;
        var home = content.Team("Defense", d[0], d[1], d[2], d[3], d[4], d[5], d[6], d[7], d[8]);
        var away = content.Team("Offense", "rio", "boom", "cinder", "soot", "nugget", "nico", "gull", "marlow", "ashlord");
        var match = Match.Exhibition(content, home, away, 3, seed, parkId: "harbor-diamond");
        var hit = FlightFixtures.Hit(match.Park, exit, launch, spray, quality, rules: match.Rules);
        Assert.False(hit.Foul);
        var preview = match.PreviewHit(hit);
        var live = match.LivePlay;
        Assert.True(live.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, preview, null, LiveSeats.CpuOnly, 0, LivePlayCommandSource.Cpu)).Snapshot.Active);
        return (match, live);
    }

    /// <summary>JumpTests' fly: 245 ft at 34° to centre on Harbor, basil in centre, the human seat on the glove.</summary>
    static (Match Match, LivePlaySystem Live) BeginJump(ContentCatalog content)
    {
        var home = content.Team("Defense", "vale", "pewter", "lace", "frost", "grit", "marlow", "vine", "basil", "hex");
        var away = content.Team("Offense", "rio", "boom", "cinder", "soot", "nugget", "nico", "gull", "konga", "ashlord");
        var match = Match.Exhibition(content, home, away, 3, 1, parkId: "harbor-diamond");
        var hit = FlightFixtures.Landing(match.Park, 245, 34, 0, rules: match.Rules);
        var preview = match.PreviewHit(hit);
        Assert.Equal("CF", preview.Position);
        var live = match.LivePlay;
        Assert.True(live.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, preview, null, HumanGlove, 0, LivePlayCommandSource.Human)).Snapshot.Active);
        return (match, live);
    }

    static PlayEvent? Tick(LivePlaySystem live) =>
        live.Apply(LivePlayCommand.Tick(Frame, LivePadInput.Dead, LivePadInput.Dead, false, LivePlayCommandSource.Cpu)).CompletedPlay;

    static void Tick(LivePlaySystem live, int frames, Func<int, LivePadInput> pad, Action? afterEach = null)
    {
        for (var i = 0; i < frames; i++)
        {
            var r = live.Apply(LivePlayCommand.Tick(Frame, pad(i), LivePadInput.Dead, false, LivePlayCommandSource.Human));
            Assert.Null(r.CompletedPlay);
            afterEach?.Invoke();
        }
    }

    static (double, double, double) Round((double X, double Y, double Z) v) => (Math.Round(v.X, 9), Math.Round(v.Y, 9), Math.Round(v.Z, 9));

    /// <summary>The c80 copy with its fielding table changed, laid over the shipped root.</summary>
    sealed class PatchedTrial : IDisposable
    {
        readonly string _dir;

        public PatchedTrial(Func<string, string> fielding)
        {
            _dir = Path.Combine(Path.GetTempPath(), "grand-sluggers-tells-" + Guid.NewGuid().ToString("N"));
            foreach (var file in Directory.GetFiles(Control.Root.Shipped, "*", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(Control.Root.Shipped, file);
                var source = Path.Combine(TrialDir, relative);
                if (!File.Exists(source)) continue;
                var to = Path.Combine(_dir, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(to)!);
                var text = File.ReadAllText(source);
                if (relative.Replace('\\', '/') == "rules/fielding.json") text = fielding(text);
                File.WriteAllText(to, text);
            }
            Content = ContentCatalog.Load(new DataRoot(Control.Root.Shipped, _dir));
        }

        public ContentCatalog Content { get; }

        public void Dispose() => Directory.Delete(_dir, recursive: true);
    }
}

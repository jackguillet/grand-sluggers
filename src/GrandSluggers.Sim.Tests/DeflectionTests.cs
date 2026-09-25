using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// 3c-5 slice 2 (#721: F693-02-expanded-ordinary-error-outcomes, -error-outcome-selection, -continuing-error-reaction, -recovery,
/// -direction, -speed-retention, -vertical-retention, -ground-response, -uniform-error-direction): a failed take does not always drop at
/// the feet. The contact and the ball's speed decide, never a second roll: a glancing touch on a hot ball sends it on with 50–80 % of
/// its speed inside ±15° of its travel, a batted ball still on the shared ground physics; the same 0.40-s stun, the same reliable
/// recovery, whoever reaches it.
/// </summary>
public sealed class DeflectionTests
{
    static readonly ContentCatalog Game = Shipped.Content;
    const double Frame = 1.0 / 60.0;

    [Fact]
    [Trait("Kind", "Balance")]
    public void TheBranchIsTheContactsAndTheSpeedsNeverASecondRoll()
    {
        var h = Game.Rules.Fielding.Handling;
        Assert.Equal((15.0, 0.80, 0.50, 0.5, 55.0), (h.DeflectSpreadDeg, h.DeflectRetainMax, h.DeflectRetainMin, h.DeflectObstruction, h.DeflectMinFtPerSec));
        var r = Game.Rules;
        // Obstruction: 1 with the ball at the body, 0 at the edge of the take's window.
        Assert.Equal(1, FieldingResolver.Obstruction(0, 10));
        Assert.Equal(0.5, FieldingResolver.Obstruction(5, 10), 9);
        Assert.Equal(0, FieldingResolver.Obstruction(10, 10));
        Assert.Equal(0, FieldingResolver.Obstruction(12, 10));
        // Retention: 0.80 for a glancing touch, 0.65 half way, 0.50 at the knockdown boundary and beyond.
        Assert.Equal(0.80, FieldingResolver.DeflectionRetention(0, r), 9);
        Assert.Equal(0.65, FieldingResolver.DeflectionRetention(0.25, r), 9);
        Assert.Equal(0.50, FieldingResolver.DeflectionRetention(0.5, r), 9);
        Assert.Equal(0.50, FieldingResolver.DeflectionRetention(1, r), 9);
        // The ball gets past only on a glancing touch of a hot ball; a square touch or an ordinary ball drops at the feet.
        Assert.True(FieldingResolver.DeflectionContinues(0.0, 55, r));
        Assert.True(FieldingResolver.DeflectionContinues(0.49, 90, r));
        Assert.False(FieldingResolver.DeflectionContinues(0.5, 90, r));
        Assert.False(FieldingResolver.DeflectionContinues(0.0, 54.9, r));
        Assert.False(FieldingResolver.DeflectionContinues(1.0, 40, r));
    }

    /// <summary>
    /// A hard, rising shortstop hop meets the edge of the glove. A certain failed take continues
    /// with contact-dependent horizontal retention and its signed vertical velocity. The fielder
    /// recovers after the ordinary stun without a second roll; replay is deterministic.
    /// </summary>
    [Fact]
    public void TheHotLinersFailedTakeGetsPastAndIsRecoveredWithoutASecondRoll()
    {
        using var certain = new PatchedGame(text => text
            .Replace("\"chanceCap\": 0.10", "\"chanceCap\": 1.0").Replace("\"handsCut\": 0.80", "\"handsCut\": 0")
            .Replace("\"hopMinApexFt\": 0.5", "\"hopMinApexFt\": 0").Replace("\"hopFullApexFt\": 1.5", "\"hopFullApexFt\": 0").Replace("\"hopPhaseHalfWidth\": 0.35", "\"hopPhaseHalfWidth\": 100"));
        var run = Drive(certain.Content, 120, -12, -18, ContactQuality.Perfect, seed: 35);
        Assert.Equal(1, run.Bobbles);
        Assert.True(run.Deflected, "a glancing touch on a hot ball gets past");
        Assert.InRange(run.Obstruction, 0, Game.Rules.Fielding.Handling.DeflectObstruction - 1e-6);
        Assert.True(run.Speed > 55);
        var retain = FieldingResolver.DeflectionRetention(run.Obstruction, Game.Rules);
        Assert.InRange(run.OutSpeed / run.HSpeedIn, retain - .03, retain + .01);   // Contact-dependent retention, with one frame of drag and roll in the read
        Assert.InRange(run.DirOffsetDeg, -15, 15);
        Assert.True(run.MaxY > run.ContactY + 0.01, $"the ball kept rising to {run.MaxY:0.00} from {run.ContactY:0.00}: the signed vertical was kept");
        Assert.True(run.Travel > 12);
        Assert.Equal(24, run.StunFrames);
        Assert.NotEmpty(run.RecoverPos);
        Assert.True(run.RecoverAt - run.TakeAt >= Game.Rules.Fielding.Handling.StunSec - 1e-9);
        Assert.Equal(1, run.Rolls);
        Assert.False(run.Play.Outcome?.Error ?? true);

        var again = Drive(certain.Content, 120, -12, -18, ContactQuality.Perfect, seed: 35);
        Assert.Equal((run.Deflected, run.Obstruction, run.DirOffsetDeg), (again.Deflected, again.Obstruction, again.DirOffsetDeg));
        Assert.Equal(run.Marks, again.Marks);
    }

    /// <summary>
    /// On a copy where every rising take fails: a 96-mph liner into left at 58 ft/s gets past, a 90-mph grounder into the hole at 47 ft/s
    /// drops at the shortstop's feet — the same edge-of-ring contact, the speed the only difference.
    /// </summary>
    [Fact]
    public void TheSpeedDecidesBetweenGettingPastAndTheKnockdown()
    {
        using var certain = new PatchedGame(text => text
            .Replace("\"chanceCap\": 0.10", "\"chanceCap\": 1.0").Replace("\"handsCut\": 0.80", "\"handsCut\": 0")
            .Replace("\"hopMinApexFt\": 0.5", "\"hopMinApexFt\": 0").Replace("\"hopFullApexFt\": 1.5", "\"hopFullApexFt\": 0").Replace("\"hopPhaseHalfWidth\": 0.35", "\"hopPhaseHalfWidth\": 100"));
        var hot = Drive(certain.Content, 120, -12, -18, ContactQuality.Perfect, seed: 1);
        Assert.Equal("SS", hot.Pos);
        Assert.Equal(1, hot.Bobbles);
        Assert.True(hot.Deflected);
        Assert.InRange(hot.Obstruction, 0, Game.Rules.Fielding.Handling.DeflectObstruction - 1e-6);
        Assert.True(hot.Speed >= 55);
        Assert.True(hot.Travel > 12);
        Assert.Equal(1, hot.Rolls);

        var ordinary = Drive(certain.Content, 75, 6, 30, ContactQuality.Perfect, seed: 1);
        Assert.Equal("2B", ordinary.Pos);
        Assert.Equal(1, ordinary.Bobbles);
        Assert.False(ordinary.Deflected, "an ordinary ball drops at the feet");
        Assert.InRange(ordinary.Obstruction, 0, 0.1);
        Assert.True(ordinary.Speed < 55);
        Assert.InRange(ordinary.OutSpeed, 5.9, 6.01);
        Assert.InRange(ordinary.Travel, 2, 3.6);
        Assert.Equal(1, ordinary.Rolls);
    }

    /// <summary>The continued path is the shared physics from the contact: the samples before it untouched, the ball reread, and it comes to rest where the roll says.</summary>
    [Fact]
    public void TheContinuedPathIsTheSharedPhysicsFromTheContact()
    {
        var park = Game.Parks[ParkId.Harbor];
        var hit = FlightFixtures.Hit(park, 100, 4, 0, ContactQuality.Perfect, rules: Game.Rules);
        var ball = BattedBall.Of(hit, park, Game.Rules);
        var path = ball.Samples;
        var t0 = BallFlight.HangTime(path, Game.Rules) + 0.30;
        var (x, y, z) = BallFlight.PointAt(path, t0, Game.Rules);
        var next = BallFlight.PointAt(path, t0 + 1.0 / 120, Game.Rules);
        var vx = (next.X - x) * 120; var vz = (next.Z - z) * 120;
        var speed = Math.Sqrt(vx * vx + vz * vz);
        Assert.True(speed > 20, $"the ball still had {speed:0.0} ft/s at {t0:0.00}");
        // Reverse it at half speed off the contact: the prefix is byte for byte the original, the continuation starts there.
        var cont = BallFlight.Continue(path, t0, x, y, z, -vx * 0.5, 0, -vz * 0.5, hit.LaunchDeg, hit.ExitVeloMph, park, Game.Rules);
        var prefix = path.Where(s => s.T < t0 - 1e-9).ToList();
        Assert.Equal(prefix, cont.Take(prefix.Count).ToList());
        Assert.Equal(t0, cont[prefix.Count].T, 9);
        Assert.Equal((x, z), (cont[prefix.Count].X, cont[prefix.Count].Z));
        var after = BallFlight.PointAt(cont, t0 + 0.25, Game.Rules);
        Assert.True(Diamond.Dist(after.X, after.Z, x, z) > 3, "the ball went back the way it came");
        Assert.True((after.X - x) * vx + (after.Z - z) * vz < 0, "against its old travel");
        Assert.True(BallFlight.RestTime(cont) > t0 + 0.5 && BallFlight.RestTime(cont) < t0 + 6, $"rest at {BallFlight.RestTime(cont):0.00}");
        Assert.Equal(BallFlight.HangTime(path, Game.Rules), BallFlight.HangTime(cont, Game.Rules), 9);   // the landing mark is the original's
        var reread = BattedBall.Reread(cont, hit.ExitVeloMph, hit.LaunchDeg, false, park, Game.Rules);
        Assert.False(reread.Foul);
        Assert.Equal(ball.Shape, reread.Shape);
    }

    // ---------------------------------------------------------------------------------
    // Harness
    // ---------------------------------------------------------------------------------

    sealed record Run(PlayEvent Play, string Pos, double TakeAt, double Chance, int Bobbles, int Rolls, bool Deflected, double Obstruction, double Speed, double HSpeedIn,
        double ContactY, double MaxY, double OutSpeed, double DirOffsetDeg, double Travel, int StunFrames, double RecoverAt, string RecoverPos, IReadOnlyList<(PlayTraceMarkKind Kind, double T)> Marks);

    static Run Drive(ContentCatalog content, double exitMph, double launchDeg, double sprayDeg, ContactQuality quality, int seed)
    {
        // The unclassed defense (§8.1): the table's reach, so the obstruction these rows read is the ball's and the ring's alone.
        var home = content.Team("Defense", "vale", "pewter", "lace", "frost", "basil", "grit", "vine", "moss", "hex").Unclassed();
        var away = content.Team("Offense", "rio", "boom", "cinder", "soot", "nugget", "nico", "gull", "marlow", "ashlord");
        var match = Match.Exhibition(content, home, away, 3, seed, parkId: ParkId.Harbor);
        var hit = FlightFixtures.Hit(match.Park, exitMph, launchDeg, sprayDeg, quality, rules: match.Rules);
        Assert.False(hit.Foul);
        var preview = match.PreviewHit(hit);
        var live = match.LivePlay;
        live.Recording = true;
        Assert.True(live.Apply(LivePlayCommand.BeginLive(Scenario.Paint, Scenario.Swing, hit, preview, null, LiveSeats.CpuOnly, 0, LivePlayCommandSource.Cpu)).Snapshot.Active);
        PlayEvent? play = null;
        var takeAt = -1.0; var chance = 0.0; var bobbles = 0; var rolls = 0; var lastChance = -1.0; var pos = ""; var deflected = false; var obstruction = 0.0; var speed = 0.0; var hIn = 0.0;
        var contactY = 0.0; var maxY = 0.0; var outSpeed = 0.0; var dir = double.NaN; var travel = 0.0; var stunFrames = 0; var recoverAt = -1.0; var recoverPos = "";
        (double X, double Y, double Z) prev = (0, 0, 0); (double X, double Z)? at = null; (double X, double Z)? inDir = null; var frames = 0;
        for (var i = 0; i < 60 * 25 && play is null; i++)
        {
            prev = (live.BallX, live.BallY, live.BallZ);
            var r = live.Apply(LivePlayCommand.Tick(Frame, LivePadInput.Dead, LivePadInput.Dead, false, LivePlayCommandSource.Cpu));
            play = r.CompletedPlay;
            if (play is not null) break;
            if (live.HandlingChance != lastChance) { if (live.HandlingChance > 0) rolls++; lastChance = live.HandlingChance; }
            var bobbledNow = live.Events.Contains(LiveEvent.Bobble);
            if (bobbledNow) bobbles++;
            if (takeAt < 0 && (live.HoldsBall || bobbledNow))
            {
                takeAt = live.ElapsedSeconds; chance = live.HandlingChance; pos = live.GlovePos; speed = live.IncomingFtPerSec;
                if (bobbledNow)
                {
                    deflected = live.Deflected; obstruction = live.ErrorObstruction; contactY = live.BallY; at = (live.BallX, live.BallZ);
                    inDir = (live.BallX - prev.X, live.BallZ - prev.Z); hIn = Math.Sqrt(inDir.Value.X * inDir.Value.X + inDir.Value.Z * inDir.Value.Z) * 60;
                }
            }
            if (bobbles > 0 && recoverAt < 0)
            {
                frames++;
                maxY = Math.Max(maxY, live.BallY);
                if (at is { } a0) travel = Math.Max(travel, Diamond.Dist(a0.X, a0.Z, live.BallX, live.BallZ));
                if (frames == 2 && at is { } a1 && inDir is { } d0)
                {
                    var ox = live.BallX - a1.X; var oz = live.BallZ - a1.Z;
                    outSpeed = Math.Sqrt(ox * ox + oz * oz) * 60;
                    var dd = (Math.Atan2(ox, oz) - Math.Atan2(d0.X, d0.Z)) * 180 / Math.PI;
                    while (dd > 180) dd -= 360;
                    while (dd < -180) dd += 360;
                    dir = dd;
                }
                if (live.StunT > 0) stunFrames++;
                if (live.HoldsBall) { recoverAt = live.ElapsedSeconds; recoverPos = live.GlovePos; }
            }
        }
        Assert.NotNull(play);
        Assert.True(takeAt > 0, "nobody took the ball");
        var marks = (live.TakeTrace(play).Marks ?? []).Select(m => (m.Kind, m.T)).ToList();
        return new Run(play, pos, takeAt, chance, bobbles, rolls, deflected, obstruction, speed, hIn, contactY, maxY, outSpeed, dir, travel, stunFrames, recoverAt, recoverPos, marks);
    }

    /// <summary>The game with its fielding table changed: an overlay carrying the one patched file over the shipped root.</summary>
    sealed class PatchedGame : IDisposable
    {
        readonly string _dir;

        public PatchedGame(Func<string, string> fielding)
        {
            _dir = Path.Combine(Path.GetTempPath(), "grand-sluggers-deflect-" + Guid.NewGuid().ToString("N"));
            var to = Path.Combine(_dir, "rules", "fielding.json");
            Directory.CreateDirectory(Path.GetDirectoryName(to)!);
            File.WriteAllText(to, fielding(File.ReadAllText(Path.Combine(Game.Root.Shipped, "rules", "fielding.json"))));
            Content = ContentCatalog.Load(new DataRoot(Game.Root.Shipped, _dir));
        }

        public ContentCatalog Content { get; }

        public void Dispose() => Directory.Delete(_dir, recursive: true);
    }
}

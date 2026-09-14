using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace GrandSluggers.Sim;

/// <summary>Identity of effective runtime inputs, including difficulty overrides. No file-system or RNG reads.</summary>
public sealed record PlayTraceIdentity(string Build, string ModuleId, string Sha256, string InputsJson)
{
    public static PlayTraceIdentity Capture(Match match)
    {
        var inputs = JsonSerializer.Serialize(new
        {
            match.Rules, match.Content.Feel, match.Park, match.Home, match.Away,
            match.Content.StarSkills,
            Chemistry = match.Home.Roster.Concat(match.Away.Roster).Distinct().OrderBy(c => c.Id, StringComparer.Ordinal)
                .SelectMany(a => match.Home.Roster.Concat(match.Away.Roster).Distinct().OrderBy(c => c.Id, StringComparer.Ordinal)
                    .Select(b => new { From = a.Id, To = b.Id, Relation = match.Chemistry.Between(a, b) })).ToArray(),
            match.Night, match.Mercy, match.Innings
        }, PlayTrace.Json);
        using var hash = SHA256.Create();
        var digest = BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(inputs))).Replace("-", "").ToLowerInvariant();
        var assembly = typeof(Match).Assembly;
        return new(assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown",
            assembly.ManifestModule.ModuleVersionId.ToString(), digest, inputs);
    }
}

public sealed record PlayTraceContext(int Seed, int Inning, bool Top, int Outs, int Balls, int Strikes,
    Character Batter, Character Pitcher, LiveSeats Seats, string Difficulty, Park Park,
    PlayTraceIdentity Identity, string Clock = "seconds since live-play start; contact for batted balls",
    string DistanceUnit = "ft", string VelocityUnit = "ft/s",
    string ReplayScope = "match seed plus preceding match commands/fixture setup required; not an RNG checkpoint");

/// <summary>Commands submitted at the public boundary, before ownership filtering. Pads retain literal presses.</summary>
public sealed record PlayTraceCommand(int I, double T, LivePlayCommand Input);
public enum PlayTraceMarkKind
{
    Contact, RunnerPlayStart, Possession, ThrowRelease, ThrowTargetReached, UncoveredWait,
    Reception, LooseBall, RunnerArrival, Out, Verdict
}

/// <summary>T is the simulation execution clock. LowerT bounds sampled runner arrivals; animation release is unobserved.</summary>
public sealed record PlayTraceMark(int I, int Command, PlayTraceMarkKind Kind, double T, double LowerT,
    int? Leg, string? Fielder, int? Bag, PlayTraceRunner? Runner, OutType? OutType,
    double? PredictedRunnerAt, PlayTraceThrow? Flight, InPlay.ThrowVerdict? Verdict, PlayTraceMarkGeometry? Geometry = null);
public sealed record PlayTraceMarkGeometry(double BallX, double BallY, double BallZ, string GlovePos,
    double GloveX, double GloveZ, bool HoldsUnthrownBall, string ReceiverPos, double? ReceiverX, double? ReceiverZ,
    double? ReceiverDistanceFt, double CoverRadiusFt, bool ReceiverInReach);
public sealed record PlayTraceThrow(string FromPos, string ReceiverPos, int Bag,
    double FromX, double FromY, double FromZ, double ToX, double ToY, double ToZ,
    double DurationSec, double SpeedMul, string Chemistry);

/// <summary>Assignments and gates are separate from measured displacement: cover/coast can differ from pursuit speed.</summary>
public sealed record PlayTraceFielder(string Pos, Character Who, double X, double Z,
    double ReadEligibleAt, bool ReadEligible, bool Selected, bool HumanOwned,
    double PursuitSpeedFtSec, bool FrozenPreview, bool DashHeld, bool Coasting,
    bool CutoffAssigned, bool BackupAssigned, double DiveRemainingSec, double JumpRemainingSec,
    double RecoilRemainingSec, double SwapLockRemainingSec, double? ObservedVx = null, double? ObservedVz = null);
public sealed record PlayTraceCoverage(int Bag, string Pos, double X, double Z, double DistanceFt,
    double RadiusFt, bool InReach, bool PossessionInReach, bool ForceAtBag);

public sealed partial class LivePlaySystem
{
    internal bool TraceHoldsBall => (Path is null && !RunnerPlay ? HasBall : HoldsBall) && !Throwing;

    internal PlayTraceMarkGeometry TraceMarkGeometry()
    {
        var receiver = ThrowBag is >= 1 and <= 4 ? CoverPos : _cutoffPos;
        var hasReceiver = _fielders.TryGetValue(receiver, out var at);
        var target = ThrowBag is >= 1 and <= 4 ? Diamond.Bag(ThrowBag) : _cutoffSpot ?? (ThrowTo.X, ThrowTo.Z);
        double? distance = hasReceiver ? Diamond.Dist(at.X, at.Z, target.Item1, target.Item2) : null;
        return new(BallX, BallY, BallZ, GlovePos, GloveX, GloveZ, HoldsBall && !Throwing,
            receiver, hasReceiver ? at.X : null, hasReceiver ? at.Z : null, distance, R.Fielding.Cover.RadiusFt,
            hasReceiver && receiver != _throwerPos && distance <= R.Fielding.Cover.RadiusFt);
    }

    internal PlayTraceContext TraceContext(LivePlayCommand command) => new(_match.Seed, _match.Inning, _match.Top,
        _match.Outs, _match.Balls, _match.Strikes, _match.Batter, _match.Pitcher,
        command.Seats ?? Seats, _match.Difficulty, Park, PlayTraceIdentity.Capture(_match));

    internal IReadOnlyList<PlayTraceFielder> TraceFielders()
    {
        var pad = Seats.OwnedFieldPad(_trace?.CurrentCommand?.FieldPad);
        return Assigned().OrderBy(kv => kv.Key, StringComparer.Ordinal).Select(kv =>
        {
            var pos = kv.Key;
            var at = pos == GlovePos ? (GloveX, GloveZ) : _fielders.TryGetValue(pos, out var feet) ? feet : Diamond.Positions[pos];
            var dash = pos == GlovePos && pad.EastHeld;
            return new PlayTraceFielder(pos, kv.Value, at.Item1, at.Item2, ReadyAt(pos), CanMove(pos), pos == GlovePos,
                HumanGlove(pos), FieldingResolver.ChaseSpeedFt(kv.Value, pos, Preview, R, dash), Preview?.Frozen ?? false,
                dash, Coasting(pos), pos == _cutoffPos, pos == _backupPos,
                pos == GlovePos ? DiveT : 0, pos == GlovePos ? JumpT : 0,
                pos == GlovePos ? RecoilT : 0, pos == GlovePos ? SwapLock : 0);
        }).ToArray();
    }

    internal IReadOnlyList<PlayTraceCoverage> TraceCoverage()
    {
        return Enumerable.Range(1, 4).Select(bag =>
        {
            var pos = CoverOf(bag);
            var goal = Diamond.Bag(bag);
            var at = pos == GlovePos ? (GloveX, GloveZ) : _fielders.TryGetValue(pos, out var feet) ? feet : goal;
            var dist = Diamond.Dist(at.Item1, at.Item2, goal.X, goal.Z);
            var reachable = !string.IsNullOrEmpty(pos) && _fielders.ContainsKey(pos) && dist <= R.Fielding.Cover.RadiusFt;
            var heldDistance = Diamond.Dist(GloveX, GloveZ, goal.X, goal.Z);
            return new PlayTraceCoverage(bag, pos, at.Item1, at.Item2, dist, R.Fielding.Cover.RadiusFt, reachable,
                HoldsBall && !Throwing && heldDistance <= R.Fielding.Cover.RadiusFt, Forces.At(bag));
        }).ToArray();
    }
}

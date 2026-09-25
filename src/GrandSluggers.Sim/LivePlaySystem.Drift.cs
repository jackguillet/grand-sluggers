namespace GrandSluggers.Sim;

/// <summary>
/// The drift pattern live (§14; the dust devils at Sunscorch Mesa). Each play draws two phases a disc from the match's hazard
/// stream, so where the devils wander is seeded and replays; a park without one draws nothing. A ball in flight between the row's
/// floor and top that passes inside a disc is pushed its row's feet square to its heading, away from the disc's middle, spread
/// over the row's seconds and finished even after the ball leaves the disc — once a play per disc. The rest of its path slides
/// with it (<see cref="NudgeBall"/>), so it lands displaced by exactly the push and every chaser re-plans. A ball on the ground,
/// a held or thrown ball and every body are untouched.
/// </summary>
public sealed partial class LivePlaySystem
{
    IReadOnlyList<DriftDisc> _drifts = [];
    readonly List<BallPushed> _pushesThisPlay = [];

    /// <summary>The park's drifting discs as this play reads them, with the play's phases.</summary>
    public IReadOnlyList<DriftDisc> DriftDiscs => _drifts;

    /// <summary>The discs that pushed the ball this play, one entry per disc, with the feet pushed so far.</summary>
    public IReadOnlyList<BallPushed> PushesThisPlay => _pushesThisPlay;

    void BeginDrifts()
    {
        _pushesThisPlay.Clear();
        _drifts = Drifts.Of(Park, R, () => _match.DrawIndex(1000) / 1000.0);
    }

    void ReadDrifts(double dt)
    {
        if (_drifts.Count == 0 || Path is null || dt <= 0) return;
        var t = ElapsedSeconds;
        foreach (var disc in _drifts)
        {
            var index = _pushesThisPlay.FindIndex(p => p.Hazard == disc.Hazard);
            if (index < 0)
            {
                if (!disc.Takes(t, BallX, BallY, BallZ)) continue;
                var before = BallFlight.PointAt(Path, Math.Max(0, t - dt), R);
                var (cx, cz) = disc.At(t);
                var (dx, dz) = Drifts.PushDir(BallX - before.X, BallZ - before.Z, BallX, BallZ, cx, cz);
                if (dx == 0 && dz == 0) continue;
                _pushesThisPlay.Add(new BallPushed(disc.Hazard, disc.Type, t, BallX, BallZ, dx, dz, 0));
                index = _pushesThisPlay.Count - 1;
                if (!_events.Contains(LiveEvent.BallPushed)) _events.Add(LiveEvent.BallPushed);
                _trace?.Mark(PlayTraceMarkKind.BallPushed, t, hazard: new PlayTraceHazard(disc.Hazard, disc.Type, cx, cz, disc.RadiusFt, t));
                Sub = "A dust devil takes it!";
            }
            var push = _pushesThisPlay[index];
            var step = Math.Min(disc.PushFt * dt / disc.PushSec, disc.PushFt - push.PushedFt);
            if (step <= 1e-9) continue;
            NudgeBall(push.Dx * step, push.Dz * step);
            BallX += push.Dx * step;
            BallZ += push.Dz * step;
            _pushesThisPlay[index] = push with { PushedFt = push.PushedFt + step };
        }
    }
}

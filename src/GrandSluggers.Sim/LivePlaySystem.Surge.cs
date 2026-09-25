namespace GrandSluggers.Sim;

/// <summary>
/// The surge pattern live (§14; the tide at Coconut Cove). Each play draws one phase from the match's hazard stream, so when the
/// waves come is seeded and replays; a park with no surge draws nothing and plays bit for bit as before. While a band's wave is
/// in, a ball rolling inside it (no higher than the row's <c>heightFt</c>) drifts toward the nearer foul line at the row's pace,
/// up to its <c>carryFt</c> a play and never across the line: the rest of its path slides with it (<see cref="NudgeBall"/>), so
/// every chaser re-plans from where the ball is. A ball in the air, a held or thrown ball and every body are untouched, and the
/// gloves decide the play.
/// </summary>
public sealed partial class LivePlaySystem
{
    IReadOnlyList<SurgeBand> _surges = [];
    readonly Dictionary<int, double> _carriedFt = new();
    readonly List<BallCarried> _carriesThisPlay = [];

    /// <summary>The play's seeded offset on every surge band's clock, seconds; 0 at a park with none.</summary>
    public double SurgePhaseSec { get; private set; }

    /// <summary>The park's surge bands as this play reads them (the night reach at night).</summary>
    public IReadOnlyList<SurgeBand> SurgeBands => _surges;

    /// <summary>The bands that carried the ball this play, one entry per band, with the feet carried so far.</summary>
    public IReadOnlyList<BallCarried> CarriesThisPlay => _carriesThisPlay;

    void BeginSurges()
    {
        _carriedFt.Clear();
        _carriesThisPlay.Clear();
        _surges = Surges.Of(Park, R, _match.Night);
        SurgePhaseSec = _surges.Count == 0 ? 0 : _match.DrawIndex(1000) / 1000.0 * _surges[0].PeriodSec;
    }

    void ReadSurges(double dt)
    {
        if (_surges.Count == 0 || Path is null || dt <= 0) return;
        var t = ElapsedSeconds;
        foreach (var band in _surges)
        {
            if (!band.In(t, SurgePhaseSec) || !band.Holds(BallX, BallY, BallZ)) continue;
            var carried = _carriedFt.TryGetValue(band.Hazard, out var c) ? c : 0;
            var (dx, dz, toLine) = Surges.TowardLine(BallX, BallZ);
            // Never onto or across the chalk: the tide moves a fair ball, it never makes it foul.
            var step = Math.Min(band.StepFt(dt), Math.Min(band.CarryFt - carried, toLine - LineMarginFt));
            if (step <= 1e-9) continue;
            NudgeBall(dx * step, dz * step);
            BallX += dx * step;
            BallZ += dz * step;
            _carriedFt[band.Hazard] = carried + step;
            var index = _carriesThisPlay.FindIndex(k => k.Hazard == band.Hazard);
            if (index < 0)
            {
                _carriesThisPlay.Add(new BallCarried(band.Hazard, band.Type, t, BallX, BallZ, step));
                if (!_events.Contains(LiveEvent.BallCarried)) _events.Add(LiveEvent.BallCarried);
                _trace?.Mark(PlayTraceMarkKind.BallCarried, t,
                    hazard: new PlayTraceHazard(band.Hazard, band.Type, band.X, band.Z, band.RadiusFt, t));
                Sub = "The tide takes it!";
            }
            else _carriesThisPlay[index] = _carriesThisPlay[index] with { CarriedFt = carried + step };
        }
    }

    /// <summary>How close to the foul line a carried ball may come: it stays fair.</summary>
    const double LineMarginFt = 1.0;
}

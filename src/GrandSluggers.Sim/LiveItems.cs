namespace GrandSluggers.Sim;

/// <summary>
/// The thrown items in one live play (§12): one body kept off the ball for a beat, or every ball on the dirt hopping. It owns
/// the item's flight clock, the bodies kept off the ball, the peel on the grass and the POW's hop, and runs them down.
/// <see cref="LivePlaySystem"/> says where an item lands and what a body kept off the ball does with the ball it holds.
/// </summary>
public sealed class LiveItems
{
    /// <summary>Bodies kept off the ball: position → seconds left on the peel or the daze.</summary>
    readonly Dictionary<string, double> _off = new(StringComparer.OrdinalIgnoreCase);
    /// <summary>The peel on the grass, where it landed, while it lasts (batting.items.peelSec).</summary>
    (double X, double Z)? _peel;
    double _peelT;
    double _powT;
    double _landAt = -1;

    /// <summary>A thrown item landed this play and took effect (a peel down, a body dazed, the dirt hopping).</summary>
    public bool Landed { get; private set; }

    /// <summary>A peel is down on the grass.</summary>
    public bool PeelDown => _peel is not null && _peelT > 0;

    /// <summary>A POW has every ball on the dirt hopping.</summary>
    public bool Hopping => _powT > 0;

    public void Reset()
    {
        _off.Clear();
        _peel = null;
        _peelT = 0;
        _powT = 0;
        Landed = false;
        _landAt = -1;
    }

    /// <summary>The item is in the air until <paramref name="at"/> on the play clock; a negative time is no item in the air.</summary>
    public void LandAt(double at) => _landAt = at;

    /// <summary>The item in the air was smashed: nothing lands.</summary>
    public void Cancel() => _landAt = -1;

    /// <summary>True once, on the first frame the item in the air has reached its landing time.</summary>
    public bool Due(double elapsed)
    {
        if (_landAt < 0 || elapsed < _landAt) return false;
        _landAt = -1;
        return true;
    }

    /// <summary>
    /// The item lands, by geometry: a POW sets the dirt hopping, a banana lays a peel at <paramref name="feet"/>. Any other
    /// item dazes the body at <paramref name="pos"/>, and its seconds are returned for the caller to keep that body off the
    /// ball (<see cref="KeepOff"/> plus what the body drops). No roll.
    /// </summary>
    public double? Land(string item, (double X, double Z) feet, RulesTable rules)
    {
        Landed = true;
        if (ErrorItems.AffectsEveryGlove(item))
        {
            _powT = ErrorItems.EffectSec(item, rules);
            return null;
        }
        if (ErrorItems.IsPeel(item))
        {
            _peel = feet;
            _peelT = rules.Batting.Items.PeelSec;
            return null;
        }
        return ErrorItems.EffectSec(item, rules);
    }

    /// <summary>Keep the body at <paramref name="pos"/> off the ball for <paramref name="sec"/>.</summary>
    public void KeepOff(string pos, double sec) => _off[pos] = sec;

    /// <summary>The body at <paramref name="pos"/> is kept off the ball.</summary>
    public bool IsOff(string pos) => _off.ContainsKey(pos);

    /// <summary>Seconds left keeping <paramref name="pos"/> off the ball, 0 when it is not.</summary>
    public double OffLeft(string pos) => _off.TryGetValue(pos, out var left) ? left : 0;

    /// <summary>
    /// One frame: the dazes and the POW run down, then the peel. Returns the bodies that stepped on the peel this frame, in
    /// <paramref name="bodies"/> order, for the caller to keep off the ball for <c>batting.items.slipSec</c>.
    /// </summary>
    public IReadOnlyList<string> Tick(double dt, IReadOnlyDictionary<string, (double X, double Z)> bodies, RulesTable rules)
    {
        if (_off.Count > 0)
            foreach (var pos in _off.Keys.ToList())
                if ((_off[pos] -= dt) <= 0) _off.Remove(pos);
        if (_powT > 0) _powT -= dt;
        if (_peel is not { } peel) return [];
        if ((_peelT -= dt) <= 0)
        {
            _peel = null;
            return [];
        }
        List<string>? slipped = null;
        foreach (var kv in bodies)
            if (!_off.ContainsKey(kv.Key) && ErrorItems.OnPeel(peel.X, peel.Z, kv.Value.X, kv.Value.Z, rules))
                (slipped ??= []).Add(kv.Key);
        return slipped ?? (IReadOnlyList<string>)[];
    }
}

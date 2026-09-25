namespace GrandSluggers.Sim;

/// <summary>
/// The CPU glove's decision clock (§8.8): when the glove holds the ball the throw waits for the body's reaction, then the decision
/// table runs once for that possession. A new possession, a throw, a hand-off or a live contact restarts it. <see cref="LivePlaySystem"/>
/// runs the table and says when to restart.
/// </summary>
public sealed class CpuThrowClock
{
    double _at = -1;

    /// <summary>The table has run for this possession.</summary>
    public bool Decided { get; private set; }

    /// <summary>A new possession: no reaction armed, no decision made.</summary>
    public void Restart()
    {
        _at = -1;
        Decided = false;
    }

    /// <summary>The throw may go at <paramref name="at"/> on the play clock (the catcher's release on a runner play), undecided.</summary>
    public void ArmAt(double at)
    {
        _at = at;
        Decided = false;
    }

    /// <summary>
    /// Whether the table may run now: never twice for one possession; the first ask arms the reaction (<paramref name="reactionSec"/>
    /// from <paramref name="elapsed"/>) and waits; after that, once the reaction is spent.
    /// </summary>
    public bool MayThrow(double elapsed, Func<double> reactionSec)
    {
        if (Decided) return false;
        if (_at < 0)
        {
            _at = elapsed + reactionSec();
            return false;
        }
        return elapsed >= _at;
    }

    /// <summary>The table runs: nothing more this possession until a restart.</summary>
    public void Decide() => Decided = true;
}

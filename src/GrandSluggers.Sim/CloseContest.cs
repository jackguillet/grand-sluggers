namespace GrandSluggers.Sim;

/// <summary>
/// One close play's mash contest (§9.6, D5): the body held short of the bag, the icon's delay, and each side's first press
/// after it, human or CPU. <see cref="ClosePlay"/> is the rule table; this is the contest a live play runs.
/// <see cref="LivePlaySystem"/> decides when a throw offers one and writes the verdict (the body on the bag, or the out).
/// </summary>
public sealed class CloseContest
{
    double _t;
    double _offenseAt = -1;
    double _defenseAt = -1;

    /// <summary>A close play is running: the tick belongs to the contest until the verdict.</summary>
    public bool Active { get; private set; }

    /// <summary>The icon is up: the presses count from here.</summary>
    public bool Icon { get; private set; }

    /// <summary>The bag the contest is at (3 or 4); 0 before the first one of the play.</summary>
    public int Bag { get; private set; }

    /// <summary>The body in the play, held short of <see cref="Bag"/> until the verdict.</summary>
    public Runner? Runner { get; private set; }

    public void Reset()
    {
        Active = false;
        Icon = false;
        Bag = 0;
        Runner = null;
        _t = 0;
        _offenseAt = _defenseAt = -1;
    }

    /// <summary>The contest starts: the body is halted short of the bag, and the icon waits its delay.</summary>
    public void Begin(Runner runner, int bag)
    {
        Runner = runner;
        runner.Halt();
        Active = true;
        _t = 0;
        Icon = false;
        Bag = bag;
        _offenseAt = _defenseAt = -1;
    }

    /// <summary>
    /// The contest's clock for one frame. False while the icon waits its delay, and on the frame it comes up (the clock
    /// restarts at the icon, and the caller raises <see cref="LiveEvent.CloseIcon"/> when <see cref="Icon"/> turned true).
    /// True once the presses count.
    /// </summary>
    public bool Clock(double dt, RulesTable rules)
    {
        _t += dt;
        if (Icon) return true;
        if (_t < ClosePlay.IconDelaySec(rules)) return false;
        Icon = true;
        _t = 0;
        return false;
    }

    /// <summary>
    /// Each side's first press after the icon: a human seat's press lands on the frame it is down, a CPU side's at its reaction
    /// (<c>running.close.cpuReaction*</c> off the body's stat). The first press wins (§9.6): once one side has pressed and the
    /// clock is past it, a side that has not pressed can only be later. Null while nobody has won; true when the offense is safe.
    /// </summary>
    public bool? Decide(bool offenseHuman, bool offensePressed, int offenseRun,
        bool defenseHuman, bool defensePressed, int defenseHands, RulesTable rules)
    {
        if (_offenseAt < 0)
        {
            if (offenseHuman)
            {
                if (offensePressed) _offenseAt = _t;
            }
            else
            {
                var cpu = ClosePlay.CpuReactionSec(offenseRun, rules);
                if (_t >= cpu) _offenseAt = cpu;
            }
        }
        if (_defenseAt < 0)
        {
            if (defenseHuman)
            {
                if (defensePressed) _defenseAt = _t;
            }
            else
            {
                var cpu = ClosePlay.CpuReactionSec(defenseHands, rules);
                if (_t >= cpu) _defenseAt = cpu;
            }
        }
        var off = _offenseAt >= 0 ? _offenseAt : double.PositiveInfinity;
        var def = _defenseAt >= 0 ? _defenseAt : double.PositiveInfinity;
        var decided = !double.IsPositiveInfinity(off) && !double.IsPositiveInfinity(def)
                      || Math.Min(off, def) < _t;
        return decided ? ClosePlay.OffenseSafe(off, def) : null;
    }

    /// <summary>The verdict is written; the play goes on from the bag. <see cref="Bag"/> keeps the last contest's bag.</summary>
    public void End()
    {
        Runner = null;
        Active = false;
        Icon = false;
    }
}

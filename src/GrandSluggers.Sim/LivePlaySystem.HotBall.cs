namespace GrandSluggers.Sim;

/// <summary>
/// A star swing's hot ball (spec §13, <see cref="StarSwingSkill.HotBall"/>, Hot Iron): the ball stays molten for
/// <see cref="Sim.HotBall.MoltenSec"/> after contact, and a glove that holds it more than <see cref="Sim.HotBall.HoldSec"/> of that
/// time drops it at its feet — a loose ball, live — and cannot take it again until it cools. The hold is the possession: from the
/// take to the throw's command. The catch that made an out stays an out. Decided by the play clock, never a roll, and the same
/// for a CPU glove and a player's.
/// </summary>
public sealed partial class LivePlaySystem
{
    /// <summary>This play's hot ball, or null: a ball put in play off a swing whose row names one.</summary>
    HotBall? _hot;

    /// <summary>The ball off the bat is still molten (§13): the glow and the hiss, until it cools. 0 when it is not a hot ball.</summary>
    public double MoltenLeftSec => _hot is { } hot && Hit is not null && !RunnerPlay ? hot.MoltenLeft(ElapsedSeconds) : 0;

    /// <summary>The ball is molten now (<see cref="MoltenLeftSec"/> &gt; 0): the tell the client draws on the ball.</summary>
    public bool Molten => MoltenLeftSec > 0;

    /// <summary>
    /// Seconds the glove on the ball may still hold it before it drops (§13): infinite for a cool ball, a ball nobody holds, or a
    /// possession that began late enough to outlast the molten window. The CPU's table reads it; a player reads the glow.
    /// </summary>
    public double HotHoldLeftSec =>
        _hot is { } hot && HoldsBall && !Throwing && Molten ? hot.HoldLeft(_heldSince, ElapsedSeconds) : double.PositiveInfinity;

    /// <summary>Arms the hot ball for the ball just put in play (a batted ball off a swing whose row names one; not a bunt).</summary>
    void BeginHotBall()
    {
        _hot = null;
        if (Hit is null || !Hit.InPlay || Hit.Class == BattedBallClass.Bunt) return;
        _hot = StarSkillTable.Or(_match.Content?.StarSkills).Swing(Hit.StarSwingUsed)?.HotBall;
    }

    /// <summary>
    /// Once a frame: the glove that has held the molten ball past its hold drops it at its feet and is kept off it until it cools
    /// (the items' keep-off, <see cref="Foil"/>). Another glove may take it at once; it is a loose ball like any other.
    /// </summary>
    void ReadHotBall()
    {
        if (_hot is not { } hot || !HoldsBall || Throwing || _heldSince < 0) return;
        if (!hot.Drops(_heldSince, ElapsedSeconds)) return;
        var pos = GlovePos;
        var who = GloveChar();
        RecordFact(new HotBallDropped(Hit?.StarSwingUsed ?? "", ElapsedSeconds, GloveX, GloveZ, who.Id, pos));
        _cpuWalkBag = 0;
        _receivedClean = false;
        Sub = $"Too hot! {who.Name} drops it.";
        Foil(pos, Math.Max(_items.OffLeft(pos), Math.Max(hot.MoltenLeft(ElapsedSeconds), 1e-6)));
        _cpuClock.Restart();
    }
}

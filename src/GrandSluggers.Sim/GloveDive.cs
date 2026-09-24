namespace GrandSluggers.Sim;

/// <summary>
/// The glove's deliberate dive (#719: F693-02-dive-jump-scoop-reach, -dive-recovery-cost): East's arm window, the lunge that
/// carried the body, and the recovery it owes from the commitment whether the ball comes or not. <see cref="LivePlaySystem"/>
/// says when East commits one and moves the body on the lunge; it reads <see cref="Recovering"/> for what waits on the body.
/// </summary>
public sealed class GloveDive
{
    /// <summary>The dive's arm window left: a take inside it may be a diving catch.</summary>
    public double ArmT { get; private set; }

    /// <summary>The dive's recovery left (<c>catch.diveRecovery*</c>), owed by <see cref="DivingPos"/>; 0 when none.</summary>
    public double RecoveryT { get; private set; }

    /// <summary>The body paying <see cref="RecoveryT"/>, held across selection.</summary>
    public string DivingPos { get; private set; } = "";

    /// <summary>The body the last lunge carried.</summary>
    public string LungePos { get; private set; } = "";

    public void Reset()
    {
        ArmT = 0;
        RecoveryT = 0;
        DivingPos = "";
        LungePos = "";
    }

    /// <summary>The body at <paramref name="pos"/> owes the dive's recovery now.</summary>
    public bool Recovering(string pos) => RecoveryT > 0 && DivingPos == pos;

    /// <summary>The body at <paramref name="pos"/> is on the ground from a dive: in the lunge's arm window, or recovering.</summary>
    public bool Down(string pos) => ArmT > 0 && LungePos == pos || Recovering(pos);

    /// <summary>A lunge carried the body at <paramref name="pos"/>.</summary>
    public void Lunged(string pos) => LungePos = pos;

    /// <summary>
    /// East commits a dive at <paramref name="pos"/>: the arm window opens for <paramref name="armSec"/>, and the recovery is owed —
    /// the later end against anything already owed. True when a recovery was charged (nothing on a table with no cost).
    /// </summary>
    public bool Commit(string pos, double armSec, double recoverySec)
    {
        ArmT = armSec;
        if (recoverySec <= 0) return false;
        RecoveryT = Math.Max(RecoveryT, recoverySec);
        DivingPos = pos;
        return true;
    }

    /// <summary>One frame: the arm window and the recovery run down; the recovery ends at readiness.</summary>
    public void Tick(double dt)
    {
        if (ArmT > 0) ArmT -= dt;
        if (RecoveryT > 0)
        {
            RecoveryT -= dt;
            if (RecoveryT <= 1e-9)
            {
                RecoveryT = 0;
                DivingPos = "";
            }
        }
    }
}

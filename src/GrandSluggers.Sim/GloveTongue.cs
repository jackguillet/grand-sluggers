namespace GrandSluggers.Sim;

/// <summary>
/// Lick Catch's pressed tongue (§8.4, AB-12): East on a tongue body snaps the tongue <c>abilities.lickReachFt</c> ahead
/// along the body's facing for <c>lickSnapSec</c>; a ball on it is taken (<see cref="FieldAbilities.TongueReaches"/>). The
/// press owes <c>lickRecoverySec</c> from the commitment, whether the ball comes or not: the body neither moves nor throws.
/// <see cref="LivePlaySystem"/> says when East commits one; the tongue's line is fixed at the press.
/// </summary>
public sealed class GloveTongue
{
    /// <summary>The tongue's time out left: a ball on it inside this is taken.</summary>
    public double SnapT { get; private set; }

    /// <summary>The recovery left, owed by <see cref="Pos"/>; 0 when none.</summary>
    public double RecoveryT { get; private set; }

    /// <summary>The body that pressed.</summary>
    public string Pos { get; private set; } = "";

    /// <summary>Where the tongue leaves the body, fixed at the press.</summary>
    public (double X, double Z) From { get; private set; }

    /// <summary>The way it snaps (<see cref="FieldAbilities.Facing"/>), fixed at the press.</summary>
    public (double X, double Z) Face { get; private set; }

    public void Reset()
    {
        SnapT = 0;
        RecoveryT = 0;
        Pos = "";
        From = (0, 0);
        Face = (0, 0);
    }

    /// <summary>The tongue of the body at <paramref name="pos"/> is out now.</summary>
    public bool Out(string pos) => SnapT > 0 && Pos == pos;

    /// <summary>The body at <paramref name="pos"/> owes the tongue's recovery now.</summary>
    public bool Recovering(string pos) => RecoveryT > 0 && Pos == pos;

    /// <summary>The press: the tongue is out for <paramref name="snapSec"/> along <paramref name="face"/>, and the recovery is owed from now.</summary>
    public void Commit(string pos, (double X, double Z) from, (double X, double Z) face, double snapSec, double recoverySec)
    {
        Pos = pos;
        From = from;
        Face = face;
        SnapT = snapSec;
        RecoveryT = Math.Max(RecoveryT, recoverySec);
    }

    /// <summary>The ball was taken: the tongue is back in; the recovery still runs.</summary>
    public void Retract() => SnapT = 0;

    /// <summary>One frame: the snap and the recovery run down.</summary>
    public void Tick(double dt)
    {
        if (SnapT > 0) SnapT = Math.Max(0, SnapT - dt);
        if (RecoveryT > 0)
        {
            RecoveryT -= dt;
            if (RecoveryT <= 1e-9) RecoveryT = 0;
        }
    }
}

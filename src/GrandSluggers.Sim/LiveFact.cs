namespace GrandSluggers.Sim;

/// <summary>
/// A typed fact the live ball recorded about what a body actually did, for any reader: a tutorial objective, a test, a
/// trace. The live system appends facts; it never keeps a field for one consumer. <see cref="LivePlaySystem.Facts"/> holds
/// this frame's, <see cref="LivePlaySystem.FactsThisPlay"/> the play's.
/// </summary>
public abstract record LiveFact;

/// <summary>
/// A glove took the ball where only its field ability's reach bonus could (§8.9): an ordinary glove at the same spot
/// would have missed it. Recorded once per take, on the play's log.
/// </summary>
public sealed record ReachBonusTake(string GloveId, string Ability) : LiveFact;

/// <summary>The pursuit assist moved this glove this frame. Braking and coasting are not an assisted route.</summary>
public sealed record AssistedRouteStep(string GloveId) : LiveFact;

/// <summary>
/// A star swing's grounder turned at its first hop (§13, <see cref="StarSwingSkill.FirstHopKickDeg"/>): the swing, when and
/// where, the signed turn in degrees (positive is counter-clockwise from above) and the chasing glove (its position) it turned away from.
/// </summary>
public sealed record FirstHopKicked(string SwingId, double T, double X, double Z, double TurnDeg, string GloveId, double BounceMul = 1) : LiveFact;

/// <summary>
/// A star swing's pause held one fielder (§13, <see cref="StarSwingSkill.FielderPauseSec"/>): the swing, the glove it held
/// (its position: the nearest fielder, the body the play would have sent) and the play second that body may move again.
/// </summary>
public sealed record FielderDazzled(string SwingId, string GloveId, double UntilT) : LiveFact;

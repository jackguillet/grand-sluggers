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

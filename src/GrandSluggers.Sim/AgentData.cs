namespace GrandSluggers.Sim.Tooling;

/// <summary>
/// The agent catalogs in <c>data/agent</c>: how sessions work, never what the game plays. The game load
/// (<see cref="ContentCatalog.Load"/>) does not read them, so a malformed research ledger cannot stop a
/// player from starting, and the packaged player does not carry them (<see cref="RuntimePackage"/>).
/// <c>cli art</c> and the tests validate them.
/// </summary>
public static class AgentData
{
    public const string Directory = "agent";

    public static IReadOnlyList<string> Validate(DataRoot root) =>
    [
        .. DebugProtocol.Validate(root),
        .. DualStills.Validate(root),
        .. DccStages.Validate(root),
        .. RaceEvidence.Validate(root)
    ];
}

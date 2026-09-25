using GrandSluggers.Sim;
using GrandSluggers.Sim.Tooling;

namespace GrandSluggers.Cli;

/// <summary>
/// The debug protocol a session loads (docs/agent-rails.md §2). By default it prints the rows that are still memory —
/// not yet promoted to a test — in full, for the session's kind; a promoted row's test is its memory, so it is counted
/// and listed only under <c>--full</c>.
/// </summary>
sealed class ProtocolCommand : Command
{
    public override string Name => "protocol";
    public override IReadOnlyList<string> Usage => ["protocol [--kind gameplay|presentation|art] [--full]"];

    public override IReadOnlyList<Option> Options =>
    [
        new("--kind", Arity.Value),
        new("--full", Arity.Switch),
    ];

    public override int Run(ContentCatalog content, CommandLine line)
    {
        var kind = line.Text("--kind");
        if (kind is not null && (kind == "any" || !DebugProtocol.Kinds.Contains(kind)))
            throw new UsageException($"--kind must be gameplay, presentation or art; got '{kind}'");
        var full = line.Has("--full");

        var protocol = DebugProtocol.Load(content.Root, new List<string>());
        var rows = protocol.Entries.Where(r => kind is null || r.LoadedBy(kind)).ToList();
        var open = rows.Where(r => !r.IsPromoted).ToList();
        var promoted = rows.Where(r => r.IsPromoted).ToList();
        Console.WriteLine($"PROTOCOL {rows.Count} rows{(kind is null ? "" : $" for {kind}")}: {open.Count} unpromoted, {promoted.Count} promoted to a test");

        foreach (var row in open)
            Print(row);
        if (full)
            foreach (var row in promoted)
                Print(row);
        else if (promoted.Count > 0)
            Console.WriteLine($"  ({promoted.Count} promoted rows hidden; their tests hold them. --full lists them.)");

        return Report(DebugProtocol.Validate(content.Root), "debug protocol matches spec §2");
    }

    static void Print(DebugProtocolEntry row)
    {
        var test = row.IsPromoted ? row.Promoted : "not promoted";
        Console.WriteLine($"  {row.Id}  [{row.Kind}, {row.Stage}, {row.Issue}]  {test}");
        Console.WriteLine($"    signature: {row.Signature}");
        Console.WriteLine($"    cause:     {row.Cause}");
        Console.WriteLine($"    fix:       {row.Fix}");
    }
}

using GrandSluggers.Sim;

namespace GrandSluggers.Cli;

sealed class ProtocolCommand : Command
{
    public override string Name => "protocol";
    public override IReadOnlyList<string> Usage => ["protocol"];

    public override int Run(ContentCatalog content, CommandLine line)
    {
        var protocol = DebugProtocol.Load(content.Root, new List<string>());
        Console.WriteLine($"PROTOCOL {protocol.Entries.Count} entries");
        foreach (var row in protocol.Entries)
        {
            var promoted = string.IsNullOrWhiteSpace(row.Promoted) ? "-" : row.Promoted;
            Console.WriteLine($"  {row.Id,-32} {row.Stage,-14} {row.Issue,-8} {promoted}");
        }
        return Report(DebugProtocol.Validate(content.Root), "debug protocol matches spec §2");
    }
}

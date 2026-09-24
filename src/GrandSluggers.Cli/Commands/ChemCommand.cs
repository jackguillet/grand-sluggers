using GrandSluggers.Sim;

namespace GrandSluggers.Cli;

sealed class ChemCommand : Command
{
    public override string Name => "chem";
    public override IReadOnlyList<string> Usage => ["chem <character-id>"];
    public override int MaxPositionals => 1;

    public override int Run(ContentCatalog content, CommandLine line)
    {
        var me = content.Must(line.Positional(0) ?? "rio");
        Console.WriteLine($"{me.Name} ({me.Faction})");
        foreach (var other in content.Characters.Values.OrderBy(c => c.Name))
        {
            if (other.Id == me.Id) continue;
            var rel = content.Chemistry.Between(me, other);
            if (rel == Chemistry.Neutral) continue;
            var mark = rel == Chemistry.Good ? "+" : "-";
            Console.WriteLine($"  {mark} {other.Name,-14} {other.Faction}");
        }
        return 0;
    }
}

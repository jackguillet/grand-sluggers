using GrandSluggers.Sim;

namespace GrandSluggers.Cli;

sealed class RosterCommand : Command
{
    public override string Name => "roster";
    public override IReadOnlyList<string> Usage => ["roster"];

    public override int Run(ContentCatalog content, CommandLine line)
    {
        foreach (var c in content.Characters.Values.OrderBy(c => c.Faction).ThenByDescending(c => c.Captain).ThenBy(c => c.Name))
        {
            var cap = c.Captain ? "C" : " ";
            Console.WriteLine($"{cap} {c.Name,-14} {c.Faction,-10} P{c.Stats.Pitch} B{c.Stats.Bat} F{c.Stats.Field} R{c.Stats.Run}  {c.StarPitch}/{c.StarSwing}  {c.FieldAbility}");
        }
        return 0;
    }
}

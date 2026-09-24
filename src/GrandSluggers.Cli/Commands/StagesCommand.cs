using GrandSluggers.Sim;

namespace GrandSluggers.Cli;

sealed class StagesCommand : Command
{
    public override string Name => "stages";
    public override IReadOnlyList<string> Usage => ["stages"];

    public override int Run(ContentCatalog content, CommandLine line)
    {
        var catalog = DccStages.Load(content.Root, new List<string>());
        Console.WriteLine($"STAGES {catalog.Stages.Count}  one-shot {catalog.OneShot}");
        foreach (var stage in catalog.Stages)
        {
            Console.WriteLine($"  {stage.N} {stage.Id,-10} {stage.Kind,-6} character {Lane(stage.Character)}");
            Console.WriteLine($"                     harbor    {Lane(stage.Harbor)}");
        }
        return Report(DccStages.Validate(content.Root), "dcc stages match spec §6; one-shot banned");

        static string Lane(DccStageLane lane) =>
            lane.Skip ? "—" : $"{lane.Checkpoint}  {lane.Flag}";
    }
}

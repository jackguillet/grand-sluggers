using GrandSluggers.Sim;

namespace GrandSluggers.Cli;

sealed class ChallengeCommand : Command
{
    public override string Name => "challenge";
    public override IReadOnlyList<string> Usage => ["challenge [--captain rio] [--seed N]"];
    public override IReadOnlyList<Option> Options => [new("--captain", Arity.Value, "--home"), new("--seed", Arity.Value, "-s")];
    public override int MaxPositionals => 1;

    public override int Run(ContentCatalog content, CommandLine line)
    {
        if (line.Has("--captain") && line.Positional(0) is not null)
            throw new UsageException("name the captain once: --captain rio or challenge rio");
        var captainId = line.Text("--captain") ?? line.Positional(0) ?? "rio";
        var seed = line.Int("--seed", 1);

        var run = Challenge.Start(content, captainId);
        var match = run.MakeMatch(content, innings: 3, seed: seed);
        Console.WriteLine($"Challenge  {match.Home.Name} vs {match.Away.Name}  at {match.Park.Name}  seed {seed}");
        Console.WriteLine($"owned {run.Owned.Count}  first rival {match.Away.Captain.Name}");
        match.AutoPlayGame();
        var recruit = run.Resolve(match);
        Console.WriteLine($"Final  {match.Away.Name} {match.AwayScore}  {match.Home.Name} {match.HomeScore}");
        if (recruit is not null)
            Console.WriteLine($"WIN  recruited {recruit.Name}  roster {run.Owned.Count}");
        else
            Console.WriteLine("LOSS  no recruit");
        return 0;
    }
}

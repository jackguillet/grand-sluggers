using GrandSluggers.Sim;

namespace GrandSluggers.Cli;

/// <summary>
/// One CLI command. It declares its flags and how many positionals it takes, so the command line is
/// refused before the catalog loads; it reads and checks the values itself, and returns the exit code.
/// </summary>
abstract class Command
{
    public abstract string Name { get; }

    /// <summary>The help lines for this command, each starting with its name.</summary>
    public abstract IReadOnlyList<string> Usage { get; }

    public virtual IReadOnlyList<Option> Options => [];

    public virtual int MaxPositionals => 0;

    public abstract int Run(ContentCatalog content, CommandLine line);

    /// <summary>Prints a validator's errors under an OK or FAIL line; the exit code is 1 on any error.</summary>
    protected static int Report(IReadOnlyList<string> errors, string ok)
    {
        if (errors.Count == 0)
        {
            Console.WriteLine("OK     " + ok);
            return 0;
        }
        Console.WriteLine("FAIL   " + errors.Count + " errors");
        foreach (var e in errors) Console.WriteLine("  - " + e);
        return 1;
    }
}

static class Commands
{
    /// <summary>Every command, in help order.</summary>
    public static readonly IReadOnlyList<Command> All =
    [
        new TutorialsCommand(),
        new RosterCommand(),
        new TeamCommand(),
        new ChemCommand(),
        new AtBatCommand(),
        new MatchCommand(),
        new ChallengeCommand(),
        new ArtCommand(),
        new ProtocolCommand(),
        new StillsCommand(),
        new StagesCommand(),
    ];

    public static Command? Find(string name) => All.FirstOrDefault(c => c.Name == name);

    public static string Help() =>
        "Grand Sluggers sim\n" + string.Concat(All.SelectMany(c => c.Usage).Select(u => "  " + u + "\n"));
}

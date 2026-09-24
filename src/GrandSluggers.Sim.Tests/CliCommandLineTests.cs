using GrandSluggers.Cli;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// The CLI refuses a command line it does not understand with exit 2, so a typo never runs the default game
/// and reads as a pass. Every command declares its flags; these tests hold for the next command too.
/// </summary>
public sealed class CliCommandLineTests
{
    static readonly Option[] MatchLike =
    [
        new("--seed", Arity.Value, "-s"),
        new("--night", Arity.Switch),
        new("--hazards", Arity.Value),
        new("--difficulty", Arity.Value),
        new("--trace", Arity.OptionalValue),
    ];

    static CommandLine Parse(params string[] args) => CommandLine.Parse(args, MatchLike, maxPositionals: 1);

    static string Refused(Action act) => Assert.Throws<UsageException>(act).Message;

    [Fact]
    public void AnUnknownCommandIsRefusedAndHelpIsNot()
    {
        Assert.Equal(2, CommandRunner.Run(["matc"]));
        Assert.Equal(0, CommandRunner.Run([]));
        Assert.Equal(0, CommandRunner.Run(["help"]));
    }

    [Theory]
    [InlineData("match", "--sede", "7")]
    [InlineData("match", "--seed", "x")]
    [InlineData("match", "--seed")]
    [InlineData("match", "--seed", "1", "--seed", "2")]
    [InlineData("match", "extra")]
    [InlineData("match", "--hazards", "maybe")]
    [InlineData("match", "--difficulty", "brutal")]
    [InlineData("match", "--cohort", "s29", "--seed", "3")]
    [InlineData("match", "--cohort", "nope")]
    [InlineData("at-bat", "dragon")]
    [InlineData("art", "--verbose")]
    public void ABadCommandLineExitsTwoAndPlaysNothing(params string[] args) =>
        Assert.Equal(2, CommandRunner.Run(args));

    [Fact]
    public void AnIdTheCatalogDoesNotHaveExitsTwo()
    {
        Assert.Equal(2, CommandRunner.Run(["match", "--park", "nope"]));
        Assert.Equal(2, CommandRunner.Run(["chem", "nobody"]));
    }

    [Fact]
    public void UnknownRepeatedAndMissingFlagsAreRefused()
    {
        Assert.Contains("unknown flag '--sede'", Refused(() => Parse("--sede", "7")));
        Assert.Contains("--seed is given twice", Refused(() => Parse("-s", "1", "--seed", "2")));
        Assert.Contains("--seed needs a value", Refused(() => Parse("--seed")));
        Assert.Contains("unexpected argument 'b'", Refused(() => Parse("a", "b")));
    }

    [Fact]
    public void ValuesAreCheckedWhereTheyAreRead()
    {
        Assert.Equal(-3, Parse("--seed", "-3").Int("--seed", 1));
        Assert.Equal(1, Parse().Int("--seed", 1));
        Assert.Contains("whole number", Refused(() => Parse("--seed", "7x").Int("--seed", 1)));

        Assert.True(Parse().OnOff("--hazards", true));
        Assert.False(Parse("--hazards", "off").OnOff("--hazards", true));
        Assert.Contains("on or off", Refused(() => Parse("--hazards", "no").OnOff("--hazards", true)));

        Assert.Null(Parse().OneOf("--difficulty", ["easy", "hard"]));
        Assert.Equal("hard", Parse("--difficulty", "hard").OneOf("--difficulty", ["easy", "hard"]));
        Assert.Contains("easy|hard", Refused(() => Parse("--difficulty", "Hard").OneOf("--difficulty", ["easy", "hard"])));
    }

    [Fact]
    public void AnOptionalValueTakesTheNextTokenOnlyWhenItIsNotAFlag()
    {
        var bare = Parse("--trace", "--night");
        Assert.True(bare.Has("--trace"));
        Assert.Null(bare.Text("--trace"));
        Assert.True(bare.Has("--night"));
        Assert.Equal("out.json", Parse("--trace", "out.json").Text("--trace"));
    }

    [Fact]
    public void EveryCommandIsNamedOnceAndHelpListsEachOfItsLines()
    {
        var names = Commands.All.Select(c => c.Name).ToList();
        Assert.Equal(names.Count, names.Distinct().Count());
        var help = Commands.Help();
        foreach (var command in Commands.All)
        {
            Assert.Same(command, Commands.Find(command.Name));
            Assert.NotEmpty(command.Usage);
            foreach (var usage in command.Usage)
            {
                Assert.StartsWith(command.Name, usage, StringComparison.Ordinal);
                Assert.Contains("  " + usage + "\n", help, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void EveryFlagIsSpelledOncePerCommandAndLooksLikeAFlag()
    {
        foreach (var command in Commands.All)
        {
            var spellings = command.Options.SelectMany(o => o.Aliases.Prepend(o.Name)).ToList();
            Assert.Equal(spellings.Count, spellings.Distinct().Count());
            Assert.All(spellings, s => Assert.StartsWith("-", s, StringComparison.Ordinal));
            Assert.All(command.Options, o => Assert.StartsWith("--", o.Name, StringComparison.Ordinal));
        }
    }
}

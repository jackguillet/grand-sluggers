using GrandSluggers.Sim;

namespace GrandSluggers.Cli;

sealed class AtBatCommand : Command
{
    public override string Name => "at-bat";
    public override IReadOnlyList<string> Usage => ["at-bat [ember|spark] [--seed N]"];
    public override IReadOnlyList<Option> Options => [new("--seed", Arity.Value, "-s")];
    public override int MaxPositionals => 1;

    public override int Run(ContentCatalog content, CommandLine line)
    {
        var matchup = line.Positional(0) ?? "ember";
        if (matchup is not ("ember" or "spark"))
            throw new UsageException($"the matchup must be ember or spark; got '{matchup}'");
        var seed = line.Int("--seed", 1);

        var park = content.Parks["harbor-diamond"];
        var ember = matchup == "ember";
        var pitcher = content.Must(ember ? "ashlord" : "rio");
        var batter = content.Must(ember ? "rio" : "ashlord");
        var onDeck = content.Must(ember ? "nico" : "cinder");
        var resolver = new AtBatResolver(content.Chemistry, content.Rules);
        var rng = new Random(seed);

        Console.WriteLine($"{pitcher.Name} vs {batter.Name} at {park.Name}  (seed {seed})");
        Console.WriteLine($"chem pitcher-batter: {content.Chemistry.Between(pitcher, batter)}  batter-on-deck: {content.Chemistry.Between(batter, onDeck)}");

        for (var i = 0; i < 8; i++)
        {
            var timing = rng.NextDouble() * 10 - 5; // -5..5 frames around the 9-frame slap window
            var input = new AtBatInput(
                Pitcher: pitcher,
                Batter: batter,
                OnDeck: onDeck,
                RunnersOn: [],
                ChargePitch: false,
                ChangeupPitch: false,
                TimingErrorFrames: timing,
                Charge01: i % 3 == 0 ? 1 : 0,
                UseStarPitch: i == 6,
                UseStarSwing: i == 7,
                Bat: ember ? content.Bats.GetValueOrDefault("harbor-lumber") : content.Bats.GetValueOrDefault("furnace-club"),
                PitcherStamina: 80);
            var r = resolver.Resolve(input, park, rng);
            var extra = r.HomeRun ? "  HR" : r.InPlay ? $"  {r.CarryFt:0} ft" : "";
            var item = r.ChemistryItemOffered ? "  [item]" : "";
            var star = r.StarSwingUsed is not null ? $"  *{r.StarSwingUsed}" : r.StarPitchUsed is not null ? $"  *{r.StarPitchUsed}" : "";
            Console.WriteLine($"  t={timing,5:0.0}  {r.Quality,-8}  {r.ExitVeloMph,5:0} mph  {r.LaunchDeg,4:0}°{extra}{item}{star}");
        }
        return 0;
    }
}

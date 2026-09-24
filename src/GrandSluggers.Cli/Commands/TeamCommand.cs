using GrandSluggers.Sim;

namespace GrandSluggers.Cli;

sealed class TeamCommand : Command
{
    public override string Name => "team";
    public override IReadOnlyList<string> Usage => ["team [spark-allstars|ember-court|mixed-rivals|rio|vale|zig|brondo|konga|ashlord]"];
    public override int MaxPositionals => 1;

    public override int Run(ContentCatalog content, CommandLine line)
    {
        var id = line.Positional(0) ?? "spark-allstars";
        var team = id.ToLowerInvariant() switch
        {
            "ember" or "ember-court" => PresetTeams.EmberCourt(content),
            "mixed" or "mixed-rivals" => PresetTeams.MixedRivals(content),
            "spark" or "spark-allstars" => PresetTeams.SparkAllStars(content),
            _ => PresetTeams.ForCaptain(content, id)
        };

        // Chemistry pays off in the field; every team starts on the same Stars (PH-16-R16).
        var buddies = team.Roster.Count(c => c.Id != team.Captain.Id && content.Chemistry.Between(team.Captain, c) == Chemistry.Good);
        var rivals = team.Roster.Count(c => c.Id != team.Captain.Id && content.Chemistry.Between(team.Captain, c) == Chemistry.Bad);
        Console.WriteLine($"{team.Name}  captain {team.Captain.Name}  good {buddies} bad {rivals} with the captain  "
            + $"starting stars {content.Rules.Stars.StartingReserve}/{content.Rules.Stars.MeterMax:0} (every team)");
        Console.WriteLine($"{"",2} {"Name",-14} {"Fac",-10} {"vs C",-8} P B F R");
        foreach (var c in team.Roster)
        {
            var rel = c.Id == team.Captain.Id ? "captain" : content.Chemistry.Between(team.Captain, c).ToString().ToLowerInvariant();
            Console.WriteLine($"  {c.Name,-14} {c.Faction,-10} {rel,-8} {c.Stats.Pitch} {c.Stats.Bat} {c.Stats.Field} {c.Stats.Run}");
        }
        return 0;
    }
}

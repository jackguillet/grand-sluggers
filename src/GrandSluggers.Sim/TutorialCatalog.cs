using System.Text.Json;

namespace GrandSluggers.Sim;

public sealed record TutorialMechanic(string Id, string Title, string Category, string Spec, string[] Sources);
public sealed record TutorialLesson(string Id, int Revision, string[] Mechanics, string Title, string Category,
    string Status, int Issue, string Reason, string[] Profiles, string[] Requires, string[] Prerequisites,
    string Setup, string Objective, string[] Controls, string[] Tests);
public sealed record TutorialBall(double CarryFt, double ExitMph, double LaunchDeg, double SprayDeg);
public sealed record TutorialSetup(string Id, string Policy, int Seed, double TimeoutSec, string[] Home,
    string[] Away, int[] Runners, Dictionary<string, TutorialBall> Balls, PitchCommand? Pitch = null,
    double MinMovement01 = 0, int Strikes = 0);
public sealed record TutorialMechanicFile(int Version, TutorialMechanic[] Mechanics);
public sealed record TutorialLessonFile(int Version, TutorialLesson[] Lessons, TutorialSetup[] Setups);
public sealed record TutorialMigrationFile(int Version, Dictionary<string, int> Mechanics);

/// <summary>Authored lessons joined to a separate mechanic inventory and runtime control/content sources.</summary>
public sealed class TutorialCatalog
{
    public TutorialMechanic[] Mechanics { get; }
    public TutorialLesson[] Lessons { get; }
    public TutorialSetup[] Setups { get; }
    public IReadOnlyDictionary<string, int> Migration { get; }
    public string Profile { get; }
    public static readonly string[] Objectives = [.. TutorialPlateObjectives.PitchIds, .. TutorialPlateObjectives.SwingIds, "manual-ground-possession", "human-dive-out", "human-double-play"];
    public static readonly string[] Policies = ["cpu-take", "cpu-strike", "cpu-ball", "grounder", "liner"];
    static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    TutorialCatalog(TutorialMechanicFile mechanics, TutorialLessonFile lessons, TutorialMigrationFile migration, string profile)
    {
        if (mechanics.Version != 1 || lessons.Version != 1 || migration.Version != 1)
            throw new InvalidDataException("Unsupported tutorial catalog version.");
        Mechanics = mechanics.Mechanics ?? throw new InvalidDataException("Missing mechanic inventory.");
        Lessons = lessons.Lessons ?? throw new InvalidDataException("Missing lesson catalog.");
        Setups = lessons.Setups ?? throw new InvalidDataException("Missing tutorial setups.");
        Migration = migration.Mechanics ?? throw new InvalidDataException("Missing migration list.");
        Profile = profile;
    }

    public static TutorialCatalog Load(ContentCatalog content)
    {
        T Read<T>(string name) => JsonSerializer.Deserialize<T>(File.ReadAllText(content.Root.Resolve("tutorials", name + ".json")), Json)
            ?? throw new InvalidDataException("Null tutorial file: " + name);
        try
        {
            var profile = content.Root.Overlay is null ? "shipped" : Path.GetFileName(content.Root.Overlay);
            var catalog = new TutorialCatalog(Read<TutorialMechanicFile>("mechanics"), Read<TutorialLessonFile>("lessons"), Read<TutorialMigrationFile>("migration"), profile);
            var errors = catalog.Validate(content);
            if (errors.Count != 0) throw new InvalidDataException(string.Join(Environment.NewLine, errors));
            return catalog;
        }
        catch (JsonException e) { throw new InvalidDataException("Invalid tutorial JSON: " + e.Message, e); }
    }

    /// <summary>Enumerated independently of lesson data. Adding a control, ability, item or star exposes missing coverage.</summary>
    public static IEnumerable<string> RuntimeSources(ContentCatalog content) =>
        RoleTables.Of(InputScheme.Pad).Concat(RoleTables.Of(InputScheme.Keys)).SelectMany(b => b.Rows.Select(r => $"control:{b.Id}/{r.Verb}")).Distinct()
        .Concat(ContentDataValidator.TutorialFieldAbilities.Select(a => "ability:" + a))
        .Concat(content.StarSkills.Pitches.Keys.Select(a => "star-pitch:" + a))
        .Concat(content.StarSkills.Swings.Keys.Select(a => "star-swing:" + a))
        .Concat(ErrorItems.All.Select(a => "item:" + a));

    public IReadOnlyList<string> Validate(ContentCatalog content)
    {
        var errors = new List<string>();
        void Require(bool ok, string message) { if (!ok) errors.Add("tutorials: " + message); }
        static bool Text(string? s) => !string.IsNullOrWhiteSpace(s);
        void Ids(IEnumerable<string> ids, string kind)
        {
            var all = ids.ToArray();
            Require(all.All(Text) && all.Distinct().Count() == all.Length, "empty or duplicate " + kind + " id");
        }
        if (Mechanics.Any(m => m is null || m.Sources is null) || Lessons.Any(l => l is null || l.Mechanics is null || l.Profiles is null || l.Requires is null || l.Prerequisites is null || l.Controls is null || l.Tests is null)
            || Setups.Any(s => s is null || s.Home is null || s.Away is null || s.Runners is null || s.Balls is null))
            return ["tutorials: null row or required collection"];
        Ids(Mechanics.Select(m => m.Id), "mechanic"); Ids(Lessons.Select(l => l.Id), "lesson"); Ids(Setups.Select(s => s.Id), "setup");
        var sources = RuntimeSources(content).ToHashSet();
        var declared = Mechanics.SelectMany(m => m.Sources).ToHashSet();
        foreach (var source in sources.Except(declared)) errors.Add("tutorials: unmapped runtime source " + source);
        foreach (var source in declared.Except(sources)) errors.Add("tutorials: unknown runtime source " + source);
        foreach (var m in Mechanics)
        {
            Require(Text(m.Title) && Text(m.Category) && Text(m.Spec), m.Id + " needs title/category/spec");
            var rows = Lessons.Where(l => l.Mechanics.Contains(m.Id)).ToArray();
            Require(rows.Length > 0, "uncovered mechanic " + m.Id);
            if (!rows.Any(l => l.Status == "implemented"))
                Require(Migration.TryGetValue(m.Id, out var issue) && issue > 0, "unowned migration debt " + m.Id);
        }
        foreach (var m in Migration)
            Require(m.Value > 0 && Mechanics.Any(x => x.Id == m.Key), "unknown/unowned migration row " + m.Key);
        foreach (var l in Lessons)
        {
            Require(l.Revision > 0 && l.Issue > 0 && Text(l.Title) && Text(l.Reason), l.Id + " needs revision/title/issue/reason");
            Require(l.Mechanics.Length > 0 && l.Mechanics.All(id => Mechanics.Any(m => m.Id == id)), l.Id + " has unknown mechanic");
            Require(l.Profiles.Length > 0 && l.Profiles.All(p => p is "shipped" or "c80"), l.Id + " has unknown profile");
            Require(l.Prerequisites.All(id => id != l.Id && Lessons.Any(x => x.Id == id)), l.Id + " has unknown/self prerequisite");
            Require(l.Requires.All(a => ContentDataValidator.TutorialFieldAbilities.Contains(a)), l.Id + " has unknown ability");
            Require(l.Controls.All(sources.Contains), l.Id + " has unknown controls");
            Require(l.Status is "planned" or "blocked" or "implemented", l.Id + " has unknown status (human acceptance is recorded separately)");
            if (l.Status != "implemented") continue;
            Require(Objectives.Contains(l.Objective), l.Id + " has unknown objective");
            Require(l.Tests.Length > 0 && l.Controls.Length > 0, l.Id + " needs regression evidence and controls");
            var setup = Setups.FirstOrDefault(s => s.Id == l.Setup);
            Require(setup is not null, l.Id + " has unknown setup");
            if (setup is null) continue;
            Require((setup.Policy == "cpu-take" && TutorialPlateObjectives.PitchIds.Contains(l.Objective))
                || (setup.Policy == "cpu-strike" && TutorialPlateObjectives.SwingIds.Contains(l.Objective) && l.Objective != "take-ball")
                || (setup.Policy == "cpu-ball" && l.Objective == "take-ball")
                || (setup.Policy == "grounder" && l.Objective is "manual-ground-possession" or "human-double-play")
                || (setup.Policy == "liner" && l.Objective == "human-dive-out"), l.Id + " setup/objective mismatch");
            if (l.Objective is "break-strike" or "rubber-strike")
                Require(setup.MinMovement01 > 0, l.Id + " needs a meaningful movement threshold");
            if (l.Objective == "bunt-fair") Require(setup.Strikes == 2, l.Id + " must teach the two-strike bunt risk");
            if (setup.Policy is "grounder" or "liner")
                Require(l.Profiles.All(setup.Balls.ContainsKey), l.Id + " lacks a profile ball fixture");
        }
        bool Cycle(string id, HashSet<string> path)
        {
            if (!path.Add(id)) return true;
            var row = Lessons.FirstOrDefault(l => l.Id == id);
            var cycle = row is not null && row.Prerequisites.Any(p => Cycle(p, new HashSet<string>(path)));
            return cycle;
        }
        foreach (var lesson in Lessons) Require(!Cycle(lesson.Id, []), lesson.Id + " prerequisite cycle");
        foreach (var s in Setups)
        {
            Require(s.Strikes is >= 0 and <= 2 && (s.Strikes == 0 || s.Policy is "cpu-strike" or "cpu-ball"), s.Id + " has invalid starting strikes");
            Require(double.IsFinite(s.MinMovement01) && s.MinMovement01 is >= 0 and <= 1, s.Id + " has invalid movement threshold");
            if (s.Policy is "cpu-strike" or "cpu-ball")
            {
                var pitch = s.Pitch ?? new PitchCommand("fastball", 0, false);
                Require(pitch.Type is "fastball" or "changeup" && !pitch.Star && !pitch.DeliveryPrepared
                    && new[] { pitch.Charge01, pitch.AimX, pitch.AimY, pitch.BreakX, pitch.RubberX, pitch.BreakMul }.All(double.IsFinite)
                    && pitch.Charge01 is >= 0 and <= 1 && Math.Abs(pitch.BreakX) <= 1 && Math.Abs(pitch.RubberX) <= 1
                    && Math.Abs(pitch.AimX) <= 4 && Math.Abs(pitch.AimY) <= 4 && pitch.BreakMul == 1,
                    s.Id + " has invalid CPU pitch");
                var crossing = PitchFlight.Crossing(pitch, rules: content.Rules);
                Require(StrikeZoneGeometry.Contains(crossing.X, crossing.Y) == (s.Policy == "cpu-strike"), s.Id + " CPU pitch disagrees with strike/ball policy");
                Require(new[] { Hand.L, Hand.R }.All(hand => !AtBatResolver.HitsBatter(0, crossing.X, crossing.Y, hand, content.Rules)), s.Id + " CPU pitch hits the batter");
            }
            else Require(s.Pitch is null, s.Id + " CPU pitch is not used by this policy");
            Require(Policies.Contains(s.Policy), s.Id + " has unknown CPU/setup policy");
            Require(double.IsFinite(s.TimeoutSec) && s.TimeoutSec > 0 && s.TimeoutSec <= 120, s.Id + " has invalid timeout");
            Require(s.Home.Length == 9 && s.Away.Length == 9 && s.Home.Distinct().Count() == 9 && s.Away.Distinct().Count() == 9
                && s.Home.Concat(s.Away).All(content.Characters.ContainsKey), s.Id + " has invalid teams");
            Require(s.Runners.All(b => b is >= 1 and <= 3) && s.Runners.Distinct().Count() == s.Runners.Length, s.Id + " has invalid runners");
            foreach (var pair in s.Balls)
            {
                var b = pair.Value;
                Require(pair.Key is "shipped" or "c80" && b is not null && double.IsFinite(b.CarryFt) && double.IsFinite(b.ExitMph)
                    && double.IsFinite(b.LaunchDeg) && double.IsFinite(b.SprayDeg) && b.CarryFt >= 0 && b.ExitMph >= 0
                    && ((b.CarryFt > 0) != (b.ExitMph > 0)) && b.LaunchDeg is > 0 and < 90 && Math.Abs(b.SprayDeg) < 45, s.Id + " has invalid ball fixture");
            }
        }
        return errors;
    }

    public TutorialLesson Lesson(string id) => Lessons.FirstOrDefault(l => l.Id == id)
        ?? throw new InvalidDataException("Unknown tutorial " + id);
}

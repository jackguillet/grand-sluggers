using System.Text.Json;

namespace GrandSluggers.Sim;

public sealed record TutorialMechanic(string Id, string Title, string Category, string Spec, string[] Sources);
public sealed record TutorialLesson(string Id, int Revision, string[] Mechanics, string Title, string Category,
    string Status, int Issue, string Reason, string[] Profiles, string[] Requires, string[] Prerequisites,
    string Setup, string Objective, string[] Controls, string[] Tests);
public sealed record TutorialBall(double CarryFt, double ExitMph, double LaunchDeg, double SprayDeg);
public sealed record TutorialSetup(string Id, string Policy, int Seed, double TimeoutSec, string[] Home,
    string[] Away, int[] Runners, Dictionary<string, TutorialBall> Balls, PitchCommand? Pitch = null,
    double MinMovement01 = 0, int Strikes = 0, double MinTimingFrames = 0, string Seat = "defense", double StartingStars = 0,
    string Skill = "", string PitcherId = "", string BatterId = "", string OnDeckId = "", double OpponentStars = 0,
    Dictionary<string, string[]>? RunnerIdsByProfile = null, int Outs = 0);
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
    public static readonly string[] Objectives = [.. TutorialPlateObjectives.PitchIds, .. TutorialPlateObjectives.SwingIds,
        "manual-ground-possession", "manual-takeover", "throw-bag-1", "throw-bag-2", "throw-bag-3", "throw-bag-4",
        "human-aerial-out", "human-dive-out", "human-jump-out", "human-double-play",
        "runner-send-halt-return", "human-dash-run", "all-runner-return", "human-slide", "human-tag-up", "human-double-off",
        "human-wall-carom", "human-buddy-rob", "human-ball-dash", "human-relay", "human-snap-relay", "human-laser-home", "human-choice-second", "human-pickoff", "tired-pitcher-swap",
        "human-steal", "human-double-steal", "human-catcher-tag",
        "human-buffered-relay", "human-retargeted-relay", "human-cancelled-relay",
        "guided-lineup", "guided-seats", "guided-pause", "guided-recovery", "guided-calibration", "star-pitch", "star-swing", "star-resource", "human-chemistry-throw", "item-effect", "human-special-ground", "human-loose-recovery", "human-uncovered-receiver", "human-force-home", "human-rundown-tag", "human-ability-reach", "human-close-offense", "human-close-defense", "human-third-force-zero-run", "game-count-sequence", "game-foul-fair", "game-half-change", "human-triple-off", "human-bobble-recovery", "human-corner-dash", "human-early-fly-return", "human-fumble-recovery", "human-third-force-cancels-run", "human-third-tag-counts-run"];
    public static readonly string[] Policies = ["cpu-take", "cpu-strike", "cpu-ball", "grounder", "liner", "airborne", "pickoff", "pitcher-swap", "steal-offense", "steal-defense", "cpu-item", "cpu-special-ground", "game-count", "game-contact", "game-half"];

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
            Require(l.Profiles.Length > 0 && l.Profiles.All(p => p is "shipped"), l.Id + " has unknown profile");
            Require(l.Prerequisites.All(id => id != l.Id && Lessons.Any(x => x.Id == id)), l.Id + " has unknown/self prerequisite");
            Require(l.Requires.All(a => ContentDataValidator.TutorialFieldAbilities.Contains(a)), l.Id + " has unknown ability");
            Require(l.Controls.All(sources.Contains), l.Id + " has unknown controls");
            Require(l.Status is "planned" or "blocked" or "implemented", l.Id + " has unknown status (human acceptance is recorded separately)");
            if (l.Status != "implemented") continue;
            Require(Objectives.Contains(l.Objective), l.Id + " has unknown objective");
            var guided = l.Objective is "guided-lineup" or "guided-seats" or "guided-pause" or "guided-recovery" or "guided-calibration";
            Require(l.Tests.Length > 0 && (guided || l.Controls.Length > 0), l.Id + " needs regression evidence and controls");
            if (guided)
            {
                Require((l.Id, l.Objective) is ("T-G01", "guided-lineup") or ("T-G05", "guided-seats")
                    or ("T-G06", "guided-pause") or ("T-G06-R", "guided-recovery") or ("T-G06-C", "guided-calibration"),
                    l.Id + " has unknown guided objective");
                if (l.Objective == "guided-calibration")
                {
                    if (l.Profiles.Contains(Profile)) Require(content.Rules.Fielding.Stick.Radial, l.Id + " profile does not offer Reset stick");
                }
                Require(l.Setup == "", l.Id + " guided lesson must use existing screens");
                continue;
            }
            var setup = Setups.FirstOrDefault(s => s.Id == l.Setup);
            Require(setup is not null, l.Id + " has unknown setup");
            if (setup is null) continue;
            Require((setup.Policy == "cpu-take" && TutorialPlateObjectives.PitchIds.Contains(l.Objective))
                || (setup.Policy == "cpu-take" && l.Objective == "star-pitch")
                || (setup.Policy == "cpu-take" && l.Objective == "star-resource")
                || (setup.Policy == "cpu-strike" && TutorialPlateObjectives.SwingIds.Contains(l.Objective) && l.Objective is not ("take-ball" or "cancel-take"))
                || (setup.Policy == "cpu-strike" && l.Objective == "star-swing")
                || (setup.Policy == "cpu-item" && l.Objective == "item-effect")
                || (setup.Policy == "game-count" && l.Objective == "game-count-sequence")
                || (setup.Policy == "game-contact" && l.Objective == "game-foul-fair")
                || (setup.Policy == "game-half" && l.Objective == "game-half-change")
                || (setup.Policy == "cpu-special-ground" && l.Objective == "human-special-ground")
                || (setup.Policy == "cpu-ball" && l.Objective is "take-ball" or "cancel-take")
                || (setup.Policy == "pickoff" && l.Objective is "human-pickoff" or "human-rundown-tag")
                || (setup.Policy == "pitcher-swap" && l.Objective == "tired-pitcher-swap")
                || (setup.Policy == "steal-offense" && l.Objective is "human-steal" or "human-double-steal")
                || (setup.Policy == "steal-defense" && l.Objective == "human-catcher-tag")
                || (setup.Policy == "grounder" && l.Objective is "manual-ground-possession" or "manual-takeover" or "human-double-play"
                    or "throw-bag-1" or "throw-bag-2" or "throw-bag-3" or "throw-bag-4"
                    or "runner-send-halt-return" or "human-dash-run" or "all-runner-return" or "human-slide"
                    or "human-choice-second" or "human-ball-dash" or "human-uncovered-receiver" or "human-force-home" or "human-ability-reach" or "human-close-offense" or "human-close-defense" or "human-third-force-zero-run" or "human-bobble-recovery" or "human-fumble-recovery" or "human-third-force-cancels-run" or "human-third-tag-counts-run")
                || (setup.Policy == "liner" && l.Objective == "human-dive-out")
                || (setup.Policy == "airborne" && l.Objective is "human-aerial-out" or "human-jump-out"
                    or "human-wall-carom" or "human-buddy-rob" or "human-relay" or "human-snap-relay" or "human-laser-home" or "human-buffered-relay" or "human-retargeted-relay" or "human-cancelled-relay" or "human-chemistry-throw" or "human-tag-up" or "human-double-off" or "human-loose-recovery" or "human-ability-reach" or "human-triple-off" or "human-corner-dash" or "human-early-fly-return"), l.Id + " setup/objective mismatch");
            if (l.Objective is "game-count-sequence" or "game-half-change" or "game-foul-fair")
                Require((l.Id is "T-G04" or "T-G04-F" or "T-G04-H") && setup.Seat == (l.Objective == "game-foul-fair" ? "offense" : "defense")
                    && setup.Strikes == (l.Objective == "game-half-change" ? 2 : 0) && setup.Runners.Length == 0,
                    l.Id + " needs its teaching seat, count and empty bases");
            if (l.Objective == "human-ability-reach") Require((l.Id == "T-F16" || l.Id == "T-A-" + setup.Skill)
                && l.Requires.Contains(setup.Skill)
                && setup.Home.Any(id => content.Characters[id].FieldAbility == setup.Skill),
                l.Id + " needs the named ability fielder in its home nine");
            if (l.Objective is "break-strike" or "rubber-strike" or "box-perfect-fair")
                Require(setup.MinMovement01 > 0, l.Id + " needs a meaningful movement threshold");
            if (l.Objective is "pull-fair" or "push-fair")
                Require(setup.MinTimingFrames > 0, l.Id + " needs a meaningful timing threshold");
            if (l.Objective == "box-perfect-fair")
                Require(setup.Pitch is not null && Math.Abs(PitchFlight.Crossing(setup.Pitch, rules: content.Rules).X) >= setup.MinMovement01 * HomeSet.BatterWalk,
                    l.Id + " needs an offset pitch for box movement");
            if (l.Objective == "bunt-fair") Require(setup.Strikes == 2, l.Id + " must teach the two-strike bunt risk");
            if (l.Objective == "star-resource") Require(l.Id == "T-G03" && setup.Strikes == 2
                && setup.Skill == content.Characters[setup.Home[0]].StarPitch && setup.StartingStars > 0,
                l.Id + " needs a two-strike star gain/spend setup");
            if (l.Objective == "item-effect") Require((l.Id == "T-X01" || l.Id == "T-I-" + setup.Skill)
                && ErrorItems.Known(setup.Skill) && setup.Seat == "offense"
                && setup.Away.Contains(setup.BatterId) && setup.Away.Contains(setup.OnDeckId)
                && content.Chemistry.ChemistryItemOffered(content.Characters[setup.BatterId], content.Characters[setup.OnDeckId]),
                l.Id + " needs an offered item and named item fixture");
            if (l.Objective == "human-special-ground") Require(l.Id == "T-X02" && setup.Skill == "ground"
                && setup.OpponentStars >= content.Rules.Stars.Costs.Own && setup.Away.Contains(setup.BatterId)
                && content.Characters[setup.BatterId].StarSwing == setup.Skill,
                l.Id + " needs a funded opponent star-ground swing");
            if (l.Objective == "star-pitch") Require((l.Id == "T-P09" || l.Id == "T-SP-" + setup.Skill)
                && content.StarSkills.Pitches.ContainsKey(setup.Skill)
                && setup.Home.Contains(setup.PitcherId.Length > 0 ? setup.PitcherId : setup.Home[0])
                && content.Characters[setup.PitcherId.Length > 0 ? setup.PitcherId : setup.Home[0]].StarPitch == setup.Skill
                && setup.StartingStars >= content.Rules.Stars.Costs.Own, l.Id + " requires the named pitcher and meter");
            if (l.Objective == "star-swing") Require((l.Id == "T-B09" || l.Id == "T-SS-" + setup.Skill)
                && content.StarSkills.Swings.ContainsKey(setup.Skill)
                && setup.Away.Contains(setup.BatterId.Length > 0 ? setup.BatterId : setup.Away[0])
                && content.Characters[setup.BatterId.Length > 0 ? setup.BatterId : setup.Away[0]].StarSwing == setup.Skill
                && setup.StartingStars >= content.Rules.Stars.Costs.Own, l.Id + " requires the named batter and meter");
            if (setup.Policy is "grounder" or "liner" or "airborne")
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
            Require(s.Seat is "offense" or "defense" or "assisted-defense", s.Id + " has unknown teaching seat");
            Require((s.PitcherId.Length == 0 || s.Home.Contains(s.PitcherId))
                && (s.BatterId.Length == 0 || s.Away.Contains(s.BatterId))
                && (s.OnDeckId.Length == 0 || s.Away.Contains(s.OnDeckId)), s.Id + " has a skill player outside its team");
            Require(double.IsFinite(s.StartingStars) && s.StartingStars >= 0 && s.StartingStars <= content.Rules.Stars.MeterMax,
                s.Id + " has invalid starting stars");
            Require(double.IsFinite(s.OpponentStars) && s.OpponentStars >= 0 && s.OpponentStars <= content.Rules.Stars.MeterMax,
                s.Id + " has invalid opponent stars");
            Require(double.IsFinite(s.MinTimingFrames) && s.MinTimingFrames is >= 0 and <= 4, s.Id + " has invalid timing threshold");
            Require(s.Strikes is >= 0 and <= 2 && (s.Strikes == 0 || s.Policy is "cpu-strike" or "cpu-ball" or "cpu-take" or "game-half"), s.Id + " has invalid starting strikes");
            Require(s.Outs is >= 0 and <= 2, s.Id + " has invalid starting outs");
            Require(double.IsFinite(s.MinMovement01) && s.MinMovement01 is >= 0 and <= 1, s.Id + " has invalid movement threshold");
            if (s.Policy is "cpu-strike" or "cpu-ball" or "cpu-item" or "game-contact")
            {
                var pitch = s.Pitch ?? new PitchCommand(PitchFamily.Fastball, 0, false);
                // A scripted pitch names a family the library has numbers for: an unauthored or
                // unknown id is a lesson that cannot be thrown, and it is reported here rather than
                // thrown out of the flight below (#810).
                var authored = content.Rules.Pitching.Families.IsAuthored(pitch.Type);
                Require(authored && !pitch.Star && !pitch.DeliveryPrepared
                    && new[] { pitch.Charge01, pitch.AimX, pitch.AimY, pitch.BreakX, pitch.RubberX, pitch.BreakMul }.All(double.IsFinite)
                    && pitch.Charge01 is >= 0 and <= 1 && Math.Abs(pitch.BreakX) <= 1 && Math.Abs(pitch.RubberX) <= 1
                    && Math.Abs(pitch.AimX) <= 4 && Math.Abs(pitch.AimY) <= 4 && pitch.BreakMul == 1,
                    s.Id + " has invalid CPU pitch");
                if (authored)
                {
                    var crossing = PitchFlight.Crossing(pitch, rules: content.Rules);
                    Require(StrikeZoneGeometry.Contains(crossing.X, crossing.Y) == (s.Policy is "cpu-strike" or "cpu-item" or "game-contact"), s.Id + " CPU pitch disagrees with strike/ball policy");
                    Require(new[] { Hand.L, Hand.R }.All(hand => !AtBatResolver.HitsBatter(0, crossing.X, crossing.Y, hand, content.Rules)), s.Id + " CPU pitch hits the batter");
                }
            }
            else if (s.Policy is "steal-offense" or "steal-defense")
                Require(s.Pitch is null || (content.Rules.Pitching.Families.IsAuthored(s.Pitch.Type) && !s.Pitch.Star
                    && double.IsFinite(s.Pitch.Charge01) && s.Pitch.Charge01 is >= 0 and <= 1),
                    s.Id + " has invalid scripted steal pitch");
            else Require(s.Pitch is null, s.Id + " CPU pitch is not used by this policy");
            Require(Policies.Contains(s.Policy), s.Id + " has unknown CPU/setup policy");
            Require(double.IsFinite(s.TimeoutSec) && s.TimeoutSec > 0 && s.TimeoutSec <= 120, s.Id + " has invalid timeout");
            Require(s.Home.Length == 9 && s.Away.Length == 9 && s.Home.Distinct().Count() == 9 && s.Away.Distinct().Count() == 9
                && s.Home.Concat(s.Away).All(content.Characters.ContainsKey), s.Id + " has invalid teams");
            Require(s.Runners.All(b => b is >= 1 and <= 3) && s.Runners.Distinct().Count() == s.Runners.Length, s.Id + " has invalid runners");
            if (s.RunnerIdsByProfile is { } named)
                foreach (var pair in named)
                    Require(pair.Key is "shipped" && pair.Value is not null
                        && pair.Value.Length == s.Runners.Length && pair.Value.Distinct().Count() == pair.Value.Length
                        && pair.Value.All(s.Away.Contains), s.Id + " has invalid profile runner identities");
            foreach (var pair in s.Balls)
            {
                var b = pair.Value;
                Require(pair.Key is "shipped" && b is not null && double.IsFinite(b.CarryFt) && double.IsFinite(b.ExitMph)
                    && double.IsFinite(b.LaunchDeg) && double.IsFinite(b.SprayDeg) && b.CarryFt >= 0 && b.ExitMph >= 0
                    && ((b.CarryFt > 0) != (b.ExitMph > 0)) && b.LaunchDeg is > 0 and < 90 && Math.Abs(b.SprayDeg) < 45, s.Id + " has invalid ball fixture");
            }
        }
        return errors;
    }

    public TutorialLesson Lesson(string id) => Lessons.FirstOrDefault(l => l.Id == id)
        ?? throw new InvalidDataException("Unknown tutorial " + id);
}

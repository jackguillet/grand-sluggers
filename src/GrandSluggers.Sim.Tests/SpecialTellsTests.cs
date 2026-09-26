using GrandSluggers.Sim;
using GrandSluggers.Sim.Front;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// AB-C15: every captain special shows a tell. Its VFX slot names a stand-in the client can draw and a folder for its art,
/// its sound is an audio slot, its beats come from its own row, and every name a player reads is the row's.
/// </summary>
public sealed class SpecialTellsTests
{
    readonly ContentCatalog _content = Shipped.Content;

    IEnumerable<(Character Who, string Id, bool Pitch)> CaptainSpecials() =>
        _content.Characters.Values.Where(c => c.Captain)
            .SelectMany(c => new[] { (c, c.StarPitch, true), (c, c.StarSwing, false) });

    [Fact]
    public void EveryCaptainSpecialNamesATellItsOwnFolderAndAKnownBuilder()
    {
        var tells = new List<string>();
        foreach (var (who, id, _) in CaptainSpecials())
        {
            Assert.True(_content.Art.TryVfx(id, out var slot), who.Id + " " + id);
            Assert.True(SpecialTells.IsBuilder(slot.Tell), id + " tell " + slot.Tell);
            Assert.Equal(ArtCatalog.VfxRoot + "/" + id, slot.Slot);
            var folder = Path.Combine(_content.Root.Shipped, "..", "unity", slot.Slot);
            Assert.True(Directory.Exists(folder), id + " has no slot folder " + slot.Slot);
            Assert.Contains(slot.Kind, new[] { "ball", "field", "contact" });
            tells.Add(slot.Tell!);
        }
        // One tell per special: no two captains' specials read alike.
        Assert.Equal(tells.Count, tells.Distinct().Count());
        Assert.Equal(20, tells.Count);
        Assert.DoesNotContain(_content.Art.Validate(_content), e => e.StartsWith("vfx", StringComparison.Ordinal));
    }

    [Fact]
    public void EveryCueIsAnAudioSlotAndEveryBuilderIsUsed()
    {
        var cues = _content.Art.Vfx.Where(v => v.Cue is not null).ToList();
        Assert.NotEmpty(cues);
        foreach (var v in cues)
        {
            Assert.True(_content.Art.TryAudio(v.Cue!, out var audio), v.Id + " cue " + v.Cue);
            Assert.Equal("sfx", audio.Kind);
            Assert.StartsWith(v.Id + "-", v.Cue!);
        }
        foreach (var builder in SpecialTells.Builders)
            Assert.Contains(_content.Art.Vfx, v => v.Tell == builder);
    }

    [Fact]
    public void OnlyTheListedStandInsAreBuilders()
    {
        Assert.Equal(SpecialTells.Builders.Count, SpecialTells.Builders.Distinct().Count());
        Assert.False(SpecialTells.IsBuilder("prism-ghosts"));
        Assert.False(SpecialTells.IsBuilder(""));
        Assert.False(SpecialTells.IsBuilder(null));
    }

    [Fact]
    public void PitchBeatsAreTheRowsOwnInstants()
    {
        var skills = _content.StarSkills;
        var rules = _content.Rules;
        double[] Beats(string id) => SpecialTells.PitchBeats(skills.Pitch(id), rules).ToArray();

        Assert.Equal([skills.Pitch("heatball")!.Rise!.From], Beats("heatball"));
        Assert.Equal([skills.Pitch("prismball")!.Loop!.At], Beats("prismball"));
        Assert.Equal([rules.Pitching.StarShapes.PhonyballSwitchAt], Beats("phonyball"));
        Assert.Equal([skills.Pitch("skullball")!.Drop!.From], Beats("skullball"));
        Assert.Equal([skills.Pitch("rockfall")!.Hitch!.At], Beats("rockfall"));
        var vanish = skills.Pitch("mirageball")!.Vanish!;
        Assert.Equal([vanish.From, vanish.To], Beats("mirageball"));
        var skips = skills.Pitch("leapfrog")!.Skips!;
        Assert.Equal([skips.FirstAt, skips.SecondAt], Beats("leapfrog"));
        // A tell with no instant (a trail, a ring) has no beat.
        Assert.Empty(Beats("charmball"));
        Assert.Empty(Beats("caskball"));
        Assert.Empty(Beats("fogball"));
        Assert.Empty(SpecialTells.PitchBeats(null, rules));
        // Every beat is inside the flight.
        foreach (var row in skills.Pitches.Values)
            Assert.All(SpecialTells.PitchBeats(row, rules), u => Assert.InRange(u, 0, 1));
    }

    [Fact]
    public void ABeatFiresOnceAsTheClockPassesIt()
    {
        var fired = 0;
        var before = 0.0;
        for (var u = 0.0; u <= 1.0001; u += 1 / 37.0)
        {
            if (SpecialTells.Crossed(before, u, 0.7)) fired++;
            before = u;
        }
        Assert.Equal(1, fired);
        Assert.True(SpecialTells.Crossed(0.69, 0.7, 0.7));
        Assert.False(SpecialTells.Crossed(0.7, 0.71, 0.7));
    }

    [Fact]
    public void ApexIsTheHighestSampleBeforeTheLanding()
    {
        Sample[] path =
        [
            new(0, 0, 3), new(0.5, 30, 20), new(1.0, 60, 31), new(1.5, 90, 22),
            new(2.0, 120, 0, Event: SampleEvent.Ground), new(2.5, 140, 9),
        ];
        Assert.Equal(1.0, SpecialTells.ApexT(path));
        Assert.Equal(0, SpecialTells.ApexT([]));
        Assert.Equal(0, SpecialTells.ApexT(null));
    }

    [Fact]
    public void TheBoltEndsWhenTheSecondJagIsBackOnTheLine()
    {
        var jag = _content.StarSkills.Swing("cask-swing")!.Jag!;
        Sample[] path = [new(0, 0, 3), new(1.0, 80, 8), new(1.6, 130, 0, Event: SampleEvent.Ground)];
        var done = SpecialTells.JagDoneT(path, jag);
        Assert.Equal(BallJag.WindowSec(path) * Math.Min(1, jag.SecondAt + jag.Span), done, 9);
        Assert.InRange(done, 0, 1.6);
        Assert.Equal(0, SpecialTells.JagDoneT([], jag));
    }

    [Fact]
    public void EveryCardNamesItsSpecialsByTheirRows()
    {
        foreach (var who in _content.Characters.Values)
        {
            var card = CharacterCard.Of(who, skills: _content.StarSkills);
            Assert.Equal(_content.StarSkills.Pitch(who.StarPitch)!.Name, card.StarPitch);
            Assert.Equal(_content.StarSkills.Swing(who.StarSwing)!.Name, card.StarSwing);
            // The fallback table is the same rows.
            Assert.Equal(card.StarPitch, CharacterCard.Of(who).StarPitch);
        }
        Assert.Equal("Skyrocket", CharacterCard.Of(_content.Must("rio"), skills: _content.StarSkills).StarPitch);
        Assert.Equal("Lily Hop", CharacterCard.Of(_content.Must("reed"), skills: _content.StarSkills).StarSwing);
        Assert.Equal("", StarSkills.PitchName(null));
        Assert.Equal("no-such-pitch", StarSkills.PitchName("no-such-pitch", _content.StarSkills));
    }

    [Fact]
    public void TheFirstStarLessonsNameTheSkillsTheirSetupsTeach()
    {
        var catalog = TutorialCatalog.Load(_content);
        foreach (var id in new[] { "T-P09", "T-B09" })
        {
            var setup = catalog.Setups.Single(s => s.Id == id);
            Assert.Equal(setup.Skill, HowToPlay.LessonSkill(id));
            var name = id == "T-P09" ? StarSkills.PitchName(setup.Skill) : StarSkills.SwingName(setup.Skill);
            Assert.Contains(name, HowToPlay.TutorialTitle(id));
            Assert.Contains(name, HowToPlay.TutorialSetup(id));
        }
        Assert.DoesNotContain("Heat", HowToPlay.TutorialTitle("T-P09"));
        Assert.DoesNotContain("Heat", HowToPlay.TutorialTitle("T-B09"));
        foreach (var setup in catalog.Setups.Where(s => s.Id.StartsWith("T-SP-", StringComparison.Ordinal) || s.Id.StartsWith("T-SS-", StringComparison.Ordinal)))
            Assert.Equal(setup.Skill, HowToPlay.LessonSkill(setup.Id));
    }

    [Fact]
    public void TheBookNamesEveryCaptainSpecialsTell()
    {
        var lines = new[] { "star-tells", "star-tells-2" }.SelectMany(p => HowToPlay.Must(p).Lines).ToList();
        foreach (var (_, id, pitch) in CaptainSpecials())
        {
            var name = pitch ? _content.StarSkills.Pitch(id)!.Name : _content.StarSkills.Swing(id)!.Name;
            Assert.Contains(lines, l => l.Contains(name + ":", StringComparison.Ordinal));
        }
        var book = File.ReadAllText(Path.Combine(_content.Root.Shipped, "..", "docs", "how-to-play.md"));
        foreach (var (_, id, pitch) in CaptainSpecials())
        {
            var name = pitch ? _content.StarSkills.Pitch(id)!.Name : _content.StarSkills.Swing(id)!.Name;
            Assert.Contains(name + ":", book);
        }
    }
}

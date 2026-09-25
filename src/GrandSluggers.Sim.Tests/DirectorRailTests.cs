using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// "Do not grow MatchDirector" as a test (#1042). Each Unity director becomes a real class that owns its state; until
/// then the files still written as <c>partial class MatchDirector</c> are listed here, and the list only shrinks. A new
/// partial fails, and <c>MatchDirector.cs</c> may not grow past its ceiling. When a director moves out, delete its row
/// and lower the ceiling to the new length.
/// </summary>
public sealed class DirectorRailTests
{
    static readonly string[] StillPartial =
    [
        "FlowDirector.cs", "MatchDirector.cs",
    ];

    /// <summary>The line count of <c>MatchDirector.cs</c> may only fall. Lower it with every director that leaves.</summary>
    const int MatchDirectorCeiling = 740;

    static string Scripts => Path.GetFullPath(Path.Combine(Shipped.Content.Root.Shipped, "..", "unity", "Assets", "Scripts"));

    [Fact]
    public void NoNewFileExtendsMatchDirector()
    {
        var partials = Directory.GetFiles(Scripts, "*.cs", SearchOption.AllDirectories)
            .Where(f => File.ReadAllText(f).Contains("partial class MatchDirector", StringComparison.Ordinal))
            .Select(Path.GetFileName)
            .OrderBy(f => f, StringComparer.Ordinal)
            .ToList();
        Assert.Empty(partials.Except(StillPartial));
        // A director that became a real class leaves the list, so the list cannot hide a regression later.
        Assert.Empty(StillPartial.Except(partials));
    }

    [Fact]
    public void MatchDirectorDoesNotGrow()
    {
        var lines = File.ReadAllLines(Path.Combine(Scripts, "Runtime", "MatchDirector.cs")).Length;
        Assert.True(lines <= MatchDirectorCeiling,
            $"MatchDirector.cs is {lines} lines, over its ceiling of {MatchDirectorCeiling}: put the code in a director that owns it");
    }

    [Theory]
    [InlineData("StealDirector")]
    [InlineData("PursuitSeatDirector")]
    [InlineData("GuidedTutorialDirector")]
    [InlineData("TutorialDirector")]
    [InlineData("StillStaging")]
    [InlineData("InPlayDirector")]
    [InlineData("ActorDirector")]
    [InlineData("SeatPads")]
    [InlineData("StarRequests")]
    [InlineData("ItemToss")]
    [InlineData("DefenseSwapWindow")]
    [InlineData("SetCamera")]
    [InlineData("AtBatDirector")]
    [InlineData("LineupFlow")]
    public void TheDirectorIsARealClass(string director)
    {
        var text = File.ReadAllText(Path.Combine(Scripts, "Runtime", director + ".cs"));
        Assert.Matches(@"(public|internal) sealed class " + director + @"\b", text);
        Assert.DoesNotContain("partial class MatchDirector", text, StringComparison.Ordinal);
    }

    /// <summary>
    /// SET clears the pitch, the live play and the at-bat on the objects that own them (<c>PlayState.NewPitch</c>,
    /// <c>LiveFieldState.NewPitch</c>, <c>AtBatDirector.NewPitch</c>), so a new field is cleared where it is declared, not in the at-bat.
    /// </summary>
    [Fact]
    public void TheNewPitchClearsStateOnItsOwners()
    {
        var atBat = File.ReadAllText(Path.Combine(Scripts, "Runtime", "AtBatDirector.cs"));
        Assert.Contains("_play.NewPitch();", atBat, StringComparison.Ordinal);
        Assert.Contains("_live.NewPitch();", atBat, StringComparison.Ordinal);
        Assert.Matches(@"(?m)^\s+NewPitch\(\);", atBat);
        foreach (var owned in new[] { "GloveAt.Clear()", "ResultBodies = null", "Pending = null", "CloseBag = 0" })
            Assert.DoesNotContain(owned, atBat, StringComparison.Ordinal);
    }

    /// <summary>
    /// The scene and the play in flight are two objects MatchDirector owns and hands to directors; its old field names
    /// only forward to them until the last partial leaves. A new scene or play field goes on the object, not here.
    /// </summary>
    [Fact]
    public void TheSceneAndThePlayAreSharedObjects()
    {
        var director = File.ReadAllText(Path.Combine(Scripts, "Runtime", "MatchDirector.cs"));
        Assert.Contains("internal readonly MatchScene Scene = new MatchScene();", director, StringComparison.Ordinal);
        Assert.Contains("internal readonly PlayState Play = new PlayState();", director, StringComparison.Ordinal);
        foreach (var forwarded in new[] { "_park", "_cam", "_heroes", "_content", "_feel" })
            Assert.Matches(@"\s" + forwarded + @" (\{ get => Scene\.|=> Scene\.)", director);
        foreach (var forwarded in new[] { "_match", "_phase", "_pitch", "_swing", "_pending", "_preview", "_path" })
            Assert.Matches(@"\s" + forwarded + @" \{ get => Play\.", director);
        Assert.Contains("internal readonly LiveFieldState Live = new LiveFieldState();", director, StringComparison.Ordinal);
        foreach (var forwarded in new[] { "_glovePos", "_throwing", "_closePlay", "_caught", "_cpuField", "_bagStamp" })
            Assert.Matches(@"\s" + forwarded + @" \{ get => Live\.", director);
    }

    [Fact]
    public void TheStillCaptureIsEditorOnly()
    {
        // #1043: a shipped player neither carries the capture nor polls for a request; the editor creates it on demand.
        var runtime = Directory.GetFiles(Path.Combine(Scripts, "Runtime"), "*.cs", SearchOption.AllDirectories)
            .Select(f => (Name: Path.GetFileName(f), Text: File.ReadAllText(f)))
            .ToList();
        Assert.DoesNotContain(runtime, f => f.Text.Contains("StillRequest.TryLoad", StringComparison.Ordinal));
        // Code that reaches the capture (a call, a component): the staging file may name it in prose.
        Assert.DoesNotContain(runtime, f => f.Text.Contains("StillCapture.", StringComparison.Ordinal)
            || f.Text.Contains("<StillCapture>", StringComparison.Ordinal));
        var editor = File.ReadAllText(Path.GetFullPath(Path.Combine(Scripts, "..", "Editor", "StillCapture.cs")));
        Assert.Contains("[InitializeOnLoad]", editor, StringComparison.Ordinal);
        Assert.Contains("File.Exists(StillRequest.RequestPath(temp))", editor, StringComparison.Ordinal);
        // Unity attaches only runtime-assembly components: the editor capture must not be one, or Play refuses it and the
        // gate waits forever. It runs on the runtime PlayHost.
        Assert.DoesNotContain(": MonoBehaviour", editor, StringComparison.Ordinal);
        Assert.Contains("AddComponent<PlayHost>()", editor, StringComparison.Ordinal);
    }

    [Fact]
    public void EditorGatesReachTheDirectorsThroughCompiledMembersNotReflection()
    {
        // #1044: a gate that reads a private field by name compiles after a rename and breaks only when it runs. The
        // editor assembly sees the Runtime's internals (InternalsVisibleTo), so a gate names the member itself.
        var editor = Path.GetFullPath(Path.Combine(Scripts, "..", "Editor"));
        var reflective = new[] { "BindingFlags.NonPublic", ".GetField(", ".GetMethod(", ".GetProperty(" };
        var offenders = Directory.GetFiles(editor, "*.cs")
            .SelectMany(f => File.ReadAllLines(f).Select((line, i) => (File: Path.GetFileName(f), Line: line, At: i + 1)))
            .Where(x => reflective.Any(r => x.Line.Contains(r, StringComparison.Ordinal)))
            .Select(x => $"{x.File}:{x.At}: {x.Line.Trim()}")
            .ToList();
        Assert.Empty(offenders);
    }
}

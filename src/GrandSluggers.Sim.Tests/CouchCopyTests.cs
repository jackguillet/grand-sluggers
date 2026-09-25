using System.Text.RegularExpressions;
using RegexMatch = System.Text.RegularExpressions.Match;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// Couch copy lives in HowToPlay / CarnivalFront / BroadcastHud (AGENTS.md; #1048), not in the Unity views. A word a
/// player reads that is written straight into a draw call, or into the play banner, fails here with its file and line.
/// The developer overlays (the F2 feel overlay, the editor still staging) are not couch copy.
/// </summary>
public sealed class CouchCopyTests
{
    static readonly string[] DeveloperTools = ["FeelOverlay.cs", "StillStaging.cs"];

    static readonly Regex Draw = new(
        @"(?:GUI\.(?:Label|Box|Button|TextField)|(?<![\w.])(?:Text|Label|Button|PitcherButton|Sticker|StatRow|TutorialText|SeatCard|FocusRows))\(");
    static readonly Regex Literal = new(@"(?<![$@\w])""((?:[^""\\]|\\.)*)""");
    static readonly Regex Banner = new(@"\b(?:_banner|_sub)\s*=\s*([^;]*)");
    /// <summary>An id, not a word: lowercase with a hyphen (<c>guided-complete</c>).</summary>
    static readonly Regex Id = new(@"^[a-z0-9]+(?:-[a-z0-9]+)+$");

    static string Runtime => Path.GetFullPath(Path.Combine(Shipped.Content.Root.Shipped, "..", "unity", "Assets", "Scripts", "Runtime"));

    static bool IsCopy(string text) => text.Any(char.IsLetter) && !Id.IsMatch(text);

    static IEnumerable<string> Offenders()
    {
        foreach (var file in Directory.GetFiles(Runtime, "*.cs").Where(f => !DeveloperTools.Contains(Path.GetFileName(f))))
        {
            var text = File.ReadAllText(file);
            int LineOf(int at) => text.Take(at).Count(c => c == '\n') + 1;
            foreach (RegexMatch call in Draw.Matches(text))
            {
                var args = Arguments(text, call.Index + call.Length - 1);
                // Label(transform, "Name", …) builds a 3D text object: the literal is its object name, not copy.
                if (args.TrimStart().StartsWith("(transform", StringComparison.Ordinal) || args.TrimStart().StartsWith("(go.transform", StringComparison.Ordinal))
                    continue;
                foreach (RegexMatch lit in Literal.Matches(args))
                    if (IsCopy(lit.Groups[1].Value))
                        yield return $"{Path.GetFileName(file)}:{LineOf(call.Index)}: \"{lit.Groups[1].Value}\"";
            }
            foreach (RegexMatch assign in Banner.Matches(text))
                foreach (RegexMatch lit in Literal.Matches(assign.Groups[1].Value))
                    if (IsCopy(lit.Groups[1].Value))
                        yield return $"{Path.GetFileName(file)}:{LineOf(assign.Index)}: banner \"{lit.Groups[1].Value}\"";
        }
    }

    /// <summary>The call's argument text, from its opening parenthesis to the matching close.</summary>
    static string Arguments(string text, int open)
    {
        var depth = 0;
        var inString = false;
        for (var i = open; i < text.Length; i++)
        {
            var c = text[i];
            if (inString)
            {
                if (c == '\\') i++;
                else if (c == '"') inString = false;
            }
            else if (c == '"') inString = true;
            else if (c == '(') depth++;
            else if (c == ')' && --depth == 0) return text.Substring(open, i - open + 1);
        }
        return text.Substring(open);
    }

    [Fact]
    public void NoWordAPlayerReadsIsWrittenInAUnityView() => Assert.Empty(Offenders());

    [Fact]
    public void TheGuardSeesALiteralInADrawCall()
    {
        var sample = "GUI.Label(new Rect(1, 2, 3, 4), \"Item smashed.\", _gold);";
        var call = Draw.Match(sample);
        Assert.True(call.Success);
        Assert.Contains(Literal.Matches(Arguments(sample, call.Index + call.Length - 1)).Select(m => m.Groups[1].Value), IsCopy);
    }
}

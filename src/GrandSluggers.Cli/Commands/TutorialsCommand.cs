using System.Text.Json;
using GrandSluggers.Sim;

namespace GrandSluggers.Cli;

sealed class TutorialsCommand : Command
{
    public override string Name => "tutorials";
    public override IReadOnlyList<string> Usage => ["tutorials [--replay recording.json]"];
    public override IReadOnlyList<Option> Options => [new("--replay", Arity.Value)];

    public override int Run(ContentCatalog content, CommandLine line)
    {
        try
        {
            var tutorials = TutorialCatalog.Load(content);
            if (line.Text("--replay") is { } path)
            {
                var recording = JsonSerializer.Deserialize<TutorialRecording>(File.ReadAllText(path))
                    ?? throw new InvalidDataException("Empty tutorial recording.");
                var run = TutorialSession.Replay(content, tutorials, recording);
                Console.WriteLine(JsonSerializer.Serialize(new { tutorials.Profile, run.Lesson.Id, run.Phase, run.Feedback, run.Successes, requiredSuccesses = TutorialProgress.RequiredSuccesses, run.Passed, run.HumanThrows }));
                return run.Feedback?.Success == true ? 0 : 1;
            }
            Console.WriteLine($"TUTORIALS {tutorials.Profile} — {tutorials.Mechanics.Length} mechanics; {tutorials.Lessons.Count(l => l.Status == "implemented")} headless lessons (human learning gate separate)");
            foreach (var lesson in tutorials.Lessons)
                Console.WriteLine($"{lesson.Id,-22} {lesson.Status,-12} #{lesson.Issue} {lesson.Title}");
            return 0;
        }
        catch (Exception e) when (e is IOException or JsonException or ArgumentException or InvalidOperationException)
        {
            Console.Error.WriteLine("tutorials: " + e.Message);
            return 1;
        }
    }
}

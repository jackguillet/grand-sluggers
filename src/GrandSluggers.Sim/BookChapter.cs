namespace GrandSluggers.Sim;

/// <summary>
/// One captain per How to play chapter. Existing portraits, no seventh anatomy.
/// </summary>
public static class BookChapter
{
    public static readonly IReadOnlyDictionary<string, string> Captains =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["contents"] = "rio",
            ["controls"] = "vale",
            ["controls-2"] = "vale",
            ["controls-3"] = "vale",
            ["roles"] = "zig",
            ["roles-batting-2"] = "zig",
            ["roles-pitching"] = "zig",
            ["roles-pitching-2"] = "zig",
            ["roles-fielding"] = "zig",
            ["roles-fielding-2"] = "zig",
            ["roles-running"] = "zig",
            ["roles-running-2"] = "zig",
            ["getting-started-modes"] = "rio",
            ["pitch-swing"] = "rio",
            ["the-box"] = "brondo",
            ["running"] = "konga",
            ["fielding"] = "ashlord",
            ["exhibition"] = "rio",
            ["lineup"] = "vale",
            ["two-pads"] = "zig",
            ["getting-started"] = "rio",
            ["screen"] = "brondo",
            ["chemistry"] = "konga",
            ["stars"] = "ashlord",
            ["abilities"] = "vale",
            ["abilities-types"] = "vale",
            ["items"] = "zig",
            ["pause-practice"] = "rio",
        };

    public static string Captain(string pageId) =>
        Captains.TryGetValue(pageId, out var id) ? id : "rio";

    public static bool EveryPageHasARosterCaptain()
    {
        foreach (var page in HowToPlay.Pages)
        {
            var id = Captain(page.Id);
            if (!Captains.ContainsKey(page.Id)) return false;
            if (!Silhouette.Captains.Any(c => c.Equals(id, StringComparison.OrdinalIgnoreCase)))
                return false;
        }
        return true;
    }
}

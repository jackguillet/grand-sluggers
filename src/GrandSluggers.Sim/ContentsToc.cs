namespace GrandSluggers.Sim;

/// <summary>
/// How to play Contents: a real table of contents. SMS p.3 shape.
/// Chapter titles and page numbers on a card. Numbers match the book header.
/// </summary>
public static class ContentsToc
{
    public sealed record Chapter(string Id, string Title, int Number);

    /// <summary>Spread titles, not every sub-page. Same four chapters as SMS, plus play.</summary>
    public static readonly IReadOnlyList<string> ChapterIds =
    [
        "controls",
        "roles",
        "pitch-swing",
        "running",
        "fielding",
        "getting-started",
        "screen",
        "chemistry",
        "abilities",
    ];

    public static readonly IReadOnlyList<Chapter> Chapters = Build();

    public const string Picture = "contents";

    static IReadOnlyList<Chapter> Build()
    {
        var list = new List<Chapter>(ChapterIds.Count);
        foreach (var id in ChapterIds)
        {
            var i = IndexOf(id);
            var page = HowToPlay.Pages[i];
            list.Add(new Chapter(page.Id, page.Title, i + 1));
        }
        return list;
    }

    static int IndexOf(string id)
    {
        for (var i = 0; i < HowToPlay.Pages.Count; i++)
        {
            if (HowToPlay.Pages[i].Id == id)
                return i;
        }
        throw new KeyNotFoundException($"No how-to-play page '{id}'");
    }

    public static (float X, float Y, float W, float H) Still(float screenW, float screenH)
    {
        var board = ControlDiagram.Board(screenW, screenH);
        var lineBand = HowToPlay.KidLineH * 2.2f;
        return (board.X, board.Y, board.W, board.H - lineBand);
    }

    /// <summary>White card on the left of the still, SMS Contents shape.</summary>
    public static (float X, float Y, float W, float H) Card(float screenW, float screenH)
    {
        var still = Still(screenW, screenH);
        var rows = Math.Max(1, Chapters.Count);
        var h = Math.Min(still.H - 24f, 20f + rows * 38f);
        return (still.X + 16f, still.Y + 16f, still.W * 0.42f, h);
    }

    public static (float X, float Y, float W, float H) Row(int index, float screenW, float screenH)
    {
        var card = Card(screenW, screenH);
        var n = Math.Max(1, Chapters.Count);
        var top = 10f;
        var h = (card.H - top - 10f) / n;
        return (card.X + 16f, card.Y + top + index * h, card.W - 32f, h);
    }

    public static (float X, float Y, float W, float H) LineBand(float screenW, float screenH)
    {
        var board = ControlDiagram.Board(screenW, screenH);
        var h = HowToPlay.KidLineH * 2.2f;
        return (board.X, board.Y + board.H - h, board.W, h);
    }
}

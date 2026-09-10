namespace GrandSluggers.Sim;

/// <summary>
/// Shared geometry for booklet text. Source strings are paragraphs, not rendered lines:
/// the renderer supplies the active font's measurement and this rail places the measured
/// blocks without letting a following paragraph reuse their wrapped space.
/// </summary>
public static class BookletLayout
{
    public readonly record struct Box(float X, float Y, float W, float H)
    {
        public float Right => X + W;
        public float Bottom => Y + H;
    }

    public sealed record TextBlock(string Text, Box Box);

    public const float BlockGap = 4f;
    public const float BlockPadY = 6f;
    public const float HeaderGap = 12f;
    public const float PageNumberPadX = 14f;
    public const float TocNumberPadX = 12f;
    public const float TocColumnGap = 12f;
    public const float BadgePadX = 8f;
    public const float BadgeGap = 6f;

    public static IReadOnlyList<TextBlock> Flow(
        IReadOnlyList<string> paragraphs,
        Box band,
        Func<string, float, float> measureHeight,
        float minimumBlockHeight = HowToPlay.KidLineH,
        float verticalPad = BlockPadY)
    {
        if (paragraphs == null) throw new ArgumentNullException(nameof(paragraphs));
        if (measureHeight == null) throw new ArgumentNullException(nameof(measureHeight));

        var blocks = new List<TextBlock>(paragraphs.Count);
        var y = band.Y;
        foreach (var paragraph in paragraphs)
        {
            var measured = Math.Max(0f, measureHeight(paragraph, band.W));
            var height = Math.Max(minimumBlockHeight, measured + verticalPad);
            blocks.Add(new TextBlock(paragraph, new Box(band.X, y, band.W, height)));
            y += height + BlockGap;
        }
        return blocks;
    }

    /// <summary>
    /// Fits ordered paragraphs into one or two reading columns. A column break may only
    /// occur between paragraphs, so a paragraph always owns one uninterrupted rectangle.
    /// </summary>
    public static IReadOnlyList<TextBlock> BestFlow(
        IReadOnlyList<string> paragraphs,
        Box band,
        Func<string, float, float> measureHeight,
        int maxColumns = 2,
        float minimumBlockHeight = HowToPlay.KidLineH)
    {
        var single = Flow(paragraphs, band, measureHeight, minimumBlockHeight);
        if (Fits(single, band) || maxColumns < 2 || paragraphs.Count < 2)
            return single;

        const float columnGap = 18f;
        var columnW = (band.W - columnGap) * 0.5f;
        IReadOnlyList<TextBlock>? best = null;
        var bestBottom = float.MaxValue;
        for (var split = 1; split < paragraphs.Count; split++)
        {
            var leftBand = new Box(band.X, band.Y, columnW, band.H);
            var rightBand = new Box(band.X + columnW + columnGap, band.Y, columnW, band.H);
            var left = Flow(paragraphs.Take(split).ToArray(), leftBand, measureHeight, minimumBlockHeight);
            var right = Flow(paragraphs.Skip(split).ToArray(), rightBand, measureHeight, minimumBlockHeight);
            var candidate = left.Concat(right).ToArray();
            var used = Math.Max(left[^1].Box.Bottom, right[^1].Box.Bottom);
            if (used < bestBottom)
            {
                best = candidate;
                bestBottom = used;
            }
            if (Fits(candidate, band))
                return candidate;
        }
        return best ?? single;
    }

    public static bool Fits(IReadOnlyList<TextBlock> blocks, Box band) =>
        blocks.All(block => block.Box.X >= band.X - 0.01f && block.Box.Right <= band.Right + 0.01f &&
            block.Box.Y >= band.Y - 0.01f && block.Box.Bottom <= band.Bottom + 0.01f);

    public static bool HasNoOverlap(IReadOnlyList<TextBlock> blocks)
    {
        for (var i = 0; i < blocks.Count; i++)
        for (var j = i + 1; j < blocks.Count; j++)
            if (Intersects(blocks[i].Box, blocks[j].Box)) return false;
        return true;
    }

    static bool Intersects(Box a, Box b) =>
        a.X < b.Right && a.Right > b.X && a.Y < b.Bottom && a.Bottom > b.Y;

    public static (Box Title, Box Page) Header(
        float screenW,
        float screenH,
        float measuredPageWidth)
    {
        var book = HowToPlay.BookPanel(screenW, screenH);
        var pageW = Math.Max(96f, measuredPageWidth + PageNumberPadX * 2f);
        var page = new Box(book.X + book.W - 16f - pageW, book.Y + 58f, pageW, 44f);
        var titleX = book.X + 88f;
        var title = new Box(titleX, book.Y + 58f, Math.Max(0f, page.X - HeaderGap - titleX), 44f);
        return (title, page);
    }

    /// <summary>Measured pills between the booklet label and tabs, wrapping onto row two.</summary>
    public static IReadOnlyList<Box> Badges(
        float screenW,
        float screenH,
        float howToWidth,
        IReadOnlyList<float> measuredLabelWidths)
    {
        var book = HowToPlay.BookPanel(screenW, screenH);
        var tabs = BookScheme.ToggleBar(screenW, screenH);
        var firstX = book.X + 88f + howToWidth + 16f;
        var wrapX = book.X + 88f;
        var right = tabs.X - 12f;
        var x = firstX;
        var y = book.Y + 10f;
        var boxes = new List<Box>(measuredLabelWidths.Count);
        foreach (var measured in measuredLabelWidths)
        {
            var width = Math.Max(88f, measured + BadgePadX * 2f);
            if (x + width > right && boxes.Count > 0)
            {
                x = wrapX;
                y = book.Y + 34f;
            }
            boxes.Add(new Box(x, y, width, 22f));
            x += width + BadgeGap;
        }
        return boxes;
    }

    public static (Box Title, Box Number) TocColumns(
        (float X, float Y, float W, float H) row,
        float widestMeasuredNumber)
    {
        var numberW = Math.Max(64f, widestMeasuredNumber + TocNumberPadX * 2f);
        numberW = Math.Min(numberW, row.W * 0.25f);
        var number = new Box(row.X + row.W - numberW, row.Y, numberW, row.H);
        var title = new Box(row.X, row.Y, Math.Max(0f, number.X - TocColumnGap - row.X), row.H);
        return (title, number);
    }

    /// <summary>A measured label rail followed by a wrapping copy rail.</summary>
    public static (Box Label, Box Body) LabeledRow(
        (float X, float Y, float W, float H) row,
        float measuredLabelWidth,
        float minimumLabelWidth = 168f)
    {
        const float gap = 12f;
        var labelW = Math.Min(row.W * 0.36f, Math.Max(minimumLabelWidth, measuredLabelWidth + 20f));
        var label = new Box(row.X, row.Y + 2f, labelW, row.H - 4f);
        var body = new Box(label.Right + gap, row.Y + 4f,
            Math.Max(0f, row.X + row.W - label.Right - gap - 8f), row.H - 8f);
        return (label, body);
    }

    /// <summary>Atomic step number, measured heading, then the wrapping instruction.</summary>
    public static (Box Number, Box Title, Box Body) NumberedRow(
        (float X, float Y, float W, float H) row,
        float measuredTitleWidth)
    {
        var number = new Box(row.X + 8f, row.Y + 8f, 36f, row.H - 16f);
        var titleX = row.X + 52f;
        var titleW = Math.Min(row.W * 0.32f, Math.Max(180f, measuredTitleWidth + 12f));
        var title = new Box(titleX, row.Y + 8f, titleW, row.H - 16f);
        var bodyX = title.Right + 12f;
        var body = new Box(bodyX, row.Y + 8f, Math.Max(0f, row.X + row.W - bodyX - 12f), row.H - 16f);
        return (number, title, body);
    }
}

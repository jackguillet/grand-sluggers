using GrandSluggers.Sim;
using Xunit;

namespace GrandSluggers.Sim.Tests;

public class BookletLayoutTests
{
    static readonly (float W, float H)[] SupportedWindows =
    [
        (1280f, 800f),
        (1024f, 768f),
    ];

    [Fact]
    public void WrappedParagraphsOwnTheirMeasuredHeight()
    {
        var band = new BookletLayout.Box(20f, 30f, 500f, 260f);
        var paragraphs = new[] { "short", "this paragraph wraps", "last" };
        var blocks = BookletLayout.Flow(paragraphs, band,
            (text, _) => text.Contains("wraps", StringComparison.Ordinal) ? 88f : 38f);

        Assert.Equal(paragraphs, blocks.Select(b => b.Text));
        Assert.Equal(94f, blocks[1].Box.H);
        Assert.Equal(blocks[0].Box.Bottom + BookletLayout.BlockGap, blocks[1].Box.Y);
        Assert.Equal(blocks[1].Box.Bottom + BookletLayout.BlockGap, blocks[2].Box.Y);
        Assert.True(BookletLayout.HasNoOverlap(blocks));
        Assert.True(BookletLayout.Fits(blocks, band));
    }

    [Fact]
    public void EveryPageSharesAnAtomicHeaderAndFittingSchemeTabs()
    {
        foreach (var (w, h) in SupportedWindows)
        {
            var book = HowToPlay.BookPanel(w, h);
            var bar = BookScheme.ToggleBar(w, h);
            Assert.True(bar.X >= book.X && bar.X + bar.W <= book.X + book.W);
            Assert.True(bar.Y >= book.Y && bar.Y + bar.H <= book.Y + 84f);

            foreach (var scheme in new[] { InputScheme.Pad, InputScheme.Keys })
            {
                var tab = BookScheme.Tab(scheme, w, h);
                var label = BookScheme.Label(scheme);
                Assert.True(tab.W - 16f >= ApproxWidth(label, HowToPlay.BookTabPt), $"{w}×{h} {label} tab wraps");
                Assert.True(tab.H - 8f >= ApproxLineHeight(HowToPlay.BookTabPt), $"{w}×{h} {label} tab clips");
            }

            foreach (var pageIndex in Enumerable.Range(0, HowToPlay.Pages.Count))
            {
                var marker = $"{pageIndex + 1} / {HowToPlay.Pages.Count}";
                var header = BookletLayout.Header(w, h, ApproxWidth(marker, HowToPlay.BookHeaderPt));
                var title = HowToPlay.Pages[pageIndex].Title.ToUpperInvariant();
                Assert.True(header.Title.W >= ApproxWidth(title, HowToPlay.BookHeaderPt),
                    $"{w}×{h} page {pageIndex + 1} title wraps: {title}");
                Assert.True(header.Title.Right + BookletLayout.HeaderGap <= header.Page.X);
                Assert.True(header.Page.W >= ApproxWidth(marker, HowToPlay.BookHeaderPt) + BookletLayout.PageNumberPadX * 2f,
                    $"{w}×{h} page marker {marker} wraps");
                Assert.True(header.Page.Right <= book.X + book.W - 16f + 0.01f);
            }
        }
    }

    [Fact]
    public void EveryRenderedPageBodyFitsAtCouchSizeInBothSchemes()
    {
        var pagesWithoutParagraphBand = new HashSet<string> { "controls", "roles", "running" };
        foreach (var (w, h) in SupportedWindows)
        foreach (var page in HowToPlay.Pages.Where(p => !pagesWithoutParagraphBand.Contains(p.Id)))
        foreach (var scheme in new[] { InputScheme.Pad, InputScheme.Keys })
        {
            var band = CopyBand(page.Id, w, h);
            var blocks = BookletLayout.BestFlow(page.Shown(scheme), band,
                (text, width) => ApproxBodyHeight(text, width, HowToPlay.BookLineMinPt));
            Assert.True(BookletLayout.HasNoOverlap(blocks), $"{w}×{h} {page.Id} {scheme} overlaps");
            Assert.True(BookletLayout.Fits(blocks, band),
                $"{w}×{h} {page.Id} {scheme} does not fit at {HowToPlay.BookLineMinPt}pt; " +
                $"band={band.W:0}×{band.H:0}; blocks={string.Join(",", blocks.Select(b => $"{b.Box.X:0}:{b.Box.H:0}:{b.Box.Bottom - band.Y:0}"))}");
        }
    }

    [Fact]
    public void ContentsNumbersAndMeasuredIntroStayInsideTheirBands()
    {
        var contents = HowToPlay.Must("contents");
        foreach (var (w, h) in SupportedWindows)
        {
            var widest = ContentsToc.Chapters.Max(c => ApproxWidth(c.Number.ToString(), 44f));
            foreach (var index in Enumerable.Range(0, ContentsToc.Chapters.Count))
            {
                var row = ContentsToc.Row(index, w, h);
                var columns = BookletLayout.TocColumns(row, widest);
                Assert.True(columns.Title.W > 0f);
                Assert.True(columns.Title.Right + BookletLayout.TocColumnGap <= columns.Number.X);
                Assert.True(columns.Number.W >= widest + BookletLayout.TocNumberPadX * 2f,
                    $"{w}×{h} contents page number wraps");
                Assert.True(columns.Number.Right <= row.X + row.W + 0.01f);
            }

            var source = ContentsToc.LineBand(w, h);
            var band = new BookletLayout.Box(source.X, source.Y, source.W, source.H);
            foreach (var scheme in new[] { InputScheme.Pad, InputScheme.Keys })
            {
                var blocks = BookletLayout.Flow(contents.Shown(scheme), band, ApproxBodyHeight);
                Assert.True(BookletLayout.HasNoOverlap(blocks), $"{w}×{h} {scheme} paragraphs overlap");
                Assert.True(BookletLayout.Fits(blocks, band), $"{w}×{h} {scheme} intro reaches the footer");
            }
        }
    }

    static BookletLayout.Box CopyBand(string id, float w, float h)
    {
        var source = id switch
        {
            "contents" => ContentsToc.LineBand(w, h),
            "getting-started" => GettingStarted.LineBand(w, h),
            "pitch-swing" => HowToComic.LineBand(w, h),
            "screen" => HudCallouts.LineBand(w, h),
            "chemistry" or "abilities" => ChemBook.LineBand(w, h),
            _ => HowToPlay.TextRect(w, h),
        };
        return new BookletLayout.Box(source.X, source.Y, source.W, source.H);
    }

    static float ApproxBodyHeight(string text, float width) =>
        ApproxBodyHeight(text, width, HowToPlay.BookLinePt);

    static float ApproxBodyHeight(string text, float width, float pointSize)
    {
        var lines = Math.Max(1, (int)Math.Ceiling(ApproxWidth(text, pointSize) / width));
        return lines * ApproxLineHeight(pointSize);
    }

    // Deliberately conservative portable stand-in. Unity supplies GUIStyle.CalcSize/CalcHeight
    // at runtime; these tests exercise the geometry contract without taking a Unity dependency.
    static float ApproxWidth(string text, float pointSize) => text.Length * pointSize * 0.55f;
    static float ApproxLineHeight(float pointSize) => pointSize * 1.2f;
}

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

            var howWidth = ApproxWidth("HOW TO PLAY", 20f);
            var badgeLabels = new[] { "Player 1 only", "Two controllers" };
            var badges = BookletLayout.Badges(w, h, howWidth,
                badgeLabels.Select(label => ApproxWidth(label, HowToPlay.BookBadgePt)).ToArray());
            var howRight = book.X + 88f + howWidth;
            Assert.True(badges[0].X > howRight, $"{w}×{h} first badge covers HOW TO PLAY");
            Assert.True(badges.All(badge => badge.Right <= bar.X - 12f));
            Assert.True(BookletLayout.HasNoOverlap(badgeLabels.Select((label, i) =>
                new BookletLayout.TextBlock(label, badges[i])).ToArray()));
            Assert.True(ApproxLineHeight(HowToPlay.BookBadgePt) <= BookletLayout.BadgeH,
                $"{w}×{h} badge text clips vertically");

            var footer = BookScheme.Footer(InputScheme.Keys);
            Assert.True(ApproxWidth(footer, HowToPlay.BookFooterPt) <= book.W - 56f,
                $"{w}×{h} keyboard footer wraps");

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
        static bool DrawsParagraphBand(string id) =>
            !id.StartsWith("controls", StringComparison.Ordinal) &&
            !id.StartsWith("roles", StringComparison.Ordinal) &&
            id is not "running" and not "getting-started" and not "abilities-types";
        foreach (var (w, h) in SupportedWindows)
        foreach (var page in HowToPlay.Pages.Where(p => DrawsParagraphBand(p.Id)))
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
    public void DiagramAndTableCellsFitTheirMeasuredCopy()
    {
        foreach (var (w, h) in SupportedWindows)
        foreach (var scheme in new[] { InputScheme.Pad, InputScheme.Keys })
        {
            foreach (var pageId in ControlDiagram.PageIds)
            {
                var calls = ControlDiagram.PageCallouts(scheme, pageId);
                var board = ControlDiagram.Board(w, h);
                var stackBand = new BookletLayout.Box(board.X, board.Y + 40f, board.W, board.H - 40f);
                var point = (float)HowToPlay.BookLinePt;
                var heights = calls.Select(call => 44f + HardwareActionsHeight(call, board.W - 24f, point)).ToArray();
                var cells = BookletLayout.MeasuredStack(stackBand, heights);
                if (!BookletLayout.Fits(cells, stackBand))
                {
                    point = HowToPlay.BookLineMinPt;
                    heights = calls.Select(call => 44f + HardwareActionsHeight(call, board.W - 24f, point)).ToArray();
                    cells = BookletLayout.MeasuredStack(stackBand, heights);
                }
                Assert.True(BookletLayout.Fits(cells, stackBand), $"{w}×{h} {pageId} hardware stack");
                foreach (var (call, index) in calls.Select((call, index) => (call, index)))
                {
                    var cell = cells[index];
                    var actions = new[] { call.Always, call.Offense, call.Defense }.Where(s => s.Length > 0).ToArray();
                    var band = new BookletLayout.Box(cell.X + 12, cell.Y + 36, cell.W - 24, cell.H - 44);
                    var blocks = BookletLayout.Flow(actions, band,
                        (text, width) => ApproxBodyHeight(text, width, point), 0f, 0f);
                    Assert.True(BookletLayout.Fits(blocks, band), $"{w}×{h} {pageId} {call.Id}");
                }
            }

            foreach (var pageId in RoleTables.PageIds)
            {
                var block = RoleTables.OnPage(scheme, pageId);
                for (var i = 0; i < block.Rows.Count; i++)
                {
                    var cell = RoleTables.RowCard(i, block.Rows.Count, w, h);
                    var line = block.Rows[i].Verb.ToUpperInvariant() + "  ·  " + block.Rows[i].Press;
                    Assert.True(ApproxBodyHeight(line, cell.W - 24, HowToPlay.BookLineMinPt) <= cell.H - 12,
                        $"{w}×{h} {pageId} {block.Rows[i].Verb}");
                }
            }

            foreach (var step in GettingStarted.Path.Select((step, index) => (step, index)))
            {
                var cell = GettingStarted.StepCell(step.index, w, h);
                var title = step.step.Title.ToUpperInvariant();
                var columns = BookletLayout.NumberedRow(cell, ApproxWidth(title, HowToPlay.BookHeaderPt));
                Assert.True(ApproxWidth(title, HowToPlay.BookHeaderPt) <= columns.Title.W,
                    $"{w}×{h} getting started title {step.step.Id}");
                Assert.True(ApproxBodyHeight(GettingStarted.Caption(step.step, scheme), columns.Body.W,
                    HowToPlay.BookLineMinPt) <= columns.Body.H, $"{w}×{h} getting started {step.step.Id}");
            }

            foreach (var row in ChemBook.Types.Select((row, index) => (row, index)))
            {
                var cell = ChemBook.TypeCell(row.index, w, h);
                var text = row.row.Title.ToUpperInvariant() + "  ·  " + row.row.Line;
                Assert.True(ApproxBodyHeight(text, cell.W - 24, HowToPlay.BookLineMinPt) <= cell.H - 12,
                    $"{w}×{h} ability {row.row.Id}");
            }
        }
    }

    [Fact]
    public void EverySpecialRendererReservesItsMeasuredTextAtSupportedWindows()
    {
        foreach (var (w, h) in SupportedWindows)
        foreach (var scheme in new[] { InputScheme.Pad, InputScheme.Keys })
        {
            foreach (var (diagram, index) in BagDiagrams.Running.Select((item, index) => (item, index)))
            {
                var card = BagDiagrams.Card(index, w, h);
                Assert.True(ApproxWidth(diagram.Title.ToUpperInvariant(), HowToPlay.BookHeaderPt) <= card.W - 20,
                    $"{w}×{h} running heading {diagram.Title}");
                var press = BagDiagrams.Press(diagram, scheme);
                var pressH = Math.Max(36f,
                    ApproxBodyHeight(press, card.W - 32, HowToPlay.BookTabPt) + 8f);
                var caption = diagram.Kind == BagDiagrams.Kind.BagMap
                    ? "PICK A RUNNER · ARM A THROW"
                    : diagram.Kind == BagDiagrams.Kind.Advance ? "SEND EVERY RUNNER" : "SEND EVERY RUNNER BACK";
                var captionH = ApproxBodyHeight(caption, card.W - 20, 26f);
                Assert.True(pressH + captionH + 62f <= card.H,
                    $"{w}×{h} running {diagram.Kind} reserves press, diagram, and caption");
            }

            foreach (var (callout, index) in BagDiagrams.Callouts.Select((item, index) => (item, index)))
            {
                var card = BagDiagrams.CalloutCard(index, w, h);
                Assert.True(ApproxWidth(callout.Title.ToUpperInvariant(), HowToPlay.BookHeaderPt) <= card.W - 20,
                    $"{w}×{h} running callout heading {callout.Title}");
                Assert.True(ApproxWidth(BagDiagrams.CalloutPress(callout, scheme), 20f) <= card.W - 28,
                    $"{w}×{h} running callout press {callout.Title}");
                Assert.True(ApproxBodyHeight(callout.Line, card.W - 20, HowToPlay.BookLineMinPt) <= card.H - 88,
                    $"{w}×{h} running callout body {callout.Title}");
            }

            foreach (var (mode, index) in GettingStarted.Modes.Select((item, index) => (item, index)))
            {
                var row = GettingStarted.ModeRow(index, w, h);
                var columns = BookletLayout.LabeledRow(row, ApproxWidth(mode.Title, 26f));
                Assert.True(ApproxWidth(mode.Title, 26f) <= columns.Label.W - 12f,
                    $"{w}×{h} mode heading {mode.Title}");
                Assert.True(ApproxBodyHeight(GettingStarted.Line(mode, scheme), columns.Body.W,
                    HowToPlay.BookLineMinPt) <= columns.Body.H, $"{w}×{h} mode body {mode.Id}");
            }

            foreach (var (spread, spreadIndex) in HudCallouts.OnScreenPage.Select((item, index) => (item, index)))
            {
                var row = HudCallouts.Row(spreadIndex, w, h);
                Assert.True(ApproxWidth(spread.Title.ToUpperInvariant(), HowToPlay.BookHeaderPt) <= row.W - 24,
                    $"{w}×{h} HUD heading {spread.Title}");
                foreach (var (mark, markIndex) in spread.Marks.Select((item, index) => (item, index)))
                {
                    var cell = HudCallouts.MarkCell(spreadIndex, markIndex, w, h);
                    Assert.True(ApproxBodyHeight("•  " + mark.Label, cell.W, 26f) <= cell.H,
                        $"{w}×{h} HUD mark {spread.Id}/{mark.Id}");
                }
            }

            foreach (var (strip, index) in HowToComic.OnPitchSwingPage.Select((item, index) => (item, index)))
            {
                var row = HowToComic.Row(index, w, h);
                var chipW = (row.W - 64f) * 0.5f;
                var motion = HowToComic.MotionOf(strip, scheme);
                var chipH = Math.Max(56f, Math.Max(
                    ApproxBodyHeight(motion.Charge, chipW - 16, HowToPlay.BookTabPt),
                    ApproxBodyHeight(motion.Commit, chipW - 16, HowToPlay.BookTabPt)) + 10f);
                Assert.True(ApproxBodyHeight(motion.Charge, chipW - 16, HowToPlay.BookTabPt) <= chipH - 10f,
                    $"{w}×{h} {strip.Id} charge chip");
                Assert.True(ApproxBodyHeight(motion.Commit, chipW - 16, HowToPlay.BookTabPt) <= chipH - 10f,
                    $"{w}×{h} {strip.Id} commit chip");
                Assert.True(ApproxBodyHeight(HowToComic.Caption(strip, scheme), row.W - 32, 26f) <= row.H - chipH - 58f,
                    $"{w}×{h} {strip.Id} caption");
            }

            foreach (var (pair, index) in ChemBook.ChemistryPairs.Select((item, index) => (item, index)))
            {
                var cell = ChemBook.ChemCell(index, w, h);
                Assert.True(ApproxWidth(pair.Title.ToUpperInvariant(), HowToPlay.BookHeaderPt) <= cell.W - 56,
                    $"{w}×{h} chemistry heading {pair.Id}");
                Assert.True(ApproxBodyHeight(pair.Caption, cell.W - 32, HowToPlay.BookLinePt) <= cell.H - 76,
                    $"{w}×{h} chemistry caption {pair.Id}");
            }
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
                Assert.True(row.H >= ApproxLineHeight(44f), $"{w}×{h} contents number clips vertically");
                Assert.True(row.H >= ApproxLineHeight(HowToPlay.BookLinePt),
                    $"{w}×{h} contents title clips vertically");
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
        var lines = text.Split('\n').Sum(line =>
            Math.Max(1, (int)Math.Ceiling(ApproxWidth(line, pointSize) / width)));
        return lines * ApproxLineHeight(pointSize);
    }

    static float HardwareActionsHeight(ControlDiagram.Callout callout, float width, float pointSize)
    {
        var actions = new[] { callout.Always, callout.Offense, callout.Defense }
            .Where(text => text.Length > 0).ToArray();
        return actions.Sum(text => ApproxBodyHeight(text, width, pointSize)) +
            Math.Max(0, actions.Length - 1) * BookletLayout.BlockGap;
    }

    // Deliberately conservative portable stand-in. Unity supplies GUIStyle.CalcSize/CalcHeight
    // at runtime; these tests exercise the geometry contract without taking a Unity dependency.
    static float ApproxWidth(string text, float pointSize) => text.Length * pointSize * 0.55f;
    static float ApproxLineHeight(float pointSize) => pointSize * 1.2f;
}

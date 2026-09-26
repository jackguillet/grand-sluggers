using System.Globalization;
using Xunit;
using GrandSluggers.Sim;
using GrandSluggers.Sim.Front;

namespace GrandSluggers.Sim.Tests;

public class LineupScreensTests
{
    readonly ContentCatalog _content = Shipped.Content;

    [Fact]
    public void TeamSetupHasNineHomeAndNineAwaySlots()
    {
        var s = LineupScreens.Open(_content, "vale", "brondo");
        Assert.Equal(LineupStep.TeamSetup, s.Step);
        Assert.Equal(9, s.HomeSlots.Count);
        Assert.Equal(9, s.AwaySlots.Count);
        Assert.Equal("vale", s.HomeSlots[0]!.Id);
        Assert.Equal("vale", s.HomeCaptain.Id);
        for (var i = 1; i < 9; i++)
            Assert.Null(s.HomeSlots[i]);
        Assert.True(s.AwayFull);
        Assert.Equal("brondo", s.AwayCaptain.Id);
        Assert.Contains(s.AwaySlots, c => c != null && c.Id.Equals("brondo", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(9, s.AwaySlots.Count(c => c != null));
        Assert.Equal(LineupSeat.Pad1, s.HomeSeat);
        Assert.Equal(LineupSeat.Cpu, s.AwaySeat);
        Assert.Null(s.InspectedBy(LineupSeat.Pad2));
        Assert.Null(s.InspectedBy(LineupSeat.Cpu));
        // The grid holds everyone; a rostered player keeps its cell and is taken.
        Assert.Equal(_content.Characters.Count, s.Pool.Count);
        Assert.True(s.OnHome(_content.Must("vale")));
        Assert.All(s.AwaySlots, a => Assert.True(s.OnAway(a)));
        Assert.Equal(s.Pool.Count(c => !s.Taken(c)), _content.Characters.Count - 10);
    }

    /// <summary>
    /// WD-28: the inspection card names the player's crews and says why they get on (or do not) with their side's captain,
    /// in the words the book's chemistry page uses; the captain's own card says so.
    /// </summary>
    [Fact]
    public void TheCardNamesCrewsAndTheChemistryReasonWithTheCaptain()
    {
        var s = LineupScreens.Open(_content, "rio", "ashlord", LineupSeat.Pad1, LineupSeat.Pad2);
        var rio = _content.Must("rio");
        var pip = _content.Must("pip");
        Assert.Equal("CREWS  Showboats · Cool Kids", s.CrewLine(rio));
        Assert.Equal("CAPTAIN  this is the captain", s.ChemLine(rio, home: true));
        Assert.Equal("CAPTAIN  Good: shared crew Cool Kids", s.ChemLine(pip, home: true));
        Assert.Equal("CAPTAIN  Poor: story rivals", s.ChemLine(rio, home: false));
        // Every reason the card can print is a word the book's chemistry page prints.
        var book = string.Join(" ", HowToPlay.Must("chemistry").Lines);
        foreach (var a in _content.Characters.Values)
        foreach (var captain in _content.CaptainIds.Select(_content.Must))
        {
            var reason = _content.Chemistry.Reason(captain, a);
            if (reason.Why != ChemistryWhy.None) Assert.Contains(CarnivalFront.ChemistryWhyWord(reason), book);
        }
    }

    /// <summary>
    /// The card's four text lines (verbs, field verb, crews, chemistry) sit under the bars and the portrait and end inside the
    /// card, and the longest crew and chemistry lines any captain and player can make fit its width at the card's fonts.
    /// </summary>
    [Fact]
    public void TheCardsCrewAndChemistryLinesFitTheCard()
    {
        var card = LineupLayout.CardPanel(true);
        var (w, h) = ((float)(card.W * 1280), (float)(card.H * 800));
        var top = CarnivalFront.LineupCardVerbsTop;
        Assert.True(CarnivalFront.LineupCardBars.Bottom <= top, "the bars run into the text lines");
        Assert.True(78 + CarnivalFront.LineupCardPortrait <= top, "the portrait runs into the text lines");
        Assert.True(top + CarnivalFront.LineupCardLines * CarnivalFront.LineupCardLinePitch <= h - 4, "the text lines leave the card");
        var s = LineupScreens.Open(_content, "rio", "ashlord", LineupSeat.Pad1, LineupSeat.Pad2);
        var width = w - 28;
        foreach (var who in _content.Characters.Values)
        {
            // Both lines are 13 px; ~0.55 em a glyph as the book's layout test measures.
            Assert.True(s.CrewLine(who).Length * 13 * .55f <= width, who.Id + ": " + s.CrewLine(who));
            foreach (var captain in _content.CaptainIds.Select(_content.Must))
            {
                var line = CarnivalFront.LineupChemLine(_content, captain, who);
                Assert.True(line.Length * 13 * .55f <= width, captain.Id + " / " + who.Id + ": " + line);
            }
        }
    }

    [Fact]
    public void CaptainCannotBeDroppedWhenLockCaptain()
    {
        var locked = LineupScreens.Open(_content, "vale", "brondo", lockCaptain: true);
        locked.FocusCell(LineupSeat.Pad1, LineupFocus.HomeRow, 1);
        Assert.Equal(LineupFocus.HomeRow, locked.Focus);
        // slot 0 is the captain
        while (locked.SlotIndex != 0)
            locked.Stick(-1, 0);
        Assert.Equal("vale", locked.HomeSlots[0]!.Id);
        Assert.False(locked.Remove());
        Assert.Equal("vale", locked.HomeSlots[0]!.Id);
        Assert.False(locked.Drop());

        var open = LineupScreens.Open(_content, "vale", "brondo", lockCaptain: false);
        open.FocusCell(LineupSeat.Pad1, LineupFocus.HomeRow, 1);
        while (open.SlotIndex != 0)
            open.Stick(-1, 0);
        Assert.True(open.Remove());
        Assert.Null(open.HomeSlots[0]);
        Assert.False(open.Taken(_content.Must("vale")));
    }

    [Fact]
    public void SouthDropsAPoolHeadIntoTheHighlightedEmptySlot()
    {
        var s = LineupScreens.Open(_content, "vale", "brondo");
        Assert.Equal(LineupFocus.Pool, s.Focus);
        var pick = s.Pool[s.PoolIndex];
        var cell = s.PoolIndex;
        Assert.Equal(1, s.SlotIndex);
        Assert.True(s.South());
        Assert.Equal(pick.Id, s.HomeSlots[1]!.Id);
        // The pick keeps its grid cell, taken; South on it again adds nobody.
        Assert.Same(pick, s.Pool[cell]);
        Assert.True(s.Taken(pick));
        Assert.True(s.FocusCell(LineupSeat.Pad1, LineupFocus.Pool, cell));
        Assert.False(s.South());
        Assert.Null(s.HomeSlots[2]);
        Assert.True(s.West());
        Assert.Null(s.HomeSlots[1]);
        Assert.False(s.Taken(pick));
    }

    [Fact]
    public void DefenseSetupMapsNineGlovesOntoDiamondPositions()
    {
        var s = Filled();
        Assert.Equal(LineupStep.DefenseSetup, s.Step);
        Assert.NotNull(s.Home);
        Assert.NotNull(s.Away);
        foreach (var pos in Diamond.Order)
        {
            Assert.True(s.Home!.Gloves.ContainsKey(pos), pos);
            Assert.True(s.Away!.Gloves.ContainsKey(pos), pos);
        }
        Assert.Equal(9, s.Home!.Gloves.Count);
        Assert.Equal(9, s.Away!.Gloves.Count);
        Assert.Equal(9, s.Home.Gloves.Values.Select(c => c.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Equal(9, s.Away.Gloves.Values.Select(c => c.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Contains(Diamond.Order, p => s.Home.PosOf(s.HomeCaptain.Id) == p);
        var cell = LineupLayout.DiamondHead(true, "P");
        var away = LineupLayout.DiamondHead(false, "P");
        Assert.True(cell.CX < away.CX, $"home diamond {cell.CX} should sit left of away {away.CX}");
        var catcher = LineupLayout.DiamondHead(true, "C");
        var cf = LineupLayout.DiamondHead(true, "CF");
        Assert.True(cf.CY > catcher.CY, $"CF {cf.CY} should sit deeper than C {catcher.CY}");
        Assert.True(s.NudgeGlove(0, 1) || s.NudgeGlove(1, 0) || s.NudgeGlove(-1, 0) || s.NudgeGlove(0, -1));
        foreach (var pos in Diamond.Order)
            Assert.True(s.Home.Gloves.ContainsKey(pos), pos);
    }

    [Fact]
    public void BattingOrder1To9RoundTrips()
    {
        var s = Filled();
        Assert.Equal(9, s.Home!.Order.Count);
        var start = s.Home.Order.Select(c => c.Id).ToArray();
        var who = start[s.OrderIndex];
        for (var i = 0; i < 9; i++)
        {
            Assert.Equal(who, s.Home.Order[s.OrderIndex].Id);
            Assert.True(s.StepBatting(1));
        }
        Assert.Equal(start, s.Home.Order.Select(c => c.Id));
        Assert.Equal(who, s.Home.Order[s.OrderIndex].Id);

        for (var i = 0; i < 9; i++)
            Assert.True(s.MoveOrderCursor(1));
        Assert.Equal(0, s.OrderIndex);
    }

    [Fact]
    public void Pad2WouldOwnTheAwayRowWithoutASecondToolkit()
    {
        var cpu = LineupScreens.Open(_content, "vale", "brondo");
        Assert.True(cpu.SeatOwns(LineupSeat.Pad1, LineupFocus.HomeRow));
        Assert.False(cpu.SeatOwns(LineupSeat.Pad1, LineupFocus.AwayRow));
        Assert.False(cpu.SeatOwns(LineupSeat.Cpu, LineupFocus.AwayRow));
        Assert.False(cpu.SeatOwns(LineupSeat.Pad2, LineupFocus.AwayRow));

        var vs = LineupScreens.Open(_content, "vale", "brondo",
            homeSeat: LineupSeat.Pad1, awaySeat: LineupSeat.Pad2);
        Assert.True(vs.SeatOwns(LineupSeat.Pad2, LineupFocus.AwayRow));
        Assert.True(vs.SeatOwns(LineupSeat.Pad2, LineupFocus.AwayOrder));
        Assert.True(vs.SeatOwns(LineupSeat.Pad2, LineupFocus.AwayDiamond));
        Assert.False(vs.SeatOwns(LineupSeat.Pad1, LineupFocus.AwayRow));
        Assert.Equal("brondo", vs.AwaySlots[0]!.Id);
        for (var i = 1; i < 9; i++)
            Assert.Null(vs.AwaySlots[i]);
    }

    [Fact]
    public void Pad1AndPad2DropIntoTheirOwnRows()
    {
        var s = LineupScreens.Open(_content, "vale", "brondo",
            homeSeat: LineupSeat.Pad1, awaySeat: LineupSeat.Pad2);
        var homePick = s.Pool[s.PoolOf(LineupSeat.Pad1)];
        Assert.True(s.South(LineupSeat.Pad1));
        Assert.Equal(homePick.Id, s.HomeSlots[1]!.Id);
        Assert.Null(s.AwaySlots[1]);
        var awayPick = s.Pool[s.PoolOf(LineupSeat.Pad2)];
        Assert.NotEqual(homePick.Id, awayPick.Id);
        Assert.True(s.South(LineupSeat.Pad2));
        Assert.Equal(awayPick.Id, s.AwaySlots[1]!.Id);
        Assert.Equal(homePick.Id, s.HomeSlots[1]!.Id);
        s.Stick(LineupSeat.Pad1, 0, -1);
        Assert.NotEqual(LineupFocus.AwayRow, s.FocusOf(LineupSeat.Pad1));
        Assert.True(s.RandomFill(LineupSeat.Pad1));
        Assert.True(s.RandomFill(LineupSeat.Pad2));
        Assert.True(s.HomeFull);
        Assert.True(s.AwayFull);
        Assert.True(s.ConfirmTeam());
        Assert.Equal(LineupFocus.HomeOrder, s.FocusOf(LineupSeat.Pad1));
        Assert.Equal(LineupFocus.AwayOrder, s.FocusOf(LineupSeat.Pad2));
        var home0 = s.Home!.Order[0].Id;
        var awayStart = s.Away!.Order.Select(c => c.Id).ToArray();
        Assert.True(s.StepBatting(LineupSeat.Pad2, 1));
        Assert.Equal(home0, s.Home.Order[0].Id);
        Assert.NotEqual(awayStart, s.Away.Order.Select(c => c.Id).ToArray());
    }

    [Fact]
    public void SitUnplugFillsAwayAndPlugEmptiesForPad2()
    {
        var s = LineupScreens.Open(_content, "vale", "brondo");
        Assert.True(s.AwayFull);
        s.Sit(LineupSeat.Pad1, LineupSeat.Pad2);
        Assert.Equal(LineupSeat.Pad2, s.AwaySeat);
        Assert.Equal("brondo", s.AwaySlots[0]!.Id);
        for (var i = 1; i < 9; i++)
            Assert.Null(s.AwaySlots[i]);
        s.Sit(LineupSeat.Pad1, LineupSeat.Cpu);
        Assert.Equal(LineupSeat.Cpu, s.AwaySeat);
        Assert.True(s.AwayFull);
        Assert.Equal("brondo", s.AwayCaptain.Id);
        Assert.DoesNotContain(s.AwaySlots, c => c != null && c.Id.Equals("vale", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void DraftPortraitsStayBetweenRosterRowsAndClearBothInspectionCards()
    {
        var roster = Enumerable.Range(0, 9).SelectMany(i => new[] { LineupLayout.HomeSlot(i), LineupLayout.AwaySlot(i) }).ToArray();
        var pool = Enumerable.Range(0, LineupLayout.PoolVisibleRows)
            .SelectMany(r => Enumerable.Range(0, LineupLayout.PoolColumns).Select(c => LineupLayout.PoolCell(r, c))).ToArray();
        foreach (var tile in pool)
        {
            foreach (var slot in roster) Assert.False(Overlap(tile, slot));
            Assert.False(Overlap(tile, LineupLayout.CardPanel(true)));
            Assert.False(Overlap(tile, LineupLayout.CardPanel(false)));
            Assert.False(Overlap(tile, LineupLayout.PoolScroll));
        }
        for (var i = 0; i < pool.Length; i++)
            for (var j = i + 1; j < pool.Length; j++) Assert.False(Overlap(pool[i], pool[j]));
        Assert.False(Overlap(LineupLayout.PoolScroll, LineupLayout.CardPanel(true)));
        Assert.False(Overlap(LineupLayout.PoolScroll, LineupLayout.CardPanel(false)));
        foreach (var home in new[] { true, false })
        {
            Assert.False(Overlap(LineupLayout.CardPanel(home), LineupLayout.ContinueButton));
            foreach (var slot in roster) Assert.False(Overlap(slot, LineupLayout.CardPanel(home)));
        }
    }

    /// <summary>
    /// The pool is a crew a row: nine wide, captain first then that crew's eight sidekicks, in captain select order, and every
    /// character once. Each column sits under its roster slot at the slot's size, and the window's rows fit between the pool
    /// title (y 283) and the away caption (y 582) on the 1280×800 board.
    /// </summary>
    [Fact]
    public void ThePoolIsAParkCrewARowUnderTheRosterSlots()
    {
        var s = LineupScreens.Open(_content, "vale", "brondo");
        Assert.Equal(_content.Characters.Count, s.Pool.Select(c => c.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Equal(_content.CaptainIds.Count, s.PoolRows);
        for (var row = 0; row < _content.CaptainIds.Count; row++)
        {
            var captain = s.Pool[row * 9];
            Assert.Equal(_content.CaptainIds[row], captain.Id);
            Assert.Same(captain, s.CrewCaptainOfRow(row));
            for (var col = 1; col < 9; col++)
            {
                var who = s.Pool[row * 9 + col];
                Assert.False(who.Captain, who.Id);
                Assert.Equal(captain.Faction, who.Faction);
            }
        }
        for (var col = 0; col < 9; col++)
        {
            var cell = LineupLayout.PoolCell(0, col);
            Assert.Equal(LineupLayout.HomeSlot(col).X, cell.X, 9);
            Assert.Equal(LineupLayout.HomeSlot(col).W, cell.W, 9);
        }
        var top = LineupLayout.GuiPixel(LineupLayout.PoolCell(0, 0), 1280, 800);
        var bottom = LineupLayout.GuiPixel(LineupLayout.PoolCell(LineupLayout.PoolVisibleRows - 1, 0), 1280, 800);
        Assert.True(top.Y >= 283, "the grid runs into the pool title");
        Assert.True(bottom.Y + bottom.H <= 582, "the grid runs into the away caption");
        Assert.True(top.H >= 80, "a pool face must read at couch distance");
    }

    /// <summary>A seat starts on its own captain's crew row, on a sidekick nobody has taken.</summary>
    [Theory]
    [InlineData("vale", "brondo")]
    [InlineData("rio", "ashlord")]
    [InlineData("fenn", "reed")]
    public void EachSeatStartsOnItsCaptainsCrew(string home, string away)
    {
        var s = LineupScreens.Open(_content, home, away, LineupSeat.Pad1, LineupSeat.Pad2);
        foreach (var (seat, captain) in new[] { (LineupSeat.Pad1, home), (LineupSeat.Pad2, away) })
        {
            var who = s.Pool[s.PoolOf(seat)];
            Assert.Equal(captain, s.CrewCaptainOfRow(s.PoolRowOf(seat))!.Id);
            Assert.False(who.Captain);
            Assert.False(s.Taken(who));
        }
        Assert.True(s.PoolRowShown(s.PoolRowOf(LineupSeat.Pad1)));
    }

    /// <summary>
    /// The window scrolls the least that keeps the moving seat's cursor on screen: the stick walks every crew row, the top row
    /// hands off to the home row and the bottom row to the away row (when the seat owns it). In 1v1 the window follows whoever
    /// moved last, and the other seat's cursor is reported off screen, never lost.
    /// </summary>
    [Fact]
    public void TheWindowFollowsTheSeatThatMoved()
    {
        var s = LineupScreens.Open(_content, "rio", "ashlord", LineupSeat.Pad1, LineupSeat.Pad2);
        var shown = LineupLayout.PoolVisibleRows;
        while (s.PoolRowOf(LineupSeat.Pad1) > 0) Assert.True(s.Stick(LineupSeat.Pad1, 0, 1));
        Assert.Equal(0, s.PoolTop);
        Assert.True(s.Stick(LineupSeat.Pad1, 0, 1));
        Assert.Equal(LineupFocus.HomeRow, s.FocusOf(LineupSeat.Pad1));
        Assert.True(s.Stick(LineupSeat.Pad1, 0, -1));
        Assert.Equal(LineupFocus.Pool, s.FocusOf(LineupSeat.Pad1));
        for (var row = 1; row < s.PoolRows; row++)
        {
            Assert.True(s.Stick(LineupSeat.Pad1, 0, -1));
            Assert.Equal(row, s.PoolRowOf(LineupSeat.Pad1));
            Assert.True(s.PoolRowShown(row));
            Assert.Equal(Math.Max(0, row - shown + 1), s.PoolTop);
        }
        Assert.Equal(s.PoolRows - shown, s.PoolTop);
        // Pad 1 is home: the bottom row does not hand it the away row.
        Assert.False(s.Stick(LineupSeat.Pad1, 0, -1));
        Assert.Equal(LineupFocus.Pool, s.FocusOf(LineupSeat.Pad1));
        // Pad 2 sits on its crew near the top, scrolled away; it moves and the window comes to it.
        var pad2Row = s.PoolRowOf(LineupSeat.Pad2);
        Assert.True(pad2Row < s.PoolTop);
        Assert.False(s.PoolRowShown(pad2Row));
        Assert.True(s.Stick(LineupSeat.Pad2, 1, 0));
        Assert.True(s.PoolRowShown(s.PoolRowOf(LineupSeat.Pad2)));
        Assert.False(s.PoolRowShown(s.PoolRowOf(LineupSeat.Pad1)));
        // Pad 2 owns the away row: the bottom crew hands it there.
        while (s.PoolRowOf(LineupSeat.Pad2) < s.PoolRows - 1) Assert.True(s.Stick(LineupSeat.Pad2, 0, -1));
        Assert.True(s.Stick(LineupSeat.Pad2, 0, -1));
        Assert.Equal(LineupFocus.AwayRow, s.FocusOf(LineupSeat.Pad2));
    }

    /// <summary>The grid's words fit (~0.55 em a glyph, as the card and book layout tests measure): every crew tag in its cell's strip, and the pool title and cursor notes on their line.</summary>
    [Fact]
    public void ThePoolWordsFitTheirStrips()
    {
        var s = LineupScreens.Open(_content, "rio", "ashlord");
        var cell = LineupLayout.PoolCell(0, 0).W * 1280;
        foreach (var id in _content.CaptainIds)
        {
            var tag = s.CrewTag(_content.Must(id));
            Assert.False(string.IsNullOrWhiteSpace(tag), id);
            Assert.True(tag.Length * 12 * .55 <= cell - 4, id + ": " + tag);
        }
        foreach (var home in new[] { true, false })
            Assert.True(CarnivalFront.LineupTakenMark(home).Length * 12 * .55 <= cell - 4);
        var title = CarnivalFront.LineupPoolTitle(s.PoolRows - LineupLayout.PoolVisibleRows, LineupLayout.PoolVisibleRows, s.PoolRows);
        Assert.True(title.Length * 13 * .55 <= 700 - 24, title);
        var notes = CarnivalFront.LineupCursorOffscreen(true, true) + "   " + CarnivalFront.LineupCursorOffscreen(false, false);
        Assert.True(notes.Length * 13 * .55 <= 200, notes);
    }

    [Fact]
    public void LayoutSeparatesBothBattingBarsDiamondsAndCards()
    {
        Assert.True(LineupLayout.HomeSlot(0).Y > LineupLayout.PoolCell(0, 0).Y);
        Assert.True(LineupLayout.PoolCell(LineupLayout.PoolVisibleRows - 1, 0).Y > LineupLayout.AwaySlot(0).Y);
        Assert.True(LineupLayout.HomeDiamondPanel.CX < LineupLayout.AwayDiamondPanel.CX);
        Assert.Equal(LineupLayout.HomeOrder(0).Y, LineupLayout.HomeOrder(8).Y);
        Assert.True(LineupLayout.HomeOrder(0).X < LineupLayout.HomeOrder(8).X);
        Assert.Equal(LineupLayout.AwayOrder(0).Y, LineupLayout.AwayOrder(8).Y);
        Assert.False(Overlap(LineupLayout.CardPanel(true), LineupLayout.CardPanel(false)));
        foreach (var home in new[] { true, false })
        {
            var cells = Diamond.Order.Select(pos => LineupLayout.DiamondHead(home, pos)).ToArray();
            foreach (var cell in cells)
            {
                Assert.False(Overlap(cell, LineupLayout.CardPanel(true)));
                Assert.False(Overlap(cell, LineupLayout.CardPanel(false)));
                for (var i = 0; i < 9; i++)
                {
                    Assert.False(Overlap(cell, LineupLayout.HomeOrder(i)));
                    Assert.False(Overlap(cell, LineupLayout.AwayOrder(i)));
                }
            }
            for (var i = 0; i < cells.Length; i++)
                for (var j = i + 1; j < cells.Length; j++) Assert.False(Overlap(cells[i], cells[j]), $"{Diamond.Order[i]} overlaps {Diamond.Order[j]}");
            foreach (var pos in Diamond.Order.Where(p => p != "C"))
            {
                var spot = LineupLayout.FieldSpot(pos);
                Assert.True(LineupLayout.InFairField(spot.X, spot.Y), pos + " must sit inside foul lines and fence");
            }
            var catcher = LineupLayout.DiamondHead(home, "C");
            var pitcher = LineupLayout.DiamondHead(home, "P");
            Assert.True(catcher.CY < pitcher.CY);
            Assert.True(LineupLayout.DiamondHead(home, "SS").CY > pitcher.CY);
            Assert.True(LineupLayout.DiamondHead(home, "3B").CX < pitcher.CX);
            Assert.True(LineupLayout.DiamondHead(home, "1B").CX > pitcher.CX);
        }
        var xs = Enumerable.Range(0, 9).Select(i => LineupLayout.HomeSlot(i).CX).ToList();
        for (var i = 1; i < 9; i++)
            Assert.True(xs[i] > xs[i - 1]);
        var seen = new HashSet<(int, int)>();
        foreach (var pos in Diamond.Order)
        {
            var c = LineupLayout.DiamondHead(true, pos);
            Assert.True(seen.Add(((int)(c.CX * 1000), (int)(c.CY * 1000))), pos);
        }
        Assert.Equal(9, seen.Count);
    }

    [Fact]
    public void TitleFitsCouchWindowAboveTheHomeBar()
    {
        const double w = LineupLayout.CouchW, h = LineupLayout.CouchH;
        var title = LineupLayout.GuiPixel(LineupLayout.Title, w, h);
        Assert.True(title.Y >= 16, $"title y {title.Y} clips the top of {w}x{h}");
        Assert.True(title.Y + title.H <= h - 8);
        Assert.True(title.X >= 16);
        Assert.True(title.X + title.W <= w - 8);
        Assert.True(title.H >= 28, "TEAM SETUP / OFFENSE / DEFENSE SETUP need a full line");
        var home = LineupLayout.GuiPixel(LineupLayout.HomeSlot(0), w, h);
        Assert.True(home.Y >= title.Y + title.H, "home bar must sit below TEAM SETUP");
        var caption = LineupLayout.GuiPixel(LineupLayout.HomeCaption, w, h);
        Assert.True(caption.Y >= title.Y + title.H - 1);
        Assert.True(caption.Y + caption.H <= home.Y + 1);
    }

    [Fact]
    public void NameLabelIsPaddedSoJesterDoesNotClip()
    {
        var cell = LineupLayout.PoolCell(0, 0);
        var name = LineupLayout.NameRect(cell);
        Assert.True(name.X > cell.X);
        Assert.True(name.X - cell.X >= cell.W * LineupLayout.LabelPadX - 1e-6);
        Assert.True(name.X + name.W < cell.X + cell.W);
        Assert.True(name.Y >= cell.Y);
        Assert.True(name.Y + name.H <= cell.Y + cell.H);
        var face = LineupLayout.FaceRect(cell);
        Assert.True(face.Y >= name.Y + name.H - 1e-6, "face sits above the name strip");
        Assert.True(face.X > cell.X);
        Assert.True(face.X + face.W < cell.X + cell.W);
        var jester = LineupLayout.NameRect(LineupLayout.HomeSlot(3));
        Assert.True(jester.X > LineupLayout.HomeSlot(3).X);
        Assert.True(jester.W >= LineupLayout.HomeSlot(3).W * 0.7);
    }

    [Fact]
    public void RosterMarksAreOrderAndPositionNotNineCaptains()
    {
        var s = Filled();
        var caps = 0;
        for (var i = 0; i < 9; i++)
        {
            var mark = LineupLayout.TeamMark(s.HomeSlots[i]);
            if (mark == "C") caps++;
            else Assert.Equal("", mark);
            Assert.Equal((i + 1).ToString(CultureInfo.InvariantCulture), LineupLayout.OrderMark(i));
        }
        Assert.Equal(1, caps);
        Assert.Equal("C", LineupLayout.TeamMark(s.HomeCaptain));
        Assert.Equal("", LineupLayout.TeamMark(_content.Must("jester")));
        foreach (var pos in Diamond.Order)
        {
            Assert.Equal(pos, LineupLayout.GloveMark(pos));
            Assert.False(pos != "C" && LineupLayout.GloveMark(pos) == "C", pos);
        }
    }

    [Fact]
    public void HomeAndAwayDiamondsDoNotStack()
    {
        Assert.True(LineupLayout.HomeDiamondPanel.X + LineupLayout.HomeDiamondPanel.W
            < LineupLayout.AwayDiamondPanel.X);
        foreach (var pos in Diamond.Order)
        {
            var home = LineupLayout.DiamondHead(true, pos);
            var away = LineupLayout.DiamondHead(false, pos);
            Assert.True(home.X + home.W < away.X, pos + " home/away heads overlap");
        }
    }

    [Fact]
    public void RolePlayersShareCaptainPortraitId()
    {
        foreach (var c in _content.Characters.Values)
        {
            var id = Silhouette.PortraitId(c);
            Assert.Contains(id, Shipped.CaptainIds);
            if (c.Captain)
                Assert.Equal(c.Id, id, ignoreCase: true);
        }
        Assert.Equal("rio", Silhouette.PortraitId(_content.Must("nico")));
        Assert.Equal("vale", Silhouette.PortraitId(_content.Must("frost")));
        Assert.Equal("zig", Silhouette.PortraitId(_content.Must("jester")));
        Assert.Equal("brondo", Silhouette.PortraitId(_content.Must("boom")));
        Assert.Equal("konga", Silhouette.PortraitId(_content.Must("vine")));
        Assert.Equal("ashlord", Silhouette.PortraitId(_content.Must("cinder")));
    }

    [Fact]
    public void HeartsStayOnTheHighlightedHeadVersusTheCaptain()
    {
        var s = LineupScreens.Open(_content, "vale", "brondo");
        Assert.Equal(ChemistryToy.None, s.ChemSticker(s.HomeCaptain));
        var buddy = s.Pool.First(c => !s.Taken(c) && _content.Chemistry.Between("vale", c.Id) == Chemistry.Good);
        var rival = s.Pool.First(c => !s.Taken(c) && _content.Chemistry.Between("vale", c.Id) == Chemistry.Bad);
        Assert.Equal(ChemistryToy.Heart, s.ChemSticker(buddy));
        Assert.Equal(ChemistryToy.Scribble, s.ChemSticker(rival));
        s.RandomFill();
        Assert.True(s.HomeStars >= 0 && s.HomeStars <= 5);
        Assert.True(s.ConfirmTeam());
        var card = s.HighlightCard();
        Assert.True(card.HasValue);
        Assert.False(string.IsNullOrWhiteSpace(card.Value.StarPitch));
    }

    [Fact]
    public void ExistingTeamBuilderDraftStillFillsNine()
    {
        var b = TeamBuilder.Draft(_content, "vale");
        Assert.Equal(9, b.Order.Count);
        Assert.Equal("vale", b.Captain.Id);
        Assert.Equal("P", b.PosOf("vale"));
    }

    [Fact]
    public void AwayPlayerCanDraftAndASecondPlayerCanJoinHome()
    {
        var s = LineupScreens.Open(_content, "rio", "ashlord", LineupSeat.Cpu, LineupSeat.Pad1);
        Assert.True(s.HomeFull);
        Assert.False(s.AwayFull);
        Assert.True(s.RandomFill(LineupSeat.Pad1));
        var away = s.AwaySlots.Select(c => c!.Id).ToArray();
        s.Sit(LineupSeat.Pad2, LineupSeat.Pad1);
        Assert.False(s.HomeFull);
        Assert.Equal(away, s.AwaySlots.Select(c => c!.Id));
        Assert.True(s.RandomFill(LineupSeat.Pad2));
        Assert.True(s.ConfirmTeam());
        Assert.Equal(LineupFocus.AwayOrder, s.FocusOf(LineupSeat.Pad1));
        Assert.Equal(LineupFocus.HomeOrder, s.FocusOf(LineupSeat.Pad2));
    }

    static bool Overlap(LineupCell a, LineupCell b) => a.X < b.X + b.W && a.X + a.W > b.X
        && a.Y < b.Y + b.H && a.Y + a.H > b.Y;

    [Fact]
    public void InspectingAndNavigatingNeverEditsTheTeam()
    {
        var s = Filled();
        var order = s.Home!.Order.Select(c => c.Id).ToArray();
        var gloves = Diamond.Order.Select(p => s.Home.Gloves[p].Id).ToArray();
        for (var i = 0; i < 9; i++) s.Stick(1, 0);
        s.ToggleArea(LineupSeat.Pad1);
        foreach (var d in new[] { (1, 0), (0, 1), (-1, 0), (0, -1) }) s.Stick(d.Item1, d.Item2);
        Assert.Equal(order, s.Home.Order.Select(c => c.Id));
        Assert.Equal(gloves, Diamond.Order.Select(p => s.Home.Gloves[p].Id));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EachSeatExplicitlySwapsOrderAndGlovesWithoutChangingTheOtherTeam(bool reversed)
    {
        var s = LineupScreens.Open(_content, "rio", "ashlord",
            reversed ? LineupSeat.Pad2 : LineupSeat.Pad1, reversed ? LineupSeat.Pad1 : LineupSeat.Pad2);
        s.RandomFill(LineupSeat.Pad1); s.RandomFill(LineupSeat.Pad2);
        Assert.True(s.ConfirmTeam());
        foreach (var seat in new[] { LineupSeat.Pad1, LineupSeat.Pad2 })
        {
            var home = seat == s.HomeSeat;
            var draft = home ? s.Home! : s.Away!;
            var other = home ? s.Away! : s.Home!;
            var otherOrder = other.Order.Select(c => c.Id).ToArray();
            var otherGloves = Diamond.Order.Select(p => other.Gloves[p].Id).ToArray();
            var order = home ? LineupFocus.HomeOrder : LineupFocus.AwayOrder;
            Assert.Equal(order, s.FocusOf(seat));
            var first = draft.Order[0].Id;
            Assert.True(s.FocusCell(seat, order, 0));
            Assert.False(s.PickOrSwap(seat));
            Assert.True(s.FocusCell(seat, order, 8));
            Assert.True(s.PickOrSwap(seat));
            Assert.Equal(first, draft.Order[8].Id);
            var field = home ? LineupFocus.HomeDiamond : LineupFocus.AwayDiamond;
            var pitcher = draft.Gloves["P"].Id;
            s.FocusCell(seat, field, 0); Assert.False(s.PickOrSwap(seat));
            s.FocusCell(seat, field, 8); Assert.True(s.PickOrSwap(seat));
            Assert.Equal(pitcher, draft.Gloves["RF"].Id);
            Assert.Equal(9, draft.Gloves.Values.Select(c => c.Id).Distinct().Count());
            Assert.Equal(otherOrder, other.Order.Select(c => c.Id));
            Assert.Equal(otherGloves, Diamond.Order.Select(p => other.Gloves[p].Id));
            Assert.False(s.FocusCell(seat, home ? LineupFocus.AwayOrder : LineupFocus.HomeOrder, 0));
        }
    }

    [Fact]
    public void CancelAndSameSlotNeverCreditASwapAndBackClearsThePick()
    {
        var s = Filled();
        Assert.False(s.PickOrSwap(LineupSeat.Pad1));
        Assert.True(s.AnyPick);
        Assert.True(s.West());
        Assert.False(s.AnyPick);
        Assert.Equal(LineupStep.DefenseSetup, s.Step);
        Assert.False(s.PickOrSwap(LineupSeat.Pad1));
        Assert.False(s.PickOrSwap(LineupSeat.Pad1));
        Assert.False(s.AnyPick);
        s.PickOrSwap(LineupSeat.Pad1);
        s.ToggleArea(LineupSeat.Pad1);
        Assert.False(s.AnyPick);
    }

    [Fact]
    public void InspectionUsesTheActualPlayerPairAndNeverHighlightsSelf()
    {
        var s = LineupScreens.Open(_content, "vale", "brondo");
        foreach (var who in _content.Characters.Values)
        {
            Assert.Equal(who.Name, s.CardFor(who)!.Value.Name);
            Assert.False(s.Buddies(who, who));
            foreach (var other in _content.Characters.Values.Where(c => c.Id != who.Id))
                Assert.Equal(_content.Chemistry.Between(who, other) == Chemistry.Good, s.Buddies(who, other));
        }
        Assert.Null(s.CardFor(null));
        Assert.False(s.Buddies(s.HomeCaptain, null));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TwoPlayersKeepIndependentCardsPicksAndReadyStates(bool reversed)
    {
        var s = LineupScreens.Open(_content, "rio", "ashlord",
            reversed ? LineupSeat.Pad2 : LineupSeat.Pad1, reversed ? LineupSeat.Pad1 : LineupSeat.Pad2);
        s.RandomFill(LineupSeat.Pad1); s.RandomFill(LineupSeat.Pad2); s.ConfirmTeam();
        s.FocusCell(LineupSeat.Pad1, reversed ? LineupFocus.AwayOrder : LineupFocus.HomeOrder, 2);
        var p1Card = s.InspectedBy(LineupSeat.Pad1);
        s.FocusCell(LineupSeat.Pad2, reversed ? LineupFocus.HomeDiamond : LineupFocus.AwayDiamond, 5);
        Assert.Same(p1Card, s.InspectedBy(LineupSeat.Pad1));
        Assert.NotSame(p1Card, s.InspectedBy(LineupSeat.Pad2));
        s.PickOrSwap(LineupSeat.Pad2);
        Assert.True(s.ToggleReady(LineupSeat.Pad1));
        Assert.False(s.BothReady);
        Assert.False(s.ToggleReady(LineupSeat.Pad2));
        Assert.True(s.HasPick(LineupSeat.Pad2));
        s.CancelPick(LineupSeat.Pad2);
        Assert.True(s.ToggleReady(LineupSeat.Pad2));
        Assert.True(s.BothReady);
        s.PickOrSwap(LineupSeat.Pad2);
        Assert.False(s.IsReady(LineupSeat.Pad2));
        Assert.True(s.IsReady(LineupSeat.Pad1));
        Assert.False(s.BothReady);
        s.West(LineupSeat.Pad1);
        Assert.False(s.IsReady(LineupSeat.Pad1));
        Assert.Equal(LineupStep.DefenseSetup, s.Step);
        s.CancelPick(LineupSeat.Pad2);
        s.ToggleReady(LineupSeat.Pad1); s.ToggleReady(LineupSeat.Pad2);
        s.Sit(reversed ? LineupSeat.Cpu : LineupSeat.Pad1, reversed ? LineupSeat.Pad1 : LineupSeat.Cpu);
        Assert.False(s.BothReady); // Losing a seat must never launch the game.
        Assert.True(s.ToggleReady(LineupSeat.Pad1));
        Assert.True(s.BothReady);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HorizontalOrderAndInwardFieldNavigationFollowEitherSeatsBar(bool away)
    {
        var s = LineupScreens.Open(_content, "rio", "ashlord",
            away ? LineupSeat.Cpu : LineupSeat.Pad1, away ? LineupSeat.Pad1 : LineupSeat.Cpu);
        s.RandomFill(LineupSeat.Pad1); s.ConfirmTeam();
        var order = away ? LineupFocus.AwayOrder : LineupFocus.HomeOrder;
        var field = away ? LineupFocus.AwayDiamond : LineupFocus.HomeDiamond;
        Assert.True(s.Stick(1, 0));
        Assert.Equal(1, s.OrderOf(LineupSeat.Pad1));
        Assert.Equal(order, s.FocusOf(LineupSeat.Pad1));
        Assert.True(s.Stick(0, away ? 1 : -1));
        Assert.Equal(field, s.FocusOf(LineupSeat.Pad1));
        s.FocusCell(LineupSeat.Pad1, field, Array.IndexOf(Diamond.Order, away ? "C" : "CF"));
        Assert.True(s.Stick(0, away ? -1 : 1));
        Assert.Equal(order, s.FocusOf(LineupSeat.Pad1));
    }

    LineupScreens Filled()
    {
        var s = LineupScreens.Open(_content, "vale", "brondo");
        Assert.True(s.RandomFill());
        Assert.True(s.HomeFull);
        Assert.True(s.ConfirmTeam());
        return s;
    }
}

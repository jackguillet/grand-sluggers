using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class LineupScreensTests
{
    readonly ContentCatalog _content = ContentCatalog.Load();

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
        Assert.True(s.Pool.Count >= 8);
        Assert.DoesNotContain(s.Pool, c => c.Id.Equals("vale", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(s.Pool, c => s.AwaySlots.Any(a => a != null && a.Id == c.Id));
    }

    [Fact]
    public void CaptainCannotBeDroppedWhenLockCaptain()
    {
        var locked = LineupScreens.Open(_content, "vale", "brondo", lockCaptain: true);
        locked.Stick(0, 1);
        Assert.Equal(LineupFocus.HomeRow, locked.Focus);
        // slot 0 is the captain
        while (locked.SlotIndex != 0)
            locked.Stick(-1, 0);
        Assert.Equal("vale", locked.HomeSlots[0]!.Id);
        Assert.False(locked.Remove());
        Assert.Equal("vale", locked.HomeSlots[0]!.Id);
        Assert.False(locked.Drop());

        var open = LineupScreens.Open(_content, "vale", "brondo", lockCaptain: false);
        open.Stick(0, 1);
        while (open.SlotIndex != 0)
            open.Stick(-1, 0);
        Assert.True(open.Remove());
        Assert.Null(open.HomeSlots[0]);
        Assert.Contains(open.Pool, c => c.Id.Equals("vale", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void SouthDropsAPoolHeadIntoTheHighlightedEmptySlot()
    {
        var s = LineupScreens.Open(_content, "vale", "brondo");
        Assert.Equal(LineupFocus.Pool, s.Focus);
        var pick = s.Pool[s.PoolIndex];
        Assert.Equal(1, s.SlotIndex);
        Assert.True(s.South());
        Assert.Equal(pick.Id, s.HomeSlots[1]!.Id);
        Assert.DoesNotContain(s.Pool, c => c.Id.Equals(pick.Id, StringComparison.OrdinalIgnoreCase));
        Assert.True(s.West());
        Assert.Null(s.HomeSlots[1]);
        Assert.Contains(s.Pool, c => c.Id.Equals(pick.Id, StringComparison.OrdinalIgnoreCase));
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
        var homePick = s.Pool[0];
        Assert.True(s.South(LineupSeat.Pad1));
        Assert.Equal(homePick.Id, s.HomeSlots[1]!.Id);
        Assert.Null(s.AwaySlots[1]);
        var awayPick = s.Pool[0];
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
    public void LayoutIsTwoBarsThenTwoDiamonds()
    {
        Assert.True(LineupLayout.HomeSlot(0).Y > LineupLayout.PoolCell(0, 12).Y);
        Assert.True(LineupLayout.PoolCell(0, 12).Y > LineupLayout.AwaySlot(0).Y);
        Assert.True(LineupLayout.HomeDiamondPanel.CX < LineupLayout.AwayDiamondPanel.CX);
        Assert.True(LineupLayout.HomeOrder(0).Y > LineupLayout.DiamondHead(true, "CF").Y);
        Assert.True(LineupLayout.DiamondHead(true, "C").Y > LineupLayout.AwayOrder(0).Y);
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
        var cell = LineupLayout.PoolCell(0, 12);
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
            Assert.Equal((i + 1).ToString(), LineupLayout.OrderMark(i));
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
            Assert.Contains(id, Silhouette.Captains);
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
        var buddy = s.Pool.First(c => _content.Chemistry.Between("vale", c.Id) == Chemistry.Good);
        var rival = s.Pool.First(c => _content.Chemistry.Between("vale", c.Id) == Chemistry.Bad);
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

    LineupScreens Filled()
    {
        var s = LineupScreens.Open(_content, "vale", "brondo");
        Assert.True(s.RandomFill());
        Assert.True(s.HomeFull);
        Assert.True(s.ConfirmTeam());
        return s;
    }
}

using Xunit;
using GrandSluggers.Sim;

namespace GrandSluggers.Sim.Tests;

public class CarnivalFrontTests
{
    [Fact]
    public void HarborIsTheRealDiamondPostcard()
    {
        Assert.Equal("GRAND SLUGGERS", CarnivalFront.Logo);
        Assert.Contains("EXHIBITION", CarnivalFront.PlayBall, StringComparison.OrdinalIgnoreCase);
        Assert.True(CarnivalFront.HarborIsTheProduct("harbor-diamond"));
        Assert.False(CarnivalFront.HarborIsTheProduct("crystal-rink"));
        Assert.Contains("real diamond", CarnivalFront.FieldCard(ContentCatalog.Load().MustPark("harbor-diamond"), ContentCatalog.Load().Rules)[0], StringComparison.OrdinalIgnoreCase);
        Assert.Equal("DAY", CarnivalFront.SkyGag(false));
        Assert.Equal("NIGHT", CarnivalFront.SkyGag(true));
        Assert.Equal("HOME", CarnivalFront.SeatMark(true));
        Assert.Equal("AWAY", CarnivalFront.SeatMark(false));
        Assert.Contains("bottom", CarnivalFront.SeatHint(true), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("top", CarnivalFront.SeatHint(false), StringComparison.OrdinalIgnoreCase);
        Assert.Equal("1 PLAYER", CarnivalFront.SeatModeLabel(false));
        Assert.Equal("2 PLAYERS", CarnivalFront.SeatModeLabel(true));
        Assert.Equal(CarnivalFront.SeatHint(true), CarnivalFront.SeatModeHint(false, true, true));
        Assert.Contains("controller 2", CarnivalFront.SeatModeHint(true, false, true), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Controller 2", CarnivalFront.SeatModeHint(true, true, true));
        const float w = 1280, h = 800;
        var one = CarnivalFront.SeatModeTab(false, w, h);
        var two = CarnivalFront.SeatModeTab(true, w, h);
        Assert.True(two.X > one.X);
        Assert.Equal(false, CarnivalFront.HitSeatMode(one.X + 8, one.Y + 8, w, h));
        Assert.Equal(true, CarnivalFront.HitSeatMode(two.X + 8, two.Y + 8, w, h));
        Assert.Null(CarnivalFront.HitSeatMode(8, 8, w, h));
        Assert.Contains("Up/down", CarnivalFront.SelectHelp, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Left/right", CarnivalFront.SelectHelp, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// The field card (FD-15, F8-a) is read from the played park: Crystal's ice, glass and air and its freezers; Funfair's cans
    /// and train by day and its chompers only at night; every hazard type and every ground a park can name has its line; and
    /// hazards off leaves the ground, wall and air lines and no hazard's.
    /// </summary>
    [Fact]
    public void F8A_TheFieldCardIsReadFromThePlayedPark()
    {
        var c = ContentCatalog.Load();
        IReadOnlyList<string> Card(string id, bool night = false, bool hazards = true) =>
            CarnivalFront.FieldCard(PlayedPark.Of(c.MustPark(id), night, hazards, c.Rules.Hazards), c.Rules);
        var crystal = Card("crystal-rink");
        Assert.Contains(crystal, l => l.StartsWith("Ice outfield", StringComparison.Ordinal));
        Assert.Contains(crystal, l => l.StartsWith("Glass boards", StringComparison.Ordinal));
        Assert.Contains(crystal, l => l.StartsWith("Heavy air", StringComparison.Ordinal));
        Assert.Contains(crystal, l => l.StartsWith("Freezers", StringComparison.Ordinal) && l.Contains("3 s", StringComparison.Ordinal));
        Assert.Contains(Card("funfair-park"), l => l.StartsWith("Warp cans", StringComparison.Ordinal));
        Assert.DoesNotContain(Card("funfair-park"), l => l.StartsWith("Chompers", StringComparison.Ordinal));
        Assert.Contains(Card("funfair-park", night: true), l => l.StartsWith("Chompers", StringComparison.Ordinal));
        var off = Card("crystal-rink", hazards: false);
        Assert.Contains(off, l => l.StartsWith("Ice outfield", StringComparison.Ordinal));
        Assert.DoesNotContain(off, l => l.StartsWith("Freezers", StringComparison.Ordinal));
        foreach (var type in HazardType.All) Assert.NotNull(CarnivalFront.HazardLine(type, c.Rules));
    }

    [Fact]
    public void HighlightedCaptainStepsTowardTheCamera()
    {
        var row = CarnivalFront.CaptainSpot(2, 6, select: true, home: false);
        var home = CarnivalFront.CaptainSpot(2, 6, select: true, home: true);
        Assert.True(home.Z < row.Z, $"home {home.Z} should be closer to camera than row {row.Z}");
        Assert.Equal(0f, home.X);
        Assert.NotEqual(row.X, home.X);
        Assert.Equal(CarnivalFront.SelectRowZ - CarnivalFront.FeaturedSelectZ, CarnivalFront.HomeStepSelectFt);
        Assert.True(CarnivalFront.CardX > 4);
        Assert.True(CarnivalFront.CardY > 2);
        Assert.InRange(CarnivalFront.LogoZ, -10, 16);
        Assert.True(Math.Abs(CarnivalFront.LogoX) < 8, $"logo off-frame x={CarnivalFront.LogoX}");
        Assert.True(CarnivalFront.LogoY > 10, $"logo through the hat y={CarnivalFront.LogoY}");
        Assert.Equal("GRAND SLUGGERS", CarnivalFront.Logo);
    }

    [Fact]
    public void TitleShowsNoCaptain()
    {
        Assert.False(CarnivalFront.TitleShowsCaptain);
        Assert.False(CarnivalFront.TitlePlacesBody(select: false));
        Assert.True(CarnivalFront.TitlePlacesBody(select: true));
        var titleHome = CarnivalFront.CaptainSpot(0, 6, select: false, home: true);
        var titleRow = CarnivalFront.CaptainSpot(0, 6, select: false, home: false);
        Assert.Equal(titleRow.Z, titleHome.Z);
        Assert.Equal(CarnivalFront.TitleRowZ, titleHome.Z);
        var xs = new HashSet<float>();
        for (var i = 0; i < 6; i++)
        {
            var spot = CarnivalFront.CaptainSpot(i, 6, select: true, home: false);
            Assert.Equal(CarnivalFront.SelectRowZ, spot.Z);
            xs.Add(spot.X);
        }
        Assert.Equal(6, xs.Count);
        var pick = CarnivalFront.CaptainSpot(2, 6, select: true, home: true);
        Assert.Equal(0f, pick.X);
        Assert.Equal(CarnivalFront.FeaturedSelectZ, pick.Z);
    }

    [Fact]
    public void TitleIsAStickerOverTheInfield()
    {
        var title = ContentCatalog.Load().Shots.Must("title");
        Assert.True(CarnivalFront.TitlePoster(title.Pos, title.Target),
            $"title is not a sticker poster cam={title.Pos} look={title.Target} " +
            $"logoDeg={CarnivalFront.OffLook(title.Pos, title.Target, CarnivalFront.TitleLogoAt):0.0}");
        var row = CarnivalFront.CaptainSpot(1, 6, select: false, home: false);
        Assert.True(row.Z > 16, $"title row should wait off-frame z={row.Z}");
    }

    [Fact]
    public void TitleLogoReadsLeftToRightFromTheTitleCamera()
    {
        var title = ContentCatalog.Load().Shots.Must("title");
        var logo = CarnivalFront.TitleLogoAt;
        var fwd = CarnivalFront.TitleLogoForward(title.Pos, logo);
        Assert.True(fwd.Z > 0, $"sticker looks back at home z={fwd.Z}");
        Assert.True(CarnivalFront.TitleLogoReads(title.Pos, logo));
        Assert.True(CarnivalFront.TitleLogoInkZ < 0);
        Assert.True(CarnivalFront.TitleLogoGlyphZ < CarnivalFront.TitleLogoInkZ);
        var atCam = CarnivalFront.TitleLogoForward(logo, title.Pos);
        Assert.True(atCam.Z < 0, "LookRotation(toCam) is the mirrored-wordmark facing");
        Assert.False(CarnivalFront.TitleLogoReads(logo, title.Pos));
    }

    [Fact]
    public void SelectLookIsTheChestNotTheBrim()
    {
        var look = CarnivalFront.SelectLook(5, 6);
        Assert.Equal(0f, look.X);
        Assert.Equal(CarnivalFront.FeaturedSelectZ, look.Z);
        Assert.True(look.Y < 3.2f, $"select look is the brim y={look.Y}");
        Assert.True(CarnivalFront.FeaturedSelectZ >= 6.5f, $"select pick too close z={CarnivalFront.FeaturedSelectZ}");
        Assert.True(CarnivalFront.FeaturedSelectZ < CarnivalFront.SelectRowZ);
        Assert.Equal(CarnivalFront.SelectRowZ - CarnivalFront.FeaturedSelectZ, CarnivalFront.HomeStepSelectFt);
    }

    [Fact]
    public void SelectCamIsTheToyNotTheBerm()
    {
        Assert.True(CarnivalFront.SelectCamIsTheToy(4.6, -10));
        Assert.False(CarnivalFront.SelectCamIsTheToy(7.8, -12), "y=7.8 at z=-12 looks down at the plate dirt");
        Assert.False(CarnivalFront.SelectCamIsTheToy(4.4, 4), "z=4 / look y=4.4 is Ashlord's brim");
        Assert.False(CarnivalFront.SelectCamIsTheToy(2.0, -10), "cam in the dirt");
        Assert.False(CarnivalFront.SelectCamIsTheToy(4.6, -22), "through the backstop cage");
    }

    [Fact]
    public void SelectCaptainsStayOnTheDirt()
    {
        var content = ContentCatalog.Load();
        Assert.Equal(0f, CarnivalFront.SelectDirtY);
        Assert.Equal(Motion.Verb.Idle, CarnivalFront.SelectPose(true, false));
        Assert.Equal(Motion.Verb.Idle, CarnivalFront.SelectPose(false, true));
        Assert.Equal(Motion.Verb.Idle, CarnivalFront.SelectPose(false, false));
        Assert.True(CarnivalFront.SelectStaysOnDirt(Motion.Verb.Idle));
        Assert.False(CarnivalFront.SelectStaysOnDirt(Motion.Verb.Cheer), "cheer bob goes through the dirt");
        Assert.False(CarnivalFront.SelectStaysOnDirt(Motion.Verb.StealLead), "steal lead is a crouch");

        foreach (var id in Silhouette.Captains)
        {
            var who = content.Must(id);
            var skin = content.Art.SkinOf(who);
            var scale = Silhouette.SharedRootScale(Silhouette.Proportions(who)).Y;
            foreach (var yours in new[] { true, false })
            {
                var pose = CarnivalFront.SelectPose(yours, theirs: false);
                Assert.True(CarnivalFront.SelectStaysOnDirt(pose), id + " select pose sinks");
                foreach (var extraId in skin.Extras)
                {
                    Assert.True(content.Art.TryExtra(extraId, out var extra), id + " " + extraId);
                    var y = CarnivalFront.SelectExtraMinY(extra, scale, yours, pose);
                    Assert.True(y >= CarnivalFront.SelectDirtY,
                        $"{id} {extraId} yours={yours} y={y} under the dirt");
                    Assert.True(
                        CarnivalFront.SelectExtraMinY(extra, scale, yours, Motion.Verb.Cheer)
                            < CarnivalFront.SelectDirtY,
                        id + " " + extraId + " cheer must fail the dirt bound");
                    Assert.True(
                        CarnivalFront.SelectExtraMinY(extra, scale, yours, Motion.Verb.StealLead)
                            < CarnivalFront.SelectDirtY,
                        id + " " + extraId + " stealLead must fail the dirt bound");
                }
            }
        }
    }

    static readonly ContentCatalog Catalog = ContentCatalog.Load();

    /// <summary>
    /// The hazards switch on the title (FD-10, §14): the line under PLAY BALL ends with it, on by default and read both
    /// ways; the field postcard's footer names its button beside night.
    /// </summary>
    [Fact]
    public void FD10_TheTitleLineAndTheFieldFooterCarryTheHazardsSwitch()
    {
        var rules = Catalog.Rules;
        Assert.Equal("3 INNINGS  ·  NORMAL  ·  CPU SKILL  ·  HAZARDS ON", CarnivalFront.TitleSetup(3, "normal", rules, true));
        Assert.Equal("3 INNINGS  ·  NORMAL  ·  CPU SKILL  ·  HAZARDS OFF", CarnivalFront.TitleSetup(3, "normal", rules, false));
        Assert.StartsWith(CarnivalFront.TitleSetup(3, "normal", rules), CarnivalFront.TitleSetup(3, "normal", rules, false));
        Assert.Contains("Up/down choose", CarnivalFront.FieldFooter);
    }

    /// <summary>
    /// FD-10-R1: the postcard says what hazards off keeps only where the switch removes something tonight, read from the
    /// switch's own rule — never at Harbor, never with hazards on; at every catalog park with a hazard, by day and at night.
    /// </summary>
    [Fact]
    public void FD10R1_ThePostcardSaysWhatOffKeepsOnlyWhereTheSwitchRemovesSomething()
    {
        var library = Catalog.Rules.Hazards;
        foreach (var park in Catalog.Parks.Values)
            foreach (var night in new[] { false, true })
            {
                Assert.Null(CarnivalFront.HazardsOffLine(park, night, hazards: true, library));
                var removes = PlayedPark.Of(park, night, false, library).Hazards.Count
                    < PlayedPark.Of(park, night, true, library).Hazards.Count;
                Assert.Equal(removes ? CarnivalFront.HazardsOffCopy : null, CarnivalFront.HazardsOffLine(park, night, false, library));
            }
        Assert.Null(CarnivalFront.HazardsOffLine(Catalog.MustPark("harbor-diamond"), false, false, library));
        Assert.Equal(CarnivalFront.HazardsOffCopy, CarnivalFront.HazardsOffLine(Catalog.MustPark("crystal-rink"), false, false, library));
        Assert.Equal(CarnivalFront.HazardsOffCopy, CarnivalFront.HazardsOffLine(Catalog.MustPark("funfair-park"), true, false, library));
    }

    /// <summary>The book pair names the switch on both schemes, and the file book names it on the stadium page.</summary>
    [Fact]
    public void FD10_TheBookPairNamesTheHazardsSwitchOnBothSchemes()
    {
        var page = HowToPlay.Pages.Single(p => p.Id == "pause-practice");
        Assert.Contains(page.Lines, l => l.Contains("Hazards    on / off", StringComparison.Ordinal));
        Assert.Contains(page.KeyLines!, l => l.Contains("R    hazards on / off", StringComparison.Ordinal));
        var book = File.ReadAllText(Path.Combine(Catalog.Root.Shipped, "..", "docs", "how-to-play.md"));
        Assert.Contains("Pick stadium, day/night and hazards", book);
        Assert.Contains("Up/down chooses a row", book);
    }
}

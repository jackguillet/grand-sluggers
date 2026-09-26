using GrandSluggers.Sim;
using GrandSluggers.Sim.Front;
using Xunit;

namespace GrandSluggers.Sim.Tests;

/// <summary>
/// The continent map picker (WD-17 A, WD-18 C): the stick moves the cursor pin to pin, every park can be reached from every
/// other, up is north, South plays the park under the cursor and East keeps the one the map opened on; the pins and their
/// labels stand inside the map and never overlap; the card reads the park under the cursor from its data.
/// </summary>
public sealed class MapPickerTests
{
    static readonly ContentCatalog Catalog = Shipped.Content;
    static WorldMap World => Catalog.World;
    static readonly (int Dx, int Up)[] Sticks = [(1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (1, -1), (-1, 1), (-1, -1)];

    [Fact]
    public void EveryParkReachesEveryOtherParkWithTheStick()
    {
        foreach (var start in Catalog.Parks.Keys)
        {
            var seen = new HashSet<string> { start };
            var frontier = new Queue<string>([start]);
            while (frontier.Count > 0)
            {
                var at = frontier.Dequeue();
                foreach (var (dx, up) in Sticks)
                {
                    var next = World.Step(at, dx, -up);
                    if (seen.Add(next)) frontier.Enqueue(next);
                }
            }
            Assert.Equal(Catalog.Parks.Count, seen.Count);
        }
    }

    /// <summary>
    /// WD-26: the neighborhood field sits apart, across the portal, and the stick crosses the gap both ways — out to the
    /// continent and back to the neighborhood — like any other pin.
    /// </summary>
    [Fact]
    public void TheStickCrossesThePortalBothWays()
    {
        Assert.All(Catalog.Parks.Keys, p => Assert.Empty(World.Unreached(p)));
        var shore = World.ParkIn(World.PortalShore()!.Id)!;
        var map = new MapPicker();
        map.Open(ParkId.Harbor);
        // Up-left from the neighborhood lands on the continent at the portal's shore; down-right comes home.
        Assert.Equal(shore, map.Move(World, -1, 1));
        Assert.Equal(ParkId.Harbor, map.Move(World, 1, -1));
    }

    /// <summary>
    /// The inset holds its own pin and label, stays inside the map, and covers no continent pin or label; the portal sits in
    /// the open between the inset and the continent; the sparks run both legs and never land on a pin, a label or the inset.
    /// </summary>
    [Fact]
    public void TheInsetAndThePortalStandApartFromTheContinent()
    {
        var panel = CarnivalFront.MapPanel;
        var apart = World.ApartRegion!;
        var inset = CarnivalFront.MapApartPanel(apart);
        Assert.True(Inside(inset, panel), "the inset leaves the map");
        var (ax, ay) = CarnivalFront.MapPin(apart);
        Assert.True(Inside(Pin(ax, ay), inset), "the neighborhood pin leaves its inset");
        Assert.True(Inside(CarnivalFront.MapLabel(apart), inset), "the neighborhood label leaves its inset");
        Assert.True(Apart(CarnivalFront.MapApartTitle(apart), Pin(ax, ay)), "the name band covers the pin");
        var (px, py) = CarnivalFront.MapPortal(apart);
        var ring = new CaptainPanelRect(px - CarnivalFront.MapPortalSize / 2, py - CarnivalFront.MapPortalSize / 2,
            CarnivalFront.MapPortalSize, CarnivalFront.MapPortalSize);
        Assert.True(Inside(ring, panel), "the portal leaves the map");
        Assert.True(Apart(ring, inset), "the portal sits on the inset");
        foreach (var region in World.Regions.Where(r => !r.Apart))
        {
            var (x, y) = CarnivalFront.MapPin(region);
            Assert.True(Apart(inset, Pin(x, y)), $"the inset covers {region.Id}'s pin");
            Assert.True(Apart(inset, CarnivalFront.MapLabel(region)), $"the inset covers {region.Id}'s label");
            Assert.True(Apart(ring, Pin(x, y)), $"the portal covers {region.Id}'s pin");
            Assert.True(Apart(ring, CarnivalFront.MapLabel(region)), $"the portal covers {region.Id}'s label");
        }
        var sparks = CarnivalFront.MapPortalSparks(World);
        var (sx, sy) = CarnivalFront.MapPin(World.PortalShore()!);
        Assert.Contains(sparks, s => Between(s, (ax, ay), (px, py)));
        Assert.Contains(sparks, s => Between(s, (px, py), (sx, sy)));
        foreach (var s in sparks)
            Assert.True(Apart(inset, new CaptainPanelRect(s.X - 3, s.Y - 3, 6, 6)), "a spark sits on the inset");
        // The card says how you get there.
        Assert.Equal(apart.Name + " · " + CarnivalFront.MapThroughPortal, CarnivalFront.MapCard(Catalog, ParkId.Harbor, false, true)[1]);
    }

    static CaptainPanelRect Pin(float x, float y) =>
        new(x - CarnivalFront.MapCursorSize / 2, y - CarnivalFront.MapCursorSize / 2, CarnivalFront.MapCursorSize, CarnivalFront.MapCursorSize);

    static bool Inside(CaptainPanelRect a, CaptainPanelRect outer) =>
        a.X >= outer.X && a.X + a.W <= outer.X + outer.W && a.Y >= outer.Y && a.Y + a.H <= outer.Y + outer.H;

    static bool Apart(CaptainPanelRect a, CaptainPanelRect b) =>
        a.X + a.W <= b.X || b.X + b.W <= a.X || a.Y + a.H <= b.Y || b.Y + b.H <= a.Y;

    static bool Between((float X, float Y) p, (float X, float Y) a, (float X, float Y) b)
    {
        var (dx, dy) = (b.X - a.X, b.Y - a.Y);
        var t = ((p.X - a.X) * dx + (p.Y - a.Y) * dy) / (dx * dx + dy * dy);
        var (qx, qy) = (a.X + t * dx, a.Y + t * dy);
        return t > 0 && t < 1 && MathF.Abs(p.X - qx) + MathF.Abs(p.Y - qy) < 0.5f;
    }

    [Fact]
    public void UpIsNorthAndRightIsEast()
    {
        foreach (var park in Catalog.Parks.Keys)
        {
            var from = World.RegionOf(park);
            var up = World.RegionOf(World.Step(park, 0, -1));
            var right = World.RegionOf(World.Step(park, 1, 0));
            Assert.True(up.Y <= from.Y, $"{park}: up went south");
            Assert.True(right.X >= from.X, $"{park}: right went west");
        }
        // From the neighborhood, down is the island.
        Assert.Equal(ParkId.Coconut, World.Step(ParkId.Harbor, 0, 1));
        // From the plains, up is the frozen north.
        Assert.Equal(ParkId.Crystal, World.Step(ParkId.Funfair, 0, -1));
    }

    [Fact]
    public void SouthPlaysTheCursorsParkAndEastKeepsYours()
    {
        var map = new MapPicker();
        Assert.Null(map.Move(World, 1, 0));
        map.Open(ParkId.Funfair);
        Assert.True(map.IsOpen);
        Assert.Equal(ParkId.Crystal, map.Move(World, 0, 1));
        Assert.Null(map.Move(World, 0, 0));
        Assert.Equal(ParkId.Crystal, map.Confirm());
        Assert.False(map.IsOpen);

        map.Open(ParkId.Harbor);
        map.Move(World, 0, -1);
        Assert.Equal(ParkId.Coconut, map.Park);
        Assert.Equal(ParkId.Harbor, map.Cancel());
        Assert.Equal(ParkId.Harbor, map.Park);
        Assert.False(map.IsOpen);
    }

    [Fact]
    public void PinsAndLabelsStandInsideTheMapAndNeverOverlap()
    {
        var panel = CarnivalFront.MapPanel;
        var boxes = new List<(string Id, CaptainPanelRect Box)>();
        foreach (var region in World.Regions)
        {
            var (x, y) = CarnivalFront.MapPin(region);
            var r = CarnivalFront.MapCursorSize / 2;
            boxes.Add((region.Id + " pin", new CaptainPanelRect(x - r, y - r, 2 * r, 2 * r)));
            boxes.Add((region.Id + " label", CarnivalFront.MapLabel(region)));
        }
        foreach (var (id, b) in boxes)
        {
            Assert.True(b.X >= panel.X && b.X + b.W <= panel.X + panel.W, $"{id} leaves the map sideways");
            Assert.True(b.Y >= panel.Y && b.Y + b.H <= panel.Y + panel.H, $"{id} leaves the map at the top or bottom");
        }
        for (var i = 0; i < boxes.Count; i++)
        for (var j = i + 1; j < boxes.Count; j++)
        {
            var (a, b) = (boxes[i].Box, boxes[j].Box);
            var apart = a.X + a.W <= b.X || b.X + b.W <= a.X || a.Y + a.H <= b.Y || b.Y + b.H <= a.Y;
            Assert.True(apart, $"{boxes[i].Id} overlaps {boxes[j].Id}");
        }
        var card = CarnivalFront.MapCardPanel;
        Assert.True(panel.X + panel.W <= card.X, "the card sits beside the map");
    }

    [Fact]
    public void TheCardReadsTheParkUnderTheCursor()
    {
        var cove = CarnivalFront.MapCard(Catalog, ParkId.Coconut, night: false, hazards: true);
        Assert.Equal("Coconut Cove", cove[0]);
        Assert.Equal("The Island", cove[1]);
        Assert.Equal("Home of Elder Fenn · Tidewater", cove[2]);
        Assert.Contains(cove, l => l.StartsWith("Sand outfield", StringComparison.Ordinal));
        Assert.Equal("Day game", cove[^1]);
        var rink = CarnivalFront.MapCard(Catalog, ParkId.Crystal, night: true, hazards: false);
        Assert.Equal("Aurora Rink", rink[0]);
        Assert.DoesNotContain(rink, l => l.StartsWith("Freezers", StringComparison.Ordinal));
        Assert.Equal("Night game: lights on", rink[^1]);
        // Every park has a card with its home captain.
        foreach (var park in Catalog.Parks.Keys)
            Assert.StartsWith("Home of ", CarnivalFront.MapCard(Catalog, park, false, true)[2], StringComparison.Ordinal);
    }

    [Fact]
    public void TheMapArtSlotIsEmptyUntilArtIsPlaced()
    {
        Assert.Equal("Resources/Art/Map/grand-reach", Catalog.Art.Map.Slot);
        Assert.False(Catalog.Art.Map.Placed);
        Assert.DoesNotContain(Catalog.Art.Validate(Catalog), e => e.StartsWith("map art", StringComparison.Ordinal));
    }
}

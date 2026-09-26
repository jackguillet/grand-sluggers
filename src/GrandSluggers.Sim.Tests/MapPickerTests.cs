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
        // From the harbor, down is the island.
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

using System.Text.Json.Serialization;
namespace GrandSluggers.Sim;

public enum Hand
{
    L,
    R
}

public enum Chemistry
{
    Bad = 0,
    Neutral = 1,
    Good = 2
}

/// <summary>
/// Where the ball met the bat (spec §5.2, D4): the cursor zone decides quality. Sour is the
/// rim of the bat, Nice the oval, Perfect its heart. Miss is off the bat or outside the window.
/// </summary>
public enum ContactQuality
{
    Miss,
    Sour,
    Nice,
    Perfect
}

/// <summary>
/// A character's ratings. <see cref="Field"/> is the displayed defensive number; <see cref="Arm"/> and
/// <see cref="Hands"/> are the explicit traits behind it (F693-02-defensive-trait-mapping). <see cref="Bat"/>
/// is the displayed batting number; <see cref="Contact"/> and <see cref="Power"/> are the explicit traits
/// behind it (PH-15-R5). <see cref="Pitch"/> is the displayed pitching number; <see cref="Velocity"/>,
/// <see cref="Movement"/>, <see cref="Control"/> and <see cref="Endurance"/> are the explicit traits behind
/// it (PH-15-R6). All eight are <b>seeded from the aggregate</b> until a character authors its own, so a
/// roster that names none behaves exactly as it did before the split.
/// </summary>
public sealed record Stats(int Pitch, int Bat, int Field, int Run)
{
    readonly int _arm;
    readonly int _hands;
    readonly int _contact;
    readonly int _power;
    readonly int _velocity;
    readonly int _movement;
    readonly int _control;
    readonly int _endurance;

    /// <summary>Throwing: speed and accuracy. Unauthored (0) tracks <see cref="Field"/>.</summary>
    public int Arm
    {
        get => _arm > 0 ? _arm : Field;
        init => _arm = value;
    }

    /// <summary>Handling: securing the ball and recovering from it. Unauthored (0) tracks <see cref="Field"/>.</summary>
    public int Hands
    {
        get => _hands > 0 ? _hands : Field;
        init => _hands = value;
    }

    /// <summary>
    /// Contact: spatial forgiveness at the plate (PH-15-R7) — it scales the cursor's barrel, so a
    /// crossing further from the center still finds the bat. Unauthored (0) tracks <see cref="Bat"/>.
    ///
    /// It does not widen the timing window (<see cref="AtBatResolver.ContactWindowFrames"/>): the
    /// barrel is all it does at the plate (#844, spec §2, §5.3). The CPU batter's timing error also
    /// reads it, and that is a separate thing: how far off the ball that bat arrives, not how wide
    /// its window is (§5.9; whether it should read Contact at all is P2-g's).
    /// </summary>
    public int Contact
    {
        get => _contact > 0 ? _contact : Bat;
        init => _contact = value;
    }

    /// <summary>Power: exit velocity and loft off the bat (spec §5.4, §5.5). Unauthored (0) tracks <see cref="Bat"/>.</summary>
    public int Power
    {
        get => _power > 0 ? _power : Bat;
        init => _power = value;
    }

    /// <summary>Velocity: pitch speed (spec §4.1, PH-15-R6) — <c>speed.mphPerPitchStat</c>. Unauthored (0) tracks <see cref="Pitch"/>.</summary>
    public int Velocity
    {
        get => _velocity > 0 ? _velocity : Pitch;
        init => _velocity = value;
    }

    /// <summary>
    /// Movement: natural break (PH-15-R6). No family's authored break reads a rating today (§4.3), so
    /// its one read is the arm's say over non-perfect contact (<c>batting.pitchFactor</c>, §5.5): the
    /// ball that is hard to square. Unauthored (0) tracks <see cref="Pitch"/>.
    /// </summary>
    public int Movement
    {
        get => _movement > 0 ? _movement : Pitch;
        init => _movement = value;
    }

    /// <summary>
    /// Control: the player's steering correction (PH-15-R6) — how fast a held stick brings the break to
    /// full (<c>flight.breakRatePerPitchStat</c>, <see cref="PitchFlight.BreakStep"/> /
    /// <see cref="PitchFlight.BreakReach"/>) and the CPU arm's scatter on its rubber intent (§4.8).
    /// Unauthored (0) tracks <see cref="Pitch"/>.
    /// </summary>
    public int Control
    {
        get => _control > 0 ? _control : Pitch;
        init => _control = value;
    }

    /// <summary>Endurance: resistance to fatigue (PH-15-R6) — the stamina pool (§4.7). Unauthored (0) tracks <see cref="Pitch"/>.</summary>
    public int Endurance
    {
        get => _endurance > 0 ? _endurance : Pitch;
        init => _endurance = value;
    }

    /// <summary>
    /// True when this rating was authored rather than seeded from <see cref="Field"/>.
    ///
    /// Not serialized. It is bookkeeping about where the number came from, not a rating, and a
    /// play trace records what happened rather than how the roster was written. Emitting it also
    /// made <see cref="Stats"/> unstable across a JSON round trip: the serialized <c>arm</c> reloads
    /// through the init setter, which marks the trait authored, so a replayed trace no longer
    /// matched the live one it replayed.
    /// </summary>
    [JsonIgnore] public bool ArmAuthored => _arm > 0;

    /// <inheritdoc cref="ArmAuthored"/>
    [JsonIgnore] public bool HandsAuthored => _hands > 0;

    /// <inheritdoc cref="ArmAuthored"/>
    [JsonIgnore] public bool ContactAuthored => _contact > 0;

    /// <inheritdoc cref="ArmAuthored"/>
    [JsonIgnore] public bool PowerAuthored => _power > 0;

    /// <inheritdoc cref="ArmAuthored"/>
    [JsonIgnore] public bool VelocityAuthored => _velocity > 0;

    /// <inheritdoc cref="ArmAuthored"/>
    [JsonIgnore] public bool MovementAuthored => _movement > 0;

    /// <inheritdoc cref="ArmAuthored"/>
    [JsonIgnore] public bool ControlAuthored => _control > 0;

    /// <inheritdoc cref="ArmAuthored"/>
    [JsonIgnore] public bool EnduranceAuthored => _endurance > 0;

    // An unauthored trait stays unauthored through a clamp, so it keeps tracking the clamped aggregate.
    public Stats Clamp() => new(
        Math.Clamp(Pitch, 1, 10),
        Math.Clamp(Bat, 1, 10),
        Math.Clamp(Field, 1, 10),
        Math.Clamp(Run, 1, 10))
    {
        Arm = _arm > 0 ? Math.Clamp(_arm, 1, 10) : 0,
        Hands = _hands > 0 ? Math.Clamp(_hands, 1, 10) : 0,
        Contact = _contact > 0 ? Math.Clamp(_contact, 1, 10) : 0,
        Power = _power > 0 ? Math.Clamp(_power, 1, 10) : 0,
        Velocity = _velocity > 0 ? Math.Clamp(_velocity, 1, 10) : 0,
        Movement = _movement > 0 ? Math.Clamp(_movement, 1, 10) : 0,
        Control = _control > 0 ? Math.Clamp(_control, 1, 10) : 0,
        Endurance = _endurance > 0 ? Math.Clamp(_endurance, 1, 10) : 0
    };
}

/// <summary>
/// A character's ordinary pitches (spec §4.3, PH-15-R1): the fastball every pitcher throws, plus
/// exactly two different families from <see cref="PitchFamily.Assignable"/>, in the accepted order
/// (PH-15-R2 captains, PH-15-R4 role players). Authored as
/// <c>"repertoire": ["&lt;second&gt;", "&lt;third&gt;"]</c>; the fastball is implied and is never listed.
///
/// Two named slots rather than a list, so "fastball plus exactly two" cannot be broken by
/// construction: there is nowhere to put a fourth pitch, nowhere to drop the fastball, and no way
/// to name a family the library does not have. A list would also cost <see cref="Character"/> its
/// value equality — <c>PlayTraceRace</c> runs the roster through <c>Distinct()</c>, and a
/// <c>List&lt;string&gt;</c> member compares by reference, so two identical rosters would stop
/// being equal.
///
/// Membership only. Nothing here is a speed, a break or a stamina cost, and the three families
/// beyond fastball and changeup do not fly yet (#807).
/// </summary>
public sealed record Repertoire(string Second, string Third)
{
    /// <summary>Ordinary pitches per character (PH-02-R1): the fastball and the two below.</summary>
    public const int Slots = 3;

    /// <summary>The second ordinary pitch, in accepted order. Slot 1 of the PH-02-R5 cycle.</summary>
    public string Second { get; } = Assignable(Second, nameof(Second));

    /// <summary>The third ordinary pitch. Never the same family as <see cref="Second"/>.</summary>
    public string Third { get; } = Different(Assignable(Third, nameof(Third)), Second);

    /// <summary>
    /// The whole repertoire in cycle order: fastball, second, third (PH-02-R5). This is a view, not
    /// state, so it is not serialized — a play trace records what a character throws through the
    /// two authored slots it round-trips.
    /// </summary>
    [JsonIgnore] public IReadOnlyList<string> Ordinary => [PitchFamily.Fastball, Second, Third];

    /// <summary>Slot 0 is the fastball, 1 the second pitch, 2 the third (PH-02-R5 cycle order).</summary>
    public string this[int slot] => slot switch
    {
        0 => PitchFamily.Fastball,
        1 => Second,
        2 => Third,
        _ => throw new ArgumentOutOfRangeException(
            nameof(slot), slot, $"a repertoire has {Slots} slots: fastball, second, third")
    };

    /// <summary>True when this character throws that family. The fastball is always true (PH-15-R1).</summary>
    public bool Has(string family) =>
        string.Equals(family, PitchFamily.Fastball, StringComparison.Ordinal)
        || string.Equals(family, Second, StringComparison.Ordinal)
        || string.Equals(family, Third, StringComparison.Ordinal);

    /// <summary>
    /// What a hand-built <see cref="Character"/> throws when a caller authors nothing — a fixture,
    /// not a roster default. Loaded data never reaches it: <c>ContentDataValidator</c> requires the
    /// <c>repertoire</c> field, so a missing or misspelled key is an error rather than a silent
    /// fallback (#807).
    /// </summary>
    public static Repertoire Default { get; } = new(PitchFamily.Changeup, PitchFamily.Curveball);

    static string Assignable(string family, string slot) =>
        PitchFamily.IsAssignable(family)
            ? family
            : throw new ArgumentException(
                string.Equals(family, PitchFamily.Fastball, StringComparison.Ordinal)
                    ? "every pitcher already throws the fastball, so it is never one of the two authored slots"
                    : $"'{family}' is not one of [{string.Join(", ", PitchFamily.Assignable)}]",
                slot);

    static string Different(string third, string second) =>
        string.Equals(third, second, StringComparison.Ordinal)
            ? throw new ArgumentException(
                $"the two authored pitches are different families; got '{third}' twice", nameof(Third))
            : third;
}

public sealed record Character(
    string Id,
    string Name,
    string Faction,
    bool Captain,
    Stats Stats,
    Hand Bats,
    Hand Throws,
    string StarPitch,
    string StarSwing,
    string FieldAbility,
    string Bio,
    /// <summary>
    /// Authored stand-up catch reach in feet (F693-02-catch-reach-envelope). Null takes the table's <c>standUpReachFt</c>.
    /// </summary>
    double? ReachFt = null)
{
    /// <summary>
    /// The three ordinary pitches this character throws (§4.3, PH-15-R1/R2/R4). Separate from
    /// <see cref="StarPitch"/>, which names a row in <c>data/abilities/star-skills.json</c> and sits
    /// outside the ordinary count: a character whose star is spelled <c>changeup</c> need not carry
    /// the changeup family, and the two are never resolved against each other
    /// (<see cref="PitchFamily"/>).
    ///
    /// An init property with a default rather than a constructor parameter, so a hand-built test
    /// character stays valid while every data row still has to author the field —
    /// <c>ContentDataValidator</c> requires it.
    /// </summary>
    public Repertoire Repertoire { get; init; } = GrandSluggers.Sim.Repertoire.Default;
}

/// <summary>A park's foul territory where it differs from <c>boundary.json</c> (F2-d): each member null keeps the table's.</summary>
public sealed record ParkFoul(double? OffsetFt = null, double? FlareStartFt = null, double? RailHeightFt = null);

/// <summary>A named outfield start (F2-d): feet from home, +Z toward centre.</summary>
public sealed record StartSpot(double X, double Z);

/// <summary>A park's named outfield starts (F2-d): each null keeps the default rule for that position.</summary>
public sealed record ParkOutfield(StartSpot? LF = null, StartSpot? CF = null, StartSpot? RF = null);

public sealed record Park(
    string Id,
    string Name,
    string Faction,
    string Surface,
    int LeftFenceFt,
    int CenterFenceFt,
    int RightFenceFt,
    double WindMph,
    IReadOnlyList<Hazard> Hazards,
    double WindDeg = 0,
    double FenceHeightFt = 8,
    /// <summary>
    /// What this park changes about the ball's air (§0.3 D21, FD-03). Null — no shipped or trial park
    /// names one — is the global table, so the park plays Harbor's air. Last, with a default, because
    /// <c>Park</c> is also built positionally (the flight probes).
    /// </summary>
    ParkEnvironment? Environment = null,
    /// <summary>
    /// Which ground each of this park's four zones names (§6.1, §16; FD-05). Null — no shipped or
    /// trial park names one — is the map derived from <see cref="Surface"/>, which is how the four
    /// zones read today (<see cref="GroundZones.Of(Park, RulesTable?)"/>). Last and defaulted for the
    /// same reason <see cref="Environment"/> is.
    /// </summary>
    ParkZones? Zones = null,
    /// <summary>
    /// The outfield fence as a polyline from pole to pole (§6.1, §16; FD-06 C, FD-06-R1, FD-12 B; F2-c).
    /// Null — no shipped or trial park names one — is the circle through the three posts at
    /// <see cref="FenceHeightFt"/>, exactly as every park has always played and drawn (<c>SF-06</c>).
    /// Last and defaulted for the same reason <see cref="Environment"/> is, and null is not written into
    /// <see cref="PlayTraceIdentity"/>, so no stored identity moves while no park names one.
    /// </summary>
    ParkFence? Fence = null,
    /// <summary>
    /// What this park is at night (§0.3, §14; FD-11 B, FD-11-R2; F4-d): the hazard instances that
    /// exist only at night. Null is a park whose night is its day. <b>Never read directly</b>: a match
    /// resolves it once (<see cref="PlayedPark.Of"/>), and every reader plays that park's
    /// <see cref="Hazards"/>, so the park a match holds carries no night block of its own. Last and
    /// defaulted for the same reason <see cref="Environment"/> is; null is not written into
    /// <see cref="PlayTraceIdentity"/>.
    /// </summary>
    ParkNight? Night = null,
    /// <summary>
    /// This park's foul territory (§6.1; FD-07 C; F2-d): any of the foul wrap's offset, the flare's start and the rail's height
    /// that differ from <c>boundary.json</c>. Null — no park names one — is the table's, exactly as every park has played.
    /// Last and defaulted, and null is not written into <see cref="PlayTraceIdentity"/>.
    /// </summary>
    ParkFoul? Foul = null,
    /// <summary>
    /// Where this park's outfielders start, when its shape needs its own (FD-07 C; F2-d; <c>SF-09</c>). Null, or a position it
    /// does not name, is the default rule: the global start's bearing and its fraction of the fence it was authored against,
    /// on this park's fence (<see cref="OutfieldStarts"/>). Last and defaulted; null is not written into the identity.
    /// </summary>
    ParkOutfield? OutfieldStarts = null)
{
    /// <summary>Where the wind blows toward, in the field frame: 0 out to CF, 90 toward the right-field line, 180 in at the plate.</summary>
    public (double X, double Z) WindDirection
    {
        get
        {
            var rad = WindDeg * Math.PI / 180.0;
            return (Math.Sin(rad), Math.Cos(rad));
        }
    }
}

/// <summary>
/// What one park changes about the air the ball flies through (§0.3 D21, FD-03; §16). Every field is
/// optional and <c>null</c> means the global table's number, so a park is Harbor plus the differences it
/// names and a park that names nothing resolves to the global table itself (<see cref="RulesTable.AtPark"/>,
/// SF-01). <b>Air only.</b> The ground (roll, bounce, skid) and the wall materials are not here — they
/// become zone and span rows of their own — and gravity, the three time scales, the plate and the infield
/// stay global in every park (D21).
/// </summary>
/// <param name="DragMul">
/// Multiplies the root's <c>flight.drag</c> rather than replacing it, so a trial root's drag stays the
/// trial's: thicker air (&gt; 1) is a shorter carry.
/// </param>
/// <param name="WindMul">
/// Replaces <c>flight.windMul</c>, how much of the flag reading the ball feels at field level. 0 is a park
/// the wind does not reach; the flag still reads <see cref="Park.WindMph"/>.
/// </param>
public sealed record ParkEnvironment(double? DragMul = null, double? WindMul = null)
{
    /// <summary>True when the park names at least one override. A park that names none plays the global table, by reference.</summary>
    public bool Names => DragMul is not null || WindMul is not null;
}

/// <summary>
/// Which ground a park's zones name (§6.1, §16; FD-05, F3-b). Every field is optional and <c>null</c>
/// means the default derived from the park's <c>surface</c>: <see cref="Outfield"/> and
/// <see cref="FoulApron"/> are the surface, <see cref="InfieldDirt"/> and <see cref="WarningTrack"/>
/// are <see cref="Ground.Dirt"/>. So a park is its surface plus the zones it overrides, and a park
/// that overrides none reads exactly as <c>surface</c> has always read.
///
/// <para>
/// <b>The zones, not their boundaries.</b> Where the lip, the track and the chalk are is the geometry
/// the field already has (<see cref="GroundZones"/>) and belongs to #730 / #732; this block only says
/// what each zone is made of. <b>No wall here</b> either: a span's material and its traits are the
/// polyline fence's (FD-06, F2-c).
/// </para>
///
/// <para>
/// <b>No shipped or trial park names one.</b> The first park with an unequal zone arrives with Crystal
/// (F9-a), as a trial, and every row of the library is today's number until then — so this block can
/// change no play in this child.
/// </para>
/// </summary>
/// <param name="InfieldDirt">The ground inside the infield lip.</param>
/// <param name="Outfield">The ground past the lip and short of the track. The park's <c>surface</c> when absent.</param>
/// <param name="WarningTrack">The ground within a track width of the fence.</param>
/// <param name="FoulApron">The ground outside the chalk, behind the plate included.</param>
public sealed record ParkZones(
    string? InfieldDirt = null,
    string? Outfield = null,
    string? WarningTrack = null,
    string? FoulApron = null)
{
    /// <summary>True when the park overrides at least one zone. A block that names none is the derived map.</summary>
    public bool Names =>
        InfieldDirt is not null || Outfield is not null || WarningTrack is not null || FoulApron is not null;
}

/// <summary>
/// A park's outfield fence as a polyline from the left-field pole to the right-field pole (§6.1, §16;
/// FD-06 C, FD-06-R1, FD-12 B; F2-c). Each point names where the fence stands and how tall it is there,
/// and the wall between two points is straight in the ground plane, with a top that runs straight from
/// one point's height to the next. So a park can have a notch, a porch or an alley, a tall wall or a
/// low corner.
///
/// <para>
/// <b>One distance per bearing (FD-06-R1).</b> The points run from bearing −45 to +45 with every bearing
/// strictly greater than the one before, which the park validator enforces by name on both roots. Every
/// ray from home then meets exactly one span, so <see cref="AtBatResolver.FenceAt"/> stays a function of
/// the bearing and every reader of it — the clip polygon, the track, the poles, the zone map, the drawn
/// loop — follows the polyline without learning a shape language. An overhang or a fence behind a fence
/// is refused, never drawn.
/// </para>
///
/// <para>
/// <b>Where a point is, in fence-relative units (FD-12).</b> A point's distance is
/// <see cref="FencePoint.FenceFrac"/> of the park's own three-post fence at that bearing — the circle
/// <see cref="AtBatResolver.FenceAt"/> answers for the park without points. The trial's posts are the
/// shipped posts through the one fence scale, so the same block lands at the same place relative to the
/// fence on both roots: the migration the zone rule makes of a point beyond the lip, with a point at
/// <c>1.0</c> exactly on each root's own fence. Heights do not scale (walls did not shrink).
/// </para>
///
/// <para>
/// Equality is by value, point for point, because the polygon and the drawn loop are cached on it:
/// two parks with the same posts and the same points are the same field, and a park with other points
/// is another field even when its list object is new (map finding 14).
/// </para>
/// </summary>
/// <param name="Points">The fence from the left-field pole to the right-field pole. Validated before a catalog holds it.</param>
public sealed record ParkFence(IReadOnlyList<FencePoint> Points)
{
    /// <summary>The wall material of span <paramref name="span"/> (from point <c>span</c> to <c>span + 1</c>): its first point's, else <c>padded</c>.</summary>
    public string SpanMaterial(int span) => Points[span].Material ?? WallMaterial.Padded;

    public bool Equals(ParkFence? other) =>
        other is not null && (ReferenceEquals(this, other) || Points.SequenceEqual(other.Points));

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var point in Points) hash.Add(point);
        return hash.ToHashCode();
    }
}

/// <summary>
/// One point of a <see cref="ParkFence"/> (FD-06 C, FD-12 B).
/// </summary>
/// <param name="BearingDeg">Degrees from centre field, the spray frame: −45 the left-field line, 0 centre, +45 the right-field line.</param>
/// <param name="FenceFrac">How far out the fence stands here, as a fraction of the park's three-post fence at this bearing: 1.0 is on today's arc.</param>
/// <param name="HeightFt">The fence top here, in feet. It must stand over the foul rail. Not scaled between roots.</param>
/// <param name="Material">
/// The wall material of the span from this point to the next — a <c>walls.json</c> row. Null is
/// <see cref="WallMaterial.Padded"/>. The right-field pole ends the fence, so its point names none.
/// </param>
public sealed record FencePoint(double BearingDeg, double FenceFrac, double HeightFt, string? Material = null);

/// <summary>
/// A park's night block (§0.3, §14, §16; FD-11 B, FD-11-R2, FD-10-R1; F4-d). Night keeps the stadium
/// lights, so play visibility is the day's: night changes only the view outside the stadium and the
/// hazards. The block may therefore carry <b>hazard instances</b> and, when F6-c defines them, look
/// fields — and never a rule that changes the at-bat, the flight, the ground or the bodies, which the
/// park validator refuses by name.
///
/// <para>
/// <see cref="Hazards"/> are in the day block's shape and pass the day block's rules (the type library,
/// the strict read, the FD-19 placement rule at the disc they play at night), and each is an instance of
/// a pattern that counts as a hazard (<see cref="HazardPattern.IsHazard"/>), so the hazards-off switch
/// removes every one of them (FD-10-R1). A hazard type's own night number (<c>fireBreath.nightRadiusMul</c>)
/// is the type's row, not this block's: it widens a day instance at night wherever it is authored.
/// </para>
/// </summary>
/// <param name="Hazards">The instances that exist only at night, in the order the file lists them. Played after the day's.</param>
public sealed record ParkNight(IReadOnlyList<Hazard> Hazards);

/// <summary>
/// The fence where the ray from home at one bearing meets it (§6.1; FD-06): how far out it stands, how
/// tall it is there and what the span there is made of. One resolution answers all three
/// (<see cref="AtBatResolver.FenceSpotAt"/>), so the distance the flight clips at, the top it clears and
/// the row it caroms off cannot come from three different fences.
/// </summary>
public readonly record struct FenceSpot(double DistanceFt, double TopFt, string Material);

public sealed record Hazard(string Type, double X, double Z, double Radius, string? Tag);

public sealed record BatItem(
    string Id,
    string Name,
    int ContactMod,
    int PowerMod,
    bool ChargeAlwaysFull,
    string Visual = "bat-wood");

public sealed record GloveItem(
    string Id,
    string Name,
    double ErrorReduction,
    int ArmMod,
    string Visual = "glove-brown");

/// <summary>
/// A nine. <paramref name="Gloves"/> is the lineup's glove diamond from Offense / Defense Setup
/// (position → player, §8.1); null means the roster order stands in for it (the preset teams).
/// </summary>
public sealed record Team(
    string Name,
    Character Captain,
    IReadOnlyList<Character> Roster,
    IReadOnlyList<Character>? Order = null,
    Character? Starter = null,
    IReadOnlyDictionary<string, Character>? Gloves = null)
{
    public IEnumerable<Character> Everyone => Roster;

    public Character Pitcher => Starter ?? Captain;

    public IReadOnlyList<Character> BattingOrder =>
        Order is { Count: > 0 } o ? o : DefaultBattingOrder(Captain, Roster);

    public static IReadOnlyList<Character> DefaultBattingOrder(Character captain, IReadOnlyList<Character> roster)
    {
        var rest = roster.Where(c => !c.Id.Equals(captain.Id, StringComparison.OrdinalIgnoreCase)).ToList();
        var order = new List<Character>(roster.Count);
        order.AddRange(rest.Take(3));
        order.Add(captain);
        order.AddRange(rest.Skip(3));
        return order;
    }
}

/// <summary>
/// One swing at one crossing. <paramref name="CrossingX"/> / <paramref name="CrossingY"/> are the
/// pitch at the plate plane in world feet (the same point the umpire and the aim tell read);
/// <paramref name="TimingErrorFrames"/> is the press minus the square press (the ball's plate time
/// less <c>batting.window.leadSec</c>) at 60 Hz, D13.
/// </summary>
public sealed record AtBatInput(
    Character Pitcher,
    Character Batter,
    Character? OnDeck,
    IReadOnlyList<Character> RunnersOn,
    bool ChargePitch,
    bool ChangeupPitch,
    double TimingErrorFrames,
    bool UseStarPitch,
    bool UseStarSwing,
    BatItem? Bat,
    int PitcherStamina,
    double SprayAimDeg = 0,
    bool PitchInZone = true,
    bool Bunt = false,
    double LaunchAim = 0,
    double Charge01 = 0,
    double BoxOffsetX = 0,
    double CrossingX = 0,
    double CrossingY = PitchFlight.PlateY,
    BuntSide BuntSide = BuntSide.None);

public sealed record AtBatResult(
    ContactQuality Quality,
    bool InPlay,
    bool Strike,
    double ExitVeloMph,
    double LaunchDeg,
    double CarryFt,
    bool HomeRun,
    bool ChemistryItemOffered,
    string? StarPitchUsed,
    string? StarSwingUsed,
    double SprayDeg = 0,
    bool Foul = false,
    bool InZone = true,
    /// <summary>The batted ball's shape from the one flight (§6.2: topper … homer, bunt). <see cref="Foul"/> is the chalk.</summary>
    BattedBallClass Class = BattedBallClass.Fly);

/// <summary>
/// One pitch (spec §4.1 – §4.3). <paramref name="Type"/> is the <b>family id</b> from the shared
/// library (<see cref="PitchFamily"/>): today <c>fastball</c> or <c>changeup</c>, tomorrow one of the
/// other three. The name stays <c>Type</c> because it is what traces and the Unity client already
/// serialize. Charge and break are modifiers on the family, never families of their own (PH-02-R1).
/// Location is the rubber walk (<paramref name="RubberX"/>, world feet per
/// <see cref="HomeSet.PitcherWalk"/>) and the stick after release (<paramref name="BreakX"/>, −1..1,
/// capped at half a zone); <paramref name="AimX"/> / <paramref name="AimY"/> are the CPU's plate-aim
/// target. <paramref name="Nice"/> is a release inside the Nice band of MAX
/// (+5% mph).
///
/// The id is <b>not</b> checked here. The record is deserialized from stored traces and built on the
/// hot path, and a constructor that threw would turn reading an old stream into a crash; instead
/// every read of the family goes through <see cref="PitchFamilyTable.Of"/> — the flight, the speed
/// and the stamina — so an id with no authored row stops the pitch the first time anything asks what
/// it does, and names itself while doing it (#810).
///
/// <paramref name="Throws"/> is the arm this delivery left, stamped by <see cref="Match.PreparePitch"/>
/// from the pitcher on the mound and settable by a harness. It is the only thing the flight knows
/// about the pitcher's hand, and it exists because a family's natural sweep mirrors with the arm
/// (<see cref="PitchFlight.SweepShiftFt"/>, #818). It is last and defaulted so every positional call
/// site and every stored command that predates it still reads, as a right-hander's.
/// </summary>
public sealed record PitchCommand(
    string Type,
    double Charge01,
    bool Star,
    double AimX = 0,
    double AimY = 0,
    double BreakX = 0,
    double RubberX = 0,
    bool DeliveryPrepared = false,
    bool Nice = false,
    double BreakMul = 1,
    Hand Throws = Hand.R);

public sealed record SwingCommand(
    bool Swing,
    double Charge01,
    double TimingErrorFrames,
    bool Star,
    double SprayAimDeg = 0,
    bool Bunt = false,
    double LaunchAim = 0,
    double BoxOffsetX = 0,
    /// <summary>A seat's pad pressed it, not the CPU batter. No rule reads it: one window for every seat (PH-17).</summary>
    bool Human = false,
    /// <summary>
    /// The square clock at the plate time (§5.8, §7.3): play seconds the batter had been squared (West held), wound back
    /// down if they let go; 0 is no square. The defense reads the square as a tell before the pitch: the corners crash and
    /// the middle covers for this long (<see cref="BuntDefense.Spots"/>). It is independent of <see cref="Bunt"/>: a
    /// square pulled back into a swing still left the corners in, and a late press with no square is a bunt nobody read.
    /// </summary>
    double SquareSec = 0,
    /// <summary>
    /// The held bunt's side at contact (§5.8, PH-14-R2): a typed fact the defense reads and the ball leans toward
    /// (<see cref="BuntHold.LeanDeg"/>). <see cref="BuntSide.None"/> on every swing that is not a bunt.
    /// </summary>
    BuntSide BuntSide = BuntSide.None)
{
    /// <summary>
    /// The held bunt at the plate (§5.8, PH-14-R4): the bat is already on the plane, so there is no timed press
    /// (the timing error is 0) and no charge; the side is the held one. Both seats and the CPU sac bunt build it here.
    /// </summary>
    public static SwingCommand HeldBunt(BuntSide side, double boxOffsetX, double squareSec = 0, bool human = true) =>
        new(true, 0, 0, false, Bunt: true, BoxOffsetX: boxOffsetX, Human: human, SquareSec: squareSec, BuntSide: side);
}

public enum PlayKind
{
    TakeBall,
    TakeStrike,
    SwingMiss,
    Foul,
    GroundOut,
    FlyOut,
    Single,
    Double,
    Triple,
    HomeRun,
    Walk,
    HitByPitch,
    Strikeout,
    StolenBase,
    CaughtStealing,
    /// <summary>A pickoff throw with every runner on their bag (§4.5, D3): a beat, no play, no count, no stamp.</summary>
    Pickoff,
    /// <summary>The ball is live and nobody has decided it yet: Complete names the play from the bodies (§10.6). Never stamped.</summary>
    InPlay,
    /// <summary>A committed pitcher abandoned the pitch: dead ball, runners advance, count unchanged.</summary>
    Balk
}

/// <summary>The catch feat on the typed outcome (§8.4): what the glove did to make the catch. The stamp reads it (§15).</summary>
public enum DefensiveFeat
{
    None,
    BuddyJump,
    SuperJump,
    Clamber,
    /// <summary>A plain jump catch in the window (West), not a wall rob.</summary>
    Jump,
    /// <summary>A dive catch or a dive scoop (East).</summary>
    Dive
}

public enum RunnerPlayResult
{
    None,
    StolenBase,
    CaughtStealing,
    PickedOff
}

public enum ThrowOrigin
{
    None,
    Catcher,
    PitcherRubber
}

/// <summary>The baseball endpoints of a resolved throw, independent of its presentation copy.</summary>
public sealed record ThrowEndpoint(ThrowOrigin Origin, int DestinationBag);

/// <summary>The five ways (spec §10.1). Caught stealing, picked off, and doubled off are tags or forces.</summary>
public enum OutType
{
    Strikeout,
    Catch,
    Force,
    Tag,
    ThrowOutAtFirst
}

/// <summary>
/// One out on the play: who, how, where. <paramref name="FromBag"/> 0 is the batter-runner.
/// <paramref name="Bag"/> is where it was made: 1..4, or 0 at the plate / in the field.
/// </summary>
public sealed record OutRecord(OutType Type, int Bag, int FromBag, Character Runner, Character? Fielder);

/// <summary>A runner's placement on the play. <paramref name="FromBag"/> 0 is the batter; <paramref name="ToBag"/> 4 scored.</summary>
public sealed record RunnerMove(Character Runner, int FromBag, int ToBag);

/// <summary>
/// A body on the field where it stood when the play died (§10.6, #574). <paramref name="Pos"/> is the
/// glove ("SS", "CF" …) or <see cref="Runner"/> for a live runner. The result beat is drawn from these;
/// nothing re-places a body from the position table until the next SET.
/// </summary>
public sealed record FieldBody(string Pos, Character Who, double X, double Z)
{
    public const string Runner = "runner";
    public bool IsRunner => Pos == Runner;
}

/// <summary>
/// Typed facts from a resolved play. Presentation, highlights, and the scenario harness read
/// these; the caption is produced from them last and is never read back (spec §15).
/// </summary>
public sealed record PlayOutcome(
    DefensiveFeat DefensiveFeat = DefensiveFeat.None,
    RunnerPlayResult RunnerResult = RunnerPlayResult.None,
    int RunnerFromBag = 0,
    int RunnerToBag = 0,
    ThrowEndpoint? ThrowEndpoint = null,
    IReadOnlyList<OutRecord>? Outs = null,
    IReadOnlyList<RunnerMove>? Advances = null,
    int BatterToBag = 0,
    bool Error = false,
    bool FieldersChoice = false,
    bool GroundRuleDouble = false,
    IReadOnlyList<FieldBody>? Bodies = null,
    IReadOnlyList<StarRequest>? StarRequests = null)
{
    public static PlayOutcome Empty { get; } = new();

    /// <summary>Every Star Pitch and Star Swing released on this pitch, as the match settled it (§12, PH-16-R12).</summary>
    public IReadOnlyList<StarRequest> Stars => StarRequests ?? [];

    /// <summary>Every body on the field at Time, where it stood (§10.6, #574): gloves and live runners.</summary>
    public IReadOnlyList<FieldBody> BodiesAtTime => Bodies ?? [];

    /// <summary>Every out on the play, in the order it was made.</summary>
    public IReadOnlyList<OutRecord> OutsMade => Outs ?? [];

    /// <summary>Every runner who changed bags, including the batter-runner (from bag 0).</summary>
    public IReadOnlyList<RunnerMove> Moves => Advances ?? [];
}

public enum StarAction
{
    Pitch,
    Swing
}

/// <summary>
/// One released special as <see cref="Match"/> settled it (§12, PH-16-R3, PH-16-R12). <paramref name="Afforded"/>
/// false is the typed "special unavailable" event: the team held fewer Stars than <paramref name="Cost"/>, so the
/// action went out as the ordinary pitch or swing at the same timing and nothing was spent. Afforded, the team paid
/// <paramref name="Cost"/> at the release, whatever the swing then met — a miss pays in full.
/// <paramref name="Home"/> names the team whose pool it read.
/// </summary>
public sealed record StarRequest(
    StarAction Action,
    bool Home,
    string CharacterId,
    string? AbilityId,
    int Cost,
    double StarsBefore,
    bool Afforded)
{
    /// <summary>What left the pool: the cost when afforded, nothing when the special was unavailable.</summary>
    public int Spent => Afforded ? Cost : 0;
}

/// <summary>The actors and match state at the start of one pitch or pickoff play.</summary>
public sealed record PlayContext(
    string BatterId,
    string PitcherId,
    int Inning,
    bool Top,
    int OutsBefore,
    int BallsBefore,
    int StrikesBefore,
    int AwayScoreBefore,
    int HomeScoreBefore,
    string? FirstRunnerId,
    string? SecondRunnerId,
    string? ThirdRunnerId);

/// <summary>The match state after the completed event and any lineup, half, or game transition.</summary>
public sealed record MatchState(
    string BatterId,
    string PitcherId,
    int Inning,
    bool Top,
    int Outs,
    int Balls,
    int Strikes,
    int AwayScore,
    int HomeScore,
    bool Over,
    string? FirstRunnerId,
    string? SecondRunnerId,
    string? ThirdRunnerId);

public sealed record PlayEvent(
    PlayKind Kind,
    AtBatResult AtBat,
    PitchCommand Pitch,
    SwingCommand Swing,
    Character Batter,
    Character Pitcher,
    Character? Fielder,
    ThrowResult? Throw,
    int RunsScored,
    IReadOnlyList<string> Scorers,
    string Caption,
    bool Heatball,
    bool Furnace,
    double HangTimeSec,
    double LandingX,
    double LandingZ,
    int OutsAfter,
    int AwayScoreAfter,
    int HomeScoreAfter,
    int OutsOnPlay = 0,
    PlayContext? Context = null,
    MatchState? NextState = null,
    PlayOutcome? Outcome = null);

/// <summary>What happened to the ball at this sample of the clipped path (spec §6.1).</summary>
public enum SampleEvent
{
    None,
    /// <summary>Ground contact (first grass, a hop, or the roll).</summary>
    Ground,
    /// <summary>Met the outfield fence below fence height: the carom starts here.</summary>
    Wall,
    /// <summary>Crossed the outfield fence above fence height between the poles: gone.</summary>
    Fence,
    /// <summary>Left the field over a foul wall or the backstop: dead in the stands.</summary>
    Stands,
    /// <summary>Touched a foul wall or the backstop below its top: foul, dead; the carom is for the camera.</summary>
    FoulWall
}

/// <summary>
/// One sample of the ball's clipped path. <see cref="T"/> is play seconds — the same clock
/// <see cref="LivePlaySystem.ElapsedSeconds"/> runs gloves and runners on (spec §6.1, one clock).
/// <see cref="Dist"/> is the horizontal distance from the plate; <see cref="X"/> / <see cref="Z"/> are the
/// field position (the wind bends the path, so the spray is not constant along it).
/// </summary>
public readonly record struct Sample(double T, double Dist, double Height, double X = 0, double Z = 0, SampleEvent Event = SampleEvent.None);

/// <summary>
/// One throw's input (§8.5): the pair's chemistry, the speed multiplier (arm × chemistry × ability)
/// the one throw clock flies it on, whether bad chemistry slanted it, the signed lateral miss in
/// feet at the target, and the thrower's Arm rating, which sets the comfortable range the long-throw
/// loss is measured from (#722). Whether it is caught is the receiver's radius, decided where it lands.
/// </summary>
public sealed record ThrowResult(
    Chemistry Relation,
    double SpeedMul,
    bool Slanted,
    double LateralFt = 0,
    int Arm = InPlay.NeutralArm,
    /// <summary>A release for this throw other than the table's (#723): Snap Throw's after a clean received teammate throw. Null is the ordinary release.</summary>
    double? ReleaseSec = null);

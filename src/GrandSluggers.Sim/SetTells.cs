namespace GrandSluggers.Sim;

/// <summary>
/// SET dirt tells as product state. Charge ring, zone locator, and ball trail
/// are queryable without F2. The ring appears after 0.15 of the button load.
/// </summary>
public static class SetTells
{
    public const double ChargePull = 0.15;

    public static bool RingOn(double charge01) => charge01 >= ChargePull;

    /// <summary>In-play YOU foot ring. Same gold language as charge, always on that glove.</summary>
    public const double YouRingScale = 4.4;

    public static bool YouRingOn(bool you, double charge01) => you || RingOn(charge01);

    public static double LiveRingScale(bool you, double charge01) =>
        RingOn(charge01) ? RingScale(charge01) : you ? YouRingScale : 0;

    /// <summary>
    /// Major radius in feet. Small values sat under the mesh as a gold pancake.
    /// Pull already clears the toy; max grows around the box.
    /// </summary>
    public static double RingScale(double charge01)
    {
        var u = Math.Clamp(charge01, 0, 1);
        return RingOn(u) ? 3.6 + u * 2.0 : 0;
    }

    /// <summary>Bottom of the tube sits on packed dirt, not on the toy's waist.</summary>
    public const double RingHeightFt = 0.06;

    /// <summary>Torus tube radius. 0.045 vanished from the plate 3/4.</summary>
    public const double RingThickFt = 0.16;

    /// <summary>Home pad / batter's box top. Ring sits on this, not at Y=0 under the mesh.</summary>
    public const double BoxDirtY = 0.26;

    /// <summary>Mound hill / rubber top. Pitching SET uses the same ring language here.</summary>
    public const double RubberDirtY = 0.96;

    /// <summary>
    /// World Y of the torus center at the player's feet. Packed dirt, never chest
    /// or a child of hero lift/grow. Presentation must not parent this to the toy.
    /// </summary>
    public static double RingWorldY(double feetZ = 0, double heroY = 0, double lift = 0)
    {
        _ = heroY;
        _ = lift;
        var dirt = Math.Abs(feetZ - Diamond.Mound) < 12 ? RubberDirtY : BoxDirtY;
        return dirt + RingThickFt;
    }

    public static (double X, double Y, double Z) RingAt(
        double feetX, double feetZ, double heroY = 0, double lift = 0) =>
        (feetX, RingWorldY(feetZ, heroY, lift), feetZ);

    public static bool ZoneOn(bool setOrFlight) => setOrFlight;

    /// <summary>
    /// The pitcher's aim tell: the crossing of the pitch as it stands, in world feet at the plate
    /// plane. Walking the rubber moves it with the body; the stick moves it during flight; it is
    /// the same point the umpire judges (spec §4.4, #577).
    /// </summary>
    public static (double X, double Y) Locator(PitchCommand pitch, RulesTable rules, string? starPitchId = null) =>
        PitchFlight.Crossing(pitch, rules, starPitchId);

    /// <summary>
    /// The ordinary-play SET ring (PH-06, PH-06-R1): <b>where the pitcher stands</b>, not where the
    /// pitch will cross. X is the rubber walked into world feet (<see cref="HomeSet.PitcherWalk"/>
    /// per unit, the same distance the body moves, §4.2); Y is the middle of the strike frame, which
    /// is a fixed height and says nothing about the family.
    ///
    /// <para>
    /// It exists because <see cref="Locator"/> cannot be drawn in SET any more. The crossing now
    /// carries the family's own drop and natural sweep (§4.2, §4.3), so a ring at the crossing tells
    /// the whole shared screen which pitch was selected before it leaves the hand — exactly what
    /// PH-02-R5's family-blind SET forbids. <see cref="Locator"/> stays for Practice and the
    /// Tutorials, where the point is to show the player what a shape does (PH-19).
    /// </para>
    /// </summary>
    /// <param name="rubberX">The pitcher's rubber position, −1..1 (<c>Match.PitcherOffsetX</c>).</param>
    public static (double X, double Y) RubberRing(double rubberX) =>
        (rubberX * HomeSet.PitcherWalk, StrikeZoneGeometry.CenterY);

    public static bool InZone(PitchCommand pitch, RulesTable rules, string? starPitchId = null) =>
        StrikeZoneGeometry.Contains(pitch, rules, starPitchId);

    /// <summary>The tell is the pitcher's: shown on the pitching seat, never to the batter as a giveaway.</summary>
    public static bool AimTellOn(bool humanPitches, bool setOrFlight) => humanPitches && setOrFlight;

    /// <summary>
    /// When the SET ring is drawn at all (PH-06-R1). In ordinary play it is <b>SET only</b> — it
    /// hides at release, because the ball itself is the cue in flight and a ring that slid with the
    /// break would draw the crossing again. Practice and the Tutorials may keep it through the
    /// flight (PH-19): there the shape is the lesson.
    /// </summary>
    /// <param name="humanPitches">A human sits the mound this half.</param>
    /// <param name="set">The at-bat is in SET (not the windup, not the flight).</param>
    /// <param name="flight">The pitch is in the air.</param>
    /// <param name="teaching">Practice or a Tutorial owns the screen.</param>
    public static bool AimTellOn(bool humanPitches, bool set, bool flight, bool teaching) =>
        humanPitches && (set || (flight && teaching));

    public static bool TrailOn(bool flight) => flight;

    /// <summary>Seconds of streak. 0.28 vanished on plate SET.</summary>
    public const double TrailSeconds = 0.62;

    /// <summary>Start width as a fraction of the live ball diameter.</summary>
    public const double TrailStartMul = 0.62;

    public const double TrailEndMul = 0.10;

    public static double TrailStartFt(double diameter) =>
        Math.Max(0.28, diameter * TrailStartMul);

    public static double TrailEndFt(double diameter) =>
        Math.Max(0.06, diameter * TrailEndMul);
}

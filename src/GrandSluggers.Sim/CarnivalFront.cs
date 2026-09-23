namespace GrandSluggers.Sim;

/// <summary>
/// Exhibition front of house: the park is the poster, captains are toys, the field is a postcard.
/// HUD draws this copy; tests lock it. Not a second UI toolkit.
/// </summary>
public static class CarnivalFront
{
    public const string Logo = "GRAND SLUGGERS";
    public const string PlayBall = "South / Space    play ball";
    public const string YouAreHome = "HOME";
    public const string YouAreAway = "AWAY";

    public static string SeatMark(bool pad1Home) => pad1Home ? YouAreHome : YouAreAway;

    public static string SeatHint(bool pad1Home) =>
        pad1Home ? "You pitch the top. You bat the bottom." : "You bat the top. You pitch the bottom.";

    public const string OnePlayer = "1 PLAYER";
    public const string TwoPlayers = "2 PLAYERS";
    public const string SelectHelp =
        "LB 1 player    RB 2 players    L/R your team    U/D the other    North HOME/AWAY    South the field    West title";
    public const string PlugPad2 = "Plug in controller 2. Until then you play the CPU.";

    public static string SeatModeLabel(bool versus) => versus ? TwoPlayers : OnePlayer;

    public static string SeatModeHint(bool versus, bool pad2, bool pad1Home)
    {
        if (!versus) return SeatHint(pad1Home);
        if (!pad2) return PlugPad2;
        return pad1Home ? "Controller 1 home. Controller 2 away." : "Controller 1 away. Controller 2 home.";
    }

    /// <summary>1 PLAYER / 2 PLAYERS tabs on pick captain. Right of the HUD card.</summary>
    public static (float X, float Y, float W, float H) SeatModeBar(float screenW, float screenH)
    {
        const float w = 440f;
        const float h = 44f;
        return (Math.Max(360f, screenW - 36f - w), 18f, w, h);
    }

    public static (float X, float Y, float W, float H) SeatModeTab(bool versus, float screenW, float screenH)
    {
        var bar = SeatModeBar(screenW, screenH);
        var w = (bar.W - 8f) * 0.5f;
        return versus
            ? (bar.X + w + 8f, bar.Y, w, bar.H)
            : (bar.X, bar.Y, w, bar.H);
    }

    public static bool? HitSeatMode(float mx, float my, float screenW, float screenH)
    {
        if (Inside(SeatModeTab(false, screenW, screenH), mx, my)) return false;
        if (Inside(SeatModeTab(true, screenW, screenH), mx, my)) return true;
        return null;
    }

    static bool Inside((float X, float Y, float W, float H) r, float mx, float my) =>
        mx >= r.X && mx <= r.X + r.W && my >= r.Y && my <= r.Y + r.H;

    public const float TitleRowZ = 26f;
    public const float SelectRowZ = 12f;
    public const float HomeStepSelectFt = 4f;
    public const float FeaturedSelectZ = 8f;
    /// <summary>
    /// Title is wordmark + dirt + UI. Cheer does not read as a poster (#685).
    /// Flip this only with a spec decision to put a body back.
    /// </summary>
    public const bool TitleShowsCaptain = false;
    /// <summary>Chest. Y=4.4 at Z=4 is Ashlord's brim.</summary>
    public const float SelectLookY = 2.6f;
    /// <summary>Chest-height. Y=7.8 looking at Z=8 from Z=-12 is the plate berm.</summary>
    public const float SelectCamMinY = 3.6f;
    public const float SelectCamMaxY = 6.0f;
    /// <summary>Downward slope (ΔY/ΔZ). 5.2/20 from the berm shot.</summary>
    public const float SelectMaxDown = 0.18f;
    /// <summary>Over the infield. Z=7 Y=8.8 sat on Rio's hat when the title had a toy.</summary>
    public const float LogoX = 0.8f;
    public const float LogoY = 12.2f;
    public const float LogoZ = 15.6f;
    public const float SelectSpacing = 7.6f;
    public const float TitleSpacing = 13.4f;
    public const float CardX = 5.6f;
    public const float CardY = 2.9f;
    public const float CardZ = 0.2f;
    /// <summary>Dirt under the select toys. Cheer bob and steal-lead crouch are field takes.</summary>
    public const float SelectDirtY = 0f;

    /// <summary>
    /// Select stands. Featured is the step-forward + highlight, not a field verb.
    /// Cheer (#628) bobs the root; stealLead sinks. Both put a foot under the mesh.
    /// </summary>
    public static Motion.Verb SelectPose(bool yours, bool theirs)
    {
        _ = yours;
        _ = theirs;
        return Motion.Verb.Idle;
    }

    public static bool SelectStaysOnDirt(Motion.Verb verb) => verb == Motion.Verb.Idle;

    /// <summary>
    /// World Y of an extra at the select plant. Foot extras share the dirt.
    /// A sinking take puts them under. Highlight scale is around the feet, so it
    /// does not push a planted extra through the mesh.
    /// </summary>
    public static double SelectExtraMinY(ExtraSlot extra, double rootScaleY, bool highlighted, Motion.Verb pose)
    {
        var plant = SelectStaysOnDirt(pose) ? SelectDirtY : SelectDirtY - Math.Max(rootScaleY, 1);
        if (FootExtra(extra)) return plant;
        return plant + (highlighted ? 0.5 : 0.4);
    }

    static bool FootExtra(ExtraSlot extra) =>
        extra.Bone.Equals("lFoot", StringComparison.OrdinalIgnoreCase)
        || extra.Bone.Equals("rFoot", StringComparison.OrdinalIgnoreCase);

    public static (float X, float Z) CaptainSpot(int index, int count, bool select, bool home)
    {
        var spacing = select ? SelectSpacing : TitleSpacing;
        var x = (index - (count - 1) * 0.5f) * spacing;
        var z = select ? SelectRowZ : TitleRowZ;
        if (select && home) return (0f, FeaturedSelectZ);
        return (x, z);
    }

    /// <summary>Select places the row. Title places no body while TitleShowsCaptain is false.</summary>
    public static bool TitlePlacesBody(bool select) => select || TitleShowsCaptain;

    public static (float X, float Y, float Z) SelectLook(int index, int count)
    {
        var spot = CaptainSpot(index, count, select: true, home: true);
        return (spot.X, SelectLookY, spot.Z);
    }

    /// <summary>
    /// Select sits at chest height and looks at the toy. High-home looking down
    /// at Z=8 is the packed-dirt berm. Y=4.4 at Z=4 is Ashlord's brim.
    /// </summary>
    public static bool SelectCamIsTheToy(double camY, double camZ)
    {
        if (camY < SelectCamMinY || camY > SelectCamMaxY) return false;
        if (camZ >= 0 || camZ <= -20) return false;
        var dy = camY - SelectLookY;
        var dz = FeaturedSelectZ - camZ;
        return dz > 0 && dy / dz < SelectMaxDown;
    }

    public static Vec3 TitleLogoAt => new(LogoX, LogoY, LogoZ);
    /// <summary>Ink on local −Z, the camera side of a board that looks into the park (#696).</summary>
    public const float TitleLogoInkZ = -0.05f;
    public const float TitleLogoGlyphZ = -0.11f;

    /// <summary>
    /// The board looks into the park with the title camera, not at the camera.
    /// LookRotation(camera − logo) puts local +X on the camera's left and mirrors
    /// GRAND SLUGGERS (#696).
    /// </summary>
    public static Vec3 TitleLogoForward(Vec3 cam, Vec3 logo)
    {
        var dx = logo.X - cam.X;
        var dz = logo.Z - cam.Z;
        var n = Math.Sqrt(dx * dx + dz * dz);
        if (n < 1e-6) return new Vec3(0, 0, 1);
        return new Vec3(dx / n, 0, dz / n);
    }

    public static bool TitleLogoReads(Vec3 cam, Vec3 logo)
    {
        var fwd = TitleLogoForward(cam, logo);
        if (fwd.Z <= 0) return false;
        var atCamX = cam.X - logo.X;
        var atCamZ = cam.Z - logo.Z;
        return fwd.X * atCamX + fwd.Z * atCamZ < 0;
    }

    /// <summary>
    /// Title is a sticker over the diamond. Fail if the board is a menu wall,
    /// a featured cheer is back without a spec decision (#685), or the wordmark
    /// is mirrored (#696).
    /// </summary>
    public static bool TitlePoster(Vec3 cam, Vec3 look)
    {
        var logo = TitleLogoAt;
        return !TitleShowsCaptain
            && TitleLogoReads(cam, logo)
            && OffLook(cam, look, logo) < 20
            && LogoY > 10
            && Math.Abs(LogoX) < 8
            && LogoZ > 0
            && LogoZ < 20;
    }

    public static double OffLook(Vec3 pos, Vec3 target, Vec3 p)
    {
        var lx = target.X - pos.X;
        var ly = target.Y - pos.Y;
        var lz = target.Z - pos.Z;
        var dx = p.X - pos.X;
        var dy = p.Y - pos.Y;
        var dz = p.Z - pos.Z;
        var ln = Math.Sqrt(lx * lx + ly * ly + lz * lz);
        var dn = Math.Sqrt(dx * dx + dy * dy + dz * dz);
        if (ln < 1e-6 || dn < 1e-6) return 180;
        var dot = Math.Clamp((lx * dx + ly * dy + lz * dz) / (ln * dn), -1, 1);
        return Math.Acos(dot) * 180 / Math.PI;
    }

    public static string SkyGag(bool night) => night ? "NIGHT" : "DAY";

    /// <summary>The difficulty rung as the title prints it (cpu.json easy / normal / hard).</summary>
    public static string DifficultyLabel(string level) => (CpuRules.IsLevel(level) ? level : "normal").ToUpperInvariant();

    /// <summary>The two numbers the title owns, side by side: innings and the CPU rung.</summary>
    public static string TitleSetup(int innings, string level) => $"{innings} INNINGS  ·  {DifficultyLabel(level)}";

    /// <summary>
    /// The title's difficulty line with what the rung changes: the CPU's skill, never a pad's swing
    /// window (PH-17, spec §5.3 — one window for every hitter, swing and rung since #860). The
    /// rules table stays in the signature so the title's call site does not move.
    /// </summary>
    public static string TitleSetup(int innings, string level, RulesTable rules) =>
        TitleSetup(innings, level) + "  ·  CPU SKILL";

    /// <summary>
    /// The title's line with the hazards switch at its end (FD-10, §14): the match option, default on, next to the other
    /// things the title owns.
    /// </summary>
    public static string TitleSetup(int innings, string level, RulesTable rules, bool hazards) =>
        TitleSetup(innings, level, rules) + "  ·  " + HazardsLabel(hazards);

    /// <summary>The field postcard's footer: every verb on the screen, the hazards switch beside night.</summary>
    public const string FieldFooter = "stick L/R the field    South lineup    West captains    N night    R hazards    Esc how to play";

    /// <summary>What the caption calls the redirect the ball went through (F4-c). Copy, not a rule.</summary>
    public static string RedirectName(string? type) => type switch
    {
        HazardType.Barrel => "barrel cannon",
        HazardType.Chomper => "chomper",
        _ => "warp can"
    };

    /// <summary>The hazards switch as the title and the field postcard print it.</summary>
    public static string HazardsLabel(bool hazards) => hazards ? "HAZARDS ON" : "HAZARDS OFF";

    /// <summary>
    /// What hazards off leaves at a park (FD-10-R1): the postcard's line under the park's own gimmick. Off removes the
    /// park's hazards and keeps its fence, walls, air and ground. Null with hazards on, and at a park that has no hazard
    /// tonight (read from the played park, <see cref="PlayedPark.Of"/>), where the switch changes nothing.
    /// </summary>
    public static string? HazardsOffLine(Park park, bool night, bool hazards, HazardRules library)
    {
        if (hazards) return null;
        // The switch's own rule decides what it removes: a park it changes has fewer instances off than on.
        var on = PlayedPark.Of(park, night, hazards: true, library).Hazards.Count;
        return PlayedPark.Of(park, night, hazards: false, library).Hazards.Count < on ? HazardsOffCopy : null;
    }

    /// <summary>The postcard's line with hazards off at a park that has one.</summary>
    public const string HazardsOffCopy = "Hazards off. The fence, the walls, the air and the ground stay.";

    public static bool HarborIsTheProduct(string parkId) =>
        parkId.Equals(ExhibitionPick.DefaultPark, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// The field card (FD-15, F8-a): what the played park changes, one short line each, read from the park itself — its outfield
    /// ground, its wall, its air, then each hazard type it plays tonight in park order. The copy is keyed by ground, material and
    /// hazard type, never by a park id, so a new park gets its card from its data. A park that changes nothing says so.
    /// </summary>
    public static IReadOnlyList<string> FieldCard(Park played, RulesTable rules)
    {
        var lines = new List<string>();
        var ground = GroundZones.Of(played, rules).Outfield;
        if (GroundLine.TryGetValue(ground, out var g)) lines.Add(g);
        if (played.Fence?.Points.Any(p => p.Material is { } m && m != WallMaterial.Padded) == true
            && played.Fence.Points.First(p => p.Material is not null).Material is { } wall && WallLine.TryGetValue(wall, out var w))
            lines.Add(w);
        if (played.Environment?.DragMul is { } drag && drag != 1)
            lines.Add(drag > 1 ? "Heavy air: flies die a little early." : "Thin air: flies carry.");
        foreach (var type in played.Hazards.Select(h => h.Type).Distinct())
            if (HazardLine(type, rules) is { } line) lines.Add(line);
        if (lines.Count == 0) lines.Add("A true field: no hazards, the real diamond.");
        return lines;
    }

    static readonly Dictionary<string, string> GroundLine = new(StringComparer.Ordinal)
    {
        [Ground.Ice] = "Ice outfield: grounders run and skid; fielders are slow to stop.",
        [Ground.Ash] = "Ash outfield.",
        [Ground.Dirt] = "Dirt outfield."
    };

    static readonly Dictionary<string, string> WallLine = new(StringComparer.Ordinal)
    {
        [WallMaterial.Glass] = "Glass boards: balls come off them hot."
    };

    /// <summary>One hazard type's line (F8-a), with its row's own numbers where they matter to a player.</summary>
    public static string? HazardLine(string type, RulesTable rules)
    {
        var row = rules.Hazards.Of(type);
        var slow = row.SlowSec is { } s ? $"{s:0.#} s" : "";
        return type switch
        {
            HazardType.FreezeVolume => $"Freezers: a fielder who runs through one slows for {slow}.",
            HazardType.LavaPit => $"Lava pits: a fielder who runs through one slows for {slow}.",
            HazardType.FireBreath => $"The statue breathes fire: it slows a fielder for {slow}" + (row.NightRadiusMul > 1 ? ", farther at night." : "."),
            HazardType.WarpPipe => "Warp cans: a ball in one pops out of another.",
            HazardType.Barrel => "Barrel cannons fire the ball out of another barrel.",
            HazardType.Chomper => "Chompers spit a fly out of another mouth.",
            HazardType.Billboard => "Put a ball under a billboard for a star.",
            HazardType.ClimbWall => "A Clamber fielder climbs the vine wall to rob.",
            HazardType.Statue => "A statue stands in play: balls bounce off it.",
            HazardType.AcUnit => "An AC unit sits in play: balls bounce off it.",
            HazardType.Tree => "Trees stand in play: balls bounce off them.",
            HazardType.Train => "A train runs along the wall.",
            _ => null
        };
    }
}

namespace GrandSluggers.Sim;

/// <summary>
/// Exhibition front of house: the park is the poster, captains are toys, the field is a postcard.
/// HUD draws this copy; tests lock it. Not a second UI toolkit.
/// </summary>
public static class CarnivalFront
{
    public const string Logo = "GRAND SLUGGERS";
    public const string PlayBall = "South / Space    play ball";
    public const float TitleRowZ = 26f;
    public const float SelectRowZ = 12f;
    public const float HomeStepSelectFt = 4f;
    public const float HomeStepTitleFt = 2.4f;
    public const float FeaturedTitleZ = 9f;
    public const float FeaturedSelectZ = 8f;
    /// <summary>Chest. Y=4.4 at Z=4 is Ashlord's brim.</summary>
    public const float SelectLookY = 2.6f;
    /// <summary>Chest-height. Y=7.8 looking at Z=8 from Z=-12 is the plate berm.</summary>
    public const float SelectCamMinY = 3.6f;
    public const float SelectCamMaxY = 6.0f;
    /// <summary>Downward slope (ΔY/ΔZ). 5.2/20 from the berm shot.</summary>
    public const float SelectMaxDown = 0.18f;
    /// <summary>Over the infield, above the home toy. Z=7 Y=8.8 sat on Rio's hat.</summary>
    public const float LogoX = 0.8f;
    public const float LogoY = 12.2f;
    public const float LogoZ = 15.6f;
    public const float TitleHeroChestY = 1.8f;
    public const float SelectSpacing = 7.6f;
    public const float TitleSpacing = 13.4f;
    public const float CardX = 5.6f;
    public const float CardY = 2.9f;
    public const float CardZ = 0.2f;

    public static (float X, float Z) CaptainSpot(int index, int count, bool select, bool home)
    {
        var spacing = select ? SelectSpacing : TitleSpacing;
        var x = (index - (count - 1) * 0.5f) * spacing;
        var z = select ? SelectRowZ : TitleRowZ;
        if (home) return (0f, select ? FeaturedSelectZ : FeaturedTitleZ);
        return (x, z);
    }

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

    public static Vec3 TitleHeroChest => new(0, TitleHeroChestY, FeaturedTitleZ);

    public static Vec3 TitleLogoAt => new(LogoX, LogoY, LogoZ);

    /// <summary>
    /// Title is one toy + a sticker over the diamond. Fail if the board
    /// sits on the hat or the hero is a corner crop.
    /// </summary>
    public static bool TitlePoster(Vec3 cam, Vec3 look)
    {
        var hero = TitleHeroChest;
        var logo = TitleLogoAt;
        return OffLook(cam, look, hero) < 22
            && OffLook(cam, look, logo) < 20
            && OffLook(cam, hero, logo) > 12
            && LogoZ > FeaturedTitleZ
            && LogoY > 10
            && Math.Abs(LogoX) < 8;
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

    public static bool HarborIsTheProduct(string parkId) =>
        parkId.Equals("harbor-diamond", StringComparison.OrdinalIgnoreCase);

    /// <summary>One line. Day vs night when the gimmick changes.</summary>
    public static string Gimmick(string parkId, bool night) => parkId.ToLowerInvariant() switch
    {
        "harbor-diamond" => night ? "Night fireworks. Still the real diamond." : "The real diamond.",
        "crystal-rink" => night ? "Ice. The lights go out." : "Ice. Don't fall down.",
        "funfair-park" => night ? "Chompers eat flies." : "Pipes swallow hoppers.",
        "rooftop-city" => "Billboards on a city roof.",
        "canopy-yard" => "Vines and barrels. Climb the wall.",
        "ember-keep" => night ? "Lava breathes farther." : "Lava in the grass.",
        _ => "A park."
    };
}

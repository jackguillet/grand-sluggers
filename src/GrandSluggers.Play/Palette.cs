using GrandSluggers.Sim;
using Raylib_cs;

namespace GrandSluggers.Play;

public static class Palette
{
    public static readonly Color Sky = C(118, 186, 232);
    public static readonly Color Grass = C(62, 168, 78);
    public static readonly Color CutGrass = C(52, 148, 68);
    public static readonly Color Dirt = C(196, 154, 96);
    public static readonly Color Mound = C(176, 132, 78);
    public static readonly Color Chalk = C(245, 245, 235);
    public static readonly Color Water = C(46, 124, 176);
    public static readonly Color Fence = C(214, 214, 206);
    public static readonly Color FoulPole = C(255, 214, 64);
    public static readonly Color Spark = C(220, 48, 42);
    public static readonly Color SparkDark = C(140, 24, 28);
    public static readonly Color Ember = C(44, 32, 52);
    public static readonly Color EmberFire = C(255, 122, 32);
    public static readonly Color Royal = C(232, 120, 168);
    public static readonly Color Carnival = C(40, 170, 90);
    public static readonly Color Goldrush = C(232, 188, 40);
    public static readonly Color Canopy = C(120, 78, 42);
    public static readonly Color Fen = C(91, 143, 98);
    public static readonly Color FenCream = C(232, 220, 192);

    public static Color Body(string faction) => faction switch
    {
        "spark" => Spark,
        "royal" => Royal,
        "carnival" => Carnival,
        "goldrush" => Goldrush,
        "canopy" => Canopy,
        "ember" => Ember,
        "fen" => Fen,
        _ => C(120, 120, 128)
    };

    public static Color Accent(string faction) => faction switch
    {
        "spark" => Gold,
        "royal" => C(180, 230, 255),
        "carnival" => C(255, 80, 160),
        "goldrush" => C(255, 120, 40),
        "canopy" => C(80, 160, 70),
        "ember" => EmberFire,
        "fen" => FenCream,
        _ => Gold
    };

    public static Color SkinTone(string faction) =>
        faction is "ember" or "canopy" ? SkinShadow : Skin;
    public static readonly Color Skin = C(242, 201, 164);
    public static readonly Color SkinShadow = C(90, 78, 92);
    public static readonly Color Ball = C(250, 248, 240);
    public static readonly Color Seam = C(200, 40, 40);
    public static readonly Color HudInk = C(24, 24, 28);
    public static readonly Color HudPaper = C(255, 255, 255);
    public static readonly Color Good = C(80, 220, 140);
    public static readonly Color Bad = C(232, 72, 72);
    public static readonly Color Gold = C(255, 204, 64);
    public static readonly Color Night = C(18, 28, 48);

    public static Color C(int r, int g, int b, int a = 255) =>
        new((byte)r, (byte)g, (byte)b, (byte)a);

    public static Color Fade(Color c, int a) => new(c.R, c.G, c.B, (byte)a);

    public static Color SkyOf(Park park) => park.Surface switch
    {
        "ice" => C(168, 198, 222),
        "ash" => C(48, 22, 28),
        _ when park.Id == "canopy-yard" => C(78, 148, 168),
        _ => Sky
    };
}

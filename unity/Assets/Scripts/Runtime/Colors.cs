using UnityEngine;

namespace GrandSluggers.UnityClient
{
    public static class Colors
    {
        public static readonly Color Sky = Hex(0x76BAE8);
        public static readonly Color Grass = Hex(0x3EA84E);
        public static readonly Color Cut = Hex(0x349444);
        public static readonly Color Dirt = Hex(0xC49A60);
        public static readonly Color Ice = Hex(0xBED8F0);
        public static readonly Color Spark = Hex(0xDC302A);
        public static readonly Color SparkDark = Hex(0x8C181C);
        public static readonly Color Ember = Hex(0x2C2034);
        public static readonly Color EmberFire = Hex(0xFF7A20);
        public static readonly Color Royal = Hex(0xE878A8);
        public static readonly Color Carnival = Hex(0x28AA5A);
        public static readonly Color Goldrush = Hex(0xE8BC28);
        public static readonly Color Canopy = Hex(0x784E2A);
        public static readonly Color Fen = Hex(0x5B8F62);
        public static readonly Color FenCream = Hex(0xE8DCC0);
        public static readonly Color FenSkin = Hex(0x7FB57A);
        public static readonly Color Skin = Hex(0xF2C9A4);
        public static readonly Color SkinShadow = new Color(0.35f, 0.3f, 0.36f);
        public static readonly Color Gold = Hex(0xFFCC40);
        public static readonly Color Chalk = Hex(0xF5F5EB);
        public static readonly Color Ball = Hex(0xFAF8F0);
        public static readonly Color Water = Hex(0x2E7CB0);
        public static readonly Color Fence = Hex(0xD6D6CE);

        /// <summary>A faction's jersey (data/art/factions.json). A faction the file does not have is neutral grey.</summary>
        public static Color Body(string faction) =>
            ArtBinder.Art?.Factions?.Of(faction) is { } f ? Look.Of(f.Body) : new Color(0.47f, 0.47f, 0.5f);

        /// <summary>A faction's trim and HUD accent (data/art/factions.json).</summary>
        public static Color Accent(string faction) =>
            ArtBinder.Art?.Factions?.Of(faction) is { } f ? Look.Of(f.Accent) : Gold;

        /// <summary>The skin tone a faction's bodies wear (data/art/factions.json).</summary>
        public static Color SkinTone(string faction) =>
            ArtBinder.Art?.Factions?.Of(faction) is { } f ? Look.Of(f.Skin) : Skin;

        public static Color Hex(int rgb) =>
            new(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, 1f);
    }
}

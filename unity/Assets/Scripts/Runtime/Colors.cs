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
        public static readonly Color Ember = Hex(0x2C2034);
        public static readonly Color EmberFire = Hex(0xFF7A20);
        public static readonly Color Skin = Hex(0xF2C9A4);
        public static readonly Color Gold = Hex(0xFFCC40);
        public static readonly Color Chalk = Hex(0xF5F5EB);
        public static readonly Color Ball = Hex(0xFAF8F0);
        public static readonly Color Water = Hex(0x2E7CB0);
        public static readonly Color Fence = Hex(0xD6D6CE);

        public static Color Hex(int rgb) =>
            new(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, 1f);
    }
}

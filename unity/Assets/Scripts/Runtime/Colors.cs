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
        public static readonly Color Royal = Hex(0xE878A8);
        public static readonly Color Carnival = Hex(0x28AA5A);
        public static readonly Color Goldrush = Hex(0xE8BC28);
        public static readonly Color Canopy = Hex(0x784E2A);
        public static readonly Color Skin = Hex(0xF2C9A4);
        public static readonly Color SkinShadow = new Color(0.35f, 0.3f, 0.36f);
        public static readonly Color Gold = Hex(0xFFCC40);
        public static readonly Color Chalk = Hex(0xF5F5EB);
        public static readonly Color Ball = Hex(0xFAF8F0);
        public static readonly Color Water = Hex(0x2E7CB0);
        public static readonly Color Fence = Hex(0xD6D6CE);

        public static Color Body(string faction)
        {
            switch (faction)
            {
                case "spark": return Spark;
                case "royal": return Royal;
                case "carnival": return Carnival;
                case "goldrush": return Goldrush;
                case "canopy": return Canopy;
                case "ember": return Ember;
                default: return new Color(0.47f, 0.47f, 0.5f);
            }
        }

        public static Color Accent(string faction)
        {
            switch (faction)
            {
                case "spark": return Gold;
                case "royal": return new Color(0.7f, 0.9f, 1f);
                case "carnival": return new Color(1f, 0.31f, 0.63f);
                case "goldrush": return EmberFire;
                case "canopy": return new Color(0.31f, 0.63f, 0.27f);
                case "ember": return EmberFire;
                default: return Gold;
            }
        }

        public static Color SkinTone(string faction) =>
            faction == "ember" || faction == "canopy" ? SkinShadow : Skin;

        public static Color Hex(int rgb) =>
            new(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, 1f);
    }
}

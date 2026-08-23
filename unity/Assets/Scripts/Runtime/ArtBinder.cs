using GrandSluggers.Sim;
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    /// <summary>
    /// Resolves catalog slots. Missing Unity files keep the procedural placeholder.
    /// </summary>
    public static class ArtBinder
    {
        static ArtCatalog _art;

        public static ArtCatalog Art => _art;

        public static void Bind(ArtCatalog art) => _art = art;

        public static bool HasPortrait(string id)
        {
            if (_art != null && _art.Skins.TryGetValue(id, out var skin))
                return !string.IsNullOrWhiteSpace(skin.Portrait);
            return false;
        }

        public static Texture2D LoadPortrait(string id)
        {
            if (_art == null || !_art.Skins.TryGetValue(id, out var skin) || string.IsNullOrWhiteSpace(skin.Portrait))
                return null;
            var key = SlotToResources(skin.Portrait);
            var tex = Resources.Load<Texture2D>(key);
            if (tex != null) return tex;
            return Resources.Load<Texture2D>("Art/" + id + "-hero");
        }

        public static string ClipPath(string clipId)
        {
            if (_art != null && _art.TryClip(clipId, out var clip)) return clip.Slot;
            return "";
        }

        public static string VfxPath(string eventId)
        {
            if (_art != null && _art.TryVfx(eventId, out var slot)) return slot.Slot;
            return "";
        }

        public static string AudioPath(string eventId)
        {
            if (_art != null && _art.TryAudio(eventId, out var slot)) return slot.Slot;
            return "";
        }

        public static string ParkKitPath(string parkId)
        {
            if (_art != null && _art.TryPark(parkId, out var kit)) return kit.Slot;
            return "";
        }

        public static SkinSlot SkinOf(Character who)
        {
            if (_art == null) return default;
            return _art.SkinOf(who);
        }

        static string SlotToResources(string slot)
        {
            const string prefix = "Resources/";
            if (slot.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase))
                return slot.Substring(prefix.Length);
            if (slot.StartsWith("Assets/", System.StringComparison.OrdinalIgnoreCase))
                return slot.Substring("Assets/".Length);
            return slot;
        }
    }
}

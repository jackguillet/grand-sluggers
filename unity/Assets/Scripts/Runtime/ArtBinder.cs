using System;
using System.Collections.Generic;
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

        /// <summary>Editor Play fills this so the SharedRig FBX loads without a Resources copy.</summary>
        public static Func<string, GameObject> EditorLoadPrefab;

        /// <summary>Editor Play fills this so clip FBX/anim loads without a Resources copy.</summary>
        public static Func<string, AnimationClip> EditorLoadClip;

        /// <summary>Editor Play: named mesh inside a kit FBX (dugout-1b, wall-panel, …).</summary>
        public static Func<string, string, GameObject> EditorLoadNamedMesh;

        static readonly Dictionary<string, AnimationClip> ClipCache =
            new Dictionary<string, AnimationClip>(StringComparer.OrdinalIgnoreCase);
        static readonly HashSet<string> ClipMiss = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public static void Bind(ArtCatalog art) => _art = art;

        static GameObject _extrasKit;
        static bool _extrasMiss;
        static GameObject _harborKit;
        static bool _harborMiss;

        /// <summary>Shared extras kit (brim, crown, goggles, …). Null keeps primitive extras.</summary>
        public static GameObject LoadExtrasKit()
        {
            if (_extrasKit != null) return _extrasKit;
            if (_extrasMiss) return null;
            const string slot = "Assets/Art/Characters/SharedRig/extras.fbx";
            var key = SlotToResources(slot);
            if (key.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
                key = key.Substring(0, key.Length - 4);
            var go = Resources.Load<GameObject>(key);
            if (go == null && EditorLoadPrefab != null)
                go = EditorLoadPrefab(slot);
            if (go == null)
            {
                _extrasMiss = true;
                return null;
            }
            _extrasKit = go;
            return go;
        }

        /// <summary>Catalog FBX if the slot has a Unity file; null keeps SharedRig primitives.</summary>
        public static GameObject LoadSharedRigPrefab()
        {
            var slot = "Assets/Art/Characters/SharedRig/hero-shared.fbx";
            if (_art != null && !string.IsNullOrWhiteSpace(_art.Rig.Slot))
                slot = _art.Rig.Slot;
            var key = SlotToResources(slot);
            if (key.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
                key = key.Substring(0, key.Length - 4);
            var go = Resources.Load<GameObject>(key);
            if (go != null) return go;
            return EditorLoadPrefab != null ? EditorLoadPrefab(slot) : null;
        }

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

        /// <summary>Catalog AnimationClip if the slot has a Unity file; null keeps MoveBones.</summary>
        public static AnimationClip LoadClip(string clipId)
        {
            if (string.IsNullOrWhiteSpace(clipId)) return null;
            if (ClipCache.TryGetValue(clipId, out var hit)) return hit;
            if (ClipMiss.Contains(clipId)) return null;

            var slot = ClipPath(clipId);
            if (string.IsNullOrWhiteSpace(slot))
            {
                ClipMiss.Add(clipId);
                return null;
            }

            var key = SlotToResources(slot);
            var loaded = Resources.Load<AnimationClip>(key);
            if (loaded == null)
                loaded = Resources.Load<AnimationClip>(key + "/" + clipId);
            if (loaded == null && EditorLoadClip != null)
                loaded = EditorLoadClip(slot);
            if (loaded == null)
            {
                ClipMiss.Add(clipId);
                return null;
            }
            ClipCache[clipId] = loaded;
            return loaded;
        }

        public static string VfxPath(string eventId)
        {
            if (_art != null && _art.TryVfx(eventId, out var slot)) return slot.Slot;
            return "";
        }

        /// <summary>Catalog prefab if the slot has a Unity file; null keeps the procedural stand-in.</summary>
        public static GameObject LoadVfx(string eventId)
        {
            var path = VfxPath(eventId);
            if (string.IsNullOrWhiteSpace(path)) return null;
            var key = SlotToResources(path);
            var go = Resources.Load<GameObject>(key);
            if (go != null) return go;
            return Resources.Load<GameObject>(key + "/" + eventId);
        }

        public static bool HasVfx(string eventId)
        {
            if (string.IsNullOrWhiteSpace(eventId)) return false;
            if (_art == null) return true;
            return _art.TryVfx(eventId, out _);
        }

        public static string AudioPath(string eventId)
        {
            if (_art != null && _art.TryAudio(eventId, out var slot)) return slot.Slot;
            return "";
        }

        /// <summary>Catalog clip if the slot has a Unity file; null keeps the generated tone.</summary>
        public static AudioClip LoadAudio(string eventId)
        {
            var path = AudioPath(eventId);
            if (string.IsNullOrWhiteSpace(path)) return null;
            var key = SlotToResources(path);
            var clip = Resources.Load<AudioClip>(key);
            if (clip != null) return clip;
            return Resources.Load<AudioClip>(key + "/" + eventId);
        }

        public static string AudioBusOf(string eventId)
        {
            if (_art != null && _art.TryAudio(eventId, out var slot) && !string.IsNullOrWhiteSpace(slot.Kind))
                return slot.Kind;
            if (!string.IsNullOrEmpty(eventId) && eventId.StartsWith("vo-", System.StringComparison.OrdinalIgnoreCase))
                return "vo";
            if (!string.IsNullOrEmpty(eventId) && eventId.StartsWith("crowd-", System.StringComparison.OrdinalIgnoreCase))
                return "crowd";
            return "sfx";
        }

        public static string ParkKitPath(string parkId)
        {
            if (_art != null && _art.TryPark(parkId, out var kit)) return kit.Slot;
            return "";
        }

        /// <summary>Harbor kit FBX. Null keeps HarborKit primitive dress.</summary>
        public static GameObject LoadParkKit(string parkId)
        {
            if (string.IsNullOrWhiteSpace(parkId)) return null;
            if (!parkId.Equals("harbor-diamond", StringComparison.OrdinalIgnoreCase))
                return null;
            if (_harborKit != null) return _harborKit;
            if (_harborMiss) return null;
            var path = ParkKitFbx(parkId);
            if (string.IsNullOrWhiteSpace(path))
            {
                _harborMiss = true;
                return null;
            }
            var key = SlotToResources(path);
            if (key.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
                key = key.Substring(0, key.Length - 4);
            var go = Resources.Load<GameObject>(key);
            if (go == null && EditorLoadPrefab != null)
                go = EditorLoadPrefab(path);
            if (go == null)
            {
                _harborMiss = true;
                return null;
            }
            _harborKit = go;
            return go;
        }

        /// <summary>Named mesh in the Harbor kit FBX. Null keeps the primitive.</summary>
        public static GameObject LoadParkMesh(string parkId, string meshName)
        {
            if (string.IsNullOrWhiteSpace(meshName)) return null;
            var path = ParkKitFbx(parkId);
            if (string.IsNullOrWhiteSpace(path)) return null;
            if (EditorLoadNamedMesh != null)
            {
                var named = EditorLoadNamedMesh(path, meshName);
                if (named != null) return named;
            }
            var kit = LoadParkKit(parkId);
            if (kit == null) return null;
            var tf = kit.transform;
            if (tf.name.Equals(meshName, StringComparison.OrdinalIgnoreCase))
                return kit;
            for (var i = 0; i < tf.childCount; i++)
            {
                var child = tf.GetChild(i);
                if (child.name.Equals(meshName, StringComparison.OrdinalIgnoreCase))
                    return child.gameObject;
                var deep = FindChild(child, meshName);
                if (deep != null) return deep.gameObject;
            }
            return null;
        }

        static Transform FindChild(Transform t, string name)
        {
            if (t.name.Equals(name, StringComparison.OrdinalIgnoreCase)) return t;
            for (var i = 0; i < t.childCount; i++)
            {
                var f = FindChild(t.GetChild(i), name);
                if (f != null) return f;
            }
            return null;
        }

        static string ParkKitFbx(string parkId)
        {
            var slot = ParkKitPath(parkId);
            if (string.IsNullOrWhiteSpace(slot)) return "";
            if (slot.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase)) return slot;
            return slot.TrimEnd('/') + "/harbor-kit.fbx";
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

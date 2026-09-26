using System;
using System.Collections.Generic;
using GrandSluggers.Sim;
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    /// <summary>
    /// Resolves catalog slots to Unity assets. A missing file is a logged placeholder, never a crash.
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

        /// <summary>The extras FBX <c>data/art/extras.json</c> names; empty until the catalog is bound, which keeps primitives.</summary>
        static string ExtrasSlot => _art?.ExtrasSlot ?? "";

        /// <summary>
        /// The park's kit row and its slots (FD-16, FR-13; <c>data/art/parks.json</c>). A park with no row, or no catalog
        /// bound, fills no slot: it draws the greybox.
        /// </summary>
        public static ParkKitSlot ParkKit(string parkId) =>
            _art != null && !string.IsNullOrWhiteSpace(parkId) && _art.TryPark(parkId, out var kit) ? kit : default;

        static GameObject _extrasKit;
        static bool _extrasMiss;
        static GameObject _harborKit;
        static bool _harborMiss;

        /// <summary>Shared extras kit (brim, crown, goggles, …). Null keeps primitive extras.</summary>
        public static GameObject LoadExtrasKit()
        {
            if (_extrasKit != null) return _extrasKit;
            if (_extrasMiss) return null;
            var slot = ExtrasSlot;
            if (string.IsNullOrEmpty(slot)) return null; // not bound yet: the primitives stand in
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

        /// <summary>
        /// Catalog slot for a take file id: a clip id or its baked mirror <c>{clip}-L</c>, optionally a motion style's own
        /// take <c>{style}/{clip}[-L]</c> (<see cref="Motion.ClipFor"/>).
        /// </summary>
        public static string ClipPath(string fileId, bool player = false)
        {
            if (_art == null || string.IsNullOrWhiteSpace(fileId)) return "";
            MotionStyle style = null;
            var slash = fileId.IndexOf('/');
            var file = fileId;
            if (slash > 0)
            {
                if (!_art.TryStyle(fileId.Substring(0, slash), out style)) return "";
                file = fileId.Substring(slash + 1);
            }
            var left = file.EndsWith("-L", StringComparison.OrdinalIgnoreCase);
            var clipId = left ? file.Substring(0, file.Length - 2) : file;
            if (!_art.TryClip(clipId, out var clip)) return "";
            var (slot, playerSlot) = _art.ClipFiles(clip, left ? Hand.L : Hand.R, style);
            return player ? playerSlot : slot;
        }

        /// <summary>Catalog AnimationClip for a take file id. Null means the take is not baked.</summary>
        public static AnimationClip LoadClip(string fileId)
        {
            if (string.IsNullOrWhiteSpace(fileId)) return null;
            if (ClipCache.TryGetValue(fileId, out var hit)) return hit;
            if (ClipMiss.Contains(fileId)) return null;

            var slot = ClipPath(fileId);
            if (string.IsNullOrWhiteSpace(slot))
            {
                ClipMiss.Add(fileId);
                return null;
            }
            var key = ResourceKey(ClipPath(fileId, player: true));
            var loaded = LoadExactResourceClip(key, fileId.Substring(fileId.LastIndexOf('/') + 1));
            if (loaded == null)
                loaded = Resources.Load<AnimationClip>(key);
            if (loaded == null && EditorLoadClip != null)
                loaded = EditorLoadClip(slot);
            if (loaded == null)
            {
                ClipMiss.Add(fileId);
                return null;
            }
            ClipCache[fileId] = loaded;
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

        /// <summary>
        /// The procedural stand-in a special's slot names (<c>vfx.json</c> <c>tell</c>, one of
        /// <see cref="SpecialTells.Builders"/>), or "" when the slot names none.
        /// </summary>
        public static string TellOf(string eventId)
        {
            if (_art == null || string.IsNullOrWhiteSpace(eventId)) return "";
            return _art.TryVfx(eventId, out var slot) && !string.IsNullOrEmpty(slot.Tell) ? slot.Tell : "";
        }

        /// <summary>The audio slot a special's tell sounds (<c>vfx.json</c> <c>cue</c>), or "".</summary>
        public static string CueOf(string eventId)
        {
            if (_art == null || string.IsNullOrWhiteSpace(eventId)) return "";
            return _art.TryVfx(eventId, out var slot) && !string.IsNullOrEmpty(slot.Cue) ? slot.Cue : "";
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
            // Only a placed kit has a mesh (data/art/parks.json placed); every other park draws the greybox.
            if (_art == null || !_art.TryPark(parkId, out var row) || !row.Placed)
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

        /// <summary>Named mesh in extras.fbx (bat-wood, glove-brown, baseball, brim, …). Null keeps the primitive.</summary>
        public static GameObject LoadExtraMesh(string meshName)
        {
            if (string.IsNullOrWhiteSpace(meshName)) return null;
            var slot = ExtrasSlot;
            if (string.IsNullOrEmpty(slot)) return null; // not bound yet: the primitives stand in
            if (EditorLoadNamedMesh != null)
            {
                var named = EditorLoadNamedMesh(slot, meshName);
                if (UsableMesh(named)) return named;
            }
            var kit = LoadExtrasKit();
            if (kit == null) return null;
            if (kit.name.Equals(meshName, StringComparison.OrdinalIgnoreCase) && UsableMesh(kit))
                return kit;
            var tf = kit.transform;
            for (var i = 0; i < tf.childCount; i++)
            {
                var child = tf.GetChild(i);
                if (child.name.Equals(meshName, StringComparison.OrdinalIgnoreCase) && UsableMesh(child.gameObject))
                    return child.gameObject;
                var deep = FindChild(child, meshName);
                if (deep != null && UsableMesh(deep.gameObject)) return deep.gameObject;
            }
            return null;
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
                if (UsableMesh(named)) return named;
            }
            var kit = LoadParkKit(parkId);
            if (kit == null) return null;
            var tf = kit.transform;
            if (tf.name.Equals(meshName, StringComparison.OrdinalIgnoreCase))
                return UsableMesh(kit) ? kit : null;
            for (var i = 0; i < tf.childCount; i++)
            {
                var child = tf.GetChild(i);
                if (child.name.Equals(meshName, StringComparison.OrdinalIgnoreCase))
                    return UsableMesh(child.gameObject) ? child.gameObject : null;
                var deep = FindChild(child, meshName);
                if (deep != null)
                    return UsableMesh(deep.gameObject) ? deep.gameObject : null;
            }
            return null;
        }

        static bool UsableMesh(GameObject go)
        {
            if (go == null) return false;
            var r = go.GetComponentInChildren<MeshRenderer>(true);
            var f = go.GetComponentInChildren<MeshFilter>(true);
            return r != null && f != null && f.sharedMesh != null;
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
            return slot.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase) ? slot : "";
        }

        public static SkinSlot SkinOf(Character who)
        {
            if (_art == null) return default;
            return _art.SkinOf(who);
        }

        static string SlotToResources(string slot)
        {
            const string assetResources = "Assets/Resources/";
            if (slot.StartsWith(assetResources, System.StringComparison.OrdinalIgnoreCase))
                return slot.Substring(assetResources.Length);
            const string prefix = "Resources/";
            if (slot.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase))
                return slot.Substring(prefix.Length);
            if (slot.StartsWith("Assets/", System.StringComparison.OrdinalIgnoreCase))
                return slot.Substring("Assets/".Length);
            return slot;
        }

        static string ResourceKey(string slot)
        {
            var key = SlotToResources(slot ?? "");
            var dot = key.LastIndexOf('.');
            if (dot > key.LastIndexOf('/')) key = key.Substring(0, dot);
            return key;
        }

        static AnimationClip LoadExactResourceClip(string key, string clipName)
        {
            if (string.IsNullOrWhiteSpace(key)) return null;
            var clips = Resources.LoadAll<AnimationClip>(key);
            for (var i = 0; i < clips.Length; i++)
            {
                var clip = clips[i];
                if (clip == null || clip.name.StartsWith("__preview", StringComparison.Ordinal)) continue;
                if (clip.name.Equals(clipName, StringComparison.OrdinalIgnoreCase)) return clip;
            }
            return null;
        }
    }
}

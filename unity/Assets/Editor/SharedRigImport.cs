using System;
using System.IO;
using System.Text;
using GrandSluggers.Sim;
using Motion = GrandSluggers.Sim.Motion;
using GrandSluggers.UnityClient;
using UnityEditor;
using UnityEngine;

namespace GrandSluggers.EditorTools
{
    /// <summary>
    /// Generic rig for hero-shared and the take FBX files. Hooks AssetDatabase
    /// load for Play so Runtime does not reference UnityEditor.
    /// </summary>
    [InitializeOnLoad]
    public class SharedRigImport : AssetPostprocessor
    {
        const string RigFolder = "Art/Characters/SharedRig/";
        const string ClipFolder = "Art/Animation/Clips/";
        const string ParkFolder = "Art/Parks/";
        const string DefaultSlot = "Assets/Art/Characters/SharedRig/hero-shared.fbx";

        static SharedRigImport()
        {
            ArtBinder.EditorLoadPrefab = path =>
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    string.IsNullOrWhiteSpace(path) ? DefaultSlot : path);
            ArtBinder.EditorLoadClip = LoadClip;
            ArtBinder.EditorLoadNamedMesh = LoadNamedMesh;
        }

        static GameObject LoadNamedMesh(string path, string name)
        {
            if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(name))
                return null;
            var log = new StringBuilder();
            log.AppendLine("want " + name + " from " + path);
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            log.AppendLine("root " + (root == null ? "null" : root.name + " children=" + root.transform.childCount));
            if (root != null)
            {
                Dump(root.transform, log, 0);
                var named = FindDeep(root.transform, name);
                if (named != null) return named.gameObject;
                foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
                {
                    var mn = mf.sharedMesh != null ? mf.sharedMesh.name : "";
                    log.AppendLine("mf " + mf.name + " mesh " + mn);
                    if (mn.Equals(name, StringComparison.OrdinalIgnoreCase))
                        return mf.gameObject;
                }
            }

            Mesh mesh = null;
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                log.AppendLine(asset.GetType().Name + ":" + asset.name);
                if (asset is GameObject go && go.name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return go;
                if (asset is Mesh m && m.name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    mesh = m;
            }

            if (mesh != null)
            {
                var wrap = new GameObject(name);
                wrap.hideFlags = HideFlags.HideAndDontSave;
                var filter = wrap.AddComponent<MeshFilter>();
                filter.sharedMesh = mesh;
                wrap.AddComponent<MeshRenderer>();
                return wrap;
            }

            try
            {
                var temp = Path.Combine(Directory.GetParent(Application.dataPath)!.FullName, "Temp", "gs-kit-assets.txt");
                File.WriteAllText(temp, log.ToString());
            }
            catch { /* editor-only breadcrumb */ }
            return null;
        }

        static void Dump(Transform t, StringBuilder log, int depth)
        {
            log.AppendLine(new string(' ', depth * 2) + t.name);
            for (var i = 0; i < t.childCount; i++)
                Dump(t.GetChild(i), log, depth + 1);
        }

        static Transform FindDeep(Transform t, string name)
        {
            if (t.name.Equals(name, StringComparison.OrdinalIgnoreCase)) return t;
            for (var i = 0; i < t.childCount; i++)
            {
                var f = FindDeep(t.GetChild(i), name);
                if (f != null) return f;
            }
            return null;
        }

        static AnimationClip LoadClip(string slot)
        {
            foreach (var path in ClipCandidates(slot))
            {
                var assets = AssetDatabase.LoadAllAssetsAtPath(path);
                AnimationClip found = null;
                for (var i = 0; i < assets.Length; i++)
                {
                    if (assets[i] is not AnimationClip c) continue;
                    if (c.name.StartsWith("__preview", StringComparison.Ordinal)) continue;
                    found = c;
                    var file = Path.GetFileNameWithoutExtension(path);
                    if (c.name.IndexOf(file, StringComparison.OrdinalIgnoreCase) >= 0)
                        return c;
                }
                if (found != null) return found;
                var direct = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if (direct != null) return direct;
            }
            return null;
        }

        static string[] ClipCandidates(string slot)
        {
            if (string.IsNullOrWhiteSpace(slot)) return Array.Empty<string>();
            if (Path.HasExtension(slot)) return new[] { slot };
            return new[] { slot + ".fbx", slot + ".anim" };
        }

        void OnPreprocessModel()
        {
            var rig = assetPath.IndexOf(RigFolder, StringComparison.OrdinalIgnoreCase) >= 0;
            var clip = assetPath.IndexOf(ClipFolder, StringComparison.OrdinalIgnoreCase) >= 0;
            var park = assetPath.IndexOf(ParkFolder, StringComparison.OrdinalIgnoreCase) >= 0;
            var sharedExtras = assetPath.EndsWith(
                "Art/Characters/SharedRig/extras.fbx", StringComparison.OrdinalIgnoreCase);
            if (!rig && !clip && !park) return;
            var imp = (ModelImporter)assetImporter;
            // Generic without an avatar: every curve, including the root bone's
            // baked lift, writes its transform by path. An avatar would treat the
            // root bone as motion root and swallow that lift as root motion.
            imp.animationType = ModelImporterAnimationType.Generic;
            imp.avatarSetup = park ? ModelImporterAvatarSetup.CreateFromThisModel : ModelImporterAvatarSetup.NoAvatar;
            imp.importAnimation = clip;
            imp.addCollider = false;
            imp.importBlendShapes = false;
            // The common-prop validator measures the imported bat submeshes,
            // including the Resources player copy. Keep this small kit readable
            // so a future FBX origin recenter cannot evade build validation.
            imp.isReadable = sharedExtras;
            imp.optimizeGameObjects = false;
        }

        /// <summary>Take files carry their catalog row: loop flag and the ball-event marker.</summary>
        void OnPostprocessAnimation(GameObject go, AnimationClip clip)
        {
            if (assetPath.IndexOf(ClipFolder, StringComparison.OrdinalIgnoreCase) < 0) return;
            var fileId = Path.GetFileNameWithoutExtension(assetPath);
            var clipId = fileId.EndsWith("-L", StringComparison.OrdinalIgnoreCase)
                ? fileId.Substring(0, fileId.Length - 2)
                : fileId;
            clip.name = fileId;
            clip.legacy = false;
            if (!Motion.TryClip(clipId, out var row)) return;
            clip.wrapMode = row.Loop ? WrapMode.Loop : WrapMode.ClampForever;
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = row.Loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            if (row.Mark == null)
            {
                AnimationUtility.SetAnimationEvents(clip, Array.Empty<AnimationEvent>());
                return;
            }
            var mark = row.Mark.Value.ToString();
            AnimationUtility.SetAnimationEvents(clip, new[]
            {
                new AnimationEvent
                {
                    time = (float)row.MarkAt,
                    functionName = mark,
                    stringParameter = mark
                }
            });
        }
    }
}

using System;
using System.IO;
using GrandSluggers.UnityClient;
using UnityEditor;
using UnityEngine;

namespace GrandSluggers.EditorTools
{
    /// <summary>
    /// Generic rig for the SharedRig drop and clip FBX takes. Hooks AssetDatabase
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
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (asset is GameObject go && go.name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return go;
            }
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (root == null) return null;
            return FindDeep(root.transform, name)?.gameObject;
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
            if (!rig && !clip && !park) return;
            var imp = (ModelImporter)assetImporter;
            imp.animationType = ModelImporterAnimationType.Generic;
            imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            imp.importAnimation = clip;
            imp.addCollider = false;
            imp.importBlendShapes = false;
            imp.isReadable = false;
        }

        void OnPostprocessAnimation(GameObject go, AnimationClip clip)
        {
            if (assetPath.IndexOf(ClipFolder, StringComparison.OrdinalIgnoreCase) < 0)
                return;
            var id = Path.GetFileNameWithoutExtension(assetPath);
            clip.name = id;
            clip.legacy = false;
            var contact = -1f;
            if (id.Equals("swing", StringComparison.OrdinalIgnoreCase)) contact = 0.30f;
            else if (id.Equals("scoop", StringComparison.OrdinalIgnoreCase)) contact = 0.22f;
            if (contact < 0f) return;
            var ev = new AnimationEvent
            {
                time = contact,
                functionName = "Contact",
                stringParameter = "Contact"
            };
            AnimationUtility.SetAnimationEvents(clip, new[] { ev });
        }
    }
}

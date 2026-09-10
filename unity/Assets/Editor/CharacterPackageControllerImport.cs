using System;
using System.IO;
using System.Linq;
using GrandSluggers.Sim;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace GrandSluggers.EditorTools
{
    /// <summary>Builds the tracked package controllers from ready manifest verbs.</summary>
    [InitializeOnLoad]
    public static class CharacterPackageControllerImport
    {
        static bool _scheduled;
        static bool _busy;

        static CharacterPackageControllerImport() => Schedule();

        public static void Schedule()
        {
            if (_scheduled) return;
            _scheduled = true;
            EditorApplication.delayCall += SyncAll;
        }

        [MenuItem("Grand Sluggers/Sync Character Package Controllers")]
        public static void SyncAll()
        {
            _scheduled = false;
            if (_busy || Application.isPlaying || EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                Schedule();
                return;
            }
            _busy = true;
            try
            {
                var data = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "data"));
                var art = ArtCatalog.Load(data);
                foreach (var package in art.Packages.Values)
                    Sync(package);
                AssetDatabase.SaveAssets();
            }
            catch (Exception ex)
            {
                Debug.LogError("Character package controller sync failed: " + ex.Message);
            }
            finally
            {
                _busy = false;
            }
        }

        static void Sync(CharacterPackageSpec package)
        {
            var controller = EnsureController(package.Controller, package, player: false);
            EnsureController(package.PlayerController, package, player: true);
            if (controller == null) return;

            var prefabPath = CharacterPackage.PrefabSlot(package.Id);
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null) return;
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var animator = root.GetComponentInChildren<Animator>(true);
                if (animator == null) animator = root.AddComponent<Animator>();
                if (animator.runtimeAnimatorController == controller) return;
                animator.runtimeAnimatorController = controller;
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static AnimatorController EnsureController(string path, CharacterPackageSpec package, bool player)
        {
            if (string.IsNullOrWhiteSpace(path)) return null;
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller == null)
            {
                var folder = Path.GetDirectoryName(path)?.Replace('\\', '/');
                if (!string.IsNullOrEmpty(folder) && !AssetDatabase.IsValidFolder(folder)) return null;
                controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            }
            if (controller == null || controller.layers.Length == 0) return controller;

            var stateMachine = controller.layers[0].stateMachine;
            var ready = package.Verbs.Where(CharacterPackage.IsReady).ToArray();
            foreach (var child in stateMachine.states.ToArray())
            {
                if (!ready.Any(verb => child.state.name.Equals(verb.Verb, StringComparison.OrdinalIgnoreCase)))
                    stateMachine.RemoveState(child.state);
            }

            AnimatorState idle = null;
            foreach (var verb in ready)
            {
                var clip = LoadExactClip(player ? verb.PlayerSource : verb.Source, verb.Clip);
                if (clip == null) continue;
                var state = stateMachine.states.Select(child => child.state).FirstOrDefault(candidate =>
                    candidate.name.Equals(verb.Verb, StringComparison.OrdinalIgnoreCase));
                if (state == null) state = stateMachine.AddState(verb.Verb);
                state.motion = clip;
                if (verb.Verb.Equals("idle", StringComparison.OrdinalIgnoreCase)) idle = state;
            }
            if (idle != null) stateMachine.defaultState = idle;
            EditorUtility.SetDirty(controller);
            return controller;
        }

        static AnimationClip LoadExactClip(string path, string clipName) =>
            AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().FirstOrDefault(clip =>
                !clip.name.StartsWith("__preview", StringComparison.Ordinal)
                && clip.name.Equals(clipName, StringComparison.OrdinalIgnoreCase));
    }
}

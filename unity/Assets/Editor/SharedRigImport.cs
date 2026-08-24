using System;
using GrandSluggers.UnityClient;
using UnityEditor;
using UnityEngine;

namespace GrandSluggers.EditorTools
{
    /// <summary>
    /// Generic rig for the SharedRig drop. Hooks AssetDatabase load for Play
    /// so Runtime does not reference UnityEditor.
    /// </summary>
    [InitializeOnLoad]
    public class SharedRigImport : AssetPostprocessor
    {
        const string Folder = "Art/Characters/SharedRig/";
        const string DefaultSlot = "Assets/Art/Characters/SharedRig/hero-shared.fbx";

        static SharedRigImport()
        {
            ArtBinder.EditorLoadPrefab = path =>
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    string.IsNullOrWhiteSpace(path) ? DefaultSlot : path);
        }

        void OnPreprocessModel()
        {
            if (assetPath.IndexOf(Folder, StringComparison.OrdinalIgnoreCase) < 0)
                return;
            var imp = (ModelImporter)assetImporter;
            imp.animationType = ModelImporterAnimationType.Generic;
            imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            imp.importAnimation = false;
            imp.addCollider = false;
            imp.importBlendShapes = false;
            imp.isReadable = false;
        }
    }
}

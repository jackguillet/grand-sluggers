using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GrandSluggers.EditorTools
{
    /// <summary>
    /// Character GLB/FBX drop: sidecar albedo, URP Lit, prefab.
    /// Unity's embedded FBX materials stay white in URP. We assign the
    /// {id}-albedo.png next to the mesh and save a prefab as the playable unit.
    /// </summary>
    public class CharacterDropImport : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            for (var i = 0; i < imported.Length; i++)
            {
                var path = imported[i].Replace('\\', '/');
                if (path.EndsWith("-albedo.png", StringComparison.OrdinalIgnoreCase)
                    && path.IndexOf("Art/Characters/", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var fbx = path.Substring(0, path.Length - "-albedo.png".Length) + ".fbx";
                    EditorApplication.delayCall += () => FinishDrop(fbx);
                    continue;
                }
                if (!IsDropFbx(path)) continue;
                var captured = path;
                EditorApplication.delayCall += () => FinishDrop(captured);
            }
        }

        static bool IsDropFbx(string path)
        {
            if (path.IndexOf("Art/Characters/", StringComparison.OrdinalIgnoreCase) < 0)
                return false;
            if (path.IndexOf("SharedRig", StringComparison.OrdinalIgnoreCase) >= 0)
                return false;
            return path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase);
        }

        static void FinishDrop(string fbxPath)
        {
            if (Application.isPlaying) return;
            var folder = Path.GetDirectoryName(fbxPath);
            if (string.IsNullOrEmpty(folder)) return;
            var id = Path.GetFileNameWithoutExtension(fbxPath);
            var albedoPath = folder.Replace('\\', '/') + "/" + id + "-albedo.png";
            var albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(albedoPath);
            if (albedo == null) return;

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) return;
            var matPath = folder.Replace('\\', '/') + "/" + id + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, matPath);
            }
            mat.shader = shader;
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", albedo);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.22f);
            EditorUtility.SetDirty(mat);

            var model = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (model == null) return;
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
            if (instance == null)
                instance = UnityEngine.Object.Instantiate(model);
            foreach (var r in instance.GetComponentsInChildren<Renderer>(true))
            {
                if (r is LineRenderer) continue;
                r.sharedMaterial = mat;
            }
            var prefabPath = folder.Replace('\\', '/') + "/" + id + ".prefab";
            PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            UnityEngine.Object.DestroyImmediate(instance);
            AssetDatabase.SaveAssets();
        }
    }
}

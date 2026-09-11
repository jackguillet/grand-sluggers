using System;
using System.Collections.Generic;
using System.IO;
using GrandSluggers.Sim;
using UnityEditor;
using UnityEngine;

namespace GrandSluggers.EditorTools
{
    public static class ArtRailsValidate
    {
        [MenuItem("Grand Sluggers/Validate Art Rails")]
        public static void Run()
        {
            var data = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "data"));
            var content = ContentCatalog.Load(data);
            var created = EnsureFolders(content.Art.Folders);
            AssetDatabase.Refresh();
            var errors = new List<string>(content.Art.Validate(content));
            errors.AddRange(ValidateCommonBatImport());
            if (created.Count > 0)
            {
                Debug.Log("Grand Sluggers art rails: created " + created.Count + " folders\n" + string.Join("\n", created));
            }
            if (errors.Count == 0)
            {
                Debug.Log("Grand Sluggers art rails OK. " + content.Art.Clips.Count + " clips, "
                    + content.Art.Skins.Count + " captain skins, " + content.Art.Parks.Count + " park kits.");
                return;
            }
            foreach (var e in errors)
                Debug.LogError("Art rails: " + e);
        }

        /// <summary>
        /// Verifies the imported mesh coordinates that HeroActor consumes. The
        /// DCC source alone cannot catch an origin recenter during FBX export.
        /// </summary>
        public static List<string> ValidateCommonBatImport()
        {
            const string slot = "Assets/Art/Characters/SharedRig/extras.fbx";
            const string visual = "bat-wood";
            var errors = new List<string>();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(slot);
            var bat = prefab == null ? null : FindDeep(prefab.transform, visual);
            var filter = bat == null ? null : bat.GetComponentInChildren<MeshFilter>(true);
            var renderer = bat == null ? null : bat.GetComponentInChildren<MeshRenderer>(true);
            if (filter == null || filter.sharedMesh == null || renderer == null)
            {
                errors.Add(visual + " did not import as a named rendered mesh from " + slot);
                return errors;
            }

            var gripSubmesh = MaterialSubmesh(renderer.sharedMaterials, "grip");
            var woodSubmesh = MaterialSubmesh(renderer.sharedMaterials, "wood");
            if (gripSubmesh < 0 || woodSubmesh < 0
                || gripSubmesh >= filter.sharedMesh.subMeshCount
                || woodSubmesh >= filter.sharedMesh.subMeshCount)
            {
                errors.Add(visual + " must retain named grip and wood submeshes");
                return errors;
            }

            var toBat = bat.worldToLocalMatrix * filter.transform.localToWorldMatrix;
            var handle = Envelope(filter.sharedMesh, gripSubmesh, toBat, _ => true);
            var barrel = Envelope(filter.sharedMesh, woodSubmesh, toBat, p => p.y > -0.5f);
            var knob = Envelope(filter.sharedMesh, woodSubmesh, toBat, p => p.y <= -0.5f);
            Check(errors, visual + " handle start", handle.MinY, -1.00f);
            Check(errors, visual + " handle end", handle.MaxY, -0.10f);
            Check(errors, visual + " handle radius", handle.Radius, 0.08f);
            Check(errors, visual + " barrel start", barrel.MinY, -0.15f);
            Check(errors, visual + " barrel end", barrel.MaxY, 1.25f);
            Check(errors, visual + " barrel radius", barrel.Radius, 0.12f);
            Check(errors, visual + " knob start", knob.MinY, -1.14f);
            Check(errors, visual + " knob end", knob.MaxY, -0.96f);
            if (handle.Count == 0 || barrel.Count == 0 || knob.Count == 0)
                errors.Add(visual + " imported geometry lost a handle, barrel, or knob component");
            if (-0.85f < handle.MinY || -0.85f > handle.MaxY)
                errors.Add(visual + " authored grip -0.85 is outside the imported handle");
            return errors;
        }

        static Transform FindDeep(Transform root, string name)
        {
            if (root.name.Equals(name, StringComparison.OrdinalIgnoreCase)) return root;
            for (var i = 0; i < root.childCount; i++)
            {
                var found = FindDeep(root.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }

        static int MaterialSubmesh(Material[] materials, string name)
        {
            for (var i = 0; i < materials.Length; i++)
                if (materials[i] != null
                    && materials[i].name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return i;
            return -1;
        }

        readonly struct MeshEnvelope
        {
            public readonly int Count;
            public readonly float MinY;
            public readonly float MaxY;
            public readonly float Radius;

            public MeshEnvelope(int count, float minY, float maxY, float radius)
            {
                Count = count;
                MinY = minY;
                MaxY = maxY;
                Radius = radius;
            }
        }

        static MeshEnvelope Envelope(
            Mesh mesh, int submesh, Matrix4x4 toBat, Predicate<Vector3> include)
        {
            var vertices = mesh.vertices;
            var indices = mesh.GetIndices(submesh);
            var seen = new HashSet<int>();
            var count = 0;
            var minY = float.PositiveInfinity;
            var maxY = float.NegativeInfinity;
            var radius = 0f;
            foreach (var index in indices)
            {
                if (!seen.Add(index)) continue;
                var point = toBat.MultiplyPoint3x4(vertices[index]);
                if (!include(point)) continue;
                count++;
                minY = Mathf.Min(minY, point.y);
                maxY = Mathf.Max(maxY, point.y);
                radius = Mathf.Max(radius, Mathf.Sqrt(point.x * point.x + point.z * point.z));
            }
            return new MeshEnvelope(count, minY, maxY, radius);
        }

        static void Check(List<string> errors, string label, float actual, float expected)
        {
            if (Mathf.Abs(actual - expected) <= 0.003f) return;
            errors.Add(label + " imported at " + actual.ToString("0.######")
                + ", expected " + expected.ToString("0.######"));
        }

        static List<string> EnsureFolders(IReadOnlyList<string> folders)
        {
            var created = new List<string>();
            var assets = Path.GetFullPath(Application.dataPath);
            var unityRoot = Path.GetDirectoryName(assets);
            foreach (var rel in folders)
            {
                var abs = Path.GetFullPath(Path.Combine(unityRoot, rel));
                if (Directory.Exists(abs)) continue;
                Directory.CreateDirectory(abs);
                created.Add(rel);
            }
            return created;
        }
    }
}

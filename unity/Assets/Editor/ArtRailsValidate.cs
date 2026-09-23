using System;
using System.Collections.Generic;
using System.IO;
using GrandSluggers.Sim;
using GrandSluggers.UnityClient;
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
            errors.AddRange(ValidateHomePlateClearance());
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

        /// <summary>Exercise the real kit and imported plate against the rendered home dirt.</summary>
        public static List<string> ValidateHomePlateClearance()
        {
            var errors = new List<string>();
            var root = new GameObject("PlateClearanceValidation");
            var materials = new HashSet<Material>();
            try
            {
                var kit = new FieldKit(root.transform);
                var chalk = kit.Fill(HarborKitPaint.Fill.Chalk);
                kit.HomePad(chalk);
                kit.Plate();
                kit.Boxes();
                var plate = kit.Anchor(FieldKit.HomePlateName);
                var dirt = kit.Anchor(FieldKit.HomeDirtName).GetComponentInChildren<Renderer>().bounds;
                if (plate.Find("PrimitivePlate") != null)
                    errors.Add("home-plate import missing: validation reached the primitive fallback");
                var whiteTopArea = 0f;
                var bounds = new Bounds();
                var first = true;
                foreach (var filter in plate.GetComponentsInChildren<MeshFilter>())
                {
                    var renderer = filter.GetComponent<MeshRenderer>();
                    if (renderer == null) continue;
                    if (first) { bounds = renderer.bounds; first = false; }
                    else bounds.Encapsulate(renderer.bounds);
                    var mesh = filter.sharedMesh;
                    var vertices = mesh.vertices;
                    var mats = renderer.sharedMaterials;
                    for (var sub = 0; sub < mesh.subMeshCount; sub++)
                    {
                        if (sub >= mats.Length || mats[sub] != chalk) continue;
                        var indices = mesh.GetTriangles(sub);
                        for (var i = 0; i < indices.Length; i += 3)
                        {
                            var a = filter.transform.TransformPoint(vertices[indices[i]]);
                            var b = filter.transform.TransformPoint(vertices[indices[i + 1]]);
                            var c = filter.transform.TransformPoint(vertices[indices[i + 2]]);
                            var normal = Vector3.Cross(b - a, c - a);
                            if (normal.normalized.y < 0.9f) continue;
                            if (Mathf.Min(a.y, Mathf.Min(b.y, c.y)) <= dirt.max.y)
                                errors.Add("home-plate white face is buried in home dirt");
                            whiteTopArea += normal.y * 0.5f;
                        }
                    }
                }
                if (whiteTopArea < (float)(HomeSet.PlateW * HomeSet.PlateDepth * 0.5))
                    errors.Add("home-plate has no readable upward white pentagon");
                Check(errors, "plate underside on dirt", bounds.min.y, dirt.max.y);
                Check(errors, "plate left edge", bounds.min.x, (float)(Diamond.Home.X - HomeSet.PlateW * 0.5));
                Check(errors, "plate right edge", bounds.max.x, (float)(Diamond.Home.X + HomeSet.PlateW * 0.5));
                Check(errors, "plate catcher point", bounds.min.z, (float)(Diamond.Home.Z + HomeSet.PlatePointZ));
                Check(errors, "plate pitcher edge", bounds.max.z, (float)(Diamond.Home.Z + HomeSet.PlateFrontZ));
                Check(errors, "plate width matches strike zone", bounds.size.x, (float)(StrikeZoneGeometry.HalfWidth * 2));
                foreach (var box in new[] { FieldKit.BoxLName, FieldKit.BoxRName })
                    foreach (var line in kit.Anchor(box).GetComponentsInChildren<Renderer>())
                        if (line.bounds.min.x <= bounds.max.x && line.bounds.max.x >= bounds.min.x
                            && line.bounds.min.z <= bounds.max.z && line.bounds.max.z >= bounds.min.z)
                            errors.Add("home-plate overlaps " + box + "/" + line.name);
            }
            finally
            {
                foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                    foreach (var mat in renderer.sharedMaterials)
                        if (mat != null) materials.Add(mat);
                UnityEngine.Object.DestroyImmediate(root);
                foreach (var mat in materials) UnityEngine.Object.DestroyImmediate(mat);
            }
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

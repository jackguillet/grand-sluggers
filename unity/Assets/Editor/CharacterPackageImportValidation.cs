using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GrandSluggers.Sim;
using UnityEditor;
using UnityEngine;

namespace GrandSluggers.EditorTools
{
    /// <summary>
    /// Checks Unity's imported package, rather than trusting that an FBX-shaped file exists.
    /// Human deformation and silhouette review remain separate still gates.
    /// </summary>
    public static class CharacterPackageImportValidation
    {
        public static IReadOnlyList<string> Validate(ContentCatalog content)
        {
            var errors = new List<string>();
            foreach (var skin in content.Art.Skins.Values)
            {
                if (!CharacterPackage.IsUnique(skin.Bind)) continue;
                if (!content.Art.TryPackage(skin.Id, out var package))
                {
                    errors.Add("import package manifest missing " + skin.Id);
                    continue;
                }
                ValidatePackage(skin, package, errors);
            }
            return errors;
        }

        static void ValidatePackage(SkinSlot skin, CharacterPackageSpec package, List<string> errors)
        {
            var id = skin.Id;
            var body = AssetDatabase.LoadAssetAtPath<GameObject>(skin.Mesh);
            if (body == null)
            {
                errors.Add("import package " + id + " body cannot load " + skin.Mesh);
                return;
            }
            ValidateModelImporter(id, "body", skin.Mesh!, expectAnimation: false, errors);
            ValidateRig(id, "body", body, skin.Bind, errors);

            var prefabPath = CharacterPackage.PrefabSlot(id);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
                errors.Add("import package " + id + " prefab cannot load " + prefabPath);

            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(package.Controller);
            var playerController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(package.PlayerController);
            if (controller == null)
                errors.Add("import package " + id + " controller cannot load " + package.Controller);
            if (playerController == null)
                errors.Add("import package " + id + " player controller cannot load " + package.PlayerController);
            if (prefab != null)
            {
                var animator = prefab.GetComponentInChildren<Animator>(true);
                if (animator == null)
                    errors.Add("import package " + id + " prefab needs Animator");
                else if (animator.runtimeAnimatorController != controller)
                    errors.Add("import package " + id + " prefab does not use " + package.Controller);
            }

            foreach (var verb in package.Verbs.Where(CharacterPackage.IsReady))
                ValidateReadyVerb(id, body, verb, controller, playerController, errors);

            ValidatePlayerLoad(skin, package, errors);
        }

        static void ValidateModelImporter(
            string id,
            string label,
            string path,
            bool expectAnimation,
            List<string> errors)
        {
            if (AssetImporter.GetAtPath(path) is not ModelImporter importer)
            {
                errors.Add("import package " + id + " " + label + " has no ModelImporter " + path);
                return;
            }
            if (importer.animationType != ModelImporterAnimationType.Generic)
                errors.Add("import package " + id + " " + label + " must import as Generic");
            if (importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
                errors.Add("import package " + id + " " + label + " must create its own avatar");
            if (importer.optimizeGameObjects)
                errors.Add("import package " + id + " " + label + " cannot optimize named sockets away");
            if (expectAnimation && !importer.importAnimation)
                errors.Add("import package " + id + " " + label + " must import animation");
        }

        static void ValidateRig(string id, string label, GameObject root, string bind, List<string> errors)
        {
            var transforms = root.GetComponentsInChildren<Transform>(true);
            foreach (var socket in CharacterPackage.Sockets)
            {
                var count = transforms.Count(t => t.name.Equals(socket, StringComparison.OrdinalIgnoreCase));
                if (count != 1)
                    errors.Add("import package " + id + " " + label + " socket " + socket
                        + " occurs " + count + " times");
            }

            var animator = root.GetComponentInChildren<Animator>(true);
            if (animator == null || animator.avatar == null || !animator.avatar.isValid)
                errors.Add("import package " + id + " " + label + " needs a valid Generic Avatar");

            if (!CharacterPackage.IsSkinned(bind)) return;
            var skins = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (skins.Length == 0)
            {
                errors.Add("import package " + id + " " + label + " has no SkinnedMeshRenderer");
                return;
            }
            foreach (var skin in skins)
            {
                var mesh = skin.sharedMesh;
                if (mesh == null || mesh.vertexCount == 0)
                {
                    errors.Add("import package " + id + " " + label + " skin " + skin.name + " has no mesh");
                    continue;
                }
                if (skin.rootBone == null || skin.bones.Length == 0 || skin.bones.Any(b => b == null))
                    errors.Add("import package " + id + " " + label + " skin " + skin.name + " has unusable bones");
                if (mesh.bindposes == null || mesh.bindposes.Length == 0)
                    errors.Add("import package " + id + " " + label + " skin " + skin.name + " has no bind poses");
                else if (mesh.bindposes.Length != skin.bones.Length)
                    errors.Add("import package " + id + " " + label + " skin " + skin.name
                        + " bind pose count " + mesh.bindposes.Length + " does not match bones " + skin.bones.Length);
                else
                {
                    for (var i = 0; i < mesh.bindposes.Length; i++)
                    {
                        if (skin.bones[i] == null) continue;
                        var expected = skin.bones[i].worldToLocalMatrix * skin.transform.localToWorldMatrix;
                        if (!Finite(mesh.bindposes[i]) || !Approximately(mesh.bindposes[i], expected, 0.02f))
                        {
                            errors.Add("import package " + id + " " + label + " skin " + skin.name
                                + " bind pose " + i + " does not match bone " + skin.bones[i].name);
                            break;
                        }
                    }
                }
                var weights = mesh.boneWeights;
                if (weights == null || weights.Length != mesh.vertexCount)
                {
                    errors.Add("import package " + id + " " + label + " skin " + skin.name
                        + " has no weight for every vertex");
                    continue;
                }
                for (var i = 0; i < weights.Length; i++)
                {
                    var total = weights[i].weight0 + weights[i].weight1 + weights[i].weight2 + weights[i].weight3;
                    if (float.IsNaN(total) || float.IsInfinity(total) || total < 0.99f || total > 1.01f)
                    {
                        errors.Add("import package " + id + " " + label + " skin " + skin.name
                            + " vertex " + i + " weight sum " + total);
                        break;
                    }
                    if (!ValidBone(weights[i].weight0, weights[i].boneIndex0, skin.bones)
                        || !ValidBone(weights[i].weight1, weights[i].boneIndex1, skin.bones)
                        || !ValidBone(weights[i].weight2, weights[i].boneIndex2, skin.bones)
                        || !ValidBone(weights[i].weight3, weights[i].boneIndex3, skin.bones))
                    {
                        errors.Add("import package " + id + " " + label + " skin " + skin.name
                            + " vertex " + i + " uses an invalid weighted bone");
                        break;
                    }
                }
            }
        }

        static bool ValidBone(float weight, int index, Transform[] bones) =>
            weight == 0f || (weight > 0f && !float.IsNaN(weight) && !float.IsInfinity(weight)
                && index >= 0 && index < bones.Length && bones[index] != null);

        static void ValidateReadyVerb(
            string id,
            GameObject body,
            PackageVerbSlot verb,
            RuntimeAnimatorController controller,
            RuntimeAnimatorController playerController,
            List<string> errors)
        {
            var label = "verb " + verb.Verb;
            ValidateModelImporter(id, label, verb.Source, expectAnimation: true, errors);
            var clip = LoadExactClip(verb.Source, verb.Clip);
            if (clip == null)
            {
                errors.Add("import package " + id + " " + label + " clip " + verb.Clip
                    + " cannot load from " + verb.Source);
                return;
            }
            if (clip.isLooping != verb.Loop)
                errors.Add("import package " + id + " " + label + " loop " + clip.isLooping
                    + " does not match manifest " + verb.Loop);
            ValidateEvents(id, verb, clip, errors);
            ValidateBindings(id, label, body, clip, errors);
            ValidateDeformation(id, label, body, clip, verb, errors);
            RequireControllerClip(id, label, controller, verb.Source, verb.Clip, errors);
            RequireControllerClip(id, "player " + label, playerController, verb.PlayerSource, verb.Clip, errors);
        }

        static void ValidateEvents(string id, PackageVerbSlot verb, AnimationClip clip, List<string> errors)
        {
            var actual = AnimationUtility.GetAnimationEvents(clip);
            foreach (var marker in verb.Markers)
            {
                if (!actual.Any(ev => ev.functionName.Equals(marker.Event, StringComparison.OrdinalIgnoreCase)
                    && Math.Abs(ev.time - marker.At) <= 0.001))
                    errors.Add("import package " + id + " verb " + verb.Verb + " missing "
                        + marker.Event + " at " + marker.At.ToString("0.###"));
            }
        }

        static void ValidateBindings(
            string id,
            string label,
            GameObject body,
            AnimationClip clip,
            List<string> errors)
        {
            var bindings = AnimationUtility.GetCurveBindings(clip)
                .Where(binding => binding.type == typeof(Transform)).ToArray();
            if (bindings.Length == 0)
            {
                errors.Add("import package " + id + " " + label + " has no transform bindings");
                return;
            }
            foreach (var path in bindings.Select(binding => binding.path).Distinct())
            {
                if (Resolve(body.transform, path) == null)
                    errors.Add("import package " + id + " " + label + " binding cannot resolve " + path);
            }
        }

        static void RequireControllerClip(
            string id,
            string label,
            RuntimeAnimatorController controller,
            string source,
            string clipName,
            List<string> errors)
        {
            if (controller == null) return;
            var found = controller.animationClips.Any(clip =>
                clip != null
                && clip.name.Equals(clipName, StringComparison.OrdinalIgnoreCase)
                && AssetDatabase.GetAssetPath(clip).Equals(source, StringComparison.OrdinalIgnoreCase));
            if (!found)
                errors.Add("import package " + id + " " + label + " is absent from controller");
        }

        static void ValidatePlayerLoad(SkinSlot skin, CharacterPackageSpec package, List<string> errors)
        {
            var id = skin.Id;
            var body = Resources.Load<GameObject>(ResourceKey(CharacterPackage.PlayerMeshSlot(id)));
            if (body == null)
            {
                errors.Add("player package " + id + " body cannot load through Resources");
                return;
            }
            ValidateRig(id, "player body", body, skin.Bind, errors);
            var controller = Resources.Load<RuntimeAnimatorController>(ResourceKey(package.PlayerController));
            if (controller == null)
                errors.Add("player package " + id + " controller cannot load through Resources");
            foreach (var verb in package.Verbs.Where(CharacterPackage.IsReady))
            {
                ValidateModelImporter(id, "player verb " + verb.Verb, verb.PlayerSource,
                    expectAnimation: true, errors);
                var clip = LoadExactResourceClip(verb.PlayerSource, verb.Clip);
                if (clip == null)
                    errors.Add("player package " + id + " verb " + verb.Verb + " cannot load through Resources");
                else
                {
                    if (clip.isLooping != verb.Loop)
                        errors.Add("player package " + id + " verb " + verb.Verb + " loop " + clip.isLooping
                            + " does not match manifest " + verb.Loop);
                    ValidateEvents(id, verb, clip, errors);
                    ValidateBindings(id, "player verb " + verb.Verb, body, clip, errors);
                    ValidateDeformation(id, "player verb " + verb.Verb, body, clip, verb, errors);
                }
            }
        }

        static void ValidateDeformation(
            string id,
            string label,
            GameObject body,
            AnimationClip clip,
            PackageVerbSlot verb,
            List<string> errors)
        {
            if (!TryBakedBounds(body, null, 0f, out var rest))
            {
                errors.Add("import package " + id + " " + label + " cannot bake its rest skin");
                return;
            }
            var end = Math.Max(0f, clip.length - (1f / Math.Max(clip.frameRate, 1f)));
            var times = new[] { 0f, clip.length * 0.5f, end }.Distinct().ToArray();
            foreach (var time in times)
            {
                if (!TryBakedBounds(body, clip, time, out var sampled))
                {
                    errors.Add("import package " + id + " " + label + " cannot bake skin at "
                        + time.ToString("0.###") + "s");
                    return;
                }
                var restSpan = Math.Max(rest.size.magnitude, 0.001f);
                var sampleSpan = sampled.size.magnitude;
                if (sampleSpan < restSpan * 0.45f || sampleSpan > restSpan * 2.2f
                    || Vector3.Distance(rest.center, sampled.center) > restSpan)
                {
                    errors.Add("import package " + id + " " + label + " deforms outside its body bounds at "
                        + time.ToString("0.###") + "s (rest " + BoundsText(rest)
                        + ", sampled " + BoundsText(sampled) + ")");
                    return;
                }
                if (verb.Verb.Equals("idle", StringComparison.OrdinalIgnoreCase)
                    && (sampled.size.y < rest.size.y * 0.70f
                        || sampled.size.y > rest.size.y * 1.35f
                        || Math.Abs(sampled.center.y - rest.center.y) > rest.size.y * 0.30f))
                {
                    errors.Add("import package " + id + " " + label + " idle collapses or displaces the body at "
                        + time.ToString("0.###") + "s (rest " + BoundsText(rest)
                        + ", sampled " + BoundsText(sampled) + ")");
                    return;
                }
            }
        }

        static bool TryBakedBounds(GameObject body, AnimationClip clip, float time, out Bounds bounds)
        {
            bounds = default;
            var instance = UnityEngine.Object.Instantiate(body);
            instance.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                instance.transform.localScale = Vector3.one;
                foreach (var animator in instance.GetComponentsInChildren<Animator>(true))
                    animator.enabled = false;
                instance.SetActive(true);
                clip?.SampleAnimation(instance, time);

                var any = false;
                foreach (var skin in instance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    var baked = new Mesh();
                    try
                    {
                        skin.BakeMesh(baked);
                        foreach (var vertex in baked.vertices)
                        {
                            var point = skin.transform.TransformPoint(vertex);
                            if (!Finite(point)) return false;
                            if (!any)
                            {
                                bounds = new Bounds(point, Vector3.zero);
                                any = true;
                            }
                            else bounds.Encapsulate(point);
                        }
                    }
                    finally
                    {
                        UnityEngine.Object.DestroyImmediate(baked);
                    }
                }
                return any && Finite(bounds.center) && Finite(bounds.size);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        static bool Finite(Vector3 value) =>
            Finite(value.x) && Finite(value.y) && Finite(value.z);

        static bool Finite(Matrix4x4 value)
        {
            for (var row = 0; row < 4; row++)
                for (var column = 0; column < 4; column++)
                    if (!Finite(value[row, column])) return false;
            return true;
        }

        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        static bool Approximately(Matrix4x4 actual, Matrix4x4 expected, float tolerance)
        {
            for (var row = 0; row < 4; row++)
            {
                for (var column = 0; column < 4; column++)
                {
                    var scale = Math.Max(1f, Math.Abs(expected[row, column]));
                    if (Math.Abs(actual[row, column] - expected[row, column]) > tolerance * scale)
                        return false;
                }
            }
            return true;
        }

        static string BoundsText(Bounds bounds) =>
            "center=" + bounds.center.ToString("F2") + " size=" + bounds.size.ToString("F2");

        static AnimationClip LoadExactClip(string path, string clipName) =>
            AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().FirstOrDefault(clip =>
                !clip.name.StartsWith("__preview", StringComparison.Ordinal)
                && clip.name.Equals(clipName, StringComparison.OrdinalIgnoreCase));

        static AnimationClip LoadExactResourceClip(string path, string clipName) =>
            Resources.LoadAll<AnimationClip>(ResourceKey(path)).FirstOrDefault(clip =>
                clip != null && !clip.name.StartsWith("__preview", StringComparison.Ordinal)
                && clip.name.Equals(clipName, StringComparison.OrdinalIgnoreCase));

        static string ResourceKey(string path)
        {
            const string prefix = "Assets/Resources/";
            var key = path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                ? path.Substring(prefix.Length) : path;
            var extension = Path.GetExtension(key);
            return extension.Length == 0 ? key : key.Substring(0, key.Length - extension.Length);
        }

        static Transform Resolve(Transform root, string path)
        {
            if (string.IsNullOrEmpty(path)) return root;
            var found = root.Find(path);
            if (found != null) return found;
            var slash = path.IndexOf('/');
            if (slash > 0 && path.Substring(0, slash).Equals(root.name, StringComparison.OrdinalIgnoreCase))
                return root.Find(path.Substring(slash + 1));
            return null;
        }
    }
}

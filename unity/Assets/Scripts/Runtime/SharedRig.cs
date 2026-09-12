using System;
using System.Collections.Generic;
using GrandSluggers.Sim;
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    /// <summary>
    /// The one body: hero-shared.fbx bound by bone name, painted by material
    /// role, with the captain's extras dropped in rig space and reparented to
    /// their socket bones. A missing FBX is a capsule and a logged error, never
    /// a second body-building path. Contract: docs/character-motion.md.
    /// </summary>
    public static class SharedRig
    {
        public sealed class Chain
        {
            /// <summary>Scale, bounce, and squash live here so clip curves on the rig root stay harmless.</summary>
            public Transform Body;
            /// <summary>The FBX root: the Animator and the clip curve paths.</summary>
            public Transform Root;
            public Transform Torso, Head;
            public Transform LUpper, LFore, RUpper, RFore;
            public Transform LThigh, LShin, RThigh, RShin;
            public Transform Bat, Glove;
            public Transform Ring;
            public Animator Animator;
            public Vector3 BaseScale;
            public bool Placeholder;
        }

        static readonly string[] Bones =
            { "torso", "head", "lUpper", "lFore", "rUpper", "rFore", "lThigh", "lShin", "rThigh", "rShin" };

        static bool _missingReported;

        public static Chain Spawn(Transform parent, Character who, SkinSlot skin)
        {
            var spec = Silhouette.Proportions(Silhouette.BodyType(who));
            var scale = Silhouette.SharedRootScale(spec);
            var body = new GameObject("body").transform;
            body.SetParent(parent, false);
            var chain = new Chain
            {
                Body = body,
                BaseScale = new Vector3((float)scale.X, (float)scale.Y, (float)scale.Z)
            };
            body.localScale = chain.BaseScale;

            var prefab = ArtBinder.LoadSharedRigPrefab();
            if (prefab == null || !TryBind(chain, prefab, who, skin))
                BuildPlaceholder(chain, who);
            AttachRing(chain);
            return chain;
        }

        static bool TryBind(Chain chain, GameObject prefab, Character who, SkinSlot skin)
        {
            var go = UnityEngine.Object.Instantiate(prefab, chain.Body, false);
            go.name = "rig";
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            var found = new Dictionary<string, Transform>(StringComparer.OrdinalIgnoreCase);
            foreach (var name in Bones)
            {
                var bone = FindDeep(go.transform, name);
                if (bone == null)
                {
                    Debug.LogError("hero-shared.fbx is missing bone " + name + "; using the placeholder body for " + who.Id);
                    UnityEngine.Object.Destroy(go);
                    return false;
                }
                found[name] = bone;
            }
            chain.Root = go.transform;
            chain.Torso = found["torso"];
            chain.Head = found["head"];
            chain.LUpper = found["lUpper"];
            chain.LFore = found["lFore"];
            chain.RUpper = found["rUpper"];
            chain.RFore = found["rFore"];
            chain.LThigh = found["lThigh"];
            chain.LShin = found["lShin"];
            chain.RThigh = found["rThigh"];
            chain.RShin = found["rShin"];
            chain.Bat = FindDeep(go.transform, "bat") ?? EnsureBone(chain.RFore, "bat", new Vector3(0, -0.68f, 0.12f));
            chain.Glove = FindDeep(go.transform, "glove") ?? EnsureBone(chain.LFore, "glove", new Vector3(0, -0.68f, 0.12f));

            // The takes are armature-only FBX files, so Unity makes the armature
            // node their prefab root and their curve paths start at "root/...".
            // The body FBX has mesh siblings and gets a wrapper root above the
            // same armature node. Drive the Animator from that node so both
            // agree on paths.
            var armatureNode = found["torso"].parent != null && found["torso"].parent.parent != null
                ? found["torso"].parent.parent
                : go.transform;
            foreach (var other in go.GetComponentsInChildren<Animator>(true))
                other.enabled = false;
            var animator = armatureNode.GetComponent<Animator>();
            if (animator == null) animator = armatureNode.gameObject.AddComponent<Animator>();
            animator.runtimeAnimatorController = null;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.enabled = true;
            chain.Animator = animator;

            Paint(go, who.Faction);
            HideLookRays(go.transform);
            AttachExtras(chain, who, skin);
            return true;
        }

        /// <summary>Material names on the FBX are palette roles. Every role paints; nothing stays import-white.</summary>
        internal static void Paint(GameObject go, string faction)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                if (r is LineRenderer) continue;
                var mats = r.sharedMaterials;
                var changed = false;
                for (var i = 0; i < mats.Length; i++)
                {
                    var role = RoleOf(mats[i]);
                    var painted = MaterialFor(role, faction);
                    if (painted == null) continue;
                    painted.name = role;
                    mats[i] = painted;
                    changed = true;
                }
                if (changed) r.sharedMaterials = mats;
            }
        }

        static string RoleOf(Material m)
        {
            if (m == null) return "";
            var n = m.name;
            var cut = n.IndexOf(" (", StringComparison.Ordinal);
            if (cut > 0) n = n.Substring(0, cut);
            cut = n.IndexOf('.');
            if (cut > 0) n = n.Substring(0, cut);
            return n.Trim().ToLowerInvariant();
        }

        static Material MaterialFor(string role, string faction) => role switch
        {
            "jersey" => Look.Toon(Colors.Body(faction)),
            "trim" => Look.Toon(Colors.Accent(faction)),
            "flesh" => Look.Toon(Colors.SkinTone(faction)),
            "slack" => Look.Lit(Color.Lerp(Color.white, Colors.Body(faction), 0.12f), smooth: 0.28f),
            "leather" => Look.Lit(Color.Lerp(Colors.Body(faction), Color.black, 0.38f), smooth: 0.18f),
            "gold" => Look.Lit(Colors.Gold, smooth: 0.4f),
            "ink" => Look.Lit(new Color(0.08f, 0.07f, 0.07f), smooth: 0.05f),
            "white" => Look.Unlit(Color.white),
            "ice" => Look.Lit(new Color(0.85f, 0.95f, 1f), smooth: 0.55f),
            "glass" => Look.Lit(new Color(0.2f, 0.85f, 0.55f), smooth: 0.6f),
            "sash" => Look.Lit(new Color(0.75f, 0.92f, 1f), smooth: 0.45f),
            "sneaker" => Look.Lit(new Color(0.96f, 0.96f, 0.96f), smooth: 0.18f),
            "ember" => Look.Unlit(Colors.EmberFire),
            _ => null
        };

        static void AttachExtras(Chain chain, Character who, SkinSlot skin)
        {
            var art = ArtBinder.Art;
            if (art == null || skin.Extras == null) return;
            foreach (var id in skin.Extras)
            {
                if (!art.TryExtra(id, out var extra))
                {
                    Debug.LogWarning("extra " + id + " on " + who.Id + " is not in extras.json");
                    continue;
                }
                var bone = BoneOf(chain, extra.Bone);
                var src = ArtBinder.LoadExtraMesh(id);
                if (bone == null || src == null)
                {
                    Debug.LogWarning("extra " + id + " for " + who.Id + " has no kit mesh or bone " + extra.Bone);
                    continue;
                }
                Hang(id, src, chain.Root, bone, who.Faction, mirror: false);
                if (id.Equals("sneakers", StringComparison.OrdinalIgnoreCase) && chain.RShin != null)
                    Hang(id, src, chain.Root, chain.RShin, who.Faction, mirror: true);
                foreach (var hidden in extra.Hides)
                    HideNamed(chain.Root, hidden);
            }
        }

        /// <summary>
        /// Kit pieces are authored on the body in rig space. Drop the piece at
        /// the rig root, then reparent it to its socket bone keeping the world
        /// pose, so it follows the bone without any assumption about how the
        /// importer oriented that bone's local frame.
        /// </summary>
        static void Hang(string id, GameObject src, Transform rig, Transform bone, string faction, bool mirror)
        {
            var piece = UnityEngine.Object.Instantiate(src, rig, false);
            piece.name = id;
            piece.transform.localPosition = Vector3.zero;
            piece.transform.localRotation = Quaternion.identity;
            piece.transform.localScale = mirror ? new Vector3(-1f, 1f, 1f) : Vector3.one;
            piece.transform.SetParent(bone, true);
            foreach (var anim in piece.GetComponentsInChildren<Animator>(true))
                anim.enabled = false;
            Paint(piece, faction);
        }

        public static Transform BoneOf(Chain c, string name)
        {
            if (name.Equals("torso", StringComparison.OrdinalIgnoreCase)) return c.Torso;
            if (name.Equals("head", StringComparison.OrdinalIgnoreCase)) return c.Head;
            if (name.Equals("lUpper", StringComparison.OrdinalIgnoreCase)) return c.LUpper;
            if (name.Equals("lFore", StringComparison.OrdinalIgnoreCase)) return c.LFore;
            if (name.Equals("rUpper", StringComparison.OrdinalIgnoreCase)) return c.RUpper;
            if (name.Equals("rFore", StringComparison.OrdinalIgnoreCase)) return c.RFore;
            if (name.Equals("lThigh", StringComparison.OrdinalIgnoreCase)) return c.LThigh;
            if (name.Equals("lShin", StringComparison.OrdinalIgnoreCase)) return c.LShin;
            if (name.Equals("rThigh", StringComparison.OrdinalIgnoreCase)) return c.RThigh;
            if (name.Equals("rShin", StringComparison.OrdinalIgnoreCase)) return c.RShin;
            if (name.Equals("bat", StringComparison.OrdinalIgnoreCase)) return c.Bat;
            if (name.Equals("glove", StringComparison.OrdinalIgnoreCase)) return c.Glove;
            if (name.Equals("root", StringComparison.OrdinalIgnoreCase)) return c.Root;
            return null;
        }

        /// <summary>Missing FBX: a capsule that does not crash, with named sockets so gear and cameras still resolve.</summary>
        static void BuildPlaceholder(Chain chain, Character who)
        {
            if (!_missingReported)
            {
                Debug.LogError("hero-shared.fbx did not bind; drawing placeholder capsules. Bake it with tools/blender/hero_shared_blockout.py");
                _missingReported = true;
            }
            chain.Placeholder = true;
            var root = new GameObject("rig").transform;
            root.SetParent(chain.Body, false);
            chain.Root = root;
            var jersey = Look.Toon(Colors.Body(who.Faction));
            var flesh = Look.Toon(Colors.SkinTone(who.Faction));
            chain.Torso = Look.Prim(PrimitiveType.Capsule, "torso", root, new Vector3(0, 2.0f, 0), new Vector3(1.4f, 1.2f, 1.0f), jersey).transform;
            chain.Head = Look.Prim(PrimitiveType.Sphere, "head", root, new Vector3(0, 4.05f, 0), Vector3.one * 1.7f, flesh).transform;
            chain.LUpper = EnsureBone(chain.Torso, "lUpper", new Vector3(-0.95f, 0.2f, 0));
            chain.LFore = EnsureBone(chain.LUpper, "lFore", new Vector3(0, -0.9f, 0));
            chain.RUpper = EnsureBone(chain.Torso, "rUpper", new Vector3(0.95f, 0.2f, 0));
            chain.RFore = EnsureBone(chain.RUpper, "rFore", new Vector3(0, -0.9f, 0));
            chain.LThigh = EnsureBone(root, "lThigh", new Vector3(-0.42f, 1.05f, 0));
            chain.LShin = EnsureBone(chain.LThigh, "lShin", new Vector3(0, -0.6f, 0));
            chain.RThigh = EnsureBone(root, "rThigh", new Vector3(0.42f, 1.05f, 0));
            chain.RShin = EnsureBone(chain.RThigh, "rShin", new Vector3(0, -0.6f, 0));
            chain.Bat = EnsureBone(chain.RFore, "bat", new Vector3(0, -0.68f, 0.12f));
            chain.Glove = EnsureBone(chain.LFore, "glove", new Vector3(0, -0.68f, 0.12f));
            chain.Animator = null;
        }

        static void AttachRing(Chain chain)
        {
            var root = new GameObject("Mark");
            var max = Math.Max(SetTells.RingScale(1), 0.01);
            var minor = (float)(SetTells.RingThickFt / max);
            var pink = Look.Unlit(new Color(1f, 0.48f, 0.78f));
            var gold = Look.Unlit(Colors.Gold);
            // World-space unit torus. HeroActor places it on packed dirt and
            // scales uniformly by RingScale so the tube stays a tube, not a pancake.
            Look.Torus("Pink", root.transform, 1f, minor, pink);
            Look.Torus("Gold", root.transform, 0.86f, minor * 0.75f, gold);
            chain.Ring = root.transform;
            root.SetActive(false);
        }

        static Transform EnsureBone(Transform parent, string name, Vector3 localPos)
        {
            var existing = FindDeep(parent, name);
            if (existing != null) return existing;
            var bone = new GameObject(name).transform;
            bone.SetParent(parent, false);
            bone.localPosition = localPos;
            bone.localRotation = Quaternion.identity;
            return bone;
        }

        /// <summary>F2 leftover look helpers. A white ray from the brim must not ship.</summary>
        static void HideLookRays(Transform root)
        {
            if (root == null) return;
            if (IsLookName(root.name))
            {
                foreach (var r in root.GetComponentsInChildren<Renderer>(true))
                    r.enabled = false;
                root.gameObject.SetActive(false);
                return;
            }
            for (var i = 0; i < root.childCount; i++)
                HideLookRays(root.GetChild(i));
        }

        static bool IsLookName(string n) =>
            n.Equals("Look", StringComparison.OrdinalIgnoreCase)
            || n.Equals("LookS", StringComparison.OrdinalIgnoreCase)
            || n.Equals("LookRay", StringComparison.OrdinalIgnoreCase);

        static void HideNamed(Transform root, string name)
        {
            var t = FindDeep(root, name);
            if (t != null) t.gameObject.SetActive(false);
        }

        public static Transform FindDeep(Transform t, string name)
        {
            if (t.name.Equals(name, StringComparison.OrdinalIgnoreCase)) return t;
            for (var i = 0; i < t.childCount; i++)
            {
                var f = FindDeep(t.GetChild(i), name);
                if (f != null) return f;
            }
            return null;
        }
    }
}

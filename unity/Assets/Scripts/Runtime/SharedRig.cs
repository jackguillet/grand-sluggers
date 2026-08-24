using System;
using System.Collections.Generic;
using GrandSluggers.Sim;
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    /// <summary>
    /// One body chain. Captains are extras on this topology. Six cuts are
    /// SMS-ladder outlines (kid / pageant / speed / brick / ape / slug), not
    /// six skeletons. hero-shared.fbx binds when present; otherwise primitives.
    /// </summary>
    public static class SharedRig
    {
        public struct BoneBind
        {
            public Quaternion Torso, Head, LUpper, LFore, RUpper, RFore, LThigh, LShin, RThigh, RShin;
        }

        public sealed class Chain
        {
            public Transform Root, Torso, Head, Cap;
            public Transform LUpper, LFore, RUpper, RFore;
            public Transform LThigh, LShin, RThigh, RShin;
            public Transform Ring;
            public Vector3 BaseScale;
            public Vector3 TorsoRest;
            public float HunchDeg;
            public BoneBind Bind;
        }

        public static Chain Spawn(Transform parent, Character who, IReadOnlyList<string> extras)
        {
            extras ??= Array.Empty<string>();
            bool Has(string id)
            {
                for (var i = 0; i < extras.Count; i++)
                    if (extras[i].Equals(id, StringComparison.OrdinalIgnoreCase)) return true;
                return false;
            }

            var body = Silhouette.BodyType(who);
            var spec = Silhouette.Proportions(body);
            var faction = who.Faction;
            var jersey = Look.Toon(Colors.Body(faction));
            var trim = Look.Toon(Colors.Accent(faction));
            var flesh = Look.Toon(Colors.SkinTone(faction));
            var slack = Look.Lit(Color.Lerp(Color.white, Colors.Body(faction), 0.12f), smooth: 0.28f);
            var leather = Look.Lit(Color.Lerp(Colors.Body(faction), Color.black, 0.38f), smooth: 0.18f);
            var ink = Look.Lit(new Color(0.08f, 0.07f, 0.07f), smooth: 0.05f);
            var white = Look.Unlit(Color.white);

            var prefab = ArtBinder.LoadSharedRigPrefab();
            if (prefab != null)
            {
                var bound = TryBindDrop(parent, body, spec, Has, prefab, jersey, trim, flesh, slack, leather, ink, white);
                if (bound != null) return bound;
            }

            var chain = new Chain();
            chain.Root = new GameObject("root").transform;
            chain.Root.SetParent(parent, false);
            // Stature is Height. Girth is Width. Do not squash the whole toy on XZ.
            chain.BaseScale = new Vector3(
                Mathf.Lerp(spec.Height, spec.Width, 0.55f),
                spec.Height,
                Mathf.Lerp(spec.Height, spec.Width, 0.45f)) * Silhouette.ToyScale;
            chain.Root.localScale = chain.BaseScale;
            chain.Bind = IdentityBind();

            var kid = body == "rio";
            var pageant = body == "vale";
            var speed = body == "zig";
            var brick = body == "brondo";
            var ape = body == "konga";
            var slug = body == "ashlord";

            var hipScale = brick
                ? new Vector3(1.75f, 1.05f, 1.35f)
                : speed
                    ? new Vector3(1.55f, 0.82f, 1.22f)
                    : pageant
                        ? new Vector3(1.05f, 0.92f, 0.88f)
                        : new Vector3(1.42f, 1.00f, 1.12f);
            Look.Prim(PrimitiveType.Sphere, "Hip", chain.Root, new Vector3(0, speed ? 0.82f : 1.05f, ape ? 0.12f : 0), hipScale, slack);

            var torsoKind = brick ? PrimitiveType.Cube : PrimitiveType.Capsule;
            var torsoScale = brick
                ? new Vector3(1.85f, 1.28f, 1.28f)
                : pageant
                    ? new Vector3(1.12f, 0.95f, 0.78f) * spec.Torso
                    : speed
                        ? new Vector3(1.55f, 0.72f, 1.05f) * spec.Torso
                        : slug
                            ? new Vector3(1.72f, 1.05f, 1.22f) * spec.Torso
                            : new Vector3(1.42f, 0.92f, 0.98f) * spec.Torso;
            var torsoY = pageant ? 2.55f : speed ? 1.85f : slug ? 2.48f : 2.28f;
            var torsoZ = ape ? 0.22f : 0f;
            chain.Torso = Look.Prim(torsoKind, "torso", chain.Root, new Vector3(0, torsoY, torsoZ), torsoScale, jersey).transform;
            chain.TorsoRest = chain.Torso.localPosition;
            chain.HunchDeg = ape ? 18f : slug ? 6f : 0f;
            Look.Prim(PrimitiveType.Cube, "Stripe", chain.Torso, new Vector3(0, 0.08f, 0.48f), new Vector3(pageant ? 0.18f : 0.32f, 0.78f, 0.1f), trim);
            Look.Prim(PrimitiveType.Cylinder, "Collar", chain.Torso, new Vector3(0, 0.48f, 0.06f), new Vector3(brick ? 0.95f : 0.68f, 0.1f, brick ? 0.95f : 0.68f), trim);

            if (Has("neck") || pageant)
            {
                var neckH = pageant ? 0.72f : 0.42f;
                Look.Prim(PrimitiveType.Cylinder, "Neck", chain.Root, new Vector3(0, torsoY + 0.95f, 0), new Vector3(0.36f, neckH, 0.36f), flesh);
            }
            if (brick)
                Look.Prim(PrimitiveType.Cylinder, "Neck", chain.Root, new Vector3(0, torsoY + 0.82f, 0), new Vector3(0.72f, 0.28f, 0.72f), flesh);
            if (Has("belly") || ape)
                Look.Prim(PrimitiveType.Sphere, "Belly", chain.Torso, new Vector3(0, -0.38f, 0.48f), new Vector3(1.28f, 0.92f, 1.12f), jersey);
            if (Has("sash") || pageant)
                Look.Prim(PrimitiveType.Cube, "Sash", chain.Torso, new Vector3(0.12f, 0.02f, 0.52f), new Vector3(1.05f, 0.16f, 0.08f), Look.Lit(new Color(0.75f, 0.92f, 1f), smooth: 0.45f));
            if (Has("cape") || slug)
            {
                Look.Prim(PrimitiveType.Cube, "Cape", chain.Torso, new Vector3(0, -0.28f, -0.72f), new Vector3(1.45f, 1.55f, 0.14f), trim);
                Look.Prim(PrimitiveType.Cube, "CapeFlare", chain.Torso, new Vector3(0, -0.85f, -0.78f), new Vector3(1.65f, 0.55f, 0.12f), trim);
            }

            var headY = pageant ? 4.85f : speed ? 3.35f : brick ? 4.05f : slug ? 4.45f : 4.05f;
            var headKind = brick ? PrimitiveType.Cube : PrimitiveType.Sphere;
            // Head size tracks Head/Height so a baby is a head on legs, not a 1.95x cap blob.
            var headMul = 1.12f * (spec.Head / Mathf.Max(0.01f, spec.Height));
            var headScale = brick
                ? new Vector3(1.45f, 1.18f, 1.35f) * headMul * 0.72f
                : Vector3.one * headMul;
            chain.Head = Look.Prim(headKind, "head", chain.Root, new Vector3(0, headY, ape ? 0.28f : 0), headScale, flesh).transform;

            Face(chain.Head, Has("ember-eyes") || slug, ink, white, flesh);

            if (Has("cheeks") || kid)
            {
                Look.Prim(PrimitiveType.Sphere, "CheekL", chain.Head, new Vector3(-0.48f, -0.22f, 0.38f), Vector3.one * 0.42f, flesh);
                Look.Prim(PrimitiveType.Sphere, "CheekR", chain.Head, new Vector3(0.48f, -0.22f, 0.38f), Vector3.one * 0.42f, flesh);
            }
            if (Has("crown") || pageant)
            {
                var ice = Look.Lit(new Color(0.85f, 0.95f, 1f), smooth: 0.55f);
                Look.Prim(PrimitiveType.Cylinder, "Crown", chain.Head, new Vector3(0, 0.62f, 0), new Vector3(0.92f, 0.18f, 0.92f), ice);
                Look.Prim(PrimitiveType.Cube, "PointA", chain.Head, new Vector3(0, 0.95f, 0), new Vector3(0.16f, 0.42f, 0.16f), ice);
                Look.Prim(PrimitiveType.Cube, "PointB", chain.Head, new Vector3(0.28f, 0.88f, 0), new Vector3(0.12f, 0.28f, 0.12f), ice);
                Look.Prim(PrimitiveType.Cube, "PointC", chain.Head, new Vector3(-0.28f, 0.88f, 0), new Vector3(0.12f, 0.28f, 0.12f), ice);
                chain.Cap = Look.Prim(PrimitiveType.Cylinder, "Cap", chain.Head, new Vector3(0, 0.42f, 0), new Vector3(0.01f, 0.01f, 0.01f), trim).transform;
            }
            else
            {
                // Dome hat, not a flat cylinder (that read as a cube from SET).
                Look.Prim(PrimitiveType.Sphere, "Dome", chain.Head, new Vector3(0, 0.42f, 0), new Vector3(1.18f, 0.72f, 1.18f), trim);
                chain.Cap = Look.Prim(PrimitiveType.Cylinder, "Cap", chain.Head, new Vector3(0, 0.28f, 0), new Vector3(1.22f, 0.14f, 1.22f), trim).transform;
                var brim = Look.Prim(PrimitiveType.Cylinder, "Brim", chain.Cap, new Vector3(0, -0.85f, 0.22f),
                    Has("brim") || kid ? new Vector3(1.85f, 0.08f, 1.55f) : new Vector3(1.45f, 0.07f, 1.22f),
                    Look.Lit(Colors.Gold, smooth: 0.4f)).transform;
                brim.localRotation = Quaternion.Euler(12, 0, 0);
            }
            if (Has("horns") || slug)
            {
                var hornL = Look.Prim(PrimitiveType.Cylinder, "HornL", chain.Head, new Vector3(-0.48f, 0.62f, -0.12f), new Vector3(0.28f, 0.72f, 0.28f), trim).transform;
                var hornR = Look.Prim(PrimitiveType.Cylinder, "HornR", chain.Head, new Vector3(0.48f, 0.62f, -0.12f), new Vector3(0.28f, 0.72f, 0.28f), trim).transform;
                hornL.localRotation = Quaternion.Euler(0, 0, 22);
                hornR.localRotation = Quaternion.Euler(0, 0, -22);
                Look.Prim(PrimitiveType.Sphere, "HornTipL", hornL, new Vector3(0, 0.55f, 0), Vector3.one * 0.55f, trim);
                Look.Prim(PrimitiveType.Sphere, "HornTipR", hornR, new Vector3(0, 0.55f, 0), Vector3.one * 0.55f, trim);
            }
            if (Has("snout") || ape)
            {
                Look.Prim(PrimitiveType.Sphere, "Snout", chain.Head, new Vector3(0, -0.22f, 0.72f), new Vector3(1.05f, 0.68f, 0.95f), flesh);
                Look.Prim(PrimitiveType.Sphere, "NostrilL", chain.Head, new Vector3(-0.18f, -0.12f, 1.05f), Vector3.one * 0.18f, ink);
                Look.Prim(PrimitiveType.Sphere, "NostrilR", chain.Head, new Vector3(0.18f, -0.12f, 1.05f), Vector3.one * 0.18f, ink);
            }
            if (Has("goggles") || speed)
            {
                var glass = Look.Lit(new Color(0.2f, 0.85f, 0.55f), smooth: 0.6f);
                Look.Prim(PrimitiveType.Cylinder, "GogL", chain.Head, new Vector3(-0.32f, 0.08f, 0.52f), new Vector3(0.62f, 0.12f, 0.62f), glass);
                Look.Prim(PrimitiveType.Cylinder, "GogR", chain.Head, new Vector3(0.32f, 0.08f, 0.52f), new Vector3(0.62f, 0.12f, 0.62f), glass);
                Look.Prim(PrimitiveType.Cube, "GogBridge", chain.Head, new Vector3(0, 0.08f, 0.5f), new Vector3(0.28f, 0.08f, 0.12f), trim);
            }
            if (Has("brick-jaw") || brick)
                Look.Prim(PrimitiveType.Cube, "Jaw", chain.Head, new Vector3(0, -0.48f, 0.28f), new Vector3(1.18f, 0.42f, 0.82f), flesh);

            var armLen = speed ? 0.48f * spec.Arms : 0.58f * spec.Arms;
            var armThick = brick ? 0.72f : ape ? 0.62f : pageant ? 0.38f : 0.52f;
            var armY = ape ? -0.08f : 0.18f;
            var armX = (ape ? 1.05f : 0.88f) * spec.Arms;
            chain.LUpper = Look.Prim(PrimitiveType.Capsule, "lUpper", chain.Torso, new Vector3(-armX, armY, ape ? 0.12f : 0), new Vector3(armThick, armLen, armThick), jersey).transform;
            chain.LFore = Look.Prim(PrimitiveType.Capsule, "lFore", chain.LUpper, new Vector3(0, -0.82f, 0), new Vector3(0.92f, 0.72f, 0.92f), flesh).transform;
            chain.RUpper = Look.Prim(PrimitiveType.Capsule, "rUpper", chain.Torso, new Vector3(armX, armY, ape ? 0.12f : 0), new Vector3(armThick, armLen, armThick), jersey).transform;
            chain.RFore = Look.Prim(PrimitiveType.Capsule, "rFore", chain.RUpper, new Vector3(0, -0.82f, 0), new Vector3(0.92f, 0.72f, 0.92f), flesh).transform;
            Hand(chain.LFore, flesh, ink);
            Hand(chain.RFore, flesh, ink);

            var thighLen = speed ? 0.42f : pageant ? 0.72f : 0.58f;
            var thighThick = brick ? 0.78f : speed ? 0.72f : pageant ? 0.42f : 0.56f;
            var thighSpread = brick ? 0.55f : speed ? 0.38f : 0.42f;
            var thighY = speed ? 0.48f : 0.62f;
            chain.LThigh = Look.Prim(PrimitiveType.Capsule, "lThigh", chain.Root, new Vector3(-thighSpread, thighY, 0), new Vector3(thighThick, thighLen, thighThick), slack).transform;
            chain.LShin = Look.Prim(PrimitiveType.Capsule, "lShin", chain.LThigh, new Vector3(0, speed ? -0.55f : -0.82f, 0), new Vector3(0.88f, speed ? 0.42f : 0.62f, 0.88f), slack).transform;
            chain.RThigh = Look.Prim(PrimitiveType.Capsule, "rThigh", chain.Root, new Vector3(thighSpread, thighY, 0), new Vector3(thighThick, thighLen, thighThick), slack).transform;
            chain.RShin = Look.Prim(PrimitiveType.Capsule, "rShin", chain.RThigh, new Vector3(0, speed ? -0.55f : -0.82f, 0), new Vector3(0.88f, speed ? 0.42f : 0.62f, 0.88f), slack).transform;
            var shoe = Has("sneakers") || kid
                ? new Vector3(1.22f, 0.52f, 1.72f)
                : slug
                    ? new Vector3(1.15f, 0.58f, 1.55f)
                    : speed
                        ? new Vector3(1.05f, 0.42f, 1.35f)
                        : pageant
                            ? new Vector3(0.62f, 0.28f, 1.05f)
                            : new Vector3(0.92f, 0.38f, 1.32f);
            Shoe(chain.LShin, shoe, leather, trim, kid || Has("sneakers"));
            Shoe(chain.RShin, shoe, leather, trim, kid || Has("sneakers"));

            var pink = Look.Unlit(new Color(1f, 0.48f, 0.78f));
            chain.Ring = Look.Prim(PrimitiveType.Cylinder, "Mark", parent, new Vector3(0, (float)SetTells.RingHeightFt, 0), new Vector3(2.6f, 0.05f, 2.6f), pink).transform;
            Look.Prim(PrimitiveType.Cylinder, "MarkGold", chain.Ring, Vector3.zero, new Vector3(0.86f, 1.4f, 0.86f), Look.Unlit(Colors.Gold));
            chain.Ring.gameObject.SetActive(false);
            return chain;
        }

        static void Face(Transform head, bool ember, Material ink, Material white, Material flesh)
        {
            var iris = ember ? Look.Unlit(Colors.EmberFire) : ink;
            // Sclera in front of the head, iris sitting on it — not a black cap-dot.
            Look.Prim(PrimitiveType.Sphere, "WhiteL", head, new Vector3(-0.28f, 0.10f, 0.52f), Vector3.one * 0.42f, white);
            Look.Prim(PrimitiveType.Sphere, "WhiteR", head, new Vector3(0.28f, 0.10f, 0.52f), Vector3.one * 0.42f, white);
            Look.Prim(PrimitiveType.Sphere, "EyeL", head, new Vector3(-0.28f, 0.10f, 0.68f), Vector3.one * 0.22f, iris);
            Look.Prim(PrimitiveType.Sphere, "EyeR", head, new Vector3(0.28f, 0.10f, 0.68f), Vector3.one * 0.22f, iris);
            Look.Prim(PrimitiveType.Cube, "BrowL", head, new Vector3(-0.28f, 0.34f, 0.52f), new Vector3(0.38f, 0.08f, 0.12f), ink);
            Look.Prim(PrimitiveType.Cube, "BrowR", head, new Vector3(0.28f, 0.34f, 0.52f), new Vector3(0.38f, 0.08f, 0.12f), ink);
            Look.Prim(PrimitiveType.Sphere, "Mouth", head, new Vector3(0, -0.28f, 0.52f), new Vector3(0.42f, 0.16f, 0.18f), ink);
            Look.Prim(PrimitiveType.Sphere, "EarL", head, new Vector3(-0.52f, 0.04f, 0.02f), Vector3.one * 0.28f, flesh);
            Look.Prim(PrimitiveType.Sphere, "EarR", head, new Vector3(0.52f, 0.04f, 0.02f), Vector3.one * 0.28f, flesh);
        }

        static void Hand(Transform fore, Material flesh, Material ink)
        {
            var palm = Look.Prim(PrimitiveType.Sphere, "Palm", fore, new Vector3(0, -0.68f, 0), Vector3.one * 0.62f, flesh).transform;
            Look.Prim(PrimitiveType.Capsule, "Thumb", palm, new Vector3(-0.38f, 0.02f, 0.18f), new Vector3(0.22f, 0.28f, 0.22f), flesh);
            Look.Prim(PrimitiveType.Capsule, "F1", palm, new Vector3(-0.16f, -0.28f, 0.12f), new Vector3(0.16f, 0.28f, 0.16f), flesh);
            Look.Prim(PrimitiveType.Capsule, "F2", palm, new Vector3(0.02f, -0.32f, 0.12f), new Vector3(0.16f, 0.32f, 0.16f), flesh);
            Look.Prim(PrimitiveType.Capsule, "F3", palm, new Vector3(0.18f, -0.26f, 0.12f), new Vector3(0.15f, 0.26f, 0.15f), flesh);
            Look.Prim(PrimitiveType.Sphere, "Knuckle", palm, new Vector3(0, 0.12f, 0.18f), Vector3.one * 0.18f, ink);
        }

        static void Shoe(Transform shin, Vector3 scale, Material leather, Material trim, bool fat)
        {
            var y = -0.62f;
            Look.Prim(PrimitiveType.Cube, "Shoe", shin, new Vector3(0, y, 0.22f), scale, leather);
            Look.Prim(PrimitiveType.Sphere, "Toe", shin, new Vector3(0, y - 0.02f, 0.22f + scale.z * 0.28f), new Vector3(scale.x * 0.85f, scale.y * 0.9f, scale.x * 0.7f), leather);
            if (fat)
                Look.Prim(PrimitiveType.Cube, "Stripe", shin, new Vector3(0, y + 0.12f, 0.18f), new Vector3(scale.x * 0.55f, 0.12f, scale.z * 0.55f), trim);
        }

        static Chain TryBindDrop(
            Transform parent, string body, Silhouette.Spec spec, Func<string, bool> Has,
            GameObject prefab, Material jersey, Material trim, Material flesh, Material slack,
            Material leather, Material ink, Material white)
        {
            var go = UnityEngine.Object.Instantiate(prefab, parent, false);
            go.name = "root";
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            foreach (var anim in go.GetComponentsInChildren<Animator>(true))
                anim.enabled = false;

            var chain = new Chain();
            chain.Root = go.transform;
            chain.Torso = FindDeep(go.transform, "torso");
            chain.Head = FindDeep(go.transform, "head");
            chain.LUpper = FindDeep(go.transform, "lUpper");
            chain.LFore = FindDeep(go.transform, "lFore");
            chain.RUpper = FindDeep(go.transform, "rUpper");
            chain.RFore = FindDeep(go.transform, "rFore");
            chain.LThigh = FindDeep(go.transform, "lThigh");
            chain.LShin = FindDeep(go.transform, "lShin");
            chain.RThigh = FindDeep(go.transform, "rThigh");
            chain.RShin = FindDeep(go.transform, "rShin");
            if (chain.Torso == null || chain.Head == null || chain.LUpper == null || chain.LFore == null
                || chain.RUpper == null || chain.RFore == null || chain.LThigh == null || chain.LShin == null
                || chain.RThigh == null || chain.RShin == null)
            {
                UnityEngine.Object.Destroy(go);
                return null;
            }

            chain.BaseScale = new Vector3(
                Mathf.Lerp(spec.Height, spec.Width, 0.55f),
                spec.Height,
                Mathf.Lerp(spec.Height, spec.Width, 0.45f)) * Silhouette.ToyScale;
            chain.Root.localScale = chain.BaseScale;
            chain.TorsoRest = chain.Torso.localPosition;
            var ape = body == "konga";
            var slug = body == "ashlord";
            chain.HunchDeg = ape ? 18f : slug ? 6f : 0f;
            chain.Bind = CaptureBind(chain);

            PaintDrop(go, jersey, flesh, slack, leather, ink, white);
            HideNamed(go.transform, "EyeL");
            HideNamed(go.transform, "EyeR");
            Face(chain.Head, Has("ember-eyes") || slug, ink, white, flesh);
            AttachHatAndExtras(chain, body, Has, jersey, trim, flesh, ink);
            AttachRing(chain, parent);
            return chain;
        }

        static bool TryDropExtra(string id, Transform bone, Vector3 localPos, Material paint)
        {
            if (bone == null) return false;
            var kit = ArtBinder.LoadExtrasKit();
            if (kit == null) return false;
            var root = UnityEngine.Object.Instantiate(kit);
            var piece = FindDeep(root.transform, id);
            if (piece == null)
            {
                UnityEngine.Object.Destroy(root);
                return false;
            }
            foreach (var anim in piece.GetComponentsInChildren<Animator>(true))
                anim.enabled = false;
            piece.SetParent(bone, false);
            piece.localPosition = localPos;
            piece.localRotation = Quaternion.identity;
            piece.localScale = Vector3.one;
            if (paint != null)
            {
                foreach (var r in piece.GetComponentsInChildren<Renderer>(true))
                    Look.Paint(r.gameObject, paint);
            }
            UnityEngine.Object.Destroy(root);
            return true;
        }

        static void AttachHatAndExtras(
            Chain chain, string body, Func<string, bool> Has,
            Material jersey, Material trim, Material flesh, Material ink)
        {
            var kid = body == "rio";
            var pageant = body == "vale";
            var speed = body == "zig";
            var slug = body == "ashlord";
            var torsoY = pageant ? 2.55f : speed ? 1.85f : slug ? 2.48f : 2.28f;

            if (Has("neck"))
            {
                if (!TryDropExtra("neck", chain.Root, new Vector3(0, torsoY + 0.95f, 0), flesh))
                {
                    var neckH = pageant ? 0.72f : 0.42f;
                    Look.Prim(PrimitiveType.Cylinder, "Neck", chain.Root, new Vector3(0, torsoY + 0.95f, 0), new Vector3(0.36f, neckH, 0.36f), flesh);
                }
            }
            if (Has("belly"))
            {
                if (!TryDropExtra("belly", chain.Torso, new Vector3(0, -0.38f, 0.48f), jersey))
                    Look.Prim(PrimitiveType.Sphere, "Belly", chain.Torso, new Vector3(0, -0.38f, 0.48f), new Vector3(1.28f, 0.92f, 1.12f), jersey);
            }
            if (Has("sash"))
            {
                var sashMat = Look.Lit(new Color(0.75f, 0.92f, 1f), smooth: 0.45f);
                if (!TryDropExtra("sash", chain.Torso, new Vector3(0.12f, 0.02f, 0.52f), sashMat))
                    Look.Prim(PrimitiveType.Cube, "Sash", chain.Torso, new Vector3(0.12f, 0.02f, 0.52f), new Vector3(1.05f, 0.16f, 0.08f), sashMat);
            }
            if (Has("cube-chest"))
            {
                if (!TryDropExtra("cube-chest", chain.Torso, Vector3.zero, jersey))
                    Look.Prim(PrimitiveType.Cube, "CubeChest", chain.Torso, Vector3.zero, new Vector3(1.05f, 1.05f, 1.05f), jersey);
            }
            if (Has("cape"))
            {
                if (!TryDropExtra("cape", chain.Torso, new Vector3(0, -0.28f, -0.72f), trim))
                {
                    Look.Prim(PrimitiveType.Cube, "Cape", chain.Torso, new Vector3(0, -0.28f, -0.72f), new Vector3(1.45f, 1.55f, 0.14f), trim);
                    Look.Prim(PrimitiveType.Cube, "CapeFlare", chain.Torso, new Vector3(0, -0.85f, -0.78f), new Vector3(1.65f, 0.55f, 0.12f), trim);
                }
            }

            if (Has("cheeks"))
            {
                if (!TryDropExtra("cheeks", chain.Head, new Vector3(0, -0.22f, 0.38f), flesh))
                {
                    Look.Prim(PrimitiveType.Sphere, "CheekL", chain.Head, new Vector3(-0.48f, -0.22f, 0.38f), Vector3.one * 0.42f, flesh);
                    Look.Prim(PrimitiveType.Sphere, "CheekR", chain.Head, new Vector3(0.48f, -0.22f, 0.38f), Vector3.one * 0.42f, flesh);
                }
            }
            if (Has("crown"))
            {
                var ice = Look.Lit(new Color(0.85f, 0.95f, 1f), smooth: 0.55f);
                if (!TryDropExtra("crown", chain.Head, new Vector3(0, 0.72f, 0), ice))
                {
                    Look.Prim(PrimitiveType.Cylinder, "Crown", chain.Head, new Vector3(0, 0.62f, 0), new Vector3(0.92f, 0.18f, 0.92f), ice);
                    Look.Prim(PrimitiveType.Cube, "PointA", chain.Head, new Vector3(0, 0.95f, 0), new Vector3(0.16f, 0.42f, 0.16f), ice);
                    Look.Prim(PrimitiveType.Cube, "PointB", chain.Head, new Vector3(0.28f, 0.88f, 0), new Vector3(0.12f, 0.28f, 0.12f), ice);
                    Look.Prim(PrimitiveType.Cube, "PointC", chain.Head, new Vector3(-0.28f, 0.88f, 0), new Vector3(0.12f, 0.28f, 0.12f), ice);
                }
                chain.Cap = Look.Prim(PrimitiveType.Cylinder, "Cap", chain.Head, new Vector3(0, 0.42f, 0), new Vector3(0.01f, 0.01f, 0.01f), trim).transform;
            }
            else if (Has("goggles") || Has("horns"))
            {
                // Goggles / horns are the hat. A gold brim here makes Zig a tiny Rio.
                chain.Cap = Look.Prim(PrimitiveType.Cylinder, "Cap", chain.Head, new Vector3(0, 0.28f, 0), new Vector3(0.01f, 0.01f, 0.01f), trim).transform;
            }
            else
            {
                var gold = Look.Lit(Colors.Gold, smooth: 0.4f);
                if (Has("brim") && TryDropExtra("brim", chain.Head, new Vector3(0, 0.42f, 0.08f), gold))
                    chain.Cap = Look.Prim(PrimitiveType.Cylinder, "Cap", chain.Head, new Vector3(0, 0.28f, 0), new Vector3(0.01f, 0.01f, 0.01f), trim).transform;
                else
                {
                    Look.Prim(PrimitiveType.Sphere, "Dome", chain.Head, new Vector3(0, 0.42f, 0), new Vector3(1.18f, 0.72f, 1.18f), trim);
                    chain.Cap = Look.Prim(PrimitiveType.Cylinder, "Cap", chain.Head, new Vector3(0, 0.28f, 0), new Vector3(1.22f, 0.14f, 1.22f), trim).transform;
                    var brim = Look.Prim(PrimitiveType.Cylinder, "Brim", chain.Cap, new Vector3(0, -0.85f, 0.22f),
                        Has("brim") || kid ? new Vector3(1.85f, 0.08f, 1.55f) : new Vector3(1.45f, 0.07f, 1.22f),
                        gold).transform;
                    brim.localRotation = Quaternion.Euler(12, 0, 0);
                }
            }
            if (Has("horns"))
            {
                if (!TryDropExtra("horns", chain.Head, new Vector3(0, 0.62f, -0.12f), trim))
                {
                    var hornL = Look.Prim(PrimitiveType.Cylinder, "HornL", chain.Head, new Vector3(-0.48f, 0.62f, -0.12f), new Vector3(0.28f, 0.72f, 0.28f), trim).transform;
                    var hornR = Look.Prim(PrimitiveType.Cylinder, "HornR", chain.Head, new Vector3(0.48f, 0.62f, -0.12f), new Vector3(0.28f, 0.72f, 0.28f), trim).transform;
                    hornL.localRotation = Quaternion.Euler(0, 0, 22);
                    hornR.localRotation = Quaternion.Euler(0, 0, -22);
                    Look.Prim(PrimitiveType.Sphere, "HornTipL", hornL, new Vector3(0, 0.55f, 0), Vector3.one * 0.55f, trim);
                    Look.Prim(PrimitiveType.Sphere, "HornTipR", hornR, new Vector3(0, 0.55f, 0), Vector3.one * 0.55f, trim);
                }
            }
            if (Has("snout"))
            {
                if (!TryDropExtra("snout", chain.Head, new Vector3(0, -0.22f, 0.72f), flesh))
                {
                    Look.Prim(PrimitiveType.Sphere, "Snout", chain.Head, new Vector3(0, -0.22f, 0.72f), new Vector3(1.05f, 0.68f, 0.95f), flesh);
                    Look.Prim(PrimitiveType.Sphere, "NostrilL", chain.Head, new Vector3(-0.18f, -0.12f, 1.05f), Vector3.one * 0.18f, ink);
                    Look.Prim(PrimitiveType.Sphere, "NostrilR", chain.Head, new Vector3(0.18f, -0.12f, 1.05f), Vector3.one * 0.18f, ink);
                }
            }
            if (Has("goggles"))
            {
                var glass = Look.Lit(new Color(0.2f, 0.85f, 0.55f), smooth: 0.6f);
                if (TryDropExtra("goggles", chain.Head, new Vector3(0, 0.12f, 0.58f), glass))
                {
                    var gog = chain.Head.Find("goggles");
                    if (gog != null) gog.localScale = Vector3.one * 1.35f;
                }
                else
                {
                    Look.Prim(PrimitiveType.Cylinder, "GogL", chain.Head, new Vector3(-0.38f, 0.12f, 0.58f), new Vector3(0.78f, 0.16f, 0.78f), glass);
                    Look.Prim(PrimitiveType.Cylinder, "GogR", chain.Head, new Vector3(0.38f, 0.12f, 0.58f), new Vector3(0.78f, 0.16f, 0.78f), glass);
                    Look.Prim(PrimitiveType.Cube, "GogBridge", chain.Head, new Vector3(0, 0.12f, 0.56f), new Vector3(0.36f, 0.10f, 0.14f), trim);
                }
            }
            if (Has("brick-jaw"))
            {
                if (!TryDropExtra("brick-jaw", chain.Head, new Vector3(0, -0.48f, 0.28f), flesh))
                    Look.Prim(PrimitiveType.Cube, "Jaw", chain.Head, new Vector3(0, -0.48f, 0.28f), new Vector3(1.18f, 0.42f, 0.82f), flesh);
            }
            if (Has("ember-eyes"))
                TryDropExtra("ember-eyes", chain.Head, new Vector3(0, 0.10f, 0.62f), Look.Unlit(Colors.EmberFire));
            if (Has("sneakers"))
            {
                var leather = Look.Lit(new Color(0.96f, 0.96f, 0.96f), smooth: 0.18f);
                var dropped = TryDropExtra("sneakers", chain.LShin, new Vector3(0, -0.62f, 0.22f), leather);
                dropped = TryDropExtra("sneakers", chain.RShin, new Vector3(0, -0.62f, 0.22f), leather) || dropped;
                if (dropped)
                {
                    HideNamed(chain.Root, "lShoe");
                    HideNamed(chain.Root, "rShoe");
                }
            }
        }

        static void AttachRing(Chain chain, Transform parent)
        {
            var pink = Look.Unlit(new Color(1f, 0.48f, 0.78f));
            chain.Ring = Look.Prim(PrimitiveType.Cylinder, "Mark", parent, new Vector3(0, (float)SetTells.RingHeightFt, 0), new Vector3(2.6f, 0.05f, 2.6f), pink).transform;
            Look.Prim(PrimitiveType.Cylinder, "MarkGold", chain.Ring, Vector3.zero, new Vector3(0.86f, 1.4f, 0.86f), Look.Unlit(Colors.Gold));
            chain.Ring.gameObject.SetActive(false);
        }

        static void PaintDrop(GameObject go, Material jersey, Material flesh, Material slack, Material leather, Material ink, Material white)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                var n = r.name;
                if (n.IndexOf("eye", StringComparison.OrdinalIgnoreCase) >= 0
                    || n.IndexOf("brow", StringComparison.OrdinalIgnoreCase) >= 0
                    || n.IndexOf("mouth", StringComparison.OrdinalIgnoreCase) >= 0)
                    Look.Paint(r.gameObject, ink);
                else if (n.IndexOf("head", StringComparison.OrdinalIgnoreCase) >= 0
                    || n.IndexOf("hand", StringComparison.OrdinalIgnoreCase) >= 0
                    || n.IndexOf("fore", StringComparison.OrdinalIgnoreCase) >= 0)
                    Look.Paint(r.gameObject, flesh);
                else if (n.IndexOf("stripe", StringComparison.OrdinalIgnoreCase) >= 0)
                    Look.Paint(r.gameObject, Look.Lit(Colors.Gold, smooth: 0.4f));
                else if (n.IndexOf("torso", StringComparison.OrdinalIgnoreCase) >= 0
                    || n.IndexOf("upper", StringComparison.OrdinalIgnoreCase) >= 0)
                    Look.Paint(r.gameObject, jersey);
                else if (n.IndexOf("shoe", StringComparison.OrdinalIgnoreCase) >= 0)
                    Look.Paint(r.gameObject, leather);
                else if (n.IndexOf("white", StringComparison.OrdinalIgnoreCase) >= 0)
                    Look.Paint(r.gameObject, white);
                else
                    Look.Paint(r.gameObject, slack);
            }
        }

        static void HideNamed(Transform root, string name)
        {
            var t = FindDeep(root, name);
            if (t != null) t.gameObject.SetActive(false);
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

        static BoneBind IdentityBind()
        {
            var b = new BoneBind();
            b.Torso = b.Head = b.LUpper = b.LFore = b.RUpper = b.RFore =
                b.LThigh = b.LShin = b.RThigh = b.RShin = Quaternion.identity;
            return b;
        }

        static BoneBind CaptureBind(Chain c)
        {
            Quaternion R(Transform t) => t != null ? t.localRotation : Quaternion.identity;
            return new BoneBind
            {
                Torso = R(c.Torso),
                Head = R(c.Head),
                LUpper = R(c.LUpper),
                LFore = R(c.LFore),
                RUpper = R(c.RUpper),
                RFore = R(c.RFore),
                LThigh = R(c.LThigh),
                LShin = R(c.LShin),
                RThigh = R(c.RThigh),
                RShin = R(c.RShin)
            };
        }
    }
}

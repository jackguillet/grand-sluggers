using System.Collections.Generic;
using GrandSluggers.Sim;
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    /// <summary>
    /// One body chain. Captains are extras on this topology. Six cuts are
    /// SMS-ladder outlines (kid / pageant / speed / brick / ape / slug), not
    /// six skeletons. A later FBX drops in Assets/Art/Characters/SharedRig.
    /// </summary>
    public static class SharedRig
    {
        public sealed class Chain
        {
            public Transform Root, Torso, Head, Cap;
            public Transform LUpper, LFore, RUpper, RFore;
            public Transform LThigh, LShin, RThigh, RShin;
            public Transform Ring;
            public Vector3 BaseScale;
            public Vector3 TorsoRest;
            public float HunchDeg;
        }

        public static Chain Spawn(Transform parent, Character who, IReadOnlyList<string> extras)
        {
            extras ??= System.Array.Empty<string>();
            bool Has(string id)
            {
                for (var i = 0; i < extras.Count; i++)
                    if (extras[i].Equals(id, System.StringComparison.OrdinalIgnoreCase)) return true;
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

            var chain = new Chain();
            chain.Root = new GameObject("root").transform;
            chain.Root.SetParent(parent, false);
            // Stature is Height. Girth is Width. Do not squash the whole toy on XZ.
            chain.BaseScale = new Vector3(
                Mathf.Lerp(spec.Height, spec.Width, 0.55f),
                spec.Height,
                Mathf.Lerp(spec.Height, spec.Width, 0.45f)) * Silhouette.ToyScale;
            chain.Root.localScale = chain.BaseScale;

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

            var gold = Look.Unlit(Colors.Gold);
            chain.Ring = Look.Prim(PrimitiveType.Cylinder, "Mark", chain.Root, new Vector3(0, 0.08f, 0), new Vector3(2.2f, 0.07f, 2.2f), gold).transform;
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
    }
}

using System.Collections.Generic;
using GrandSluggers.Sim;
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    /// <summary>
    /// One body chain. Captains are extras on this topology. A later FBX
    /// drops in Assets/Art/Characters/SharedRig and binds the same names.
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
            var slack = Look.Lit(Color.Lerp(Color.white, Colors.Body(faction), 0.08f), smooth: 0.2f);
            var leather = Look.Lit(Color.Lerp(Colors.Body(faction), Color.black, 0.35f), smooth: 0.12f);

            var chain = new Chain();
            chain.Root = new GameObject("root").transform;
            chain.Root.SetParent(parent, false);
            chain.BaseScale = new Vector3(spec.Width, spec.Height, spec.Width) * 1.15f;
            chain.Root.localScale = chain.BaseScale;

            Look.Prim(PrimitiveType.Sphere, "Hip", chain.Root, new Vector3(0, 1.05f, 0), new Vector3(1.45f, 1.05f, 1.15f), slack);

            var torsoKind = Has("cube-chest") ? PrimitiveType.Cube : PrimitiveType.Capsule;
            var torsoScale = Has("cube-chest")
                ? new Vector3(1.75f, 1.22f, 1.22f)
                : new Vector3(1.48f, 0.88f, 0.98f) * spec.Torso;
            chain.Torso = Look.Prim(torsoKind, "torso", chain.Root, new Vector3(0, 2.35f, 0), torsoScale, jersey).transform;
            Look.Prim(PrimitiveType.Cube, "Stripe", chain.Torso, new Vector3(0, 0.12f, 0.48f), new Vector3(0.28f, 0.82f, 0.1f), trim);
            Look.Prim(PrimitiveType.Cylinder, "Collar", chain.Torso, new Vector3(0, 0.52f, 0.08f), new Vector3(0.72f, 0.12f, 0.72f), trim);

            if (Has("neck"))
                Look.Prim(PrimitiveType.Cylinder, "Neck", chain.Root, new Vector3(0, 3.45f, 0), new Vector3(0.42f, 0.55f, 0.42f), flesh);
            if (Has("belly"))
                Look.Prim(PrimitiveType.Sphere, "Belly", chain.Torso, new Vector3(0, -0.42f, 0.42f), new Vector3(1.25f, 0.85f, 1.0f), jersey);
            if (Has("sash"))
                Look.Prim(PrimitiveType.Cube, "Sash", chain.Torso, new Vector3(0.18f, 0.08f, 0.55f), new Vector3(0.95f, 0.18f, 0.08f), Look.Lit(new Color(0.75f, 0.92f, 1f), smooth: 0.45f));
            if (Has("cape"))
                Look.Prim(PrimitiveType.Cube, "Cape", chain.Torso, new Vector3(0, -0.22f, -0.72f), new Vector3(1.35f, 1.35f, 0.16f), trim);

            var headY = Has("neck") ? 4.55f : 4.15f;
            var headKind = Has("brick-jaw") ? PrimitiveType.Cube : PrimitiveType.Sphere;
            var headScale = Has("brick-jaw")
                ? new Vector3(1.65f, 1.35f, 1.55f) * spec.Head
                : Vector3.one * (1.95f * spec.Head);
            chain.Head = Look.Prim(headKind, "head", chain.Root, new Vector3(0, headY, 0), headScale, flesh).transform;
            var ink = Look.Lit(new Color(0.08f, 0.07f, 0.07f), smooth: 0.05f);
            var eye = Has("ember-eyes") ? Look.Unlit(Colors.EmberFire) : ink;
            const float eyeSize = 0.38f;
            Look.Prim(PrimitiveType.Sphere, "EyeL", chain.Head, new Vector3(-0.32f, 0.08f, 0.52f), Vector3.one * eyeSize, eye);
            Look.Prim(PrimitiveType.Sphere, "EyeR", chain.Head, new Vector3(0.32f, 0.08f, 0.52f), Vector3.one * eyeSize, eye);
            var white = Look.Unlit(Color.white);
            Look.Prim(PrimitiveType.Sphere, "WhiteL", chain.Head, new Vector3(-0.32f, 0.08f, 0.44f), Vector3.one * (eyeSize * 1.7f), white);
            Look.Prim(PrimitiveType.Sphere, "WhiteR", chain.Head, new Vector3(0.32f, 0.08f, 0.44f), Vector3.one * (eyeSize * 1.7f), white);
            Look.Prim(PrimitiveType.Cube, "BrowL", chain.Head, new Vector3(-0.32f, 0.32f, 0.48f), new Vector3(0.38f, 0.1f, 0.14f), ink);
            Look.Prim(PrimitiveType.Cube, "BrowR", chain.Head, new Vector3(0.32f, 0.32f, 0.48f), new Vector3(0.38f, 0.1f, 0.14f), ink);
            Look.Prim(PrimitiveType.Cube, "Mouth", chain.Head, new Vector3(0, -0.28f, 0.5f), new Vector3(0.48f, 0.12f, 0.12f), ink);
            Look.Prim(PrimitiveType.Sphere, "EarL", chain.Head, new Vector3(-0.55f, 0.05f, 0.05f), Vector3.one * 0.28f, flesh);
            Look.Prim(PrimitiveType.Sphere, "EarR", chain.Head, new Vector3(0.55f, 0.05f, 0.05f), Vector3.one * 0.28f, flesh);

            if (Has("cheeks"))
            {
                Look.Prim(PrimitiveType.Sphere, "CheekL", chain.Head, new Vector3(-0.48f, -0.22f, 0.38f), Vector3.one * 0.42f, flesh);
                Look.Prim(PrimitiveType.Sphere, "CheekR", chain.Head, new Vector3(0.48f, -0.22f, 0.38f), Vector3.one * 0.42f, flesh);
            }
            if (Has("crown"))
            {
                var ice = Look.Lit(new Color(0.85f, 0.95f, 1f), smooth: 0.55f);
                Look.Prim(PrimitiveType.Cylinder, "Crown", chain.Head, new Vector3(0, 0.72f, 0), new Vector3(0.95f, 0.2f, 0.95f), ice);
                Look.Prim(PrimitiveType.Cube, "Point", chain.Head, new Vector3(0, 1.08f, 0), new Vector3(0.22f, 0.55f, 0.22f), ice);
                chain.Cap = Look.Prim(PrimitiveType.Cylinder, "Cap", chain.Head, new Vector3(0, 0.42f, 0), new Vector3(0.01f, 0.01f, 0.01f), trim).transform;
            }
            else
            {
                chain.Cap = Look.Prim(PrimitiveType.Cylinder, "Cap", chain.Head, new Vector3(0, 0.48f, 0), new Vector3(1.35f, 0.24f, 1.35f), trim).transform;
                var brim = Has("brim") ? new Vector3(1.65f, 0.16f, 1.2f) : new Vector3(1.25f, 0.14f, 0.85f);
                Look.Prim(PrimitiveType.Cube, "Brim", chain.Cap, new Vector3(0, -0.55f, 0.72f), brim, Look.Lit(Colors.Gold, smooth: 0.4f));
            }
            if (Has("horns"))
            {
                Look.Prim(PrimitiveType.Cube, "HornL", chain.Head, new Vector3(-0.52f, 0.72f, -0.08f), new Vector3(0.28f, 1.15f, 0.28f), trim);
                Look.Prim(PrimitiveType.Cube, "HornR", chain.Head, new Vector3(0.52f, 0.72f, -0.08f), new Vector3(0.28f, 1.15f, 0.28f), trim);
            }
            if (Has("snout"))
                Look.Prim(PrimitiveType.Sphere, "Snout", chain.Head, new Vector3(0, -0.22f, 0.62f), new Vector3(0.95f, 0.62f, 0.85f), flesh);
            if (Has("goggles"))
            {
                var glass = Look.Lit(new Color(0.2f, 0.85f, 0.55f), smooth: 0.6f);
                Look.Prim(PrimitiveType.Cylinder, "GogL", chain.Head, new Vector3(-0.34f, 0.1f, 0.48f), new Vector3(0.58f, 0.12f, 0.58f), glass);
                Look.Prim(PrimitiveType.Cylinder, "GogR", chain.Head, new Vector3(0.34f, 0.1f, 0.48f), new Vector3(0.58f, 0.12f, 0.58f), glass);
            }
            if (Has("brick-jaw"))
                Look.Prim(PrimitiveType.Cube, "Jaw", chain.Head, new Vector3(0, -0.48f, 0.32f), new Vector3(1.15f, 0.42f, 0.85f), flesh);

            var armLen = 0.62f * spec.Arms;
            const float armThick = 0.55f;
            chain.LUpper = Look.Prim(PrimitiveType.Capsule, "lUpper", chain.Torso, new Vector3(-0.92f * spec.Arms, 0.18f, 0), new Vector3(armThick, armLen, armThick), jersey).transform;
            chain.LFore = Look.Prim(PrimitiveType.Capsule, "lFore", chain.LUpper, new Vector3(0, -0.82f, 0), new Vector3(0.92f, 0.72f, 0.92f), flesh).transform;
            chain.RUpper = Look.Prim(PrimitiveType.Capsule, "rUpper", chain.Torso, new Vector3(0.92f * spec.Arms, 0.18f, 0), new Vector3(armThick, armLen, armThick), jersey).transform;
            chain.RFore = Look.Prim(PrimitiveType.Capsule, "rFore", chain.RUpper, new Vector3(0, -0.82f, 0), new Vector3(0.92f, 0.72f, 0.92f), flesh).transform;
            Look.Prim(PrimitiveType.Sphere, "LHand", chain.LFore, new Vector3(0, -0.68f, 0), Vector3.one * 0.78f, flesh);
            Look.Prim(PrimitiveType.Sphere, "RHand", chain.RFore, new Vector3(0, -0.68f, 0), Vector3.one * 0.78f, flesh);

            const float thighThick = 0.58f;
            const float thighSpread = 0.42f;
            chain.LThigh = Look.Prim(PrimitiveType.Capsule, "lThigh", chain.Root, new Vector3(-thighSpread, 0.62f, 0), new Vector3(thighThick, 0.62f, thighThick), slack).transform;
            chain.LShin = Look.Prim(PrimitiveType.Capsule, "lShin", chain.LThigh, new Vector3(0, -0.82f, 0), new Vector3(0.88f, 0.62f, 0.88f), slack).transform;
            chain.RThigh = Look.Prim(PrimitiveType.Capsule, "rThigh", chain.Root, new Vector3(thighSpread, 0.62f, 0), new Vector3(thighThick, 0.62f, thighThick), slack).transform;
            chain.RShin = Look.Prim(PrimitiveType.Capsule, "rShin", chain.RThigh, new Vector3(0, -0.82f, 0), new Vector3(0.88f, 0.62f, 0.88f), slack).transform;
            var shoe = Has("sneakers")
                ? new Vector3(1.15f, 0.48f, 1.55f)
                : new Vector3(0.88f, 0.36f, 1.28f);
            Look.Prim(PrimitiveType.Cube, "LShoe", chain.LShin, new Vector3(0, -0.68f, 0.18f), shoe, leather);
            Look.Prim(PrimitiveType.Cube, "RShoe", chain.RShin, new Vector3(0, -0.68f, 0.18f), shoe, leather);

            var gold = Look.Unlit(Colors.Gold);
            chain.Ring = Look.Prim(PrimitiveType.Cylinder, "Mark", chain.Root, new Vector3(0, 0.08f, 0), new Vector3(2.0f, 0.07f, 2.0f), gold).transform;
            chain.Ring.gameObject.SetActive(false);
            return chain;
        }
    }
}

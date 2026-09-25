using System;
using System.Linq;
using GrandSluggers.Sim;
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    public sealed class ParkView : MonoBehaviour
    {
        Transform _root;
        BallView _ball;
        bool _night;

        public BallView Ball => _ball;
        HarborKit _kit;
        /// <summary>
        /// The Harbor kit this view draws with, or null for a greybox park. Found once in the scene (HarborDiamond places it)
        /// or made for a park whose lawn slot names it; the camera, the scoreboard and the stills read it from here.
        /// </summary>
        public HarborKit Kit => _kit;
        readonly System.Collections.Generic.List<(SolidBody Body, Transform Actor)> _movers = new();

        /// <summary>
        /// The play clock (F4-f): every drawn mover stands where the sim places it at this second, so the train the player sees
        /// is the train the ball meets. Outside a live play the clock is 0 and each mover is at its spot.
        /// </summary>
        public void SetPlayClock(double t)
        {
            foreach (var (body, actor) in _movers)
            {
                if (actor == null) continue;
                var (x, z) = body.At(t);
                actor.position = new Vector3((float)x, actor.position.y, (float)z);
            }
        }
        public bool Night => _night;
        /// <summary>The park this view last built: the one being played.</summary>
        public Park Park { get; private set; }

        RulesTable _rules;
        FeelTable _feel;

        public void Build(Park park, bool night, RulesTable rules, FeelTable feel)
        {
            _rules = rules ?? Rules.Default;
            _feel = feel;
            BuildPark(park, night);
        }

        void BuildPark(Park park, bool night)
        {
            if (_root != null) Destroy(_root.gameObject);
            _root = new GameObject("Park").transform;
            _root.SetParent(transform, false);
            _night = night;
            _freezePose = 0;
            _movers.Clear();
            // The park as it plays tonight (PlayedPark.Of): a played park resolves to itself, so this only matters for a
            // caller that hands the catalog's park, whose night instances would otherwise be missing at night.
            park = PlayedPark.Of(park, night, hazards: true, _rules.Hazards);
            Park = park;

            // The park's look is its kit's data (FD-16, FR-04; F6-c): the sky and light rows its slots name, and the
            // palette its greybox draws in. No park id chooses a light, a sky or a color.
            var kitRow = ArtBinder.ParkKit(park.Id);
            var looks = ArtBinder.Art?.Looks;
            if (Camera.main != null)
            {
                SkyLook skyLook = null;
                LightLook lightLook = null;
                var skyId = kitRow.Filler(ParkKitSlots.Sky);
                var lightId = kitRow.Filler(ParkKitSlots.Light);
                if (looks != null && skyId != null) looks.Skies.TryGetValue(skyId, out skyLook);
                if (looks != null && lightId != null) looks.Lights.TryGetValue(lightId, out lightLook);
                Look.Apply(Camera.main, skyLook, lightLook, night);
            }

            var palette = kitRow.Palette;
            var grassMat = palette != null ? Look.Lit(palette.Grass) : Look.Lit(Colors.Grass, Look.Grass, 18f, 0.08f);
            var dirtMat = palette != null ? Look.Lit(palette.Dirt) : Look.Lit(Colors.Dirt, Look.Dirt, 8f, 0.12f);
            var waterMat = palette != null ? Look.Lit(palette.Water) : Look.Lit(Colors.Water, smooth: 0.85f);

            if (_kit == null) _kit = FindAnyObjectByType<HarborKit>(FindObjectsInactive.Include);
            var kit = _kit;
            // The Harbor kit exists for a park whose lawn slot names it (FD-16, FR-13; data/art/parks.json).
            if (kit == null && ArtBinder.ParkKit(park.Id).Fills(ParkKitSlots.Lawn, ParkKitSlots.HarborLawn))
            {
                var go = new GameObject("HarborKit");
                kit = _kit = go.AddComponent<HarborKit>();
                kit.EnsureAnchors();
            }
            if (kit != null) kit.Bind(park, night);
            var placed = kit != null && kit.OwnsDiamond;
            // Harbor owns the lawn and cuts dugout pits. A 620-ft sheet here
            // capped the wells. Water stays past the infield so the pit floor shows.
            if (placed)
                Quad("Water", new Vector3(0, -1.4f, 480), new Vector3(1100, 1, 700), waterMat);
            else
            {
                Quad("Water", new Vector3(0, -1.4f, 240), new Vector3(1100, 1, 1100), waterMat);
                Quad("Outfield", new Vector3(0, -0.12f, 190), new Vector3(620, 0.35f, 620), grassMat);
                // The one field kit (FD-16, #859): the diamond, rail and wall Harbor draws, from the same
                // geometry owner, in this park's dirt and wall. HarborKit draws it for Harbor.
                new FieldKit(_root).Build(park, FieldSkin(palette, dirtMat));
            }
            // The dress stands beside the kit, never in it: no dress piece stands inside the kit's
            // backstop or in the dugout span along either foul line (F6-a2 #881, FieldKitSourceTests).
            // The Harbor kit draws the dress its slots name; every other park's dress is the builders its
            // slots name (F6-d, FD-16-R1), never a method chosen by its id.
            if (!placed) Dress(park, kitRow);
            Hazards(park, kitRow);

            _ball = gameObject.GetComponent<BallView>();
            if (_ball == null) _ball = gameObject.AddComponent<BallView>();
            _ball.Build(_root, _feel.BallShadow);
        }

        /// <summary>
        /// A park the Harbor kit does not draw is the greybox in its own light, sky and palette (FR-13; data/art/looks.json).
        /// Its stands are the one park-neutral bowl (<see cref="ParkStands"/>) when its stands slot names the kit bowl, painted
        /// by its palette's stands block; an empty slot is plain grey blocks and crowd cards. Never hand-built per-park dress.
        /// </summary>
        void Dress(Park park, ParkKitSlot kitRow)
        {
            var stands = kitRow.Palette?.Stands;
            if (kitRow.Fills(ParkKitSlots.Stands, ParkKitSlots.KitBowl) && stands != null)
                new ParkStands(_root).Build(park, stands);
            else
                GreyboxStands();
        }

        /// <summary>
        /// What this park hands the field kit: its dirt, the warning track, and its wall, cap and pole, from the palette of
        /// its kit row (F6-c; data/art/parks.json). The chalk, the bags, the plate and the mound are the kit's own. The
        /// panels alternate from the first span; a palette with no alternate repeats the wall.
        /// </summary>
        FieldKit.Skin FieldSkin(ParkPalette palette, Material dirt)
        {
            var wall = palette != null ? Look.Lit(palette.Wall) : Look.Lit(new Color(0.22f, 0.48f, 0.28f), smooth: 0.18f);
            var wallAlt = palette?.WallAlt != null ? Look.Lit(palette.WallAlt) : wall;
            var cap = palette != null ? Look.Lit(palette.Cap) : Look.Lit(Colors.Gold, smooth: 0.4f);
            var pole = palette != null ? Look.Lit(palette.Pole) : Look.Lit(Colors.Gold, smooth: 0.45f);
            return new FieldKit.Skin
            {
                Dirt = dirt,
                HomeDirt = dirt,
                Track = palette?.Track != null ? Look.Lit(palette.Track) : Look.Lit(new Color(0.72f, 0.52f, 0.32f), Look.Dirt, 6f, 0.1f),
                WallFaces = new[] { wallAlt, wall },
                WallCap = cap,
                Pole = pole,
                Screen = Look.Unlit(Colors.Gold)
            };
        }

        /// <summary>The stands of a park whose stands slot is empty: plain concrete and three crowd cards.</summary>
        void GreyboxStands()
        {
            var conc = Look.Lit(new Color(0.78f, 0.8f, 0.82f), smooth: 0.12f);
            Cube("HomePlateStand", new Vector3(0, 16, -56), new Vector3(120, 28, 22), conc);
            Cube("LeftStand", new Vector3(-108, 14, 36), new Vector3(28, 24, 110), conc);
            Cube("RightStand", new Vector3(108, 14, 36), new Vector3(28, 24, 110), conc);
            CrowdCard("CrowdH", new Vector3(0, 18, -66), new Vector3(110, 18, 1));
            CrowdCard("CrowdL", new Vector3(-120, 16, 36), new Vector3(1, 16, 90));
            CrowdCard("CrowdR", new Vector3(120, 16, 36), new Vector3(1, 16, 90));
        }

        void CrowdCard(string name, Vector3 pos, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(_root, false);
            go.transform.position = pos;
            go.transform.localScale = scale;
            Destroy(go.GetComponent<Collider>());
            var mat = Look.Lit(Color.white, Look.Crowd, 1f, 0.05f);
            Look.Paint(go, mat);
        }

        public void Tick(Vector3 ball, float dt)
        {
            if (_kit != null && _kit.OwnsDiamond) _kit.Tick(ball, dt);
        }

        /// <summary>Fireworks are Harbor's night dress; no other park has any.</summary>
        public void BurstFireworks(Vector3 at)
        {
            if (_kit != null && _kit.OwnsDiamond) _kit.BurstFireworks(at);
        }

        /// <summary>
        /// The train toy, at the park's own train instance, facing along its bearing from home as the old literal train did
        /// (it stood at a code spot, spray 18°, 12 ft inside the fence, not where the park's data put it).
        /// </summary>
        void MidwayTrain(Hazard h)
        {
            var red = Look.Lit(new Color(0.86f, 0.16f, 0.22f), smooth: 0.18f);
            var cream = Look.Lit(new Color(0.96f, 0.92f, 0.82f), smooth: 0.16f);
            var yellow = Look.Lit(Colors.Gold, smooth: 0.4f);
            var wood = Look.Lit(new Color(0.46f, 0.28f, 0.14f), smooth: 0.1f);
            var p = new Vector3((float)h.X, 0, (float)h.Z);
            var spray = Mathf.Atan2((float)h.X, (float)h.Z) * Mathf.Rad2Deg;

            var root = new GameObject("TrackTrain").transform;
            // A mover (F4-f): the sim places it on the play clock; SetPlayClock moves the drawn one to the same place.
            var mover = SolidBodies.Of(Park, _rules).FirstOrDefault(b => b.Moves && Math.Abs(b.X - h.X) < 1e-6 && Math.Abs(b.Z - h.Z) < 1e-6);
            if (mover != null) _movers.Add((mover, root));
            root.SetParent(_root, false);
            root.position = p;
            root.rotation = Quaternion.Euler(0, spray, 0);

            Look.Prim(PrimitiveType.Cube, "Engine", root, new Vector3(0, 4.2f, 0), new Vector3(8.5f, 6.4f, 14), red);
            Look.Prim(PrimitiveType.Cube, "Cab", root, new Vector3(0, 8.4f, -4.2f), new Vector3(8.2f, 4.6f, 6.4f), cream);
            Look.Prim(PrimitiveType.Cylinder, "Stack", root, new Vector3(0, 10.2f, 3.6f), new Vector3(2.2f, 2.4f, 2.2f), wood);
            Look.Prim(PrimitiveType.Cube, "Boxcar", root, new Vector3(0, 4.0f, -16f), new Vector3(8.2f, 6.2f, 14), yellow);
            Look.Prim(PrimitiveType.Cube, "Stripe", root, new Vector3(0, 4.0f, -16f), new Vector3(8.4f, 1.4f, 14.2f), red);
            Look.Prim(PrimitiveType.Cube, "Plate", root, new Vector3(4.3f, 5.6f, -16f), new Vector3(0.2f, 2.4f, 3.2f), cream);
            Wheel("WheelFL", root, new Vector3(-3.4f, 1.3f, 4.2f), wood);
            Wheel("WheelFR", root, new Vector3(3.4f, 1.3f, 4.2f), wood);
            Wheel("WheelBL", root, new Vector3(-3.4f, 1.3f, -16f), wood);
            Wheel("WheelBR", root, new Vector3(3.4f, 1.3f, -16f), wood);
        }

        static void Wheel(string name, Transform parent, Vector3 localPos, Material mat)
        {
            var go = Look.Prim(PrimitiveType.Cylinder, name, parent, localPos, new Vector3(2.4f, 0.55f, 2.4f), mat);
            go.transform.localRotation = Quaternion.Euler(0, 0, 90f);
        }

        void ChomperMouth(Transform parent, Hazard h)
        {
            var stem = Look.Lit(new Color(0.16f, 0.48f, 0.18f), smooth: 0.12f);
            var lip = Look.Lit(new Color(0.82f, 0.12f, 0.16f), smooth: 0.18f);
            var hole = Look.Unlit(new Color(0.08f, 0.02f, 0.04f));
            var tooth = Look.Unlit(new Color(0.96f, 0.92f, 0.78f));
            var r = Mathf.Max(3.2f, (float)h.Radius * 0.38f);
            var root = new GameObject("Chomper-" + (h.Tag ?? "?")).transform;
            root.SetParent(parent, false);
            root.position = new Vector3((float)h.X, 0, (float)h.Z);
            Look.Prim(PrimitiveType.Cylinder, "Stem", root, new Vector3(0, 4.2f, 0), new Vector3(r * 0.7f, 4.2f, r * 0.7f), stem);
            Look.Prim(PrimitiveType.Sphere, "Head", root, new Vector3(0, 9.2f, 0.6f), Vector3.one * r * 2.1f, stem);
            Look.Prim(PrimitiveType.Cylinder, "Mouth", root, new Vector3(0, 9.0f, r * 0.85f), new Vector3(r * 1.55f, r * 0.22f, r * 1.15f), hole);
            Look.Prim(PrimitiveType.Cube, "Jaw", root, new Vector3(0, 8.2f, r * 0.9f), new Vector3(r * 1.7f, r * 0.35f, r * 0.7f), lip);
            Look.Prim(PrimitiveType.Cube, "ToothL", root, new Vector3(-r * 0.35f, 8.55f, r * 1.05f), new Vector3(r * 0.22f, r * 0.28f, r * 0.18f), tooth);
            Look.Prim(PrimitiveType.Cube, "ToothR", root, new Vector3(r * 0.35f, 8.55f, r * 1.05f), new Vector3(r * 0.22f, r * 0.28f, r * 0.18f), tooth);
            Glow("ChompGlow", new Vector3((float)h.X, 9f, (float)h.Z), new Color(0.7f, 0.12f, 0.18f), 1.1f, 28f);
        }

        void VineWall(string name, Vector3 pos, float w, float h, float d, Material bark, Material vine)
        {
            var root = new GameObject(name).transform;
            root.SetParent(_root, false);
            root.position = pos;
            Look.Prim(PrimitiveType.Cube, "Face", root, new Vector3(0, h * 0.5f, 0), new Vector3(w, h, d), bark);
            Look.Prim(PrimitiveType.Cube, "LedgeLow", root, new Vector3(0, 2.2f, d * 0.42f), new Vector3(w * 0.92f, 0.45f, 1.6f), bark);
            Look.Prim(PrimitiveType.Cube, "LedgeClamber", root, new Vector3(0, 4.2f, d * 0.42f), new Vector3(w * 0.92f, 0.45f, 1.8f), bark);
            Look.Prim(PrimitiveType.Cube, "LedgeHigh", root, new Vector3(0, 6.4f, d * 0.42f), new Vector3(w * 0.92f, 0.45f, 1.6f), bark);
            Look.Prim(PrimitiveType.Cube, "Vines", root, new Vector3(0, h * 0.55f, d * 0.52f), new Vector3(w * 0.7f, h * 0.9f, 0.35f), vine);
        }

        void ClimbWall(Hazard h)
        {
            var bark = Look.Lit(new Color(0.36f, 0.21f, 0.11f), smooth: 0.08f);
            var vine = Look.Lit(new Color(0.22f, 0.48f, 0.18f), smooth: 0.12f);
            var p = new Vector3((float)h.X, 0, (float)h.Z);
            var w = Mathf.Max(28f, (float)h.Radius * 0.7f);
            VineWall("ClimbWall", p, w, 14f, 6f, bark, vine);
        }

        void BarrelCannon(Hazard h)
        {
            var r = Mathf.Max(2.2f, (float)h.Radius);
            var tag = string.IsNullOrWhiteSpace(h.Tag) ? "?" : h.Tag;
            var wood = Look.Lit(new Color(0.46f, 0.28f, 0.11f), smooth: 0.12f);
            var band = Look.Lit(new Color(0.62f, 0.50f, 0.28f), smooth: 0.35f);
            var hole = Look.Unlit(new Color(0.06f, 0.04f, 0.03f));
            var pip = Look.Unlit(Color.white);

            var root = new GameObject("BarrelCannon-" + tag).transform;
            root.SetParent(_root, false);
            root.position = new Vector3((float)h.X, 0, (float)h.Z);
            root.rotation = Quaternion.Euler(-18f, 0f, 0f);

            Look.Prim(PrimitiveType.Cylinder, "Body", root, new Vector3(0, r * 0.9f, 0), new Vector3(r * 1.7f, r * 0.9f, r * 1.7f), wood);
            Look.Prim(PrimitiveType.Cylinder, "BandLow", root, new Vector3(0, r * 0.35f, 0), new Vector3(r * 1.82f, r * 0.12f, r * 1.82f), band);
            Look.Prim(PrimitiveType.Cylinder, "BandHigh", root, new Vector3(0, r * 1.45f, 0), new Vector3(r * 1.82f, r * 0.12f, r * 1.82f), band);
            Look.Prim(PrimitiveType.Cylinder, "Lip", root, new Vector3(0, r * 1.88f, 0), new Vector3(r * 2.0f, r * 0.16f, r * 2.0f), band);
            Look.Prim(PrimitiveType.Cylinder, "Mouth", root, new Vector3(0, r * 1.72f, 0), new Vector3(r * 1.25f, r * 0.28f, r * 1.25f), hole);
            Look.Prim(PrimitiveType.Cylinder, "Well", root, new Vector3(0, r * 1.2f, 0), new Vector3(r * 1.1f, r * 0.5f, r * 1.1f), hole);
            var n = tag == "A" ? 1 : tag == "B" ? 2 : 3;
            for (var i = 0; i < n; i++)
            {
                var x = (i - (n - 1) * 0.5f) * (r * 0.28f);
                Look.Prim(PrimitiveType.Cube, "Pip" + i, root, new Vector3(x, r * 0.9f, r * 0.92f), new Vector3(r * 0.16f, r * 0.16f, 0.12f), pip);
            }
        }

        void LavaPit(Hazard h)
        {
            var r = Mathf.Max(3f, (float)h.Radius);
            var stone = Look.Lit(new Color(0.22f, 0.14f, 0.16f), smooth: 0.1f);
            var lava = Look.Unlit(Colors.EmberFire);
            var glow = Look.Unlit(new Color(1f, 0.35f, 0.08f));
            var root = new GameObject("LavaPit").transform;
            root.SetParent(_root, false);
            root.position = new Vector3((float)h.X, 0, (float)h.Z);
            // The rim is a solid disc, so the lava sits on top of it (inside it, it never showed).
            Look.Prim(PrimitiveType.Cylinder, "Rim", root, new Vector3(0, 0.3f, 0), new Vector3(r * 2.2f, 0.3f, r * 2.2f), stone);
            Look.Prim(PrimitiveType.Cylinder, "Well", root, new Vector3(0, 0.66f, 0), new Vector3(r * 1.8f, 0.06f, r * 1.8f), lava);
            Look.Prim(PrimitiveType.Cylinder, "Glow", root, new Vector3(0, 0.74f, 0), new Vector3(r * 1.2f, 0.04f, r * 1.2f), glow);
            Glow("LavaGlow", new Vector3((float)h.X, 1.4f, (float)h.Z), Colors.EmberFire, 1.4f, r * 6f);
        }

        void FireStatue(Vector3 p, float radius, bool breath, float nightMul = 1f)
        {
            var stone = Look.Lit(new Color(0.22f, 0.14f, 0.16f), smooth: 0.1f);
            var ember = Look.Lit(Colors.Ember, smooth: 0.12f);
            var fire = Look.Unlit(Colors.EmberFire);
            var gold = Look.Lit(Colors.Gold, smooth: 0.45f);
            var root = new GameObject(breath ? "FireBreath" : "KeepStatue").transform;
            root.SetParent(_root, false);
            root.position = p;

            var ped = Mathf.Clamp(radius * 0.38f, 2.2f, 4.2f);
            Look.Prim(PrimitiveType.Cylinder, "Pedestal", root, new Vector3(0, 1.0f, 0), new Vector3(ped * 2, 1.0f, ped * 2), stone);
            Look.Prim(PrimitiveType.Cube, "Plinth", root, new Vector3(0, 2.05f, 0), new Vector3(ped * 1.7f, 0.22f, ped * 1.7f), gold);
            Look.Prim(PrimitiveType.Capsule, "LegL", root, new Vector3(-0.55f, 3.4f, 0), new Vector3(0.65f, 1.25f, 0.65f), ember);
            Look.Prim(PrimitiveType.Capsule, "LegR", root, new Vector3(0.55f, 3.4f, 0), new Vector3(0.65f, 1.25f, 0.65f), ember);
            Look.Prim(PrimitiveType.Cube, "Torso", root, new Vector3(0, 5.3f, 0), new Vector3(2.1f, 2.2f, 1.2f), ember);
            Look.Prim(PrimitiveType.Sphere, "Head", root, new Vector3(0, 6.9f, 0.15f), Vector3.one * 1.35f, ember);
            Look.Prim(PrimitiveType.Cube, "HornL", root, new Vector3(-0.55f, 7.7f, 0), new Vector3(0.28f, 0.9f, 0.28f), gold);
            Look.Prim(PrimitiveType.Cube, "HornR", root, new Vector3(0.55f, 7.7f, 0), new Vector3(0.28f, 0.9f, 0.28f), gold);
            var armL = Look.Prim(PrimitiveType.Capsule, "ArmL", root, new Vector3(-1.5f, 5.6f, 0.2f), new Vector3(0.45f, 1.15f, 0.45f), ember);
            armL.transform.localRotation = Quaternion.Euler(0, 0, 28f);
            var armR = Look.Prim(PrimitiveType.Capsule, "ArmR", root, new Vector3(1.5f, 5.6f, 0.2f), new Vector3(0.45f, 1.15f, 0.45f), ember);
            armR.transform.localRotation = Quaternion.Euler(0, 0, -28f);
            if (breath)
            {
                // The breath's night reach is its own type row's number (#847), handed in by the instance.
                var amp = _night ? nightMul : 1f;
                var br = radius * amp;
                Look.Prim(PrimitiveType.Cylinder, "Breath", root, new Vector3(0, 6.6f, 2.8f), new Vector3(br * 0.55f, br * 0.55f, br * 0.55f), fire);
                var cone = Look.Prim(PrimitiveType.Cylinder, "Flame", root, new Vector3(0, 6.4f, 5.4f * amp), new Vector3(br * 1.1f, br * 0.7f, br * 1.1f), fire);
                cone.transform.localRotation = Quaternion.Euler(78f, 0f, 0f);
                Glow("BreathGlow", p + new Vector3(0, 6.4f, 4.2f * amp), Colors.EmberFire, _night ? 2.8f : 1.6f, br * 5f);
            }
            else
            {
                Look.Prim(PrimitiveType.Cube, "Sash", root, new Vector3(0.2f, 5.2f, 0.65f), new Vector3(0.8f, 0.2f, 0.1f), gold);
            }
        }

        void StarBillboard(Hazard h)
        {
            var gold = Look.Lit(Colors.Gold, smooth: 0.42f);
            var frame = Look.Lit(new Color(0.18f, 0.16f, 0.2f), smooth: 0.2f);
            var star = Look.Unlit(new Color(1f, 0.92f, 0.35f));
            var neon = Look.Unlit(new Color(1f, 0.28f, 0.72f));
            var root = new GameObject("StarBillboard").transform;
            root.SetParent(_root, false);
            root.position = new Vector3((float)h.X, 0, (float)h.Z);
            Look.Prim(PrimitiveType.Cylinder, "Pole", root, new Vector3(0, 10f, 0), new Vector3(1.2f, 10f, 1.2f), frame);
            Look.Prim(PrimitiveType.Cube, "Frame", root, new Vector3(0, 20f, 0), new Vector3(26, 18, 2.4f), frame);
            Look.Prim(PrimitiveType.Cube, "Face", root, new Vector3(0, 20f, -0.4f), new Vector3(23, 15, 1.2f), gold);
            Look.Prim(PrimitiveType.Cube, "Star", root, new Vector3(0, 20.4f, -1.1f), new Vector3(7.2f, 7.2f, 0.5f), star);
            var diamond = Look.Prim(PrimitiveType.Cube, "StarTilt", root, new Vector3(0, 20.4f, -1.15f), new Vector3(7.2f, 7.2f, 0.4f), star);
            diamond.transform.localRotation = Quaternion.Euler(0, 0, 45f);
            Look.Prim(PrimitiveType.Cube, "Neon", root, new Vector3(0, 28.4f, 0), new Vector3(26.4f, 0.4f, 2.6f), neon);
            Glow("SignGlow", new Vector3((float)h.X, 20f, (float)h.Z), Colors.Gold, 1.1f, 48f);
        }

        void AcUnit(Hazard h)
        {
            var steel = Look.Lit(new Color(0.55f, 0.55f, 0.58f), smooth: 0.3f);
            var dark = Look.Lit(new Color(0.28f, 0.28f, 0.30f), smooth: 0.18f);
            var fan = Look.Lit(new Color(0.22f, 0.24f, 0.28f), smooth: 0.4f);
            var root = new GameObject("AcUnit").transform;
            root.SetParent(_root, false);
            root.position = new Vector3((float)h.X, 0, (float)h.Z);
            Look.Prim(PrimitiveType.Cube, "Body", root, new Vector3(0, 2.2f, 0), new Vector3(8, 4.4f, 8), steel);
            Look.Prim(PrimitiveType.Cube, "Grille", root, new Vector3(0, 2.4f, 4.15f), new Vector3(6.2f, 2.8f, 0.2f), dark);
            Look.Prim(PrimitiveType.Cylinder, "Fan", root, new Vector3(0, 4.7f, 0), new Vector3(3.6f, 0.35f, 3.6f), fan);
            Look.Prim(PrimitiveType.Cube, "VentL", root, new Vector3(-4.15f, 2.4f, 0), new Vector3(0.2f, 2.4f, 5.2f), dark);
            Look.Prim(PrimitiveType.Cube, "VentR", root, new Vector3(4.15f, 2.4f, 0), new Vector3(0.2f, 2.4f, 5.2f), dark);
            Look.Prim(PrimitiveType.Cylinder, "Pipe", root, new Vector3(3.2f, 0.7f, -3.2f), new Vector3(0.7f, 0.7f, 0.7f), dark);
        }

        void FreezeStatue(Vector3 p, float radius, int pose)
        {
            var ice = Look.Lit(new Color(0.68f, 0.88f, 1f), smooth: 0.78f);
            var stone = Look.Lit(new Color(0.70f, 0.76f, 0.84f), smooth: 0.22f);
            var frost = Look.Lit(new Color(0.82f, 0.94f, 1f), smooth: 0.88f);
            var gold = Look.Lit(Colors.Gold, smooth: 0.5f);

            var root = new GameObject("FreezeStatue").transform;
            root.SetParent(_root, false);
            root.position = p;
            root.rotation = Quaternion.Euler(0, pose == 1 ? -35f : pose == 2 ? 25f : 8f, 0);

            var ped = Mathf.Clamp(radius * 0.38f, 2.0f, 3.4f);
            Look.Prim(PrimitiveType.Cylinder, "FrostRing", root, new Vector3(0, 0.06f, 0), new Vector3(radius * 2, 0.05f, radius * 2), frost);
            Look.Prim(PrimitiveType.Cylinder, "Pedestal", root, new Vector3(0, 0.85f, 0), new Vector3(ped * 2, 0.85f, ped * 2), stone);
            Look.Prim(PrimitiveType.Cube, "Plinth", root, new Vector3(0, 1.75f, 0), new Vector3(ped * 1.7f, 0.22f, ped * 1.7f), frost);
            Look.Prim(PrimitiveType.Capsule, "LegL", root, new Vector3(-0.45f, 3.0f, 0), new Vector3(0.55f, 1.15f, 0.55f), ice);
            Look.Prim(PrimitiveType.Capsule, "LegR", root, new Vector3(0.45f, 3.0f, 0), new Vector3(0.55f, 1.15f, 0.55f), ice);
            Look.Prim(PrimitiveType.Cube, "Torso", root, new Vector3(0, 4.7f, 0), new Vector3(1.7f, 1.9f, 1.05f), ice);
            Look.Prim(PrimitiveType.Sphere, "Head", root, new Vector3(0, 6.15f, 0), Vector3.one * 1.15f, ice);

            if (pose == 0)
            {
                Look.Prim(PrimitiveType.Capsule, "ArmL", root, new Vector3(-1.15f, 4.9f, 0.15f), new Vector3(0.4f, 0.95f, 0.4f), ice);
                Look.Prim(PrimitiveType.Capsule, "ArmR", root, new Vector3(1.2f, 5.2f, 0.35f), new Vector3(0.4f, 0.95f, 0.4f), ice);
                var bat = Look.Prim(PrimitiveType.Cylinder, "Bat", root, new Vector3(1.35f, 6.4f, 0.2f), new Vector3(0.22f, 1.5f, 0.22f), ice);
                bat.transform.localRotation = Quaternion.Euler(0, 0, 28f);
                Look.Prim(PrimitiveType.Cube, "Cap", root, new Vector3(0, 6.7f, 0.15f), new Vector3(1.1f, 0.22f, 1.1f), frost);
            }
            else if (pose == 1)
            {
                Look.Prim(PrimitiveType.Capsule, "ArmL", root, new Vector3(-1.1f, 4.6f, 0), new Vector3(0.4f, 0.9f, 0.4f), ice);
                var armR = Look.Prim(PrimitiveType.Capsule, "ArmR", root, new Vector3(0.55f, 6.0f, 0), new Vector3(0.4f, 1.1f, 0.4f), ice);
                armR.transform.localRotation = Quaternion.Euler(0, 0, -40f);
                Look.Prim(PrimitiveType.Sphere, "Ball", root, new Vector3(1.5f, 7.1f, 0), Vector3.one * 0.45f, frost);
            }
            else
            {
                Look.Prim(PrimitiveType.Capsule, "ArmL", root, new Vector3(-1.7f, 5.0f, 0), new Vector3(0.38f, 1.05f, 0.38f), ice);
                Look.Prim(PrimitiveType.Capsule, "ArmR", root, new Vector3(1.7f, 5.0f, 0), new Vector3(0.38f, 1.05f, 0.38f), ice);
                Look.Prim(PrimitiveType.Cylinder, "Crown", root, new Vector3(0, 6.75f, 0), new Vector3(0.9f, 0.18f, 0.9f), gold);
                Look.Prim(PrimitiveType.Cube, "Point", root, new Vector3(0, 7.1f, 0), new Vector3(0.18f, 0.45f, 0.18f), gold);
                Look.Prim(PrimitiveType.Cube, "Sash", root, new Vector3(0.15f, 4.7f, 0.55f), new Vector3(0.7f, 0.16f, 0.08f), frost);
            }
        }

        void WarpCan(Hazard h)
        {
            var r = Mathf.Max(2.4f, (float)h.Radius);
            var tag = string.IsNullOrWhiteSpace(h.Tag) ? "?" : h.Tag;
            var bodyCol = TagColor(tag);
            var body = Look.Lit(bodyCol, smooth: 0.22f);
            var lip = Look.Lit(Color.Lerp(bodyCol, Color.white, 0.28f), smooth: 0.3f);
            var hole = Look.Unlit(new Color(0.05f, 0.04f, 0.06f));
            var badge = Look.Unlit(Color.Lerp(bodyCol, Color.white, 0.18f));
            var pip = Look.Unlit(Color.white);

            var root = new GameObject("WarpCan-" + tag).transform;
            root.SetParent(_root, false);
            root.position = new Vector3((float)h.X, 0, (float)h.Z);
            root.rotation = Quaternion.Euler(-16f, 0f, 0f);

            Look.Prim(PrimitiveType.Cylinder, "Body", root, new Vector3(0, r * 0.82f, 0), new Vector3(r * 1.7f, r * 0.82f, r * 1.7f), body);
            Look.Prim(PrimitiveType.Cylinder, "Lip", root, new Vector3(0, r * 1.72f, 0), new Vector3(r * 2.2f, r * 0.2f, r * 2.2f), lip);
            Look.Prim(PrimitiveType.Cylinder, "Well", root, new Vector3(0, r * 1.28f, 0), new Vector3(r * 1.35f, r * 0.52f, r * 1.35f), hole);
            Look.Prim(PrimitiveType.Cylinder, "Mouth", root, new Vector3(0, r * 1.86f, 0), new Vector3(r * 1.5f, r * 0.1f, r * 1.5f), hole);
            Look.Prim(PrimitiveType.Cylinder, "Band", root, new Vector3(0, r * 0.55f, 0), new Vector3(r * 1.82f, r * 0.16f, r * 1.82f), badge);
            Look.Prim(PrimitiveType.Cube, "Plate", root, new Vector3(0, r * 1.05f, r * 0.92f), new Vector3(r * 0.95f, r * 0.7f, 0.18f), badge);
            var n = tag == "A" ? 1 : tag == "B" ? 2 : 3;
            for (var i = 0; i < n; i++)
            {
                var x = (i - (n - 1) * 0.5f) * (r * 0.28f);
                Look.Prim(PrimitiveType.Cube, "Pip" + i, root, new Vector3(x, r * 1.05f, r * 1.02f), new Vector3(r * 0.18f, r * 0.18f, 0.12f), pip);
            }
        }

        static Color TagColor(string tag) => tag switch
        {
            "A" => new Color(0.92f, 0.16f, 0.24f),
            "B" => new Color(1f, 0.78f, 0.12f),
            "C" => new Color(0.18f, 0.48f, 0.92f),
            _ => Colors.Carnival
        };

        /// <summary>
        /// The park's hazards (FD-16, FD-09, FR-13; F6-d): each instance as the toy its type names in
        /// data/art/hazard-actors.json when the park's hazardActors slot names toy-actors, else the greybox of its pattern;
        /// and under every instance whose pattern acts in play, a flat ring at the disc the sim reads — the radius, its
        /// type's night multiple at night, plus the reach pad — in the pattern's ring color. The ring is the size of what
        /// plays. The park is the played park, so a night-only instance is here at night and nowhere by day.
        /// </summary>
        void Hazards(Park park, ParkKitSlot kitRow)
        {
            var actors = ArtBinder.Art?.Actors;
            var toys = actors != null && kitRow.Fills(ParkKitSlots.HazardActors, ParkKitSlots.ToyActors);
            foreach (var h in park.Hazards)
            {
                var row = _rules.Hazards.Of(h.Type);
                var toy = toys ? actors.Toy(h.Type) : null;
                if (toy != null) Toy(toy, h);
                else PatternGreybox(h, row, actors);
                if (actors != null && System.Linq.Enumerable.Contains(HazardPattern.Hazards, row.Pattern)
                    && actors.Rings.TryGetValue(row.Pattern, out var ring))
                    Ring(h, (float)HazardActors.PlayDiscFt(h.Radius, row, _night), Look.Of(ring));
            }
        }

        int _freezePose;

        /// <summary>One toy by name (data/art/hazard-actors.json). A name the catalog allows but this view cannot draw is an error.</summary>
        void Toy(string toy, Hazard h)
        {
            var p = new Vector3((float)h.X, 0, (float)h.Z);
            switch (toy)
            {
                case HazardActors.FreezeStatue: FreezeStatue(p, (float)h.Radius, _freezePose++); break;
                case HazardActors.LavaPit: LavaPit(h); break;
                case HazardActors.FireStatue: FireStatue(p, (float)h.Radius, true, (float)_rules.Hazards.Of(h.Type).NightRadiusMul); break;
                case HazardActors.KeepStatue: FireStatue(p, (float)h.Radius, false); break;
                case HazardActors.WarpCan: WarpCan(h); break;
                case HazardActors.BarrelCannon: BarrelCannon(h); break;
                case HazardActors.StarBillboard: StarBillboard(h); break;
                case HazardActors.VineClimb: ClimbWall(h); break;
                case HazardActors.ChomperMouth: ChomperMouth(_root, h); break;
                case HazardActors.MidwayTrain: MidwayTrain(h); break;
                case HazardActors.AcUnit: AcUnit(h); break;
                case HazardActors.JungleTree: JungleTree(p, (float)h.Radius); break;
                case HazardActors.LilyPad: LilyPad(h); break;
                default: Debug.LogError("ParkView: no hazard toy " + toy); break;
            }
        }

        /// <summary>
        /// The marsh's lily pad: a flat pad with a notch and a flower, the size of its disc. A mover (F4-f): the sim places it on
        /// the play clock and SetPlayClock moves the drawn one to the same place.
        /// </summary>
        void LilyPad(Hazard h)
        {
            var pad = Look.Lit(new Color(0.24f, 0.56f, 0.26f), smooth: 0.35f);
            var petal = Look.Lit(new Color(0.96f, 0.62f, 0.78f), smooth: 0.2f);
            var heart = Look.Lit(Colors.Gold, smooth: 0.3f);
            var r = Mathf.Max(2.5f, (float)h.Radius);
            var root = new GameObject("LilyPad").transform;
            root.SetParent(_root, false);
            root.position = new Vector3((float)h.X, 0, (float)h.Z);
            var mover = SolidBodies.Of(Park, _rules).FirstOrDefault(b => b.Moves && Math.Abs(b.X - h.X) < 1e-6 && Math.Abs(b.Z - h.Z) < 1e-6);
            if (mover != null) _movers.Add((mover, root));
            Look.Prim(PrimitiveType.Cylinder, "Pad", root, new Vector3(0, 0.25f, 0), new Vector3(r * 2f, 0.12f, r * 2f), pad);
            var notch = Look.Prim(PrimitiveType.Cube, "Notch", root, new Vector3(r * 0.55f, 0.3f, 0), new Vector3(r * 0.9f, 0.14f, 0.5f), Look.Lit(new Color(0.16f, 0.34f, 0.18f), smooth: 0.2f));
            notch.transform.localRotation = Quaternion.Euler(0, 20f, 0);
            for (var i = 0; i < 5; i++)
            {
                var a = i * 72f;
                var petalGo = Look.Prim(PrimitiveType.Sphere, "Petal" + i, root, Quaternion.Euler(0, a, 0) * new Vector3(0, 0.9f, 0.55f), new Vector3(0.6f, 0.35f, 1.1f), petal);
                petalGo.transform.localRotation = Quaternion.Euler(-25f, a, 0);
            }
            Look.Prim(PrimitiveType.Sphere, "Heart", root, new Vector3(0, 1.0f, 0), Vector3.one * 0.5f, heart);
        }

        /// <summary>The canopy grove's jungle-tree hazard toy.</summary>
        void JungleTree(Vector3 p, float radius)
        {
            var bark = Look.Lit(new Color(0.36f, 0.21f, 0.11f), smooth: 0.08f);
            var leaf = Look.Lit(new Color(0.12f, 0.4f, 0.18f), smooth: 0.1f);
            var moss = Look.Lit(new Color(0.22f, 0.48f, 0.18f), smooth: 0.1f);
            var root = new GameObject("Tree").transform;
            root.SetParent(_root, false);
            root.position = p;
            var h = Mathf.Clamp(radius * 1.6f, 10f, 22f);
            Look.Prim(PrimitiveType.Cylinder, "Trunk", root, new Vector3(0, h * 0.5f, 0), new Vector3(radius * 0.42f, h * 0.5f, radius * 0.42f), bark);
            Look.Prim(PrimitiveType.Sphere, "Canopy", root, new Vector3(0, h + radius * 0.35f, 0), Vector3.one * radius * 1.6f, leaf);
            Look.Prim(PrimitiveType.Sphere, "Canopy2", root, new Vector3(radius * 0.45f, h + radius * 0.1f, radius * 0.2f), Vector3.one * radius * 1.1f, moss);
            Look.Prim(PrimitiveType.Cube, "RootL", root, new Vector3(-radius * 0.4f, 0.4f, 0), new Vector3(radius * 0.7f, 0.7f, 0.7f), bark);
            Look.Prim(PrimitiveType.Cube, "RootR", root, new Vector3(radius * 0.4f, 0.4f, 0), new Vector3(radius * 0.7f, 0.7f, 0.7f), bark);
        }

        /// <summary>The greybox of a hazard whose park names no toys: a post in its pattern's ring color (white for a pattern that does not act).</summary>
        void PatternGreybox(Hazard h, HazardTypeRules row, HazardActors actors)
        {
            var color = actors != null && actors.Rings.TryGetValue(row.Pattern, out var c) ? Look.Of(c) : Color.white;
            Cylinder("Greybox-" + h.Type, new Vector3((float)h.X, 0, (float)h.Z), 1.5f, 6f, Look.Lit(color, smooth: 0.2f));
        }

        /// <summary>A flat ring on the grass at <paramref name="discFt"/> around the instance: the size the sim reads.</summary>
        void Ring(Hazard h, float discFt, Color color)
        {
            var ring = Look.Torus("PlayDisc-" + h.Type, _root, discFt, 0.35f, Look.Unlit(color), seg: 48, sides: 6);
            ring.transform.position = new Vector3((float)h.X, 0.12f, (float)h.Z);
        }

        void Glow(string name, Vector3 pos, Color color, float intensity, float range)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_root, false);
            go.transform.position = pos;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
        }

        void Quad(string name, Vector3 pos, Vector3 scale, Material mat) =>
            Cube(name, pos, scale, mat);

        void Cube(string name, Vector3 pos, Vector3 scale, Material mat, Quaternion? rot = null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(_root, false);
            go.transform.SetPositionAndRotation(pos, rot ?? Quaternion.identity);
            go.transform.localScale = scale;
            Destroy(go.GetComponent<Collider>());
            Look.Paint(go, mat);
        }

        void Cylinder(string name, Vector3 pos, float radius, float height, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(_root, false);
            go.transform.position = pos + new Vector3(0, height * 0.5f, 0);
            go.transform.localScale = new Vector3(radius * 2, height * 0.5f, radius * 2);
            Destroy(go.GetComponent<Collider>());
            Look.Paint(go, mat);
        }
    }
}

using System;
using GrandSluggers.Sim;
using GrandSluggers.Sim.Tooling;
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    /// <summary>
    /// Harbor as placed Hierarchy objects. The diamond, the rail and the wall are the one
    /// <see cref="FieldKit"/> every park draws (FD-16, #859), built here under Harbor's placed
    /// anchors; this component adds Harbor's own dress on top of it — the wall ads and pads, the
    /// striped lawn, the dugouts, the scoreboard, the bowls, the town, the night floods, the
    /// fireworks and the camera shot anchors. None of that dress is drawn at another park.
    /// </summary>
    public sealed class HarborKit : MonoBehaviour
    {

        public Transform DirtPad;
        public Transform HomeDirt;
        public Transform DirtDiamond;
        public Transform HomePlate;
        public Transform HomePoint;
        public Transform BoxL;
        public Transform BoxR;
        public Transform FoulL;
        public Transform FoulR;
        public Transform Mound;
        public Transform Rubber;
        public Transform ShotPlate;
        public Transform ShotMound;
        public Transform ShotDiamond;
        public Transform ShotThrow;
        public Transform WarningTrack;
        public Transform Backstop;
        public Transform Dugouts;
        public Transform WallDress;
        public Transform Grass;
        public Transform Scoreboard;
        public Transform Bleachers;
        public Transform Town;
        public Transform Fireworks;
        public Transform Poles;
        public Transform Bag1;
        public Transform Bag2;
        public Transform Bag3;

        // 1B at +X, 3B at −X. Open to the infield; the fascia is the rail along the pit.
        // Numbers live in HarborDugout so the still-gate cannot drift.
        public const float DugoutX = HarborDugout.X;
        public const float DugoutZ = HarborDugout.Z;
        public const float DugoutHalfAlong = HarborDugout.HalfAlong;
        public const float DugoutHalfDeep = HarborDugout.HalfDeep;
        public const float DugoutFasciaY = HarborDugout.FasciaY;

        bool _dressed;
        CameraShots _shots;
        internal Park _park;
        bool _night;
        FieldKit _field;
        Transform _awayTens, _awayOnes, _homeTens, _homeOnes, _innDigit;
        Transform _plateAwayTens, _plateAwayOnes, _plateHomeTens, _plateHomeOnes;
        Material _ledOn;
        Material _ledOff;
        readonly Firework[] _sparks = new Firework[28];
        public bool OwnsDiamond { get; private set; }
        ParkKitSlot _slots;

        struct Firework
        {
            public Transform Tf;
            public Vector3 Vel;
            public float Life;
        }

        void Awake()
        {
            EnsureAnchors();
        }

        public void Bind(Park park, RulesTable rules, bool night = false)
        {
            _park = park;
            _rules = rules;
            _diamond = DiamondGeometry.Of(rules);
            _field = null;
            _night = night;
            // The Harbor kit draws a park whose lawn slot it fills (FD-16, FR-13), never a park chosen by its id.
            _slots = ArtBinder.ParkKit(park != null ? park.Id : null);
            OwnsDiamond = _slots.Fills(ParkKitSlots.Lawn, ParkKitSlots.HarborLawn);
            gameObject.SetActive(OwnsDiamond);
            if (OwnsDiamond)
            {
                Dress();
                HookDigits();
                ApplyNight();
                ApplyShots();
            }
        }

        public void ShowBackstop(bool on)
        {
            if (Backstop != null) Backstop.gameObject.SetActive(on);
        }

        public void SyncShots(CameraShots shots)
        {
            _shots = shots;
            if (OwnsDiamond) ApplyShots();
        }

        public bool TryShot(string id, out Vector3 pos, out Vector3 look, out float fov)
        {
            pos = default;
            look = default;
            fov = 0;
            var tf = ShotNamed(id);
            if (tf == null) return false;
            pos = tf.position;
            var aim = tf.Find("Look");
            look = aim != null ? aim.position : pos + tf.forward * 40f;
            fov = tf.localScale.x > 1f ? tf.localScale.x : 0f;
            return true;
        }

        Transform ShotNamed(string id)
        {
            if (string.Equals(id, "plate", System.StringComparison.OrdinalIgnoreCase)) return ShotPlate;
            if (string.Equals(id, "mound", System.StringComparison.OrdinalIgnoreCase)) return ShotMound;
            if (string.Equals(id, "diamond", System.StringComparison.OrdinalIgnoreCase)) return ShotDiamond;
            if (string.Equals(id, "throw", System.StringComparison.OrdinalIgnoreCase)) return ShotThrow;
            if (string.IsNullOrEmpty(id)) return null;
            return transform.Find("Shot" + char.ToUpperInvariant(id[0]) + id.Substring(1));
        }

        /// <summary>The one field kit, built under this component's placed anchors.</summary>
        FieldKit Field => _field ??= new FieldKit(transform, _rules);

        /// <summary>The bound table and its diamond (<see cref="Bind"/>); null until a park is bound.</summary>
        RulesTable _rules;
        DiamondGeometry _diamond;

        /// <summary>The middle of the square, halfway to second, on the bound table (0 before a park is bound: the slabs are placeholders until <see cref="Dress"/> places them).</summary>
        float DiamondCentreZ => _diamond != null ? ParkDiamond.CenterZ(_diamond) : 0f;

        public void EnsureAnchors()
        {
            // Harbor's own scene slabs, hidden at dress (the 100-ft dirt that ate the infield grass).
            DirtPad = Anchor("DirtPad", new Vector3(0f, 0.04f, DiamondCentreZ), new Vector3(92f, 0.18f, 92f), Quaternion.identity);
            DirtDiamond = Anchor("DirtDiamond", new Vector3(0f, 0.12f, DiamondCentreZ), new Vector3(100f, 0.24f, 100f), Quaternion.Euler(0f, 45f, 0f));
            // The kit's anchors: the scene's placed objects where it has them, found by the kit's names.
            // The kit places each one from the geometry owner when it draws.
            HomeDirt = Field.Anchor(FieldKit.HomeDirtName);
            HomePlate = Field.Anchor(FieldKit.HomePlateName);
            HomePoint = Field.Anchor(FieldKit.HomePointName);
            BoxL = Field.Anchor(FieldKit.BoxLName);
            BoxR = Field.Anchor(FieldKit.BoxRName);
            FoulL = Field.Anchor(FieldKit.FoulLName);
            FoulR = Field.Anchor(FieldKit.FoulRName);
            Mound = Field.Anchor(FieldKit.MoundName);
            Rubber = Field.Anchor(FieldKit.RubberName);
            ShotPlate = ShotAnchor("ShotPlate",
                new Vector3((float)StillPose.PlateCamX, (float)StillPose.PlateCamY, (float)StillPose.PlateCamZ),
                new Vector3((float)StillPose.PlateLookX, (float)StillPose.PlateLookY, (float)StillPose.PlateLookZ),
                (float)StillPose.PlateFov);
            // The mound shot stands behind the bound table's rubber, so it waits for a park (Bind → Dress).
            if (_diamond != null)
                ShotMound = ShotAnchor("ShotMound",
                    new Vector3((float)StillPose.MoundCamX, (float)StillPose.MoundCamY, (float)StillPose.MoundCamZ(_diamond)),
                    new Vector3((float)StillPose.MoundLookX, (float)StillPose.MoundLookY, (float)StillPose.MoundLookZ),
                    (float)StillPose.MoundFov);
            ShotDiamond = ShotAnchor("ShotDiamond", new Vector3(20f, 20f, 55f), new Vector3(0f, 14f, 220f), 48f);
            ShotThrow = ShotAnchor("ShotThrow", new Vector3(0f, 6.2f, -14f), new Vector3(0f, 1.4f, 0f), 40f);
            WarningTrack = Field.Anchor(FieldKit.TrackName);
            Backstop = Folder("Backstop");
            Dugouts = Folder("Dugouts");
            WallDress = Folder("WallDress");
            Grass = Folder("Grass");
            Scoreboard = Folder("Scoreboard");
            Bleachers = Folder("Bleachers");
            Town = Folder("Town");
            Fireworks = Folder("Fireworks");
            Poles = Field.Anchor(FieldKit.PolesName);
            Bag1 = Field.Anchor(FieldKit.BagName(1));
            Bag2 = Field.Anchor(FieldKit.BagName(2));
            Bag3 = Field.Anchor(FieldKit.BagName(3));
        }

        public void Dress()
        {
            EnsureAnchors();
            if (_dressed && WallDress != null && WallDress.childCount > 0)
            {
                HookDigits();
                return;
            }
            DressDiamond();
            Field.Bags();
            DressPlace();
            HookDigits();
            _dressed = true;
        }

        /// <summary>
        /// SMS diamond language from the title still: dirt *paths* and pads,
        /// grass in the Y, mound as a hill, two white boxes + pentagon at home.
        /// The pieces are the field kit's; Harbor hands it the packed dirt at the plate.
        /// </summary>
        void DressDiamond()
        {
            var packed = Look.Lit(new Color(0.78f, 0.56f, 0.34f), Look.Dirt, 5f, 0.12f);

            // Kill the 100-ft dirt slab that ate the infield grass.
            Place(DirtPad, new Vector3(0f, 0.04f, 2f), new Vector3(0.2f, 0.02f, 0.2f), Quaternion.identity);
            if (DirtPad != null) DirtPad.gameObject.SetActive(false);
            Place(DirtDiamond, new Vector3(0f, 0.05f, DiamondCentreZ), new Vector3(0.2f, 0.02f, 0.2f), Quaternion.Euler(0f, 45f, 0f));
            if (DirtDiamond != null) DirtDiamond.gameObject.SetActive(false);

            Field.HomePad(packed);
            Field.Plate();
            Field.Boxes();
            Retire("CatcherBoxIn");
            Field.Mound();
        }

        /// <summary>Pieces an older Harbor dress left under this root; none is drawn any more.</summary>
        void Retire(params string[] names)
        {
            foreach (var n in names)
            {
                var old = transform.Find(n);
                if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
            }
        }

        Transform Anchor(string name, Vector3 pos, Vector3 scale, Quaternion rot)
        {
            var tf = transform.Find(name);
            if (tf != null) return tf;
            var go = new GameObject(name);
            tf = go.transform;
            tf.SetParent(transform, false);
            tf.position = pos;
            tf.localScale = scale;
            tf.rotation = rot;
            return tf;
        }

        Transform ShotAnchor(string name, Vector3 pos, Vector3 look, float fov)
        {
            var tf = transform.Find(name);
            if (tf == null)
            {
                var go = new GameObject(name);
                tf = go.transform;
                tf.SetParent(transform, false);
                tf.position = pos;
                tf.localScale = Vector3.one;
                tf.LookAt(look);
            }
            var aim = tf.Find("Look");
            if (aim == null)
            {
                var child = new GameObject("Look");
                aim = child.transform;
                aim.SetParent(tf, false);
                aim.position = look;
            }
            HideLookRay(aim);
            return tf;
        }

        void ApplyShots()
        {
            if (_shots == null) return;
            PlaceShot(ShotPlate, "plate");
            PlaceShot(ShotMound, "mound");
            PlaceShot(ShotDiamond, "diamond");
            PlaceShot(ShotThrow, "throw");
        }

        void PlaceShot(Transform tf, string id)
        {
            if (tf == null || !_shots.TryGet(id, out var s)) return;
            var pos = new Vector3((float)s.Pos.X, (float)s.Pos.Y, (float)s.Pos.Z);
            var look = new Vector3((float)s.Target.X, (float)s.Target.Y, (float)s.Target.Z);
            tf.position = pos;
            tf.localScale = Vector3.one;
            tf.LookAt(look);
            var aim = tf.Find("Look");
            if (aim != null)
            {
                aim.position = look;
                HideLookRay(aim);
            }
        }

        /// <summary>
        /// Shot Look children used to store FOV in scale and drew a white ray
        /// from the brim. Runtime cameras are data/feel/shots.json only.
        /// </summary>
        static void HideLookRay(Transform aim)
        {
            if (aim == null) return;
            foreach (var r in aim.GetComponentsInChildren<Renderer>(true))
                r.enabled = false;
            foreach (var lr in aim.GetComponentsInChildren<LineRenderer>(true))
                lr.enabled = false;
            aim.gameObject.SetActive(false);
        }

        static void Place(Transform tf, Vector3 pos, Vector3 scale, Quaternion rot)
        {
            if (tf == null) return;
            tf.position = pos;
            tf.localScale = scale;
            tf.rotation = rot;
        }

        static void Wipe(Transform tf)
        {
            if (tf == null) return;
            for (var i = tf.childCount - 1; i >= 0; i--)
                UnityEngine.Object.DestroyImmediate(tf.GetChild(i).gameObject);
        }

        Transform Folder(string name)
        {
            var tf = transform.Find(name);
            if (tf != null) return tf;
            var go = new GameObject(name);
            tf = go.transform;
            tf.SetParent(transform, false);
            return tf;
        }

        /// <summary>
        /// The field kit's pieces in Harbor's colors, with Harbor's dress between them in the order it
        /// has always been drawn: the striped lawn after the track, the dugouts before the wall, the ads
        /// on the wall as each span goes up.
        /// </summary>
        void DressPlace()
        {
            if (_park == null) return;
            var field = Field;
            field.Track(_park, Look.Lit(new Color(0.72f, 0.52f, 0.32f), Look.Dirt, 6f, 0.1f));
            DressGrass();
            Retire("PathHome1", "Path1to2", "Path2to3", "Path3toHome", "BagDirt1", "BagDirt2", "BagDirt3");
            field.InfieldDirt(Look.Lit(Colors.Dirt, Look.Dirt, 8f, 0.1f));
            field.FoulLines(_park);
            var yellow = Look.Unlit(Colors.Gold);
            field.Poles(_park, yellow, yellow);
            DressBackstop();
            // Each piece of Harbor's dress fills its kit slot (FD-16); a slot the park leaves empty draws nothing here.
            if (Fills(ParkKitSlots.Dugouts, ParkKitSlots.HarborDugouts)) DressDugouts(); else Wipe(Dugouts);
            if (Fills(ParkKitSlots.Wall, ParkKitSlots.HarborWall)) DressWall(); else Wipe(WallDress);
            if (Fills(ParkKitSlots.Scoreboard, ParkKitSlots.HarborScoreboard)) DressScoreboard(); else Wipe(Scoreboard);
            if (Fills(ParkKitSlots.Stands, ParkKitSlots.HarborStands)) DressBleachers(); else Wipe(Bleachers);
            if (Fills(ParkKitSlots.Backdrop, ParkKitSlots.HarborTown)) DressTown(); else Wipe(Town);
            if (Fills(ParkKitSlots.Night, ParkKitSlots.HarborFireworks)) DressNight(); else Wipe(Fireworks);
        }

        bool Fills(string slot, string builder) => _slots.Fills(slot, builder);

        void DressBackstop()
        {
            Wipe(Backstop);
        }

        void DressDugouts()
        {
            Wipe(Dugouts);
            if (!(DropDugout(DugoutX, "1B") && DropDugout(-DugoutX, "3B")))
            {
                Wipe(Dugouts);
                BuildDugout(DugoutX, "1B");
                BuildDugout(-DugoutX, "3B");
            }
            // Dirt floor under the lawn holes so the scoop still sees a pit, not sky.
            var pad = Look.Lit(new Color(0.58f, 0.40f, 0.24f), Look.Dirt, 3f, 0.1f);
            PitWell(1f, pad);
            PitWell(-1f, pad);
        }

        void PitWell(float xSign, Material pad)
        {
            var yaw = HarborDugout.YawDeg(xSign > 0f ? 1 : -1);
            var rot = Quaternion.Euler(0f, yaw, 0f);
            Box(Dugouts, xSign > 0f ? "Well1B" : "Well3B",
                new Vector3(xSign * DugoutX, HarborDugout.PitFloorY, DugoutZ),
                new Vector3(DugoutHalfDeep * 2f + 1f, 0.22f, DugoutHalfAlong * 2f + 0.6f),
                rot, pad);
        }

        bool DropDugout(float x, string side)
        {
            var mesh = "dugout-1b";
            var yaw = HarborDugout.YawDeg(x > 0f ? 1 : -1);
            var rot = Quaternion.Euler(0f, yaw, 0f);
            var go = DropMesh(mesh, Dugouts, "Dug" + side, new Vector3(x, 0f, DugoutZ), rot, Vector3.one, paint: true);
            if (go == null) return false;
            var mf = go.GetComponentInChildren<MeshFilter>(true);
            var alongSpan = 0f;
            if (mf != null && mf.sharedMesh != null)
            {
                var size = mf.sharedMesh.bounds.size;
                alongSpan = Mathf.Max(size.x, size.z);
            }
            if (!HarborDugout.KitSpansTheOpening(alongSpan))
            {
                UnityEngine.Object.DestroyImmediate(go);
                return false;
            }
            var along = rot * Vector3.forward * (DugoutHalfAlong * 1.2f);
            var back = rot * Vector3.right;
            SitRow(
                Dugouts,
                "Dug" + side + "Sit",
                new Vector3(x, 0f, DugoutZ) + back * (DugoutHalfDeep - 1.6f) + Vector3.up * HarborDugout.PitFloorY,
                along,
                6,
                side == "1B" ? 11 : 71,
                side == "1B");
            return true;
        }

        /// <summary>
        /// Sunken pit on the hip wall. Local −X is the field rail, +Z toward
        /// the bag, stairs at the home end (−Z). Roof covers the bench only.
        /// Missing kit FBX still looks like this.
        /// </summary>
        void BuildDugout(float x, string side)
        {
            var wood = Look.Toon(new Color(0.42f, 0.26f, 0.12f));
            var roofMat = Look.Toon(new Color(0.14f, 0.32f, 0.20f));
            var pad = Look.Lit(new Color(0.58f, 0.40f, 0.24f), Look.Dirt, 3f, 0.1f);
            var well = Look.Lit(new Color(0.50f, 0.34f, 0.20f), Look.Dirt, 2f, 0.12f);
            var rail = Look.Toon(Colors.Gold);
            var post = Look.Toon(new Color(0.28f, 0.22f, 0.16f));
            var conc = Look.Lit(new Color(0.62f, 0.60f, 0.56f), smooth: 0.12f);
            var meshMat = Look.Toon(new Color(0.22f, 0.20f, 0.18f));
            var yaw = HarborDugout.YawDeg(x > 0f ? 1 : -1);
            var rot = Quaternion.Euler(0f, yaw, 0f);
            var origin = new Vector3(x, 0f, DugoutZ);
            Vector3 At(float lx, float ly, float lz) => origin + rot * new Vector3(lx, ly, lz);

            var field = -DugoutHalfDeep;
            var back = DugoutHalfDeep;
            var along = DugoutHalfAlong * 2f;
            var deep = DugoutHalfDeep * 2f;
            var pit = HarborDugout.PitDepth;
            var floorY = HarborDugout.PitFloorY;
            var railY = DugoutFasciaY;
            var home = -DugoutHalfAlong;
            var bag = DugoutHalfAlong;
            var gate = 3.5f;
            var meshAlong = along - gate - 0.4f;
            var meshMid = (home + gate + bag) * 0.5f;

            Box(Dugouts, "Dug" + side + "Floor",
                At(0.2f, floorY, 0f), new Vector3(deep + 0.5f, 0.22f, along + 0.5f), rot, pad);
            Box(Dugouts, "Dug" + side + "BackWall",
                At(back, (floorY + railY + 3.2f) * 0.5f, 0f),
                new Vector3(0.38f, railY + 3.2f + pit, along + 0.2f), rot, well);
            Box(Dugouts, "Dug" + side + "EndHome",
                At(0f, (floorY + railY) * 0.5f, home),
                new Vector3(deep + 0.2f, railY + pit, 0.34f), rot, well);
            Box(Dugouts, "Dug" + side + "EndBag",
                At(0f, (floorY + railY + 3.2f) * 0.5f, bag),
                new Vector3(deep + 0.2f, railY + 3.2f + pit, 0.34f), rot, well);
            Box(Dugouts, "Dug" + side + "Sill",
                At(field, floorY + 0.35f, meshMid),
                new Vector3(0.42f, 0.7f, meshAlong), rot, pad);
            Box(Dugouts, "Dug" + side + "RailPad",
                At(field, railY, meshMid),
                new Vector3(0.58f, 0.48f, meshAlong + 0.3f), rot, pad);
            Box(Dugouts, "Dug" + side + "Fascia",
                At(field - 0.28f, railY + 0.42f, meshMid),
                new Vector3(0.16f, 0.62f, meshAlong + 0.15f), rot, rail);
            Box(Dugouts, "Dug" + side + "Roof",
                At(back - 1.4f, railY + 3.0f, 0f),
                new Vector3(deep * 0.55f, 0.24f, along + 0.4f), rot, roofMat);
            Box(Dugouts, "Dug" + side + "Bench",
                At(back - 1.45f, floorY + 0.82f, 0f),
                new Vector3(1.55f, 0.22f, along - 4f), rot, wood);

            var barH = railY - 0.4f + pit - 0.5f;
            var barY = floorY + 0.5f + barH * 0.5f;
            for (var i = 0; i < 11; i++)
            {
                var t = i / 10f;
                var z = home + gate + 0.4f + t * (meshAlong - 0.8f);
                Box(Dugouts, "Dug" + side + "MeshV" + i,
                    At(field - 0.02f, barY, z), new Vector3(0.06f, barH, 0.08f), rot, meshMat);
            }

            var stepH = pit / HarborDugout.StairCount;
            for (var i = 0; i < HarborDugout.StairCount; i++)
            {
                var y = -stepH * (i + 0.5f);
                var z = home + 0.15f + i * HarborDugout.StairDepth;
                Box(Dugouts, "Dug" + side + "Stair" + i,
                    At(0f, y, z),
                    new Vector3(deep - 1f, stepH, HarborDugout.StairDepth + 0.04f), rot, conc);
            }

            SitRow(
                Dugouts,
                "Dug" + side + "Sit",
                At(back - 1.7f, floorY, 0f),
                rot * Vector3.forward * (along * 0.62f),
                5,
                side == "1B" ? 11 : 71,
                side == "1B");
        }

        void DressGrass()
        {
            Wipe(Grass);
            var dark = Look.Lit(Colors.Grass, Look.Grass, 16f, 0.08f);
            var light = Look.Lit(Colors.Cut, Look.Grass, 12f, 0.08f);
            var y = ParkDiamond.GrassY;
            var h = ParkDiamond.GrassThick;
            var stripe = ParkDiamond.StripeWidth;
            var z0 = ParkDiamond.GrassZ0;
            var zEnd = _park != null ? ParkDiamond.GrassZ1(_park) : 380f;
            var halfW = ParkDiamond.GrassHalfWidth(zEnd);

            var n = Mathf.CeilToInt(halfW / stripe);
            for (var i = -n; i <= n; i++)
            {
                var xc = ParkDiamond.StripeCenterX(i);
                var sx = stripe + 0.4f;
                var mat = (i & 1) == 0 ? dark : light;
                StripeColumn("G" + i, xc, sx, z0, zEnd, y, h, mat);
            }
        }

        void StripeColumn(string name, float xc, float sx, float zLo, float zHi, float y, float h, Material lawn)
        {
            if (!HarborDugout.TryHoleZ(xc, out var hz0, out var hz1) || hz1 < zLo || hz0 > zHi)
            {
                LawnBand(name, xc, y, (zLo + zHi) * 0.5f, sx, h, zHi - zLo, lawn);
                return;
            }
            if (zLo < hz0)
                LawnBand(name + "S", xc, y, (zLo + hz0) * 0.5f, sx, h, hz0 - zLo, lawn);
            if (zHi > hz1)
                LawnBand(name + "N", xc, y, (hz1 + zHi) * 0.5f, sx, h, zHi - hz1, lawn);
        }

        void LawnBand(string name, float x, float y, float z, float sx, float sy, float sz, Material lawn)
        {
            if (sx < 1f || sz < 1f) return;
            Cube(Grass, name, new Vector3(x, y, z), new Vector3(sx, sy, sz), lawn);
        }

        /// <summary>
        /// The kit's wall loop in Harbor's green pad and gold cap. The dugout fascia is the rail along
        /// each pit (<see cref="HarborDugout.WallOpensHere"/>), so the kit leaves those rail spans to it.
        /// Harbor's ads, ivy and center-field mark hang on each span as it goes up.
        /// </summary>
        void DressWall()
        {
            Wipe(WallDress);
            if (_park == null) return;
            // D15: the drawn wall is the park's fence, the top the flight clips against.
            var h = HarborWall.OutfieldHeight(_park);
            var pad = Look.Lit(new Color(0.18f, 0.46f, 0.30f), Look.Grass, 3f, 0.08f);
            var cap = Look.Unlit(Colors.Gold);
            var ivy = Look.Lit(new Color(0.14f, 0.48f, 0.22f), Look.Grass, 2f, 0.08f);
            var ads = new[]
            {
                Look.Unlit(Colors.Spark),
                Look.Unlit(Colors.Gold),
                Look.Unlit(new Color(0.18f, 0.42f, 0.72f)),
                Look.Unlit(new Color(0.94f, 0.94f, 0.9f)),
                Look.Unlit(new Color(0.12f, 0.16f, 0.28f)),
                Look.Unlit(new Color(0.95f, 0.55f, 0.18f)),
                Look.Unlit(new Color(0.22f, 0.55f, 0.38f)),
                Look.Unlit(new Color(0.72f, 0.22f, 0.38f))
            };
            var mark = Look.Unlit(Colors.Gold);
            var spark = Look.Unlit(Colors.Spark);
            Field.Wall(_park, new[] { pad }, cap, (x, z) => HarborDugout.WallOpensHere(x, z, _rules), span =>
            {
                var i = span.Index;
                var mid = span.Mid;
                var outward = span.Outward;
                var thick = span.Thick;
                var hh = (span.H0 + span.H1) * 0.5f;
                var w = Vector3.Distance(span.A, span.B);
                var rot = Quaternion.LookRotation(outward, Vector3.up);
                var ad = HarborPostcard.OnWallFace(hh, HarborPostcard.AdHeightFt);
                if (hh > 10f && i % 2 == 0)
                    Box(WallDress, "Ad" + i, mid - outward * (thick * 0.55f) + Vector3.up * ad.Y,
                        new Vector3(Mathf.Min(HarborPostcard.AdWidthFt, w * 0.72f), ad.Height, 0.45f), rot, ads[i % ads.Length]);
                if (hh > 10f && i % 3 == 0)
                    Box(WallDress, "Ivy" + i, mid - outward * (thick * 0.6f) + Vector3.up * 3.2f, new Vector3(8.5f, 5.4f, 0.4f), rot, ivy);
                if (Mathf.Abs(mid.x) < 8f && mid.z > 300f)
                {
                    var big = HarborPostcard.OnWallFace(h, 6.4f);
                    var small = HarborPostcard.OnWallFace(h, 3.2f);
                    Box(WallDress, "MarkSpark", mid - outward * (thick * 0.7f) + Vector3.up * big.Y, new Vector3(big.Height, big.Height, 0.5f), rot, spark);
                    Box(WallDress, "MarkGold", mid - outward * (thick * 0.82f) + Vector3.up * small.Y, new Vector3(small.Height, small.Height, 0.4f), rot, mark);
                }
            });
        }

        void DressScoreboard()
        {
            var z = (float)_park.CenterFenceFt + HarborPostcard.ScoreboardPastFenceFt;
            var house = Look.Lit(new Color(0.12f, 0.14f, 0.18f), smooth: 0.08f);
            var face = Look.Unlit(new Color(0.10f, 0.22f, 0.14f));
            _ledOn = Look.Unlit(new Color(1f, 0.78f, 0.18f));
            _ledOff = Look.Unlit(new Color(0.08f, 0.12f, 0.09f));
            var led = _ledOn;
            var label = Look.Unlit(new Color(0.92f, 0.94f, 0.9f));
            var digit = HarborPostcard.DigitHeightFt / 3.1f;
            Cube(Scoreboard, "ScoreHouse", new Vector3(0f, 28f, z), new Vector3(64f, 40f, 10f), house);
            Cube(Scoreboard, "ScoreFace", new Vector3(0f, 28f, z - 5.2f), new Vector3(56f, 28f, 0.7f), face);
            Cube(Scoreboard, "ScoreSpark", new Vector3(0f, 46f, z - 5.5f), new Vector3(22f, 4.4f, 0.6f), Look.Unlit(Colors.Spark));
            Cube(Scoreboard, "ScoreBar", new Vector3(0f, 12f, z - 5f), new Vector3(48f, 1.6f, 0.6f), Look.Unlit(Colors.Gold));
            Cube(Scoreboard, "LblAway", new Vector3(-16.5f, 40.2f, z - 5.7f), new Vector3(10f, 1.8f, 0.35f), label);
            Cube(Scoreboard, "LblHome", new Vector3(16.5f, 40.2f, z - 5.7f), new Vector3(10f, 1.8f, 0.35f), Look.Unlit(Colors.Spark));
            Cube(Scoreboard, "LblInn", new Vector3(0f, 18.4f, z - 5.7f), new Vector3(6.4f, 1.4f, 0.35f), Look.Unlit(Colors.Gold));
            _awayTens = MakeDigit(Scoreboard, "AwayTens", new Vector3(-20.4f, 30.2f, z - 5.8f), led, digit);
            _awayOnes = MakeDigit(Scoreboard, "AwayOnes", new Vector3(-12.2f, 30.2f, z - 5.8f), led, digit);
            _homeTens = MakeDigit(Scoreboard, "HomeTens", new Vector3(12.2f, 30.2f, z - 5.8f), led, digit);
            _homeOnes = MakeDigit(Scoreboard, "HomeOnes", new Vector3(20.4f, 30.2f, z - 5.8f), led, digit);
            _innDigit = MakeDigit(Scoreboard, "InnDigit", new Vector3(0f, 22.4f, z - 5.8f), led, digit * 0.72f);
            Cube(Scoreboard, "HomeHouse", new Vector3(0f, 20.4f, -38.8f), new Vector3(22f, 11f, 2.8f), house);
            Cube(Scoreboard, "HomeFace", new Vector3(0f, 20.4f, -37.3f), new Vector3(18.5f, 7.6f, 0.35f), face);
            Cube(Scoreboard, "HomeLblA", new Vector3(-5.6f, 23.0f, -37.05f), new Vector3(3.2f, 0.7f, 0.22f), label);
            Cube(Scoreboard, "HomeLblH", new Vector3(5.6f, 23.0f, -37.05f), new Vector3(3.2f, 0.7f, 0.22f), Look.Unlit(Colors.Spark));
            _plateAwayTens = MakeDigit(Scoreboard, "PlateAwayTens", new Vector3(-6.8f, 20.2f, -37.0f), led, 0.55f);
            _plateAwayOnes = MakeDigit(Scoreboard, "PlateAwayOnes", new Vector3(-4.4f, 20.2f, -37.0f), led, 0.55f);
            _plateHomeTens = MakeDigit(Scoreboard, "PlateHomeTens", new Vector3(4.4f, 20.2f, -37.0f), led, 0.55f);
            _plateHomeOnes = MakeDigit(Scoreboard, "PlateHomeOnes", new Vector3(6.8f, 20.2f, -37.0f), led, 0.55f);
            SetScore(0, 0, 1);
        }

        void HookDigits()
        {
            if (Scoreboard == null) return;
            if (_awayTens == null) _awayTens = Scoreboard.Find("AwayTens");
            if (_awayOnes == null) _awayOnes = Scoreboard.Find("AwayOnes");
            if (_homeTens == null) _homeTens = Scoreboard.Find("HomeTens");
            if (_homeOnes == null) _homeOnes = Scoreboard.Find("HomeOnes");
            if (_innDigit == null) _innDigit = Scoreboard.Find("InnDigit");
            if (_plateAwayTens == null) _plateAwayTens = Scoreboard.Find("PlateAwayTens");
            if (_plateAwayOnes == null) _plateAwayOnes = Scoreboard.Find("PlateAwayOnes");
            if (_plateHomeTens == null) _plateHomeTens = Scoreboard.Find("PlateHomeTens");
            if (_plateHomeOnes == null) _plateHomeOnes = Scoreboard.Find("PlateHomeOnes");
            if (_ledOn == null) _ledOn = Look.Unlit(new Color(1f, 0.78f, 0.18f));
            if (_ledOff == null) _ledOff = Look.Unlit(new Color(0.08f, 0.12f, 0.09f));
        }

        public void SetScore(int away, int home, int inning)
        {
            if (_awayTens == null) HookDigits();
            if (_awayTens == null) return;
            away = Mathf.Clamp(away, 0, 99);
            home = Mathf.Clamp(home, 0, 99);
            inning = Mathf.Clamp(inning, 1, 9);
            PaintDigit(_awayTens, away / 10);
            PaintDigit(_awayOnes, away % 10);
            PaintDigit(_homeTens, home / 10);
            PaintDigit(_homeOnes, home % 10);
            PaintDigit(_innDigit, inning);
            PaintDigit(_plateAwayTens, away / 10);
            PaintDigit(_plateAwayOnes, away % 10);
            PaintDigit(_plateHomeTens, home / 10);
            PaintDigit(_plateHomeOnes, home % 10);
        }

        void DressBleachers()
        {
            Wipe(Bleachers);
            if (_park == null) return;
            var conc = Look.Lit(new Color(0.70f, 0.72f, 0.76f), smooth: 0.1f);
            var rail = Look.Lit(Colors.Gold, smooth: 0.4f);
            var roof = Look.Unlit(new Color(0.94f, 0.94f, 0.90f));
            var post = Look.Lit(new Color(0.80f, 0.80f, 0.78f), smooth: 0.18f);
            var crowd = Look.Lit(Color.white, Look.Crowd, 5f, 0.06f);
            DressHomeBowl(conc, crowd, rail);
            DressWingBowl(1, conc, crowd, roof, post);
            DressWingBowl(-1, conc, crowd, roof, post);
            DressCornerBowl(1, conc, crowd, roof, post);
            DressCornerBowl(-1, conc, crowd, roof, post);
        }

        void DressHomeBowl(Material conc, Material crowd, Material rail)
        {
            var rise = HarborStands.RowRun;
            for (var row = 0; row < HarborStands.HomeRows; row++)
            {
                var y = HarborStands.RowY(row);
                var z = HarborStands.HomeZ0 - row * rise;
                var half = HarborStands.HomeHalf0 + row * 3.2f;
                Cube(Bleachers, "HomeStep" + row, new Vector3(0, y, z), new Vector3(half * 2f, 0.95f, rise + 0.35f), conc);
                Cube(Bleachers, "HomeCrowd" + row,
                    new Vector3(0, y + 0.55f + HarborStands.PersonFt * 0.42f, z - 0.45f),
                    new Vector3(half * 1.88f, HarborStands.PersonFt * 1.45f, 0.55f), crowd);
                if (row == 0)
                    SmallFans("HomeFan", new Vector3(0, y + 0.2f, z + 0.2f), new Vector3(half * 1.6f, 0, 0), Vector3.back, 0);
            }
            Cube(Bleachers, "RailHome", new Vector3(0, 2.0f, HarborStands.HomeZ0 + 3.2f), new Vector3(70, 1.15f, 1.15f), rail);
        }

        void DressWingBowl(int sign, Material conc, Material crowd, Material roof, Material post)
        {
            var z0 = HarborStands.WingZ0;
            var z1 = HarborStands.WingZ1;
            for (var row = 0; row < HarborStands.WingRows; row++)
            {
                var a = new Vector3(sign * HarborStands.WingX(z0, row), 0f, z0);
                var b = new Vector3(sign * HarborStands.WingX(z1, row), 0f, z1);
                var along = b - a;
                along.y = 0f;
                if (along.sqrMagnitude < 4f) continue;
                var mid = (a + b) * 0.5f;
                mid.y = HarborStands.RowY(row);
                var rot = Quaternion.LookRotation(along.normalized, Vector3.up);
                var inward = Vector3.Cross(Vector3.up, along.normalized) * sign;
                Box(Bleachers, (sign > 0 ? "RStep" : "LStep") + row, mid,
                    new Vector3(HarborStands.RowRun + 0.55f, 1.15f, along.magnitude + 1.1f), rot, conc);
                Box(Bleachers, (sign > 0 ? "RCrowd" : "LCrowd") + row,
                    mid + Vector3.up * (0.65f + HarborStands.PersonFt * 0.42f) + inward * 0.15f,
                    new Vector3(0.55f, HarborStands.PersonFt * 1.45f, along.magnitude * 0.96f), rot, crowd);
                if (row == 0)
                    SmallFans(sign > 0 ? "RFan" : "LFan", mid + inward * 0.4f, along, inward, sign > 0 ? 400 : 200);
            }
        }

        void DressCornerBowl(int sign, Material conc, Material crowd, Material roof, Material post)
        {
            var n = HarborStands.CornerSegs;
            var rows = HarborStands.CornerRows;
            for (var i = 0; i < n; i++)
            {
                var t0 = i / (float)n;
                var t1 = (i + 1) / (float)n;
                var s0 = sign * Mathf.Lerp(HarborStands.CornerSpray0, HarborStands.CornerSpray1, t0);
                var s1 = sign * Mathf.Lerp(HarborStands.CornerSpray0, HarborStands.CornerSpray1, t1);
                var p0 = HarborPostcard.WallPoint(_park, s0);
                var p1 = HarborPostcard.WallPoint(_park, s1);
                var r0 = new Vector3((float)p0.X, 0f, (float)p0.Z);
                var r1 = new Vector3((float)p1.X, 0f, (float)p1.Z);
                if (r0.sqrMagnitude < 1f || r1.sqrMagnitude < 1f) continue;
                var n0 = r0.normalized;
                var n1 = r1.normalized;
                for (var row = 0; row < rows; row++)
                {
                    var behind = HarborStands.CornerBehind0 + row * HarborStands.RowRun;
                    var a = r0 + n0 * behind;
                    var b = r1 + n1 * behind;
                    var along = b - a;
                    along.y = 0f;
                    if (along.sqrMagnitude < 0.4f) continue;
                    var mid = (a + b) * 0.5f;
                    mid.y = HarborStands.CornerRowY(_park, row);
                    var radial = mid;
                    radial.y = 0f;
                    if (radial.sqrMagnitude < 1f) continue;
                    radial.Normalize();
                    var rot = Quaternion.LookRotation(along.normalized, Vector3.up);
                    var tag = (sign > 0 ? "Rf" : "Lf") + i + "r" + row;
                    Box(Bleachers, "CStep" + tag, mid,
                        new Vector3(HarborStands.RowRun + 0.6f, 1.2f, along.magnitude + 1.0f), rot, conc);
                    Box(Bleachers, "CCrowd" + tag,
                        mid - radial * 0.25f + Vector3.up * (0.7f + HarborStands.PersonFt * 0.42f),
                        new Vector3(0.55f, HarborStands.PersonFt * 1.5f, along.magnitude * 0.95f), rot, crowd);
                }
            }
        }

        void SmallFans(string name, Vector3 origin, Vector3 along, Vector3 inward, int seed)
        {
            var len = along.magnitude;
            if (len < 4f) return;
            var dir = along / len;
            var n = Mathf.Max(4, Mathf.RoundToInt(len / (HarborStands.SeatFt * 2.1f)));
            var scale = HarborStands.PersonFt / 1.8f;
            var jersey = new[]
            {
                Colors.Spark, Colors.Royal, Colors.Gold, Color.white,
                new Color(0.22f, 0.62f, 0.32f), new Color(0.82f, 0.28f, 0.18f),
                new Color(0.72f, 0.35f, 0.78f), new Color(0.18f, 0.42f, 0.72f),
                new Color(0.95f, 0.55f, 0.18f)
            };
            var flesh = Look.Toon(new Color(1f, 0.80f, 0.68f));
            var dark = Look.Toon(new Color(0.36f, 0.24f, 0.16f));
            var tall = HarborStands.PersonFt;
            for (var i = 0; i < n; i++)
            {
                var u = n <= 1 ? 0f : i / (float)(n - 1) - 0.5f;
                var p = origin + dir * (u * len);
                p += inward * ((Hash01(seed + i) - 0.5f) * 0.35f);
                var body = Look.Toon(jersey[i % jersey.Length]);
                if (DropFan("fan-sit", Bleachers, name + i, p, body, scale))
                    continue;
                Capsule(Bleachers, name + "Body" + i, p + new Vector3(0f, tall * 0.42f, 0f),
                    new Vector3(tall * 0.22f, tall * 0.42f, tall * 0.22f), body);
                Sphere(Bleachers, name + "Head" + i, p + new Vector3(0f, tall * 0.82f, 0f),
                    tall * 0.12f, i % 5 == 0 ? dark : flesh);
            }
        }

        void SitRow(Transform parent, string name, Vector3 origin, Vector3 along, int n, int seed, bool home)
        {
            if (parent == null) return;
            var jersey = home
                ? new[] { Colors.Spark, Color.white, Colors.Gold, Colors.Spark, new Color(0.94f, 0.94f, 0.9f) }
                : new[] { Colors.Royal, new Color(0.14f, 0.18f, 0.34f), Color.white, Colors.Gold, new Color(0.82f, 0.28f, 0.18f) };
            var flesh = Look.Toon(new Color(1f, 0.80f, 0.68f));
            var dark = Look.Toon(new Color(0.36f, 0.24f, 0.16f));
            for (var i = 0; i < n; i++)
            {
                var u = n <= 1 ? 0f : i / (float)(n - 1) - 0.5f;
                var p = origin + along * u;
                p.z += (Hash01(seed + i) - 0.5f) * 0.2f;
                var body = Look.Toon(jersey[i % jersey.Length]);
                if (DropFan("fan-sit", parent, name + i, p, body))
                    continue;
                Capsule(parent, name + "Body" + i, p + new Vector3(0f, 0.78f, 0f), new Vector3(0.58f, 0.42f, 0.58f), body);
                Sphere(parent, name + "Head" + i, p + new Vector3(0f, 1.32f, 0f), 0.32f, i % 4 == 0 ? dark : flesh);
            }
        }

        void CrowdBank(Transform parent, string name, Vector3 origin, Vector3 along, Vector3 across, int seats, int deep, int seed)
        {
            if (parent == null) return;
            var root = parent.Find(name);
            if (root == null)
            {
                var go = new GameObject(name);
                root = go.transform;
                root.SetParent(parent, false);
            }
            var jersey = new[]
            {
                Colors.Spark, Colors.Royal, Color.white, Colors.Gold,
                new Color(0.14f, 0.18f, 0.34f), new Color(0.82f, 0.28f, 0.18f)
            };
            var flesh = Look.Toon(new Color(1f, 0.80f, 0.68f));
            var dark = Look.Toon(new Color(0.36f, 0.24f, 0.16f));
            var n = 0;
            for (var d = 0; d < deep; d++)
            {
                for (var s = 0; s < seats; s++)
                {
                    var u = seats <= 1 ? 0f : s / (float)(seats - 1) - 0.5f;
                    var p = origin + along * u + across * d;
                    p.x += (Hash01(seed + n) - 0.5f) * 0.45f;
                    p.z += (Hash01(seed + n + 9) - 0.5f) * 0.4f;
                    var body = Look.Toon(jersey[n % jersey.Length]);
                    var tall = HarborPostcard.CrowdPersonFt;
                    var scale = tall / 1.8f;
                    if (DropFan("fan-stand", root, "Fan" + n, p, body, scale))
                    {
                        n++;
                        continue;
                    }
                    Capsule(root, "Body" + n, p + new Vector3(0f, tall * 0.42f, 0f), new Vector3(tall * 0.38f, tall * 0.42f, tall * 0.38f), body);
                    Sphere(root, "Head" + n, p + new Vector3(0f, tall * 0.82f, 0f), tall * 0.16f, n % 5 == 0 ? dark : flesh);
                    n++;
                }
            }
        }

        void DressTown()
        {
            Wipe(Town);
            var z0 = (float)_park.CenterFenceFt + HarborPostcard.TownPastFenceFt;
            var h = HarborPostcard.TownHeightFt;
            var clap = Look.Lit(new Color(0.78f, 0.58f, 0.36f), Look.Dirt, 4f, 0.12f);
            var brick = Look.Lit(new Color(0.62f, 0.28f, 0.20f), Look.Dirt, 5f, 0.1f);
            var red = Look.Lit(Colors.SparkDark, Look.Dirt, 3f, 0.12f);
            var pane = Look.Unlit(new Color(0.22f, 0.36f, 0.48f));
            var roof = Look.Unlit(Colors.Gold);
            Cube(Town, "Wharf", new Vector3(-88, h * 0.38f, z0 + 8f), new Vector3(42, h * 0.76f, 28), clap);
            Cube(Town, "SparkHall", new Vector3(-18, h * 0.48f, z0 + 22f), new Vector3(28, h * 0.96f, 24), red);
            Cube(Town, "Loft", new Vector3(58, h * 0.55f, z0 + 12f), new Vector3(34, h * 1.1f, 26), brick);
            Cube(Town, "BlockR", new Vector3(118, h * 0.42f, z0 - 6f), new Vector3(40, h * 0.84f, 22), brick);
            Cube(Town, "BlockL", new Vector3(-140, h * 0.32f, z0 - 18f), new Vector3(36, h * 0.64f, 20), clap);
            Cube(Town, "Pier", new Vector3(110, 6, z0 - 28f), new Vector3(78, 6, 18), brick);
            Cylinder(Town, "Light", new Vector3(168, 0, z0 - 42f), 2.6f, 42f, Look.Lit(new Color(0.35f, 0.3f, 0.22f), Look.Dirt, 1f, 0.1f));
            Cube(Town, "RoofSpark", new Vector3(-18, h * 0.98f, z0 + 22f), new Vector3(32, 5, 26), roof);
            Windows(Town, "WinWharf", new Vector3(-88, h * 0.4f, z0 - 6f), 5, 4, pane);
            Windows(Town, "WinHall", new Vector3(-18, h * 0.5f, z0 + 10f), 4, 5, pane);
            Windows(Town, "WinLoft", new Vector3(58, h * 0.55f, z0 - 1f), 4, 6, pane);
            Windows(Town, "WinBlock", new Vector3(118, h * 0.42f, z0 - 17f), 4, 4, pane);
        }

        static void Windows(Transform parent, string name, Vector3 origin, int cols, int rows, Material pane)
        {
            for (var r = 0; r < rows; r++)
            {
                for (var c = 0; c < cols; c++)
                {
                    var x = origin.x + (c - (cols - 1) * 0.5f) * 5.6f;
                    var y = origin.y + (r - (rows - 1) * 0.5f) * 6.2f;
                    Cube(parent, name + r + c, new Vector3(x, y, origin.z), new Vector3(3.2f, 3.8f, 0.45f), pane);
                }
            }
        }

        void DressNight()
        {
            if (Fireworks != null) Fireworks.position = new Vector3(0, 12, 400);
            if (Fireworks != null && Fireworks.Find("FloodL") == null)
            {
                Glow(Fireworks, "FloodL", new Vector3(-90, 28, 40), new Color(1f, 0.92f, 0.75f), 1.8f, 140f);
                Glow(Fireworks, "FloodR", new Vector3(90, 28, 40), new Color(1f, 0.92f, 0.75f), 1.8f, 140f);
                Glow(Fireworks, "FloodH", new Vector3(0, 22, -40), new Color(1f, 0.90f, 0.70f), 1.4f, 110f);
                Glow(Fireworks, "FloodC", new Vector3(0, 32, 240), new Color(0.85f, 0.9f, 1f), 1.2f, 180f);
            }
            ApplyNight();
        }

        void ApplyNight()
        {
            if (Fireworks != null) Fireworks.gameObject.SetActive(_night);
        }

        public void Tick(Vector3 ball, float dt)
        {
            for (var i = 0; i < _sparks.Length; i++)
            {
                var s = _sparks[i];
                if (s.Tf == null || s.Life <= 0) continue;
                s.Life -= dt;
                s.Vel += new Vector3(0, -22f * dt, 0);
                s.Tf.position += s.Vel * dt;
                var u = Mathf.Clamp01(s.Life);
                s.Tf.localScale = Vector3.one * (0.4f + 1.2f * u);
                s.Tf.gameObject.SetActive(s.Life > 0);
                _sparks[i] = s;
            }
        }

        public void BurstFireworks(Vector3 at)
        {
            if (!_night || Fireworks == null) return;
            var cols = new[]
            {
                Colors.Spark, Colors.Gold, new Color(1f, 0.45f, 0.2f),
                new Color(0.45f, 0.75f, 1f), new Color(1f, 0.35f, 0.7f)
            };
            var origin = at.z < 80f ? new Vector3(0, 18, 390) : at + Vector3.up * 8f;
            for (var i = 0; i < _sparks.Length; i++)
            {
                if (_sparks[i].Tf != null) Destroy(_sparks[i].Tf.gameObject);
                var col = cols[i % cols.Length];
                var go = Look.Prim(PrimitiveType.Sphere, "Spark" + i, Fireworks, origin, Vector3.one * 1.4f, Look.Unlit(col));
                go.transform.position = origin;
                var a = i / (float)_sparks.Length * Mathf.PI * 2f;
                var lift = 18f + (i % 5) * 6f;
                _sparks[i] = new Firework
                {
                    Tf = go.transform,
                    Vel = new Vector3(Mathf.Cos(a) * 22f, lift, Mathf.Sin(a) * 16f),
                    Life = 1.6f + (i % 4) * 0.12f
                };
            }
        }

        static readonly int[] DigitMask = HarborPostcard.DigitMask;

        Transform MakeDigit(Transform parent, string name, Vector3 pos, Material on, float scale)
        {
            if (parent == null) return null;
            var root = parent.Find(name);
            if (root == null)
            {
                var go = new GameObject(name);
                root = go.transform;
                root.SetParent(parent, false);
            }
            root.position = pos;
            var w = 1.7f * scale;
            var h = 3.1f * scale;
            var t = 0.38f * scale;
            var dim = _ledOff != null ? _ledOff : on;
            Seg(root, "A", new Vector3(0f, h * 0.5f, 0f), new Vector3(w, t, t), dim);
            Seg(root, "B", new Vector3(w * 0.5f, h * 0.25f, 0f), new Vector3(t, h * 0.42f, t), dim);
            Seg(root, "C", new Vector3(w * 0.5f, -h * 0.25f, 0f), new Vector3(t, h * 0.42f, t), dim);
            Seg(root, "D", new Vector3(0f, -h * 0.5f, 0f), new Vector3(w, t, t), dim);
            Seg(root, "E", new Vector3(-w * 0.5f, -h * 0.25f, 0f), new Vector3(t, h * 0.42f, t), dim);
            Seg(root, "F", new Vector3(-w * 0.5f, h * 0.25f, 0f), new Vector3(t, h * 0.42f, t), dim);
            Seg(root, "G", new Vector3(0f, 0f, 0f), new Vector3(w * 0.92f, t, t), dim);
            return root;
        }

        static void Seg(Transform parent, string name, Vector3 local, Vector3 scale, Material mat)
        {
            if (parent.Find(name) != null) return;
            var go = Look.Prim(PrimitiveType.Cube, name, parent, local, scale, mat);
            go.transform.localPosition = local;
            go.transform.localScale = scale;
        }

        void PaintDigit(Transform root, int value)
        {
            if (root == null) return;
            value = Mathf.Clamp(value, 0, 9);
            var mask = DigitMask[value];
            var lit = _ledOn != null ? _ledOn : Look.Unlit(new Color(1f, 0.78f, 0.18f));
            var dim = _ledOff != null ? _ledOff : Look.Unlit(new Color(0.08f, 0.12f, 0.09f));
            PaintSeg(root, "A", (mask & 1) != 0, lit, dim);
            PaintSeg(root, "B", (mask & 2) != 0, lit, dim);
            PaintSeg(root, "C", (mask & 4) != 0, lit, dim);
            PaintSeg(root, "D", (mask & 8) != 0, lit, dim);
            PaintSeg(root, "E", (mask & 16) != 0, lit, dim);
            PaintSeg(root, "F", (mask & 32) != 0, lit, dim);
            PaintSeg(root, "G", (mask & 64) != 0, lit, dim);
        }

        static void PaintSeg(Transform root, string name, bool on, Material lit, Material dim)
        {
            var tf = root.Find(name);
            if (tf == null) return;
            var r = tf.GetComponent<Renderer>();
            if (r == null) r = tf.GetComponentInChildren<Renderer>(true);
            if (r != null) r.sharedMaterial = on ? lit : dim;
        }

        static float Hash01(int i)
        {
            var n = (uint)(i * 16777619);
            n ^= n >> 13;
            n *= 1274126177u;
            return (n & 0xFFFF) / 65535f;
        }

        bool DropFan(string mesh, Transform parent, string name, Vector3 pos, Material jersey, float scale = 1f)
        {
            var go = DropMesh(mesh, parent, name, pos, Quaternion.identity, Vector3.one * Mathf.Max(0.2f, scale), paint: false);
            if (go == null) return false;
            PaintFan(go, jersey);
            return true;
        }

        /// <summary>
        /// Harbor's dress meshes (the dugouts, the fans) come from the same kit FBX as the field kit's
        /// bags, plate and mound, and are painted from the same table, so they drop through the kit.
        /// </summary>
        Transform DropMesh(string meshName, Transform parent, string instanceName, Vector3 pos, Quaternion rot, Vector3 scale, bool paint) =>
            Field.DropMesh(meshName, parent, instanceName, pos, rot, scale, paint);

        void PaintFan(Transform fan, Material jersey)
        {
            var flesh = Field.Fill(HarborKitPaint.Fill.Flesh);
            var gold = Field.Fill(HarborKitPaint.Fill.Gold);
            foreach (var r in fan.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                if (mats == null || mats.Length == 0)
                {
                    r.sharedMaterial = jersey;
                    continue;
                }
                var next = new Material[mats.Length];
                for (var i = 0; i < mats.Length; i++)
                {
                    var n = mats[i] != null ? mats[i].name.ToLowerInvariant() : "";
                    if (n.Contains("flesh") || n.Contains("head")) next[i] = flesh;
                    else if (n.Contains("gold") || n.Contains("cap")) next[i] = gold;
                    else next[i] = jersey;
                }
                r.sharedMaterials = next;
            }
        }

        static void Cube(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            Box(parent, name, pos, scale, Quaternion.identity, mat);
        }

        static void Box(Transform parent, string name, Vector3 pos, Vector3 scale, Quaternion rot, Material mat)
        {
            if (parent == null) return;
            var tf = parent.Find(name);
            GameObject go;
            if (tf != null)
            {
                go = tf.gameObject;
                Look.Paint(go, mat);
            }
            else
                go = Look.Prim(PrimitiveType.Cube, name, parent, Vector3.zero, Vector3.one, mat);
            go.transform.SetPositionAndRotation(pos, rot);
            go.transform.localScale = scale;
        }

        static void Capsule(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            if (parent == null) return;
            var go = Look.Prim(PrimitiveType.Capsule, name, parent, Vector3.zero, Vector3.one, mat);
            go.transform.position = pos;
            go.transform.localScale = scale;
        }

        static void Sphere(Transform parent, string name, Vector3 pos, float radius, Material mat)
        {
            if (parent == null) return;
            var go = Look.Prim(PrimitiveType.Sphere, name, parent, Vector3.zero, Vector3.one, mat);
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * (radius * 2f);
        }

        static void Cylinder(Transform parent, string name, Vector3 pos, float radius, float height, Material mat)
        {
            if (parent == null) return;
            var go = Look.Prim(PrimitiveType.Cylinder, name, parent, Vector3.zero, Vector3.one, mat);
            go.transform.position = pos + new Vector3(0, height * 0.5f, 0);
            go.transform.localScale = new Vector3(radius * 2, height * 0.5f, radius * 2);
        }

        static void Glow(Transform parent, string name, Vector3 pos, Color color, float intensity, float range)
        {
            if (parent == null) return;
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
        }
    }
}

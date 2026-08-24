using GrandSluggers.Sim;
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    public sealed class HeroActor : MonoBehaviour
    {
        public enum Pose
        {
            Idle, Walk, Run,
            ChargePitch, ThrowPitch, Throw,
            ChargeSwing, Swing, CheckSwing, Bunt,
            Catch, Dive, Jump, StealLead, Slide, Cheer, Miss,
            Field, Spin, Charm, Clamber, Crouch, Scoop
        }

        Transform _root, _torso, _head, _cap, _lArm, _rArm, _lFore, _rFore, _bat, _glove, _lThigh, _rThigh, _lShin, _rShin, _ring;
        Pose _pose = Pose.Idle;
        float _charge;
        float _chargeRing;
        string _pitchType = "fastball";
        float _t;
        float _poseT;
        float _lift;
        bool _grow;
        bool _lit;
        bool _heldBat;
        bool _heldGlove;
        bool _batsLeft;
        bool _throwsLeft;
        bool _captain;
        string _id = "";
        string _body = "rio";
        string _batVisual = "";
        string _gloveVisual = "";
        Vector3 _look = Vector3.forward;
        Vector3 _baseScale = Vector3.one;
        Vector3 _torsoRest = new Vector3(0, 2.28f, 0);
        float _hunchDeg;
        SharedRig.BoneBind _bind;
        Vector3 _ground;
        bool _hasGround;
        float _speed;
        bool _snap;

        public string Id => _id;
        public Pose Current => _pose;
        public float PoseTime => _poseT;
        public Transform CatchHand => _glove != null ? _glove : (_throwsLeft ? _rFore : _lFore);
        public Transform ThrowHand => _throwsLeft ? _lFore : _rFore;

        public void Bind(Character who)
        {
            if (who.Id == _id && _root != null) return;
            _id = who.Id;
            if (_root != null) Destroy(_root.gameObject);
            Build(who);
        }

        public void SetPose(Pose pose, float charge = 0f, string pitchType = null)
        {
            if (pose != _pose) _poseT = 0f;
            _pose = pose;
            _charge = Mathf.Clamp01(charge);
            if (!string.IsNullOrEmpty(pitchType)) _pitchType = pitchType;
        }

        public void SetHeld(bool bat, bool glove)
        {
            _heldBat = bat;
            _heldGlove = glove;
        }

        public void SetGear(BatItem bat, GloveItem glove)
        {
            if (_root == null) return;
            var batVis = GearMesh.BatVisual(bat);
            var gloveVis = GearMesh.GloveVisual(glove);
            if (batVis != _batVisual) BuildBat(batVis);
            if (gloveVis != _gloveVisual) BuildGlove(gloveVis);
        }

        public void SetGrow(bool on) => _grow = on;

        public void SetHighlight(bool on) => _lit = on;

        public void SetChargeRing(float charge01) => _chargeRing = Mathf.Clamp01(charge01);

        /// <summary>Still-gate: apply the pose in one step. Live play keeps the slerp.</summary>
        public void SnapTick(float poseT)
        {
            _poseT = poseT;
            _t = poseT;
            _snap = true;
            Tick(0f);
            _snap = false;
        }

        public void Place(Vector3 pos, Vector3 look)
        {
            var ground = new Vector3(pos.x, 0f, pos.z);
            if (_hasGround && Time.deltaTime > 1e-5f)
            {
                var inst = Vector3.Distance(ground, _ground) / Time.deltaTime;
                _speed = Mathf.Lerp(_speed, inst, 0.4f);
            }
            _hasGround = true;
            _ground = ground;

            float lift;
            if (_pose == Pose.Jump || _pose == Pose.Clamber)
                lift = (float)MoveBones.JumpLift(_poseT);
            else if (_pose == Pose.Dive) lift = 0.2f;
            else if (_pose == Pose.Slide) lift = 0.15f;
            else if (_pose == Pose.Crouch || _pose == Pose.StealLead || _pose == Pose.Bunt) lift = -0.35f;
            else if (_pose == Pose.Cheer) lift = 0.25f;
            else if (TryAuthoredLift(out var authoredLift)) lift = authoredLift;
            else if (_pose == Pose.Run) lift = (float)MoveBones.Evaluate(MoveBones.Verb.Run, _t, _poseT).Lift;
            else lift = 0f;
            _lift = _pose is Pose.Jump or Pose.Clamber ? lift : Mathf.Lerp(_lift, lift, 0.28f);
            transform.position = pos + Vector3.up * _lift;
            _look = look.sqrMagnitude < 0.01f ? Vector3.forward : look.normalized;
            if (_pose != Pose.Spin)
            {
                var yaw = Quaternion.LookRotation(new Vector3(_look.x, 0f, _look.z));
                transform.rotation = Quaternion.Slerp(transform.rotation, yaw, 0.35f);
            }
        }

        public void Tick(float dt)
        {
            _t += dt;
            _poseT += dt;
            if (_root != null)
            {
                var g = _grow ? 1.45f : 1f;
                var bounce = 0f;
                if (_pose == Pose.Idle || _pose == Pose.Field)
                    bounce = 0.07f * Mathf.Abs(Mathf.Sin(_t * 5.4f));
                var squash = Vector3.one;
                if (_pose == Pose.Swing && _poseT >= 0.12f && _poseT < 0.32f)
                    squash = new Vector3(1.14f, 0.84f, 1.14f);
                else if (_pose == Pose.Dive)
                    squash = new Vector3(1.22f, 0.76f, 1.18f);
                else if (_pose == Pose.Jump || _pose == Pose.Clamber)
                    squash = new Vector3(0.86f, 1.18f, 0.86f);
                var want = Vector3.Scale(_baseScale * g, squash);
                _root.localScale = Vector3.Lerp(_root.localScale, want, 0.22f);
                _root.localPosition = new Vector3(0f, bounce, 0f);
            }
            if (_ring != null)
            {
                var on = SetTells.RingOn(_chargeRing);
                _ring.gameObject.SetActive(on);
                if (on)
                {
                    if (_ring.parent != transform)
                        _ring.SetParent(transform, false);
                    var s = (float)SetTells.RingScale(_chargeRing);
                    var pulse = s + 0.08f * Mathf.Sin(_t * 7f);
                    _ring.localPosition = new Vector3(0f, (float)SetTells.RingHeightFt, 0f);
                    _ring.localRotation = Quaternion.identity;
                    _ring.localScale = new Vector3(pulse, (float)SetTells.RingThickFt, pulse);
                }
            }
            Animate();
        }

        void Build(Character who)
        {
            _body = Silhouette.BodyType(who);
            _captain = who.Captain;
            _batsLeft = who.Bats == Hand.L;
            _throwsLeft = who.Throws == Hand.L;
            var extras = ArtBinder.Art != null
                ? ArtBinder.SkinOf(who).Extras
                : System.Array.Empty<string>();
            var chain = SharedRig.Spawn(transform, who, extras);
            _root = chain.Root;
            _baseScale = chain.BaseScale;
            _torso = chain.Torso;
            _head = chain.Head;
            _cap = chain.Cap;
            _lArm = chain.LUpper;
            _lFore = chain.LFore;
            _rArm = chain.RUpper;
            _rFore = chain.RFore;
            _lThigh = chain.LThigh;
            _lShin = chain.LShin;
            _rThigh = chain.RThigh;
            _rShin = chain.RShin;
            _ring = chain.Ring;
            _torsoRest = chain.TorsoRest;
            _hunchDeg = chain.HunchDeg;
            _bind = chain.Bind;
            BuildBat("bat-wood");
            BuildGlove("glove-brown");
            if (_bat != null) _bat.gameObject.SetActive(false);
        }

        void BuildBat(string visual)
        {
            _batVisual = visual ?? "bat-wood";
            if (_bat != null) Destroy(_bat.gameObject);
            var hand = _batsLeft ? _lFore : _rFore;
            if (hand == null) return;
            var go = new GameObject("Bat");
            go.transform.SetParent(hand, false);
            go.transform.localPosition = new Vector3(0, -1.4f, 0.1f);
            go.transform.localRotation = Quaternion.Euler(0, 0, 20);
            go.transform.localScale = Vector3.one * Silhouette.BatScale;
            FillBat(go.transform, _batVisual);
            _bat = go.transform;
        }

        void FillBat(Transform root, string visual)
        {
            switch (visual)
            {
                case "bat-spark":
                {
                    var wood = Look.Lit(new Color(0.78f, 0.18f, 0.16f), smooth: 0.22f);
                    var gold = Look.Lit(Colors.Gold, smooth: 0.45f);
                    Look.Prim(PrimitiveType.Cylinder, "Handle", root, new Vector3(0, -0.55f, 0), new Vector3(0.16f, 0.7f, 0.16f), wood);
                    Look.Prim(PrimitiveType.Cylinder, "Barrel", root, new Vector3(0, 0.55f, 0), new Vector3(0.28f, 1.05f, 0.28f), wood);
                    Look.Prim(PrimitiveType.Cube, "Spark", root, new Vector3(0, 0.7f, 0.16f), new Vector3(0.08f, 0.7f, 0.08f), gold);
                    break;
                }
                case "bat-wand":
                {
                    var ice = Look.Lit(new Color(0.72f, 0.88f, 1f), smooth: 0.55f);
                    var pink = Look.Lit(new Color(0.95f, 0.55f, 0.78f), smooth: 0.4f);
                    Look.Prim(PrimitiveType.Cylinder, "Shaft", root, Vector3.zero, new Vector3(0.1f, 1.85f, 0.1f), ice);
                    Look.Prim(PrimitiveType.Sphere, "Tip", root, new Vector3(0, 1.7f, 0), Vector3.one * 0.28f, pink);
                    break;
                }
                case "bat-short":
                {
                    var green = Look.Lit(new Color(0.18f, 0.72f, 0.38f), smooth: 0.25f);
                    var rainbow = Look.Lit(new Color(1f, 0.35f, 0.62f), smooth: 0.4f);
                    Look.Prim(PrimitiveType.Cylinder, "Stick", root, Vector3.zero, new Vector3(0.26f, 1.05f, 0.26f), green);
                    Look.Prim(PrimitiveType.Cylinder, "Ring", root, new Vector3(0, 0.55f, 0), new Vector3(0.34f, 0.1f, 0.34f), rainbow);
                    break;
                }
                case "bat-brick":
                {
                    var gold = Look.Lit(new Color(0.9f, 0.72f, 0.16f), smooth: 0.18f);
                    var grip = Look.Lit(new Color(0.35f, 0.22f, 0.08f), smooth: 0.1f);
                    Look.Prim(PrimitiveType.Cylinder, "Handle", root, new Vector3(0, -0.7f, 0), new Vector3(0.2f, 0.55f, 0.2f), grip);
                    Look.Prim(PrimitiveType.Cube, "Brick", root, new Vector3(0, 0.45f, 0), new Vector3(0.7f, 1.35f, 0.42f), gold);
                    break;
                }
                case "bat-barrel":
                {
                    var wood = Look.Lit(new Color(0.5f, 0.3f, 0.12f), smooth: 0.12f);
                    var hoop = Look.Lit(new Color(0.72f, 0.62f, 0.28f), smooth: 0.3f);
                    Look.Prim(PrimitiveType.Cylinder, "Handle", root, new Vector3(0, -0.65f, 0), new Vector3(0.18f, 0.55f, 0.18f), wood);
                    Look.Prim(PrimitiveType.Cylinder, "Cask", root, new Vector3(0, 0.5f, 0), new Vector3(0.55f, 0.95f, 0.55f), wood);
                    Look.Prim(PrimitiveType.Cylinder, "HoopA", root, new Vector3(0, 0.15f, 0), new Vector3(0.6f, 0.07f, 0.6f), hoop);
                    Look.Prim(PrimitiveType.Cylinder, "HoopB", root, new Vector3(0, 0.85f, 0), new Vector3(0.6f, 0.07f, 0.6f), hoop);
                    break;
                }
                case "bat-furnace":
                {
                    var iron = Look.Lit(new Color(0.12f, 0.08f, 0.1f), smooth: 0.08f);
                    var fire = Look.Lit(Colors.EmberFire, smooth: 0.35f);
                    Look.Prim(PrimitiveType.Cylinder, "Handle", root, new Vector3(0, -0.55f, 0), new Vector3(0.2f, 0.65f, 0.2f), iron);
                    Look.Prim(PrimitiveType.Cylinder, "Club", root, new Vector3(0, 0.55f, 0), new Vector3(0.38f, 1.05f, 0.38f), iron);
                    for (var i = 0; i < 4; i++)
                    {
                        var a = i * 90f * Mathf.Deg2Rad;
                        Look.Prim(PrimitiveType.Cube, "Spike" + i, root,
                            new Vector3(Mathf.Cos(a) * 0.28f, 0.7f, Mathf.Sin(a) * 0.28f),
                            new Vector3(0.14f, 0.45f, 0.14f), fire);
                    }
                    break;
                }
                case "bat-gold":
                {
                    var gold = Look.Lit(Colors.Gold, smooth: 0.5f);
                    Look.Prim(PrimitiveType.Cylinder, "Bat", root, Vector3.zero, new Vector3(0.22f, 1.7f, 0.22f), gold);
                    break;
                }
                default:
                {
                    var wood = Look.Lit(new Color(0.45f, 0.28f, 0.12f), smooth: 0.15f);
                    Look.Prim(PrimitiveType.Cylinder, "Bat", root, Vector3.zero, new Vector3(0.22f, 1.7f, 0.22f), wood);
                    break;
                }
            }
        }

        void BuildGlove(string visual)
        {
            _gloveVisual = visual ?? "glove-brown";
            if (_glove != null) Destroy(_glove.gameObject);
            var hand = _throwsLeft ? _rFore : _lFore;
            if (hand == null) return;
            var go = new GameObject("Glove");
            go.transform.SetParent(hand, false);
            go.transform.localPosition = new Vector3(0, -0.72f, 0.12f);
            go.transform.localRotation = Quaternion.Euler(20, 0, 0);
            var leather = _gloveVisual == "glove-gold"
                ? Look.Lit(new Color(0.92f, 0.74f, 0.18f), smooth: 0.32f)
                : Look.Lit(new Color(0.42f, 0.24f, 0.12f), smooth: 0.12f);
            var scale = Silhouette.GloveScale * (_gloveVisual == "glove-gold" ? 1.12f : 1f);
            Look.Prim(PrimitiveType.Sphere, "Palm", go.transform, Vector3.zero, Vector3.one * (0.7f * scale), leather);
            Look.Prim(PrimitiveType.Cube, "Web", go.transform, new Vector3(0, 0.05f, 0.28f), new Vector3(0.55f, 0.08f, 0.42f) * scale, leather);
            Look.Prim(PrimitiveType.Capsule, "Thumb", go.transform, new Vector3(-0.32f, 0.05f, 0.1f), new Vector3(0.22f, 0.32f, 0.22f) * scale, leather);
            Look.Prim(PrimitiveType.Capsule, "Fingers", go.transform, new Vector3(0.12f, 0.22f, 0.08f), new Vector3(0.42f, 0.28f, 0.22f) * scale, leather);
            _glove = go.transform;
        }

        void Animate()
        {
            var pose = Locomotion(_pose);
            var bob = 0.04f * Mathf.Sin(_t * 2.4f);
            if (pose == Pose.Cheer) bob = Mathf.Abs(Mathf.Sin(_t * 6f)) * 0.12f;
            if (_torso != null) _torso.localPosition = _torsoRest + new Vector3(0, bob, 0);

            var batOn = _heldBat;
            var gloveOn = _heldGlove;
            if (ToVerb(pose) is MoveBones.Verb verb)
            {
                batOn = pose is Pose.ChargeSwing or Pose.Swing or Pose.CheckSwing or Pose.Bunt or Pose.Miss;
                gloveOn = pose is Pose.ChargePitch or Pose.ThrowPitch or Pose.Throw
                    or Pose.Jump or Pose.Clamber or Pose.Scoop;
                if (pose is Pose.ChargeSwing or Pose.Swing or Pose.Slide) gloveOn = false;
                MoveBones.Sample sample;
                var clipId = ClipId(verb);
                var authoredPose = TryAuthored(clipId, out var authored);
                if (authoredPose)
                    sample = authored;
                else
                    sample = MoveBones.Evaluate(verb, _t, _poseT, _charge, _pitchType);
                if ((pose is Pose.ChargeSwing or Pose.Swing) && _batsLeft)
                    sample = MoveBones.MirrorArms(sample);
                if ((pose is Pose.ChargePitch or Pose.ThrowPitch or Pose.Throw) && _throwsLeft)
                    sample = MoveBones.MirrorArms(sample);
                var clipT = _poseT;
                if (!string.IsNullOrEmpty(clipId) && ArtBinder.Art != null
                    && ArtBinder.Art.TryClip(clipId, out var slot) && slot.Loop)
                    clipT = _t;
                // Authored eulers are offsets on the bind pose (Q(e)*bind).
                // SampleAnimation replaces bind and laid the scoop mesh on its side.
                var playedDrop = !authoredPose && TrySampleDrop(clipId, clipT);
                if (!playedDrop)
                {
                    var boneSnap = pose is Pose.Swing or Pose.ThrowPitch or Pose.Throw or Pose.Jump or Pose.Scoop or Pose.Slide;
                    Apply(sample,
                        _snap ? 1f : boneSnap ? 0.55f : 0.32f,
                        _snap ? 1f : boneSnap ? 0.48f : 0.34f);
                }
                else if ((pose is Pose.Swing && _batsLeft) || ((pose is Pose.ThrowPitch or Pose.Throw) && _throwsLeft))
                    MirrorBoundArms();
                if (_bat != null)
                {
                    _bat.gameObject.SetActive(batOn);
                    if (batOn) _bat.localRotation = Q(sample.Bat);
                }
                if (_glove != null) _glove.gameObject.SetActive(gloveOn && !batOn);
                return;
            }

            var lArm = Quaternion.Euler(12, 0, 18);
            var rArm = Quaternion.Euler(12, 0, -18);
            var lLeg = Quaternion.identity;
            var rLeg = Quaternion.identity;
            var batRot = Quaternion.Euler(0, 0, 20);
            var torsoRot = Quaternion.identity;
            var headRot = Quaternion.identity;

            switch (pose)
            {
                case Pose.Walk:
                {
                    var s = Mathf.Sin(_t * 8f);
                    lArm = Quaternion.Euler(28f * s, 0, 16);
                    rArm = Quaternion.Euler(-28f * s, 0, -16);
                    lLeg = Quaternion.Euler(22f * s, 0, 0);
                    rLeg = Quaternion.Euler(-22f * s, 0, 0);
                    break;
                }
                case Pose.Run:
                {
                    var stride = Mathf.Repeat(_t * 2.55f, 1f);
                    float k;
                    if (stride < 0.25f)
                    {
                        k = stride / 0.25f;
                        lLeg = Quaternion.Euler(LerpK(8, 58, k), 0, 0);
                        rLeg = Quaternion.Euler(LerpK(-8, -42, k), 0, 0);
                        lArm = Quaternion.Euler(LerpK(-20, -62, k), 6, 8);
                        rArm = Quaternion.Euler(LerpK(20, 58, k), -6, -8);
                        torsoRot = Quaternion.Euler(14, LerpK(0, 8, k), 0);
                    }
                    else if (stride < 0.5f)
                    {
                        k = (stride - 0.25f) / 0.25f;
                        lLeg = Quaternion.Euler(LerpK(58, -8, k), 0, 0);
                        rLeg = Quaternion.Euler(LerpK(-42, 8, k), 0, 0);
                        lArm = Quaternion.Euler(LerpK(-62, 20, k), 6, 8);
                        rArm = Quaternion.Euler(LerpK(58, -20, k), -6, -8);
                        torsoRot = Quaternion.Euler(16, LerpK(8, 0, k), 0);
                    }
                    else if (stride < 0.75f)
                    {
                        k = (stride - 0.5f) / 0.25f;
                        lLeg = Quaternion.Euler(LerpK(-8, -42, k), 0, 0);
                        rLeg = Quaternion.Euler(LerpK(8, 58, k), 0, 0);
                        lArm = Quaternion.Euler(LerpK(20, 58, k), 6, 8);
                        rArm = Quaternion.Euler(LerpK(-20, -62, k), -6, -8);
                        torsoRot = Quaternion.Euler(14, LerpK(0, -8, k), 0);
                    }
                    else
                    {
                        k = (stride - 0.75f) / 0.25f;
                        lLeg = Quaternion.Euler(LerpK(-42, 8, k), 0, 0);
                        rLeg = Quaternion.Euler(LerpK(58, -8, k), 0, 0);
                        lArm = Quaternion.Euler(LerpK(58, -20, k), 6, 8);
                        rArm = Quaternion.Euler(LerpK(-62, 20, k), -6, -8);
                        torsoRot = Quaternion.Euler(16, LerpK(-8, 0, k), 0);
                    }
                    break;
                }
                case Pose.ChargePitch:
                    lArm = Quaternion.Euler(12, 0, 28);
                    rArm = PitchSlot(-35 - 70 * _charge, 18, -38);
                    lLeg = Quaternion.Euler(8 + 25 * _charge, 0, 0);
                    rLeg = Quaternion.Euler(-10 * _charge, 0, 0);
                    torsoRot = Quaternion.Euler(-8 - 18 * _charge, 12, 0);
                    gloveOn = true;
                    break;
                case Pose.ThrowPitch:
                {
                    gloveOn = true;
                    var wind = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_poseT / 0.10f));
                    var stride = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((_poseT - 0.10f) / 0.12f));
                    var rel = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((_poseT - 0.22f) / 0.12f));
                    var fol = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((_poseT - 0.34f) / 0.16f));
                    lArm = Quaternion.Slerp(
                        Quaternion.Euler(12, 0, 28),
                        Quaternion.Euler(40, 0, 22),
                        Mathf.Clamp01(wind * 0.35f + stride * 0.35f + rel * 0.3f));
                    var back = PitchSlot(-105, 18, -38);
                    var slot = PitchSlot(10, 5, -20);
                    var outArm = PitchSlot(78, -10, -10);
                    var wrap = PitchSlot(105, -22, 12);
                    if (_poseT < 0.10f) rArm = Quaternion.Slerp(back, back, wind);
                    else if (_poseT < 0.22f) rArm = Quaternion.Slerp(back, slot, stride);
                    else if (_poseT < 0.34f) rArm = Quaternion.Slerp(slot, outArm, rel);
                    else rArm = Quaternion.Slerp(outArm, wrap, fol);
                    lLeg = Quaternion.Euler(18 + 22 * stride, 0, 0);
                    rLeg = Quaternion.Euler(-8 + 28 * rel, 0, 0);
                    torsoRot = Quaternion.Euler(-18 + 36 * rel, 12 - 30 * rel, 0);
                    break;
                }
                case Pose.Throw:
                {
                    gloveOn = true;
                    var k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_poseT / 0.22f));
                    var f = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((_poseT - 0.22f) / 0.18f));
                    lArm = Quaternion.Euler(32 - 8 * k, 0, 22);
                    var dirt = Quaternion.Euler(55, 8, -12);
                    var whip = Quaternion.Euler(70, -8, -18);
                    var follow = Quaternion.Euler(110, -18, 8);
                    rArm = Quaternion.Slerp(dirt, Quaternion.Slerp(whip, follow, f), k);
                    torsoRot = Quaternion.Euler(18 - 6 * k, -18 * k, 0);
                    lLeg = Quaternion.Euler(28 - 12 * k, 0, 0);
                    rLeg = Quaternion.Euler(12, 0, 0);
                    break;
                }
                case Pose.ChargeSwing:
                    batOn = true;
                    gloveOn = false;
                    lArm = Quaternion.Euler(-10, 20, 30);
                    rArm = Quaternion.Euler(-35 - 50 * _charge, -40, -55);
                    batRot = Quaternion.Euler(75 + 28 * _charge, 0, 10);
                    break;
                case Pose.Swing:
                {
                    batOn = true;
                    gloveOn = false;
                    var loadL = Quaternion.Euler(-10, 20, 30);
                    var loadR = Quaternion.Euler(-85, -40, -55);
                    var loadBat = Quaternion.Euler(103, 0, 10);
                    var loadT = Quaternion.Euler(0, -8, 0);
                    var slotL = Quaternion.Euler(10, -10, 18);
                    var slotR = Quaternion.Euler(-10, 10, -20);
                    var slotBat = Quaternion.Euler(20, 40, 8);
                    var slotT = Quaternion.Euler(8, 20, -4);
                    var cutL = Quaternion.Euler(28, -48, 6);
                    var cutR = Quaternion.Euler(22, 72, 28);
                    var cutBat = Quaternion.Euler(-55, 110, 12);
                    var cutT = Quaternion.Euler(10, 55, -8);
                    var wrapL = Quaternion.Euler(40, -70, -10);
                    var wrapR = Quaternion.Euler(8, 95, 40);
                    var wrapBat = Quaternion.Euler(-70, 155, 20);
                    var wrapT = Quaternion.Euler(6, 78, -12);
                    if (_poseT < 0.12f)
                    {
                        var k = Mathf.SmoothStep(0f, 1f, _poseT / 0.12f);
                        lArm = Quaternion.Slerp(loadL, slotL, k);
                        rArm = Quaternion.Slerp(loadR, slotR, k);
                        batRot = Quaternion.Slerp(loadBat, slotBat, k);
                        torsoRot = Quaternion.Slerp(loadT, slotT, k);
                    }
                    else if (_poseT < 0.28f)
                    {
                        var k = Mathf.SmoothStep(0f, 1f, (_poseT - 0.12f) / 0.16f);
                        lArm = Quaternion.Slerp(slotL, cutL, k);
                        rArm = Quaternion.Slerp(slotR, cutR, k);
                        batRot = Quaternion.Slerp(slotBat, cutBat, k);
                        torsoRot = Quaternion.Slerp(slotT, cutT, k);
                    }
                    else
                    {
                        var k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((_poseT - 0.28f) / 0.22f));
                        lArm = Quaternion.Slerp(cutL, wrapL, k);
                        rArm = Quaternion.Slerp(cutR, wrapR, k);
                        batRot = Quaternion.Slerp(cutBat, wrapBat, k);
                        torsoRot = Quaternion.Slerp(cutT, wrapT, k);
                        headRot = Quaternion.Euler(8 * k, 18 * k, 0);
                    }
                    lLeg = Quaternion.Euler(12, 0, 0);
                    rLeg = Quaternion.Euler(-18, 20, 0);
                    break;
                }
                case Pose.CheckSwing:
                    batOn = true;
                    gloveOn = false;
                    lArm = Quaternion.Euler(-6, 12, 22);
                    rArm = Quaternion.Euler(-18, -22, -28);
                    batRot = Quaternion.Euler(42, 18, 8);
                    torsoRot = Quaternion.Euler(6, 12, 0);
                    break;
                case Pose.Bunt:
                    batOn = true;
                    gloveOn = false;
                    lArm = Quaternion.Euler(8, 35, 8);
                    rArm = Quaternion.Euler(8, -35, -8);
                    batRot = Quaternion.Euler(90, 0, 90);
                    torsoRot = Quaternion.Euler(12, 0, 0);
                    lLeg = Quaternion.Euler(28, 6, 8);
                    rLeg = Quaternion.Euler(18, -6, -8);
                    break;
                case Pose.Catch:
                {
                    var k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_poseT / 0.16f));
                    lArm = Quaternion.Slerp(Quaternion.Euler(25, 0, 25), Quaternion.Euler(-58, 0, 18), k);
                    rArm = Quaternion.Slerp(Quaternion.Euler(25, 0, -25), Quaternion.Euler(-58, 0, -18), k);
                    gloveOn = true;
                    break;
                }
                case Pose.Scoop:
                {
                    gloveOn = true;
                    var drop = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_poseT / 0.12f));
                    var pick = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((_poseT - 0.12f) / 0.16f));
                    var up = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((_poseT - 0.28f) / 0.16f));
                    lArm = Quaternion.Slerp(
                        Quaternion.Euler(12, 0, 18),
                        Quaternion.Slerp(Quaternion.Euler(62, 8, 10), Quaternion.Euler(28, 0, 16), up),
                        Mathf.Max(drop, pick));
                    rArm = Quaternion.Slerp(
                        Quaternion.Euler(12, 0, -18),
                        Quaternion.Slerp(Quaternion.Euler(70, -12, -8), Quaternion.Euler(22, 0, -16), up),
                        Mathf.Max(drop, pick));
                    torsoRot = Quaternion.Euler(LerpK(8, 42, drop) - 22 * up, 0, 0);
                    lLeg = Quaternion.Euler(38 + 18 * drop - 12 * up, 8, 10);
                    rLeg = Quaternion.Euler(28 + 22 * drop - 10 * up, -6, -8);
                    break;
                }
                case Pose.Field:
                    lArm = Quaternion.Euler(25, 0, 25);
                    rArm = Quaternion.Euler(25, 0, -25);
                    gloveOn = true;
                    break;
                case Pose.Dive:
                    lArm = Quaternion.Euler(-80, 0, 10);
                    rArm = Quaternion.Euler(-80, 0, -10);
                    torsoRot = Quaternion.Euler(70, 0, 0);
                    gloveOn = true;
                    break;
                case Pose.Jump:
                case Pose.Clamber:
                    lArm = Quaternion.Euler(-70, 0, 15);
                    rArm = Quaternion.Euler(-70, 0, -15);
                    lLeg = Quaternion.Euler(40, 8, 0);
                    rLeg = Quaternion.Euler(40, -8, 0);
                    gloveOn = true;
                    break;
                case Pose.Spin:
                    lArm = Quaternion.Euler(10, 0, 70);
                    rArm = Quaternion.Euler(10, 0, -70);
                    transform.rotation *= Quaternion.Euler(0, 720f * Time.deltaTime, 0);
                    gloveOn = true;
                    break;
                case Pose.Charm:
                    lArm = Quaternion.Euler(0, 0, 40);
                    rArm = Quaternion.Euler(0, 0, -40);
                    break;
                case Pose.Slide:
                {
                    gloveOn = false;
                    var tuck = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_poseT / 0.18f));
                    var pop = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((_poseT - 0.18f) / 0.22f));
                    lArm = Quaternion.Slerp(Quaternion.Euler(10, 8, 18), Quaternion.Euler(-28, 12, 22), tuck);
                    rArm = Quaternion.Slerp(Quaternion.Euler(10, -8, -18), Quaternion.Euler(-48, -18, -12), tuck);
                    lLeg = Quaternion.Slerp(Quaternion.Euler(20, 0, 0), Quaternion.Slerp(Quaternion.Euler(88, 10, 12), Quaternion.Euler(42, 6, 6), pop), tuck);
                    rLeg = Quaternion.Slerp(Quaternion.Euler(12, 0, 0), Quaternion.Slerp(Quaternion.Euler(102, -8, -10), Quaternion.Euler(28, -4, -6), pop), tuck);
                    torsoRot = Quaternion.Euler(LerpK(12, 62, tuck) - 28 * pop, 0, 0);
                    break;
                }
                case Pose.StealLead:
                    lArm = Quaternion.Euler(28, 8, 22);
                    rArm = Quaternion.Euler(12, -12, -28);
                    lLeg = Quaternion.Euler(42, 10, 10);
                    rLeg = Quaternion.Euler(18, -6, -8);
                    torsoRot = Quaternion.Euler(22, 12, 0);
                    gloveOn = false;
                    break;
                case Pose.Crouch:
                    lArm = Quaternion.Euler(40, 0, 18);
                    rArm = Quaternion.Euler(40, 0, -18);
                    lLeg = Quaternion.Euler(68, 8, 12);
                    rLeg = Quaternion.Euler(68, -8, -12);
                    torsoRot = Quaternion.Euler(32, 0, 0);
                    gloveOn = true;
                    break;
                case Pose.Cheer:
                    lArm = Quaternion.Euler(-110, 0, 18);
                    rArm = Quaternion.Euler(-110, 0, -18);
                    batOn = false;
                    gloveOn = false;
                    break;
                case Pose.Miss:
                    batOn = true;
                    gloveOn = false;
                    lArm = Quaternion.Euler(8, -12, 12);
                    rArm = Quaternion.Euler(22, 28, 18);
                    batRot = Quaternion.Euler(-12, 55, 8);
                    torsoRot = Quaternion.Euler(14, -8, 0);
                    headRot = Quaternion.Euler(22, -16, 0);
                    break;
            }

            var batting = pose is Pose.ChargeSwing or Pose.Swing or Pose.CheckSwing or Pose.Bunt or Pose.Miss;
            var pitching = pose is Pose.ChargePitch or Pose.ThrowPitch or Pose.Throw;
            if (batting && _batsLeft) MirrorArms(ref lArm, ref rArm);
            if (pitching && _throwsLeft) MirrorArms(ref lArm, ref rArm);

            var poseSnap = pose is Pose.Swing or Pose.ThrowPitch or Pose.Throw or Pose.Scoop or Pose.Slide;
            var kArm = _snap ? 1f : poseSnap ? 0.55f : 0.2f;
            var kLeg = _snap ? 1f : poseSnap ? 0.45f : 0.25f;
            if (_hunchDeg != 0f)
                torsoRot = Quaternion.Euler(_hunchDeg, 0, 0) * torsoRot;
            if (_torso != null)
                _torso.localRotation = Quaternion.Slerp(_torso.localRotation, torsoRot * _bind.Torso, kArm);
            if (_head != null)
                _head.localRotation = Quaternion.Slerp(_head.localRotation, headRot * _bind.Head, kArm);
            if (_lArm != null) _lArm.localRotation = Quaternion.Slerp(_lArm.localRotation, lArm * _bind.LUpper, kArm);
            if (_rArm != null) _rArm.localRotation = Quaternion.Slerp(_rArm.localRotation, rArm * _bind.RUpper, kArm);
            if (_lThigh != null) _lThigh.localRotation = Quaternion.Slerp(_lThigh.localRotation, lLeg * _bind.LThigh, kLeg);
            if (_rThigh != null) _rThigh.localRotation = Quaternion.Slerp(_rThigh.localRotation, rLeg * _bind.RThigh, kLeg);
            if (_lShin != null) _lShin.localRotation = Quaternion.Slerp(_lShin.localRotation, Quaternion.Euler(12, 0, 0) * _bind.LShin, kLeg);
            if (_rShin != null) _rShin.localRotation = Quaternion.Slerp(_rShin.localRotation, Quaternion.Euler(12, 0, 0) * _bind.RShin, kLeg);
            if (_bat != null)
            {
                _bat.gameObject.SetActive(batOn);
                _bat.localRotation = batRot;
            }
            if (_glove != null)
                _glove.gameObject.SetActive(gloveOn && !batOn);
        }

        static MoveBones.Verb? ToVerb(Pose pose) => pose switch
        {
            Pose.Walk => MoveBones.Verb.Walk,
            Pose.Run => MoveBones.Verb.Run,
            Pose.Jump or Pose.Clamber => MoveBones.Verb.Jump,
            Pose.ChargePitch => MoveBones.Verb.ChargePitch,
            Pose.ThrowPitch => MoveBones.Verb.Pitch,
            Pose.ChargeSwing => MoveBones.Verb.ChargeSwing,
            Pose.Swing => MoveBones.Verb.Swing,
            Pose.Throw => MoveBones.Verb.Throw,
            Pose.Scoop => MoveBones.Verb.Scoop,
            Pose.Slide => MoveBones.Verb.Slide,
            _ => null
        };

        bool TryAuthoredLift(out float lift)
        {
            lift = 0f;
            if (ToVerb(_pose) is not MoveBones.Verb verb) return false;
            if (!TryAuthored(ClipId(verb), out var sample)) return false;
            lift = (float)sample.Lift;
            return true;
        }

        bool TryAuthored(string clipId, out MoveBones.Sample sample)
        {
            sample = default;
            if (clipId == null || ArtBinder.Art == null) return false;
            var t = _t;
            if (ArtBinder.Art.TryClip(clipId, out var clip) && !clip.Loop)
                t = _poseT;
            return ArtBinder.Art.TryAuthored(clipId, t, out sample);
        }

        static string ClipId(MoveBones.Verb verb) => verb switch
        {
            MoveBones.Verb.Idle => "idle",
            MoveBones.Verb.Walk => "walk",
            MoveBones.Verb.Run => "run",
            MoveBones.Verb.Jump => "jump",
            MoveBones.Verb.Swing => "swing",
            MoveBones.Verb.Pitch => "pitch",
            MoveBones.Verb.Scoop => "scoop",
            MoveBones.Verb.Slide => "slide",
            MoveBones.Verb.Throw => "throw",
            _ => null
        };

        void Apply(MoveBones.Sample s, float kArm, float kLeg)
        {
            var torso = s.Torso;
            if (_hunchDeg != 0f)
                torso = new MoveBones.Euler(torso.X + _hunchDeg, torso.Y, torso.Z);
            // Scoop eulers were authored for identity rest. Q(e)*FBX-bind rolls the
            // mesh onto its back. Other verbs keep the imported bind.
            var scoop = _pose == Pose.Scoop;
            var id = Quaternion.identity;
            Ease(ref _torso, torso, kArm, scoop ? id : _bind.Torso);
            Ease(ref _head, s.Head, kArm, scoop ? id : _bind.Head);
            Ease(ref _lArm, s.LUpper, kArm, scoop ? id : _bind.LUpper);
            Ease(ref _lFore, s.LFore, kArm, scoop ? id : _bind.LFore);
            Ease(ref _rArm, s.RUpper, kArm, scoop ? id : _bind.RUpper);
            Ease(ref _rFore, s.RFore, kArm, scoop ? id : _bind.RFore);
            Ease(ref _lThigh, s.LThigh, kLeg, scoop ? id : _bind.LThigh);
            Ease(ref _lShin, s.LShin, kLeg, scoop ? id : _bind.LShin);
            Ease(ref _rThigh, s.RThigh, kLeg, scoop ? id : _bind.RThigh);
            Ease(ref _rShin, s.RShin, kLeg, scoop ? id : _bind.RShin);
        }

        static void Ease(ref Transform tf, MoveBones.Euler e, float k, Quaternion bind)
        {
            if (tf == null) return;
            tf.localRotation = Quaternion.Slerp(tf.localRotation, Q(e) * bind, k);
        }

        bool TrySampleDrop(string clipId, float t)
        {
            var clip = ArtBinder.LoadClip(clipId);
            if (clip == null || _root == null) return false;
            if (t < 0f) t = 0f;
            if (clip.length > 1e-4f) t = Mathf.Min(t, clip.length);
            var scale = _root.localScale;
            var pos = _root.localPosition;
            clip.SampleAnimation(_root.gameObject, t);
            var arm = _root.Find("hero-shared");
            if (arm != null) clip.SampleAnimation(arm.gameObject, t);
            _root.localScale = scale;
            _root.localPosition = pos;
            if (_torso != null) _torso.localPosition = _torsoRest;
            return true;
        }

        void MirrorBoundArms()
        {
            MirrorLocal(ref _lArm, ref _rArm);
            MirrorLocal(ref _lFore, ref _rFore);
        }

        static void MirrorLocal(ref Transform a, ref Transform b)
        {
            if (a == null || b == null) return;
            var ea = a.localRotation.eulerAngles;
            var eb = b.localRotation.eulerAngles;
            a.localRotation = Quaternion.Euler(eb.x, -eb.y, -eb.z);
            b.localRotation = Quaternion.Euler(ea.x, -ea.y, -ea.z);
        }

        static Quaternion Q(MoveBones.Euler e) =>
            Quaternion.Euler((float)e.X, (float)e.Y, (float)e.Z);

        Pose Locomotion(Pose pose)
        {
            if (pose is not (Pose.Idle or Pose.Field)) return pose;
            if (_speed > 14f) return Pose.Run;
            if (_speed > 3.5f) return Pose.Walk;
            return pose;
        }

        static float LerpK(float a, float b, float u) => a + (b - a) * u;

        static void MirrorArms(ref Quaternion lArm, ref Quaternion rArm)
        {
            var l = lArm.eulerAngles;
            var r = rArm.eulerAngles;
            lArm = Quaternion.Euler(r.x, -r.y, -r.z);
            rArm = Quaternion.Euler(l.x, -l.y, -l.z);
        }

        Quaternion PitchSlot(float x, float y, float z)
        {
            return _pitchType switch
            {
                "curve" => Quaternion.Euler(x - 25, y, z - 12),
                "slider" => Quaternion.Euler(x + 28, y + 18, z + 8),
                _ => Quaternion.Euler(x, y, z)
            };
        }
    }
}

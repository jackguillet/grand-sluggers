using GrandSluggers.Sim;
using Motion = GrandSluggers.Sim.Motion;
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    /// <summary>
    /// One body on the shared rig, driven by verbs. Every verb plays a Blender
    /// take at a time the sim computes; a left-handed batter or thrower plays
    /// the baked mirror. No pose lives in this file. Contract:
    /// docs/character-motion.md. Measurement helpers: HeroActor.Evidence.cs.
    /// </summary>
    public sealed partial class HeroActor : MonoBehaviour
    {
        const float FadeSeconds = 0.10f;

        SharedRig.Chain _chain;
        Transform _root, _body, _torso, _head, _lArm, _rArm, _lFore, _rFore, _batSocket, _bat, _batModel, _glove, _lThigh, _rThigh, _lShin, _rShin, _ring;
        ClipPlayer _player;
        Motion.Verb _verb = Motion.Verb.Idle;
        float _charge;
        float _chargeRing;
        string _pitchType = "fastball";
        float _t;
        float _poseT;
        bool _grow;
        bool _lit;
        bool _hint;
        bool _you;
        bool _heldBat;
        bool _heldGlove;
        bool _batsLeft;
        bool _throwsLeft;
        bool _snap;
        bool _missingClipReported;
        string _id = "";
        string _batVisual = "";
        string _gloveVisual = "";
        Vector3 _look = Vector3.forward;
        Vector3 _baseScale = Vector3.one;
        Vector3 _ground;
        bool _hasGround;
        float _speed;

        public string Id => _id;
        public Motion.Verb Current => _verb;
        public float PoseTime => _poseT;
        public Transform CatchHand => _glove != null ? _glove : (_throwsLeft ? _rFore : _lFore);
        public Transform ThrowHand => _throwsLeft ? _lFore : _rFore;

        public void Bind(Character who)
        {
            if (who.Id == _id && _root != null) return;
            _id = who.Id;
            Teardown();
            Build(who);
        }

        void OnDisable()
        {
            if (_ring != null) _ring.gameObject.SetActive(false);
        }

        void OnDestroy() => Teardown();

        void Teardown()
        {
            _player?.Dispose();
            _player = null;
            if (_ring != null) Destroy(_ring.gameObject);
            _ring = null;
            if (_body != null) Destroy(_body.gameObject);
            _body = _root = null;
        }

        public void SetPose(Motion.Verb verb, float charge = 0f, string pitchType = null)
        {
            if (verb != _verb) _poseT = 0f;
            _verb = verb;
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
            var batVis = GearMesh.HittingBatVisual();
            var gloveVis = GearMesh.GloveVisual(glove);
            if (batVis != _batVisual) BuildBat(batVis);
            if (gloveVis != _gloveVisual) BuildGlove(gloveVis);
        }

        public void SetGrow(bool on) => _grow = on;

        public void SetHighlight(bool on) => _lit = on;

        public void SetHint(bool on) => _hint = on;

        public void SetYou(bool on) => _you = on;

        public void SetChargeRing(float charge01) => _chargeRing = Mathf.Clamp01(charge01);

        /// <summary>Still-gate: cut to the verb at this time. Live play crossfades.</summary>
        public void SnapTick(float poseT)
        {
            _poseT = poseT;
            _t = poseT;
            _snap = true;
            Tick(0f);
            _snap = false;
        }

        /// <summary>Sample a timed verb on the same clock as its ball event.</summary>
        public void SampleMotion(float poseTime)
        {
            _poseT = Mathf.Max(0, poseTime);
            Tick(0f);
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
            transform.position = pos;
            _look = look.sqrMagnitude < 0.01f ? Vector3.forward : look.normalized;
            if (_verb != Motion.Verb.Spin)
            {
                var yaw = Quaternion.LookRotation(new Vector3(_look.x, 0f, _look.z));
                transform.rotation = Quaternion.Slerp(transform.rotation, yaw, 0.35f);
            }
        }

        /// <summary>Still-gate: stand on the dirt looking at a world point.</summary>
        public void PlaceStill(Vector3 pos, Vector3 lookAt)
        {
            transform.position = pos;
            var d = lookAt - pos;
            d.y = 0f;
            if (d.sqrMagnitude < 0.01f) d = Vector3.back;
            transform.rotation = Quaternion.LookRotation(d);
            _look = d.normalized;
            _ground = new Vector3(pos.x, 0f, pos.z);
            _hasGround = true;
        }

        public void Tick(float dt)
        {
            _t += dt;
            _poseT += dt;
            if (_body != null)
            {
                var g = (_grow ? 1.45f : 1f) * (_lit ? 1.18f : _hint ? 1.12f : 1f);
                var squash = Vector3.one;
                if (_verb == Motion.Verb.Swing)
                {
                    var s = SwingPresentation.RootSquash(_poseT);
                    squash = new Vector3((float)s.X, (float)s.Y, (float)s.Z);
                }
                var want = Vector3.Scale(_baseScale * g, squash);
                _body.localScale = _snap ? want : Vector3.Lerp(_body.localScale, want, 0.22f);
            }
            PlaceRing();
            Animate(dt);
        }

        void PlaceRing()
        {
            if (_ring == null) return;
            var on = SetTells.YouRingOn(_you, _chargeRing);
            _ring.gameObject.SetActive(on);
            if (!on) return;
            if (_ring.parent != null)
                _ring.SetParent(null, true);
            var s = (float)SetTells.LiveRingScale(_you, _chargeRing);
            var pulse = s + 0.08f * Mathf.Sin(_t * 7f);
            var feetX = _hasGround ? _ground.x : transform.position.x;
            var feetZ = _hasGround ? _ground.z : transform.position.z;
            var at = SetTells.RingAt(feetX, feetZ, transform.position.y, 0f);
            _ring.SetPositionAndRotation(
                new Vector3((float)at.X, (float)at.Y, (float)at.Z),
                Quaternion.identity);
            _ring.localScale = Vector3.one * pulse;
        }

        void Build(Character who)
        {
            _batsLeft = who.Bats == Hand.L;
            _throwsLeft = who.Throws == Hand.L;
            var skin = ArtBinder.Art != null ? ArtBinder.SkinOf(who) : default;
            _chain = SharedRig.Spawn(transform, who, skin);
            _body = _chain.Body;
            _root = _chain.Root;
            _baseScale = _chain.BaseScale;
            _torso = _chain.Torso;
            _head = _chain.Head;
            _lArm = _chain.LUpper;
            _lFore = _chain.LFore;
            _rArm = _chain.RUpper;
            _rFore = _chain.RFore;
            _batSocket = _chain.Bat;
            _lThigh = _chain.LThigh;
            _lShin = _chain.LShin;
            _rThigh = _chain.RThigh;
            _rShin = _chain.RShin;
            _ring = _chain.Ring;
            _player = _chain.Animator != null ? new ClipPlayer(_chain.Animator) : null;
            BuildBat(GearMesh.HittingBatVisual());
            BuildGlove("glove-brown");
            if (_bat != null) _bat.gameObject.SetActive(false);
        }

        void BuildBat(string visual)
        {
            _batVisual = visual ?? GearMesh.HittingBatVisual();
            if (_bat != null) Destroy(_bat.gameObject);
            _batModel = null;
            if (_batSocket == null) return;
            var go = new GameObject("Bat");
            go.transform.SetParent(_batSocket, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            var model = new GameObject("Model").transform;
            model.SetParent(go.transform, false);
            // The DCC socket owns the swing. This one bind conversion maps the
            // bat-wood +Y mesh axis into that socket and rotates its authored
            // grip offset by the same quaternion so the handle stays at origin.
            var axis = new Vector3(
                (float)SwingPresentation.ModelBarrelAxisAtSocket.X,
                (float)SwingPresentation.ModelBarrelAxisAtSocket.Y,
                (float)SwingPresentation.ModelBarrelAxisAtSocket.Z);
            model.localRotation = Quaternion.FromToRotation(Vector3.up, axis);
            var modelGrip = new Vector3(
                (float)SwingPresentation.ModelGrip.X,
                (float)SwingPresentation.ModelGrip.Y,
                (float)SwingPresentation.ModelGrip.Z);
            model.localScale = Vector3.one * Silhouette.BatScale;
            model.localPosition = -(model.localRotation * (modelGrip * Silhouette.BatScale));
            if (!TryDropToy(_batVisual, model) && !TryDropToy(GearMesh.HittingBatVisual(), model))
            {
                var wood = Look.Lit(new Color(0.45f, 0.28f, 0.12f), smooth: 0.15f);
                Look.Prim(PrimitiveType.Cylinder, "Bat", model, Vector3.zero, new Vector3(0.22f, 1.7f, 0.22f), wood);
            }
            _batModel = model;
            _bat = go.transform;
        }

        static bool TryDropToy(string id, Transform parent)
        {
            if (parent == null || string.IsNullOrWhiteSpace(id)) return false;
            var src = ArtBinder.LoadExtraMesh(id);
            if (src == null) return false;
            var go = Instantiate(src, parent);
            go.name = id;
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            foreach (var anim in go.GetComponentsInChildren<Animator>(true))
                anim.enabled = false;
            return go.GetComponentInChildren<MeshRenderer>(true) != null;
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
            var gold = _gloveVisual == "glove-gold";
            go.transform.localScale = Vector3.one * ToyMesh.GloveRootScale(gold);
            if (!TryDropToy(_gloveVisual, go.transform) && !TryDropToy("glove-brown", go.transform))
            {
                var leather = gold
                    ? Look.Lit(new Color(0.92f, 0.74f, 0.18f), smooth: 0.32f)
                    : Look.Lit(new Color(0.42f, 0.24f, 0.12f), smooth: 0.12f);
                Look.Prim(PrimitiveType.Sphere, "Palm", go.transform, Vector3.zero, Vector3.one * 0.7f, leather);
            }
            _glove = go.transform;
        }

        /// <summary>Which take, at what time. Nothing here rotates a bone.</summary>
        void Animate(float dt)
        {
            var verb = Locomotion(_verb);
            var cue = Motion.CueFor(verb);
            var hand = Motion.UsesBattingHand(verb)
                ? (_batsLeft ? Hand.L : Hand.R)
                : (_throwsLeft ? Hand.L : Hand.R);
            var file = Motion.ClipFile(cue.Clip, hand);
            var time = cue.Clock switch
            {
                Motion.Clock.World => (double)_t,
                Motion.Clock.Charge => Motion.LoadAtFor(verb, _charge),
                _ => verb switch
                {
                    Motion.Verb.Swing => AtBatMotion.SwingClipTime(_poseT, _charge),
                    Motion.Verb.ThrowPitch => AtBatMotion.PitchClipTime(_poseT, _charge),
                    _ => (double)_poseT
                }
            };
            if (_player != null)
            {
                var clip = ArtBinder.LoadClip(file);
                if (clip == null)
                {
                    if (!_missingClipReported)
                    {
                        Debug.LogError("take " + file + " is missing; " + _id + " holds idle. Bake it with tools/blender/hero_shared_takes.py");
                        _missingClipReported = true;
                    }
                    clip = ArtBinder.LoadClip("idle");
                    time = _t;
                }
                if (clip != null)
                {
                    var length = Mathf.Max(clip.length, 1e-4f);
                    var sample = cue.Clock == Motion.Clock.World && Motion.TryClip(cue.Clip, out var slot) && slot.Loop
                        ? (float)(time % length)
                        : Mathf.Clamp((float)time, 0f, length);
                    _player.Play(clip, sample, _snap ? 0f : FadeSeconds);
                    _player.Evaluate(_snap ? 0f : dt);
                }
            }
            if (verb == Motion.Verb.Spin && dt > 0f)
                transform.rotation *= Quaternion.Euler(0, 720f * dt, 0);

            var batting = Motion.UsesBattingHand(verb);
            var gloveOn = verb switch
            {
                Motion.Verb.ChargePitch or Motion.Verb.ThrowPitch or Motion.Verb.Throw
                    or Motion.Verb.Jump or Motion.Verb.Clamber or Motion.Verb.Scoop
                    or Motion.Verb.Catch or Motion.Verb.Field or Motion.Verb.Dive
                    or Motion.Verb.Crouch or Motion.Verb.Spin => true,
                Motion.Verb.Slide or Motion.Verb.StealLead or Motion.Verb.Cheer => false,
                _ => _heldGlove && !batting
            };
            if (_bat != null) _bat.gameObject.SetActive(batting);
            if (_glove != null) _glove.gameObject.SetActive(gloveOn && !batting);
        }

        Motion.Verb Locomotion(Motion.Verb verb)
        {
            if (verb is not (Motion.Verb.Idle or Motion.Verb.Field or Motion.Verb.Walk or Motion.Verb.Run))
                return verb;
            if (_speed > CartoonJuice.RunFtPerSec) return Motion.Verb.Run;
            if (_speed > CartoonJuice.WalkFtPerSec) return Motion.Verb.Walk;
            if (verb is Motion.Verb.Walk or Motion.Verb.Run)
                return _heldGlove ? Motion.Verb.Field : Motion.Verb.Idle;
            return verb;
        }
    }
}

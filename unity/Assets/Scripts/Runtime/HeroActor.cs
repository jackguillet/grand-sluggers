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
        Vector3 _ground;
        bool _hasGround;
        float _speed;

        public string Id => _id;
        public Transform CatchHand => _glove != null ? _glove : (_throwsLeft ? _rFore : _lFore);

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
                _root.localScale = Vector3.Lerp(_root.localScale, _baseScale * g, 0.15f);
            }
            if (_ring != null)
            {
                _ring.gameObject.SetActive(_lit);
                if (_lit)
                {
                    var pulse = 2.2f + 0.18f * Mathf.Sin(_t * 8f);
                    _ring.localScale = new Vector3(pulse, 0.08f, pulse);
                }
            }
            Animate();
        }

        void Build(Character who)
        {
            var body = Colors.Body(who.Faction);
            var accent = Colors.Accent(who.Faction);
            var skin = Colors.SkinTone(who.Faction);
            var pants = Color.Lerp(Color.white, body, 0.08f);
            var shoe = Color.Lerp(body, Color.black, 0.35f);
            _body = Silhouette.BodyType(who);
            var spec = Silhouette.Proportions(_body);
            _captain = who.Captain;
            _batsLeft = who.Bats == Hand.L;
            _throwsLeft = who.Throws == Hand.L;

            _root = new GameObject("Rig").transform;
            _root.SetParent(transform, false);
            _baseScale = new Vector3(spec.Width, spec.Height, spec.Width) * 1.15f;
            _root.localScale = _baseScale;

            var jersey = Look.Toon(body);
            var trim = Look.Toon(accent);
            var flesh = Look.Toon(skin);
            var slack = Look.Lit(pants, smooth: 0.2f);
            var leather = Look.Lit(shoe, smooth: 0.12f);

            var hipScale = _body == "brondo" ? new Vector3(1.45f, 0.55f, 1.05f)
                : _body == "vale" ? new Vector3(0.85f, 0.75f, 0.62f)
                : _body == "zig" ? new Vector3(1.35f, 0.5f, 0.95f)
                : _body == "konga" ? new Vector3(1.4f, 0.65f, 1.05f)
                : new Vector3(1.15f, 0.7f, 0.85f);
            Look.Prim(PrimitiveType.Capsule, "Hip", _root, new Vector3(0, 1.15f, 0), hipScale, slack);

            var torsoKind = _body == "brondo" ? PrimitiveType.Cube : PrimitiveType.Capsule;
            var torsoScale = _body switch
            {
                "vale" => new Vector3(1.05f, 1.22f, 0.62f) * spec.Torso,
                "zig" => new Vector3(1.25f, 0.72f, 0.95f) * spec.Torso,
                "brondo" => new Vector3(1.55f, 1.12f, 1.12f),
                "konga" => new Vector3(1.55f, 1.08f, 1.05f) * spec.Torso,
                "ashlord" => new Vector3(1.5f, 1.18f, 0.92f) * spec.Torso,
                _ => new Vector3(1.28f, 0.95f, 0.82f) * spec.Torso
            };
            _torso = Look.Prim(torsoKind, "Torso", _root, new Vector3(0, 2.55f, 0), torsoScale, jersey).transform;
            Look.Prim(PrimitiveType.Cube, "Stripe", _torso, new Vector3(0, 0.15f, 0.42f), new Vector3(0.18f, 0.7f, 0.08f), trim);

            if (_body == "vale")
                Look.Prim(PrimitiveType.Cylinder, "Neck", _root, new Vector3(0, 3.55f, 0), new Vector3(0.28f, 0.42f, 0.28f), flesh);
            if (_body == "konga")
                Look.Prim(PrimitiveType.Sphere, "Belly", _torso, new Vector3(0, -0.35f, 0.35f), new Vector3(1.05f, 0.7f, 0.85f), jersey);
            if (_captain && _body == "vale")
                Look.Prim(PrimitiveType.Cube, "Sash", _torso, new Vector3(0.15f, 0.05f, 0.48f), new Vector3(0.7f, 0.12f, 0.06f), Look.Lit(new Color(0.75f, 0.92f, 1f), smooth: 0.45f));
            if (_captain && _body == "ashlord")
                Look.Prim(PrimitiveType.Cube, "Cape", _torso, new Vector3(0, -0.15f, -0.62f), new Vector3(1.15f, 1.15f, 0.12f), trim);

            var headY = _body == "vale" ? 4.65f : _body == "zig" ? 3.85f : 4.35f;
            var headScale = Vector3.one * (1.55f * spec.Head);
            if (_body == "brondo") headScale = new Vector3(1.45f, 1.15f, 1.35f) * spec.Head;
            var headKind = _body == "brondo" ? PrimitiveType.Cube : PrimitiveType.Sphere;
            _head = Look.Prim(headKind, "Head", _root, new Vector3(0, headY, 0), headScale, flesh).transform;
            var ink = Look.Lit(new Color(0.08f, 0.07f, 0.07f), smooth: 0.05f);
            var eye = _body == "ashlord" && _captain
                ? Look.Unlit(Colors.EmberFire)
                : ink;
            var eyeSize = _body == "zig" ? 0.28f : _body == "vale" ? 0.16f : 0.22f;
            Look.Prim(PrimitiveType.Sphere, "EyeL", _head, new Vector3(-0.28f, 0.1f, 0.58f), Vector3.one * eyeSize, eye);
            Look.Prim(PrimitiveType.Sphere, "EyeR", _head, new Vector3(0.28f, 0.1f, 0.58f), Vector3.one * eyeSize, eye);
            var white = Look.Unlit(Color.white);
            Look.Prim(PrimitiveType.Sphere, "WhiteL", _head, new Vector3(-0.28f, 0.1f, 0.52f), Vector3.one * (eyeSize * 1.55f), white);
            Look.Prim(PrimitiveType.Sphere, "WhiteR", _head, new Vector3(0.28f, 0.1f, 0.52f), Vector3.one * (eyeSize * 1.55f), white);
            Look.Prim(PrimitiveType.Cube, "BrowL", _head, new Vector3(-0.28f, 0.28f, 0.52f), new Vector3(0.28f, 0.07f, 0.12f), ink);
            Look.Prim(PrimitiveType.Cube, "BrowR", _head, new Vector3(0.28f, 0.28f, 0.52f), new Vector3(0.28f, 0.07f, 0.12f), ink);
            Look.Prim(PrimitiveType.Cube, "Mouth", _head, new Vector3(0, -0.22f, 0.55f), new Vector3(0.32f, 0.08f, 0.1f), ink);

            if (_captain && _body == "rio")
            {
                Look.Prim(PrimitiveType.Sphere, "CheekL", _head, new Vector3(-0.42f, -0.18f, 0.38f), Vector3.one * 0.32f, flesh);
                Look.Prim(PrimitiveType.Sphere, "CheekR", _head, new Vector3(0.42f, -0.18f, 0.38f), Vector3.one * 0.32f, flesh);
            }
            if (_captain && _body == "vale")
            {
                var ice = Look.Lit(new Color(0.85f, 0.95f, 1f), smooth: 0.55f);
                Look.Prim(PrimitiveType.Cylinder, "Crown", _head, new Vector3(0, 0.62f, 0), new Vector3(0.7f, 0.14f, 0.7f), ice);
                Look.Prim(PrimitiveType.Cube, "Point", _head, new Vector3(0, 0.92f, 0), new Vector3(0.16f, 0.4f, 0.16f), ice);
                _cap = Look.Prim(PrimitiveType.Cylinder, "Cap", _head, new Vector3(0, 0.42f, 0), new Vector3(0.01f, 0.01f, 0.01f), trim).transform;
            }
            else
            {
                _cap = Look.Prim(PrimitiveType.Cylinder, "Cap", _head, new Vector3(0, 0.42f, 0), new Vector3(1.15f, 0.18f, 1.15f), trim).transform;
                var brim = _body == "rio" ? new Vector3(1.35f, 0.12f, 0.95f) : new Vector3(1.1f, 0.12f, 0.7f);
                Look.Prim(PrimitiveType.Cube, "Brim", _cap, new Vector3(0, -0.6f, 0.7f), brim, Look.Lit(Colors.Gold, smooth: 0.4f));
            }

            if (_captain && _body == "ashlord")
            {
                Look.Prim(PrimitiveType.Cube, "HornL", _head, new Vector3(-0.48f, 0.62f, -0.05f), new Vector3(0.2f, 0.95f, 0.2f), trim);
                Look.Prim(PrimitiveType.Cube, "HornR", _head, new Vector3(0.48f, 0.62f, -0.05f), new Vector3(0.2f, 0.95f, 0.2f), trim);
            }
            if (_captain && _body == "konga")
                Look.Prim(PrimitiveType.Sphere, "Snout", _head, new Vector3(0, -0.18f, 0.62f), new Vector3(0.82f, 0.5f, 0.7f), flesh);
            if (_captain && _body == "zig")
            {
                var glass = Look.Lit(new Color(0.2f, 0.85f, 0.55f), smooth: 0.6f);
                Look.Prim(PrimitiveType.Cylinder, "GogL", _head, new Vector3(-0.32f, 0.12f, 0.52f), new Vector3(0.45f, 0.08f, 0.45f), glass);
                Look.Prim(PrimitiveType.Cylinder, "GogR", _head, new Vector3(0.32f, 0.12f, 0.52f), new Vector3(0.45f, 0.08f, 0.45f), glass);
            }
            if (_captain && _body == "brondo")
                Look.Prim(PrimitiveType.Cube, "Jaw", _head, new Vector3(0, -0.42f, 0.28f), new Vector3(0.95f, 0.35f, 0.7f), flesh);

            var armLen = 0.7f * spec.Arms;
            var armThick = _body == "brondo" ? 0.5f : _body == "vale" ? 0.28f : 0.38f;
            _lArm = Look.Prim(PrimitiveType.Capsule, "LArm", _torso, new Vector3(-0.85f * spec.Arms, 0.25f, 0), new Vector3(armThick, armLen, armThick), jersey).transform;
            _lFore = Look.Prim(PrimitiveType.Capsule, "LFore", _lArm, new Vector3(0, -0.85f, 0), new Vector3(0.85f, 0.7f, 0.85f), flesh).transform;
            _rArm = Look.Prim(PrimitiveType.Capsule, "RArm", _torso, new Vector3(0.85f * spec.Arms, 0.25f, 0), new Vector3(armThick, armLen, armThick), jersey).transform;
            _rFore = Look.Prim(PrimitiveType.Capsule, "RFore", _rArm, new Vector3(0, -0.85f, 0), new Vector3(0.85f, 0.7f, 0.85f), flesh).transform;
            Look.Prim(PrimitiveType.Sphere, "LHand", _lFore, new Vector3(0, -0.7f, 0), Vector3.one * 0.55f, flesh);
            Look.Prim(PrimitiveType.Sphere, "RHand", _rFore, new Vector3(0, -0.7f, 0), Vector3.one * 0.55f, flesh);

            var thighThick = _body == "zig" ? 0.55f : _body == "brondo" ? 0.58f : _body == "vale" ? 0.32f : 0.42f;
            var thighSpread = _body == "brondo" ? 0.52f : _body == "zig" ? 0.5f : 0.38f;
            _lThigh = Look.Prim(PrimitiveType.Capsule, "LThigh", _root, new Vector3(-thighSpread, 0.7f, 0), new Vector3(thighThick, 0.7f, thighThick), slack).transform;
            _lShin = Look.Prim(PrimitiveType.Capsule, "LShin", _lThigh, new Vector3(0, -0.9f, 0), new Vector3(0.8f, 0.7f, 0.8f), slack).transform;
            var shoeScale = _body == "rio" ? new Vector3(0.95f, 0.38f, 1.35f)
                : _body == "ashlord" ? new Vector3(0.85f, 0.4f, 1.25f)
                : _body == "vale" ? new Vector3(0.5f, 0.22f, 0.9f)
                : new Vector3(0.7f, 0.28f, 1.1f);
            Look.Prim(PrimitiveType.Cube, "LShoe", _lShin, new Vector3(0, -0.72f, 0.12f), shoeScale, leather);
            _rThigh = Look.Prim(PrimitiveType.Capsule, "RThigh", _root, new Vector3(thighSpread, 0.7f, 0), new Vector3(thighThick, 0.7f, thighThick), slack).transform;
            _rShin = Look.Prim(PrimitiveType.Capsule, "RShin", _rThigh, new Vector3(0, -0.9f, 0), new Vector3(0.8f, 0.7f, 0.8f), slack).transform;
            Look.Prim(PrimitiveType.Cube, "RShoe", _rShin, new Vector3(0, -0.72f, 0.12f), shoeScale, leather);

            BuildBat("bat-wood");
            BuildGlove("glove-brown");
            if (_bat != null) _bat.gameObject.SetActive(false);

            var gold = Look.Unlit(Colors.Gold);
            _ring = Look.Prim(PrimitiveType.Cylinder, "Mark", _root, new Vector3(0, 0.08f, 0), new Vector3(2.0f, 0.07f, 2.0f), gold).transform;
            _ring.gameObject.SetActive(false);
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
            go.transform.localScale = Vector3.one;
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
            var scale = _gloveVisual == "glove-gold" ? 1.18f : 1f;
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
            if (_torso != null) _torso.localPosition = new Vector3(0, 2.55f + bob, 0);

            var batOn = _heldBat;
            var gloveOn = _heldGlove;
            if (ToVerb(pose) is MoveBones.Verb verb)
            {
                batOn = pose is Pose.ChargeSwing or Pose.Swing or Pose.CheckSwing or Pose.Bunt or Pose.Miss;
                gloveOn = pose is Pose.ChargePitch or Pose.ThrowPitch or Pose.Throw or Pose.Jump or Pose.Clamber;
                if (pose is Pose.ChargeSwing or Pose.Swing) gloveOn = false;
                var sample = MoveBones.Evaluate(verb, _t, _poseT, _charge, _pitchType);
                if ((pose is Pose.ChargeSwing or Pose.Swing) && _batsLeft)
                    sample = MoveBones.MirrorArms(sample);
                if ((pose is Pose.ChargePitch or Pose.ThrowPitch or Pose.Throw) && _throwsLeft)
                    sample = MoveBones.MirrorArms(sample);
                var snap = pose is Pose.Swing or Pose.ThrowPitch or Pose.Throw or Pose.Jump;
                Apply(sample, snap ? 0.55f : 0.32f, snap ? 0.48f : 0.34f);
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

            var snap = pose is Pose.Swing or Pose.ThrowPitch or Pose.Throw or Pose.Scoop or Pose.Slide;
            var kArm = snap ? 0.55f : 0.2f;
            var kLeg = snap ? 0.45f : 0.25f;
            if (_torso != null)
                _torso.localRotation = Quaternion.Slerp(_torso.localRotation, torsoRot, kArm);
            if (_head != null)
                _head.localRotation = Quaternion.Slerp(_head.localRotation, headRot, kArm);
            if (_lArm != null) _lArm.localRotation = Quaternion.Slerp(_lArm.localRotation, lArm, kArm);
            if (_rArm != null) _rArm.localRotation = Quaternion.Slerp(_rArm.localRotation, rArm, kArm);
            if (_lThigh != null) _lThigh.localRotation = Quaternion.Slerp(_lThigh.localRotation, lLeg, kLeg);
            if (_rThigh != null) _rThigh.localRotation = Quaternion.Slerp(_rThigh.localRotation, rLeg, kLeg);
            if (_lShin != null) _lShin.localRotation = Quaternion.Slerp(_lShin.localRotation, Quaternion.Euler(12, 0, 0), kLeg);
            if (_rShin != null) _rShin.localRotation = Quaternion.Slerp(_rShin.localRotation, Quaternion.Euler(12, 0, 0), kLeg);
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
            _ => null
        };

        void Apply(MoveBones.Sample s, float kArm, float kLeg)
        {
            Ease(ref _torso, s.Torso, kArm);
            Ease(ref _head, s.Head, kArm);
            Ease(ref _lArm, s.LUpper, kArm);
            Ease(ref _lFore, s.LFore, kArm);
            Ease(ref _rArm, s.RUpper, kArm);
            Ease(ref _rFore, s.RFore, kArm);
            Ease(ref _lThigh, s.LThigh, kLeg);
            Ease(ref _lShin, s.LShin, kLeg);
            Ease(ref _rThigh, s.RThigh, kLeg);
            Ease(ref _rShin, s.RShin, kLeg);
        }

        static void Ease(ref Transform tf, MoveBones.Euler e, float k)
        {
            if (tf == null) return;
            tf.localRotation = Quaternion.Slerp(tf.localRotation, Q(e), k);
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

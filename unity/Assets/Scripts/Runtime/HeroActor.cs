using GrandSluggers.Sim;
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    public sealed class HeroActor : MonoBehaviour
    {
        public enum Pose { Idle, ChargePitch, Throw, ChargeSwing, Swing, Field, Catch, Dive, Jump, Spin, Charm, Clamber }

        Transform _root, _torso, _head, _cap, _lArm, _rArm, _lFore, _rFore, _bat, _lThigh, _rThigh, _ring;
        Pose _pose = Pose.Idle;
        float _charge;
        string _pitchType = "fastball";
        float _t;
        float _lift;
        bool _grow;
        bool _lit;
        string _id = "";
        Vector3 _look = Vector3.forward;
        Vector3 _baseScale = Vector3.one;

        public string Id => _id;

        public void Bind(Character who)
        {
            if (who.Id == _id && _root != null) return;
            _id = who.Id;
            if (_root != null) Destroy(_root.gameObject);
            Build(who);
        }

        public void SetPose(Pose pose, float charge = 0f, string pitchType = null)
        {
            _pose = pose;
            _charge = Mathf.Clamp01(charge);
            if (!string.IsNullOrEmpty(pitchType)) _pitchType = pitchType;
        }

        public void SetGrow(bool on) => _grow = on;

        public void SetHighlight(bool on) => _lit = on;

        public void Place(Vector3 pos, Vector3 look)
        {
            var lift = _pose == Pose.Jump || _pose == Pose.Clamber ? 4.2f
                : _pose == Pose.Dive ? 0.2f
                : 0f;
            _lift = Mathf.Lerp(_lift, lift, 0.2f);
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

            float h = 1f, wide = 1f, head = 1f, arms = 1f;
            switch (who.Id)
            {
                case "vale": h = 1.1f; wide = 0.82f; head = 0.92f; break;
                case "zig": h = 0.78f; wide = 1.15f; head = 1.28f; break;
                case "brondo": h = 1.02f; wide = 1.32f; head = 0.95f; break;
                case "konga": h = 1.22f; wide = 1.4f; head = 1.05f; arms = 1.25f; break;
                case "ashlord": h = 1.16f; wide = 1.28f; head = 1.1f; break;
                case "rio": h = 1.0f; wide = 1.0f; head = 1.08f; break;
            }

            _root = new GameObject("Rig").transform;
            _root.SetParent(transform, false);
            _baseScale = new Vector3(wide, h, wide) * 1.15f;
            _root.localScale = _baseScale;

            var jersey = Look.Lit(body, smooth: 0.18f);
            var trim = Look.Lit(accent, smooth: 0.35f);
            var flesh = Look.Lit(skin, smooth: 0.28f);
            var slack = Look.Lit(pants, smooth: 0.2f);
            var leather = Look.Lit(shoe, smooth: 0.12f);
            var wood = Look.Lit(new Color(0.45f, 0.28f, 0.12f), smooth: 0.15f);

            Look.Prim(PrimitiveType.Capsule, "Hip", _root, new Vector3(0, 1.15f, 0), new Vector3(1.15f, 0.7f, 0.85f), slack);
            _torso = Look.Prim(PrimitiveType.Capsule, "Torso", _root, new Vector3(0, 2.55f, 0), new Vector3(1.35f, 1.05f, 0.85f), jersey).transform;
            Look.Prim(PrimitiveType.Cube, "Stripe", _torso, new Vector3(0, 0.15f, 0.42f), new Vector3(0.18f, 0.7f, 0.08f), trim);

            _head = Look.Prim(PrimitiveType.Sphere, "Head", _root, new Vector3(0, 4.35f, 0), Vector3.one * (1.55f * head), flesh).transform;
            var ink = Look.Lit(new Color(0.08f, 0.07f, 0.07f), smooth: 0.05f);
            Look.Prim(PrimitiveType.Sphere, "EyeL", _head, new Vector3(-0.28f, 0.1f, 0.58f), Vector3.one * 0.22f, ink);
            Look.Prim(PrimitiveType.Sphere, "EyeR", _head, new Vector3(0.28f, 0.1f, 0.58f), Vector3.one * 0.22f, ink);
            _cap = Look.Prim(PrimitiveType.Cylinder, "Cap", _head, new Vector3(0, 0.42f, 0), new Vector3(1.15f, 0.18f, 1.15f), trim).transform;
            Look.Prim(PrimitiveType.Cube, "Brim", _cap, new Vector3(0, -0.6f, 0.7f), new Vector3(1.1f, 0.12f, 0.7f), Look.Lit(Colors.Gold, smooth: 0.4f));

            if (who.Id == "ashlord")
            {
                Look.Prim(PrimitiveType.Cube, "HornL", _head, new Vector3(-0.45f, 0.55f, 0), new Vector3(0.18f, 0.7f, 0.18f), trim);
                Look.Prim(PrimitiveType.Cube, "HornR", _head, new Vector3(0.45f, 0.55f, 0), new Vector3(0.18f, 0.7f, 0.18f), trim);
            }
            if (who.Id == "konga")
                Look.Prim(PrimitiveType.Sphere, "Snout", _head, new Vector3(0, -0.15f, 0.55f), new Vector3(0.7f, 0.45f, 0.55f), flesh);

            _lArm = Look.Prim(PrimitiveType.Capsule, "LArm", _torso, new Vector3(-0.85f * arms, 0.25f, 0), new Vector3(0.38f, 0.7f * arms, 0.38f), jersey).transform;
            _lFore = Look.Prim(PrimitiveType.Capsule, "LFore", _lArm, new Vector3(0, -0.85f, 0), new Vector3(0.85f, 0.7f, 0.85f), flesh).transform;
            _rArm = Look.Prim(PrimitiveType.Capsule, "RArm", _torso, new Vector3(0.85f * arms, 0.25f, 0), new Vector3(0.38f, 0.7f * arms, 0.38f), jersey).transform;
            _rFore = Look.Prim(PrimitiveType.Capsule, "RFore", _rArm, new Vector3(0, -0.85f, 0), new Vector3(0.85f, 0.7f, 0.85f), flesh).transform;
            Look.Prim(PrimitiveType.Sphere, "RHand", _rFore, new Vector3(0, -0.7f, 0), Vector3.one * 0.55f, flesh);

            _bat = Look.Prim(PrimitiveType.Cylinder, "Bat", _rFore, new Vector3(0, -1.4f, 0.1f), new Vector3(0.22f, 1.7f, 0.22f), wood).transform;
            _bat.localRotation = Quaternion.Euler(0, 0, 20);

            _lThigh = Look.Prim(PrimitiveType.Capsule, "LThigh", _root, new Vector3(-0.38f, 0.7f, 0), new Vector3(0.42f, 0.7f, 0.42f), slack).transform;
            Look.Prim(PrimitiveType.Capsule, "LShin", _lThigh, new Vector3(0, -0.9f, 0), new Vector3(0.8f, 0.7f, 0.8f), slack);
            Look.Prim(PrimitiveType.Cube, "LShoe", _lThigh, new Vector3(0, -1.55f, 0.12f), new Vector3(0.7f, 0.28f, 1.1f), leather);
            _rThigh = Look.Prim(PrimitiveType.Capsule, "RThigh", _root, new Vector3(0.38f, 0.7f, 0), new Vector3(0.42f, 0.7f, 0.42f), slack).transform;
            Look.Prim(PrimitiveType.Capsule, "RShin", _rThigh, new Vector3(0, -0.9f, 0), new Vector3(0.8f, 0.7f, 0.8f), slack);
            Look.Prim(PrimitiveType.Cube, "RShoe", _rThigh, new Vector3(0, -1.55f, 0.12f), new Vector3(0.7f, 0.28f, 1.1f), leather);

            _bat.gameObject.SetActive(false);

            var gold = Look.Unlit(Colors.Gold);
            _ring = Look.Prim(PrimitiveType.Cylinder, "Glove", _root, new Vector3(0, 0.08f, 0), new Vector3(2.0f, 0.07f, 2.0f), gold).transform;
            _ring.gameObject.SetActive(false);
        }

        void Animate()
        {
            var bob = Mathf.Sin(_t * 2.4f) * 0.04f;
            if (_torso != null) _torso.localPosition = new Vector3(0, 2.55f + bob, 0);

            var lArm = Quaternion.Euler(12, 0, 18);
            var rArm = Quaternion.Euler(12, 0, -18);
            var batOn = false;
            var batRot = Quaternion.Euler(0, 0, 20);

            switch (_pose)
            {
                case Pose.ChargePitch:
                    lArm = Quaternion.Euler(8, 0, 25);
                    rArm = PitchSlot(-20 - 55 * _charge, 10, -30);
                    break;
                case Pose.Throw:
                    lArm = Quaternion.Euler(20, 0, 35);
                    rArm = PitchSlot(78, -10, -10);
                    break;
                case Pose.ChargeSwing:
                    batOn = true;
                    lArm = Quaternion.Euler(-10, 20, 30);
                    rArm = Quaternion.Euler(-35 - 50 * _charge, -40, -55);
                    batRot = Quaternion.Euler(75 + 28 * _charge, 0, 10);
                    break;
                case Pose.Swing:
                    batOn = true;
                    lArm = Quaternion.Euler(20, -30, 10);
                    rArm = Quaternion.Euler(15, 50, 20);
                    batRot = Quaternion.Euler(-40, 80, 0);
                    break;
                case Pose.Catch:
                    lArm = Quaternion.Euler(-50, 0, 20);
                    rArm = Quaternion.Euler(-50, 0, -20);
                    break;
                case Pose.Field:
                    lArm = Quaternion.Euler(25, 0, 25);
                    rArm = Quaternion.Euler(25, 0, -25);
                    break;
                case Pose.Dive:
                    lArm = Quaternion.Euler(-80, 0, 10);
                    rArm = Quaternion.Euler(-80, 0, -10);
                    if (_torso != null) _torso.localRotation = Quaternion.Euler(70, 0, 0);
                    break;
                case Pose.Jump:
                case Pose.Clamber:
                    lArm = Quaternion.Euler(-70, 0, 15);
                    rArm = Quaternion.Euler(-70, 0, -15);
                    break;
                case Pose.Spin:
                    lArm = Quaternion.Euler(10, 0, 70);
                    rArm = Quaternion.Euler(10, 0, -70);
                    transform.rotation *= Quaternion.Euler(0, 720f * Time.deltaTime, 0);
                    break;
                case Pose.Charm:
                    lArm = Quaternion.Euler(0, 0, 40);
                    rArm = Quaternion.Euler(0, 0, -40);
                    break;
            }

            if (_torso != null && _pose != Pose.Dive)
                _torso.localRotation = Quaternion.Slerp(_torso.localRotation, Quaternion.identity, 0.2f);
            if (_lArm != null) _lArm.localRotation = Quaternion.Slerp(_lArm.localRotation, lArm, 0.2f);
            if (_rArm != null) _rArm.localRotation = Quaternion.Slerp(_rArm.localRotation, rArm, 0.2f);
            if (_bat != null)
            {
                _bat.gameObject.SetActive(batOn);
                _bat.localRotation = batRot;
            }
        }

        Quaternion PitchSlot(float x, float y, float z)
        {
            // Fastball and changeup share a high 3/4 slot (the lie). Curve is over the top. Slider is lower.
            return _pitchType switch
            {
                "curve" => Quaternion.Euler(x - 25, y, z - 12),
                "slider" => Quaternion.Euler(x + 28, y + 18, z + 8),
                _ => Quaternion.Euler(x, y, z)
            };
        }
    }
}

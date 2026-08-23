using UnityEngine;

namespace GrandSluggers.UnityClient
{
    public sealed class SpecialFx : MonoBehaviour
    {
        Transform _root;
        Transform _decoy;
        Transform _barrel;
        readonly Transform[] _hearts = new Transform[6];
        readonly Transform[] _bits = new Transform[8];
        LineRenderer _laser;
        LineRenderer _tongue;
        Transform _burn;
        float _t;
        Vector3 _ball;
        Vector3 _decoyPos;

        public void Build(Transform parent)
        {
            if (_root != null) Destroy(_root.gameObject);
            _root = new GameObject("SpecialFx").transform;
            _root.SetParent(parent, false);

            _decoy = Ballish("Decoy", new Color(1f, 1f, 1f, 0.35f), 1.2f);
            _barrel = Cyl("BarrelBall", new Color(0.45f, 0.26f, 0.1f), new Vector3(1.6f, 2.2f, 1.6f));

            var pink = Look.Unlit(new Color(1f, 0.35f, 0.55f, 0.9f));
            for (var i = 0; i < _hearts.Length; i++)
                _hearts[i] = Look.Prim(PrimitiveType.Sphere, "Heart", _root, Vector3.zero, Vector3.one * 0.55f, pink).transform;

            var wood = Look.Lit(new Color(0.4f, 0.22f, 0.1f), smooth: 0.1f);
            for (var i = 0; i < _bits.Length; i++)
                _bits[i] = Look.Prim(PrimitiveType.Cube, "Frag", _root, Vector3.zero, new Vector3(0.8f, 0.35f, 0.5f), wood).transform;

            _laser = Line("Laser", Colors.EmberFire, 0.35f);
            _tongue = Line("Tongue", new Color(1f, 0.4f, 0.55f), 0.28f);

            _burn = Look.Prim(PrimitiveType.Cylinder, "Burn", _root, Vector3.zero, new Vector3(18f, 0.08f, 18f),
                Look.Lit(Colors.EmberFire, smooth: 0.4f)).transform;
            HideAll();
        }

        public void Tick(
            float dt,
            Vector3 ball,
            bool flight,
            bool inPlay,
            bool starPitch,
            string starPitchId,
            string starSwingId,
            Vector3 tongueFrom,
            Vector3 laserTo,
            bool showTongue,
            bool showLaser,
            bool showBurn,
            bool showFrags)
        {
            if (_root == null) return;
            _t += dt;
            _ball = ball;

            var pitchOn = flight && starPitch && !string.IsNullOrEmpty(starPitchId);
            PlaceDecoy(pitchOn && starPitchId == "phonyball", ball, dt);
            PlaceBarrel(pitchOn && starPitchId == "caskball", ball);
            Hearts((pitchOn && starPitchId == "charmball") || starSwingId == "heart-swing", ball);
            Fragments(showFrags || starSwingId == "cask-swing" || starSwingId == "shell-swing", ball);
            Burn(showBurn || starSwingId == "furnace" || starSwingId == "heat-swing", ball);
            Beam(_tongue, showTongue, tongueFrom, ball);
            Beam(_laser, showLaser, tongueFrom, laserTo == Vector3.zero ? ball : laserTo);
        }

        public Vector3 DecoyBall => _decoyPos;

        void PlaceDecoy(bool on, Vector3 real, float dt)
        {
            if (_decoy == null) return;
            _decoy.gameObject.SetActive(on);
            if (!on) return;
            if (_decoyPos.sqrMagnitude < 0.01f) _decoyPos = new Vector3(0, 5.4f, 60.5f);
            _decoyPos = Vector3.Lerp(_decoyPos, new Vector3(0, 2.4f, 0), 1f - Mathf.Exp(-1.6f * dt));
            _decoy.position = _decoyPos;
        }

        void PlaceBarrel(bool on, Vector3 p)
        {
            if (_barrel == null) return;
            _barrel.gameObject.SetActive(on);
            if (!on) return;
            _barrel.position = p;
            _barrel.rotation = Quaternion.Euler(_t * 220f, 40f, _t * 90f);
        }

        void Hearts(bool on, Vector3 around)
        {
            for (var i = 0; i < _hearts.Length; i++)
            {
                if (_hearts[i] == null) continue;
                _hearts[i].gameObject.SetActive(on);
                if (!on) continue;
                var a = _t * 2.2f + i * Mathf.PI * 2f / _hearts.Length;
                _hearts[i].position = around + new Vector3(Mathf.Cos(a) * 2.4f, 1.6f + Mathf.Sin(_t * 3f + i) * 0.5f, Mathf.Sin(a) * 2.4f);
            }
        }

        void Fragments(bool on, Vector3 around)
        {
            for (var i = 0; i < _bits.Length; i++)
            {
                if (_bits[i] == null) continue;
                _bits[i].gameObject.SetActive(on);
                if (!on) continue;
                var a = i * Mathf.PI * 2f / _bits.Length;
                var r = 2.5f + Mathf.Sin(_t * 6f + i) * 1.2f;
                _bits[i].position = around + new Vector3(Mathf.Cos(a) * r, 0.4f, Mathf.Sin(a) * r);
                _bits[i].rotation = Quaternion.Euler(0, a * Mathf.Rad2Deg, 20);
            }
        }

        void Burn(bool on, Vector3 p)
        {
            if (_burn == null) return;
            _burn.gameObject.SetActive(on);
            if (on) _burn.position = new Vector3(p.x, 0.2f, p.z);
        }

        void Beam(LineRenderer lr, bool on, Vector3 from, Vector3 to)
        {
            if (lr == null) return;
            lr.enabled = on;
            if (!on) return;
            lr.SetPosition(0, from + Vector3.up * 2.4f);
            lr.SetPosition(1, to);
        }

        public void ResetDecoy() => _decoyPos = Vector3.zero;

        void HideAll()
        {
            if (_decoy != null) _decoy.gameObject.SetActive(false);
            if (_barrel != null) _barrel.gameObject.SetActive(false);
            if (_burn != null) _burn.gameObject.SetActive(false);
            if (_laser != null) _laser.enabled = false;
            if (_tongue != null) _tongue.enabled = false;
            foreach (var h in _hearts) if (h != null) h.gameObject.SetActive(false);
            foreach (var b in _bits) if (b != null) b.gameObject.SetActive(false);
        }

        Transform Ballish(string name, Color c, float scale)
        {
            var go = Look.Prim(PrimitiveType.Sphere, name, _root, Vector3.zero, Vector3.one * scale, Look.Unlit(c));
            go.SetActive(false);
            return go.transform;
        }

        Transform Cyl(string name, Color c, Vector3 scale)
        {
            var go = Look.Prim(PrimitiveType.Cylinder, name, _root, Vector3.zero, scale, Look.Lit(c, smooth: 0.12f));
            go.SetActive(false);
            return go.transform;
        }

        LineRenderer Line(string name, Color c, float width)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_root, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.startWidth = width;
            lr.endWidth = width * 0.4f;
            lr.material = new Material(Shader.Find("Sprites/Default") ?? Look.LitShader);
            lr.startColor = c;
            lr.endColor = new Color(c.r, c.g, c.b, 0.2f);
            lr.enabled = false;
            return lr;
        }
    }
}

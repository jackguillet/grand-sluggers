using GrandSluggers.Sim;
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    public sealed class CameraRig : MonoBehaviour
    {
        Camera _cam;
        Vector3 _pos;
        Vector3 _look;
        float _fov = 48f;
        float _punch;
        float _blend = 6f;

        public Camera Cam
        {
            get
            {
                if (_cam == null) _cam = Camera.main;
                return _cam;
            }
        }

        public void UseFeel(FeelTable feel)
        {
            if (feel != null && feel.CameraBlend > 0)
                _blend = (float)feel.CameraBlend;
        }

        public void Bind(Camera cam)
        {
            _cam = cam != null ? cam : Camera.main;
            var live = Cam;
            if (live == null) return;
            UnlockFov(live);
            _pos = live.transform.position;
            _look = live.transform.position + live.transform.forward * 40f;
            _fov = live.fieldOfView;
        }

        static void UnlockFov(Camera cam)
        {
            if (cam == null) return;
            // HarborDiamond's Main Camera is serialized Physical / 50mm. FOV writes
            // are ignored until this is off, so SET stays a telephoto of the cage.
            cam.usePhysicalProperties = false;
        }

        Camera[] Targets()
        {
            var all = Camera.allCameras;
            return all != null && all.Length > 0 ? all : (Cam != null ? new[] { Cam } : System.Array.Empty<Camera>());
        }

        public void Cut(Vector3 pos, Vector3 look, float fov = 48f)
        {
            _pos = pos;
            _look = look;
            _fov = fov;
            foreach (var cam in Targets())
            {
                if (cam == null) continue;
                UnlockFov(cam);
                cam.transform.position = pos;
                cam.transform.LookAt(look);
                cam.fieldOfView = fov;
            }
        }

        public void Aim(Vector3 pos, Vector3 look, float fov = 48f)
        {
            _pos = pos;
            _look = look;
            _fov = fov;
        }

        public void Punch(float amount = 10f) => _punch = amount;

        public void Smash(Vector3 at)
        {
            _pos = at + new Vector3(2.6f, 2.4f, -7.2f);
            _look = at + new Vector3(0f, 0.4f, 1.2f);
            _fov = 26f;
            _punch = 16f;
        }

        public void FramePitch() =>
            Cut(new Vector3(10.2f, 6.2f, 75.8f), new Vector3(0.4f, 1.6f, 2.2f), 42f);

        /// <summary>Over-the-batter 3/4 looking at the mound. Plate and chalk boxes read. Not catcher-spine.</summary>
        public void FramePlate() =>
            Cut(new Vector3(-10.8f, 5.2f, -4.4f), new Vector3(2.55f, 0.95f, 11f), 50f);

        public void FrameThrow(Vector3 from, Vector3 to)
        {
            var dir = to - from;
            dir.y = 0f;
            if (dir.sqrMagnitude < 1f) dir = Vector3.forward;
            dir.Normalize();
            var side = Vector3.Cross(Vector3.up, dir);
            Aim(from - dir * 12f + Vector3.up * 6.2f + side * 4.2f, to + Vector3.up * 1.6f, 40f);
        }

        public void Tick(float dt)
        {
            var k = _blend > 0 ? _blend : 6f;
            var snap = 1f - Mathf.Exp(-k * dt);
            var rotSnap = 1f - Mathf.Exp(-(k + 1f) * dt);
            foreach (var cam in Targets())
            {
                if (cam == null) continue;
                UnlockFov(cam);
                cam.transform.position = Vector3.Lerp(cam.transform.position, _pos, snap);
                var to = _look - cam.transform.position;
                if (to.sqrMagnitude > 0.0001f)
                {
                    var want = Quaternion.LookRotation(to.normalized, Vector3.up);
                    cam.transform.rotation = Quaternion.Slerp(cam.transform.rotation, want, rotSnap);
                }
                cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, _fov - _punch, 1f - Mathf.Exp(-10f * dt));
            }
            _punch = Mathf.MoveTowards(_punch, 0f, dt * 28f);
        }
    }
}

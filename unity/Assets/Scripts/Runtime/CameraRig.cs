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

        public Camera Cam => _cam;

        public void UseFeel(FeelTable feel)
        {
            if (feel != null && feel.CameraBlend > 0)
                _blend = (float)feel.CameraBlend;
        }

        public void Bind(Camera cam)
        {
            _cam = cam;
            _pos = cam.transform.position;
            _look = cam.transform.position + cam.transform.forward * 40f;
            _fov = cam.fieldOfView;
        }

        public void Cut(Vector3 pos, Vector3 look, float fov = 48f)
        {
            _pos = pos;
            _look = look;
            _fov = fov;
            if (_cam == null) return;
            _cam.transform.position = pos;
            _cam.transform.LookAt(look);
            _cam.fieldOfView = fov;
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
            Aim(new Vector3(9.8f, 6.4f, 76.5f), new Vector3(0.4f, 2.2f, 1.0f), 42f);

        /// <summary>Over-the-batter 3/4 looking at the mound. Plate and chalk boxes read. Not catcher-spine.</summary>
        public void FramePlate() =>
            Aim(new Vector3(-5.2f, 5.5f, -6.8f), new Vector3(3.4f, 2.8f, 58f), 52f);

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
            if (_cam == null) return;
            var k = _blend > 0 ? _blend : 6f;
            _cam.transform.position = Vector3.Lerp(_cam.transform.position, _pos, 1f - Mathf.Exp(-k * dt));
            var currentLook = _cam.transform.position + _cam.transform.forward * 40f;
            var look = Vector3.Lerp(currentLook, _look, 1f - Mathf.Exp(-(k + 1f) * dt));
            _cam.transform.LookAt(look);
            _punch = Mathf.MoveTowards(_punch, 0f, dt * 28f);
            _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, _fov - _punch, 1f - Mathf.Exp(-10f * dt));
        }
    }
}

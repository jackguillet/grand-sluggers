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

        public Camera Cam => _cam;

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
            Aim(new Vector3(-6.2f, 6.4f, 78f), new Vector3(0.2f, 3.1f, 3.4f), 36f);

        public void FramePlate() =>
            Aim(new Vector3(6.4f, 5.8f, -12f), new Vector3(0f, 3.2f, 44f), 34f);

        public void FrameThrow(Vector3 from, Vector3 to)
        {
            var dir = to - from;
            dir.y = 0f;
            if (dir.sqrMagnitude < 1f) dir = Vector3.forward;
            dir.Normalize();
            var side = Vector3.Cross(Vector3.up, dir);
            Aim(from - dir * 18f + Vector3.up * 7.5f + side * 5.5f, to + Vector3.up * 1.4f, 42f);
        }

        public void Tick(float dt)
        {
            if (_cam == null) return;
            _cam.transform.position = Vector3.Lerp(_cam.transform.position, _pos, 1f - Mathf.Exp(-6f * dt));
            var currentLook = _cam.transform.position + _cam.transform.forward * 40f;
            var look = Vector3.Lerp(currentLook, _look, 1f - Mathf.Exp(-7f * dt));
            _cam.transform.LookAt(look);
            _punch = Mathf.MoveTowards(_punch, 0f, dt * 28f);
            _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, _fov - _punch, 1f - Mathf.Exp(-10f * dt));
        }
    }
}

using GrandSluggers.Sim;
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    /// <summary>Named shots from data. CameraRig is the motor; this is the shot list.</summary>
    public sealed class CameraDirector : MonoBehaviour
    {
        CameraRig _rig;
        CameraShots _shots;

        public string Shot { get; private set; } = "title";

        public void Bind(CameraRig rig, CameraShots shots, FeelTable feel)
        {
            _rig = rig;
            _shots = shots;
            if (_rig != null)
                _rig.UseFeel(feel);
        }

        public void Play(string id)
        {
            var s = Must(id);
            Shot = s.Id;
            _rig.Aim(V(s.Pos), V(s.Target), (float)s.Fov);
        }

        public void PlayLook(string id, Vector3 look)
        {
            var s = Must(id);
            Shot = s.Id;
            _rig.Aim(V(s.Pos), look, (float)s.Fov);
        }

        public void Cut(string id)
        {
            var s = Must(id);
            Shot = s.Id;
            _rig.Cut(V(s.Pos), V(s.Target), (float)s.Fov);
        }

        public void ThrowTo(Vector3 from, Vector3 to)
        {
            var s = Must("throw");
            Shot = s.Id;
            var dir = to - from;
            dir.y = 0f;
            if (dir.sqrMagnitude < 1f) dir = Vector3.forward;
            dir.Normalize();
            var side = Vector3.Cross(Vector3.up, dir);
            var height = (float)(s.Pos.Y > 0 ? s.Pos.Y : 7.5);
            _rig.Aim(from - dir * 18f + Vector3.up * height + side * 5.5f, to + Vector3.up * 1.4f, (float)s.Fov);
        }

        public void AimRaw(string name, Vector3 pos, Vector3 look, float fov)
        {
            Shot = name;
            _rig.Aim(pos, look, fov);
        }

        public void SmashAt(Vector3 at)
        {
            var s = Must("smash");
            Shot = s.Id;
            _rig.Aim(at + V(s.Pos), at + V(s.Target), (float)s.Fov);
            _rig.Punch(16f);
        }

        CameraShot Must(string id)
        {
            if (_shots != null && _shots.TryGet(id, out var shot))
                return shot;
            throw new System.InvalidOperationException("No camera shot '" + id + "'");
        }

        static Vector3 V(Vec3 p) => new Vector3((float)p.X, (float)p.Y, (float)p.Z);
    }
}

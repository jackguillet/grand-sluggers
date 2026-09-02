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
            var kit = HarborKit.Instance;
            if (kit != null) kit.SyncShots(shots);
        }

        public void Play(string id)
        {
            var s = Must(id);
            Shot = s.Id;
            Vector3 pos;
            Vector3 look;
            float fov;
            if (Placed(id, out pos, out look, out fov))
                _rig.Aim(pos, look, fov >= 10f ? fov : (float)s.Fov);
            else
                _rig.Aim(V(s.Pos), V(s.Target), (float)s.Fov);
        }

        public void PlayLook(string id, Vector3 look)
        {
            var s = Must(id);
            Shot = s.Id;
            Vector3 pos;
            Vector3 placedLook;
            float fov;
            if (Placed(id, out pos, out placedLook, out fov))
                _rig.Aim(pos, look, fov >= 10f ? fov : (float)s.Fov);
            else
                _rig.Aim(V(s.Pos), look, (float)s.Fov);
        }

        public void Cut(string id)
        {
            var s = Must(id);
            Shot = s.Id;
            Vector3 pos;
            Vector3 look;
            float fov;
            if (Placed(id, out pos, out look, out fov))
                _rig.Cut(pos, look, fov >= 10f ? fov : (float)s.Fov);
            else
                _rig.Cut(V(s.Pos), V(s.Target), (float)s.Fov);
        }

        static bool Placed(string id, out Vector3 pos, out Vector3 look, out float fov)
        {
            // Kit shot transforms used to store FOV in localScale.x, which shears the
            // Look child. Runtime cameras are data/feel/shots.json only.
            pos = default;
            look = default;
            fov = 0;
            _ = id;
            return false;
        }

        /// <summary>
        /// Named shot, look sitting on a live subject. Wall / fly / homer follow-cams.
        /// 1P and 1v1 share <see cref="PlayCamera.Shot"/>.
        /// </summary>
        public void Follow(string id, Vector3 subject)
        {
            var s = Must(id);
            Shot = s.Id;
            var framed = PlayCamera.Follow(s, new Vec3(subject.x, subject.y, subject.z));
            _rig.Aim(
                new Vector3((float)framed.Pos.X, (float)framed.Pos.Y, (float)framed.Pos.Z),
                new Vector3((float)framed.Look.X, (float)framed.Look.Y, (float)framed.Look.Z),
                (float)framed.Fov);
        }

        /// <summary>
        /// Live play: top-down on the dirt under the ball. CF stays the top of the frame.
        /// </summary>
        public void HoldInPlay(Vector3 at)
        {
            var s = Must(PlayCamera.InPlay);
            Shot = s.Id;
            var framed = PlayCamera.FollowGround(s, new Vec3(at.x, at.y, at.z));
            _rig.Aim(
                new Vector3((float)framed.Pos.X, (float)framed.Pos.Y, (float)framed.Pos.Z),
                new Vector3((float)framed.Look.X, (float)framed.Look.Y, (float)framed.Look.Z),
                (float)framed.Fov);
        }

        public void ThrowTo(Vector3 from, Vector3 to, bool tag = false)
        {
            _ = from;
            _ = tag;
            HoldInPlay(to);
        }

        public void AimRaw(string name, Vector3 pos, Vector3 look, float fov)
        {
            Shot = name;
            _rig.Aim(pos, look, fov);
        }

        public void CutRaw(string name, Vector3 pos, Vector3 look, float fov)
        {
            Shot = name;
            _rig.Cut(pos, look, fov);
        }

        public void SmashAt(Vector3 at)
        {
            var s = Must("smash");
            Shot = s.Id;
            _rig.Aim(at + V(s.Pos), at + V(s.Target), (float)s.Fov);
            _rig.Punch(16f);
        }

        public void SmashCut(Vector3 at)
        {
            var s = Must("smash");
            Shot = s.Id;
            _rig.Cut(at + V(s.Pos), at + V(s.Target), (float)s.Fov);
        }

        public void CutLook(string id, Vector3 look)
        {
            var s = Must(id);
            Shot = s.Id;
            Vector3 pos;
            Vector3 placed;
            float fov;
            if (Placed(id, out pos, out placed, out fov))
                _rig.Cut(pos, look, fov >= 10f ? fov : (float)s.Fov);
            else
                _rig.Cut(V(s.Pos), look, (float)s.Fov);
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

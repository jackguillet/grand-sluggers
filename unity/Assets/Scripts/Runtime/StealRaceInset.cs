using GrandSluggers.Sim;
using GrandSluggers.Sim.Front;
using UnityEngine;

namespace GrandSluggers.UnityClient
{
    /// <summary>A second view of the same live scene. The plate camera and its hittable pitch remain unchanged.</summary>
    public sealed class StealRaceInset : MonoBehaviour
    {
        Camera _camera;
        RenderTexture _texture;
        bool _shown;
        GUIStyle _label;

        public void Show(PlayCamera.Framing frame)
        {
            var rect = BroadcastHud.StealInset.Pixel(Screen.width, Screen.height);
            var width = Mathf.Max(1, (int)rect.W);
            var height = Mathf.Max(1, (int)rect.H);
            if (_camera == null)
            {
                var go = new GameObject("Runner race inset");
                go.transform.SetParent(transform, false);
                _camera = go.AddComponent<Camera>();
                if (Camera.main != null) _camera.CopyFrom(Camera.main);
                _camera.tag = "Untagged";
                _camera.usePhysicalProperties = false;
                _camera.rect = new Rect(0, 0, 1, 1);
                _camera.depth = -1;
            }
            if (_texture == null || _texture.width != width || _texture.height != height)
            {
                _camera.targetTexture = null;
                if (_texture != null) { _texture.Release(); Destroy(_texture); }
                _texture = new RenderTexture(width, height, 24);
                _texture.Create();
                _camera.targetTexture = _texture;
            }
            _camera.transform.position = V(frame.Pos);
            _camera.transform.LookAt(V(frame.Look));
            _camera.fieldOfView = (float)frame.Fov;
            _camera.aspect = (float)width / height;
            _camera.enabled = true;
            _shown = true;
        }

        public void Hide()
        {
            _shown = false;
            if (_camera != null) _camera.enabled = false;
        }

        void OnGUI()
        {
            if (!_shown || _texture == null) return;
            var r = BroadcastHud.StealInset.Pixel(Screen.width, Screen.height);
            var rect = new Rect((float)r.X, (float)r.Y, (float)r.W, (float)r.H);
            var oldDepth = GUI.depth;
            GUI.depth = 1; // behind the ordinary HUD, coaching and pause screen
            GUI.DrawTexture(rect, _texture, ScaleMode.StretchToFill);
            _label ??= new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.UpperCenter };
            _label.fontSize = Mathf.RoundToInt(Screen.height * .018f);
            GUI.Label(rect, BroadcastHud.StealInsetTitle, _label);
            GUI.depth = oldDepth;
        }

        void OnDestroy()
        {
            if (_camera != null) _camera.targetTexture = null;
            if (_texture != null) { _texture.Release(); Destroy(_texture); }
        }

        static Vector3 V(Vec3 p) => new((float)p.X, (float)p.Y, (float)p.Z);
    }
}

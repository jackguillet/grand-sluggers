using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace GrandSluggers.UnityClient
{
    public static class Look
    {
        static Shader _lit;
        static Texture2D _grass, _dirt, _crowd, _rio, _vale, _zig, _brondo, _konga, _ashlord;

        public static Shader LitShader
        {
            get
            {
                if (_lit == null)
                    _lit = Shader.Find("Universal Render Pipeline/Lit")
                           ?? Shader.Find("Sprites/Default")
                           ?? Shader.Find("Standard");
                return _lit;
            }
        }

        public static Texture2D Grass => _grass ??= Load("tex-grass.jpg", true);
        public static Texture2D Dirt => _dirt ??= Load("tex-dirt.jpg", true);
        public static Texture2D Crowd => _crowd ??= Load("tex-crowd.jpg", false);
        public static Texture2D Rio => _rio ??= Load("rio-hero.jpg", false);

        public static Texture2D Portrait(string id)
        {
            switch (id)
            {
                case "vale": return _vale ??= Load("vale-hero.jpg", false);
                case "zig": return _zig ??= Load("zig-hero.jpg", false);
                case "brondo": return _brondo ??= Load("brondo-hero.jpg", false);
                case "konga": return _konga ??= Load("konga-hero.jpg", false);
                case "ashlord": return _ashlord ??= Load("ashlord-hero.jpg", false);
                default: return Rio;
            }
        }

        public static Material Lit(Color color, Texture tex = null, float tile = 1f, float smooth = 0.22f)
        {
            var m = new Material(LitShader);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            else m.color = color;
            if (tex != null)
            {
                if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
                m.mainTexture = tex;
                m.mainTextureScale = new Vector2(tile, tile);
            }
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
            return m;
        }

        public static Material Unlit(Color color)
        {
            var sh = Shader.Find("Universal Render Pipeline/Unlit") ?? LitShader;
            var m = new Material(sh);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            else m.color = color;
            return m;
        }

        public static void Paint(GameObject go, Material mat)
        {
            var r = go.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = mat;
        }

        public static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 localPos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            Object.Destroy(go.GetComponent<Collider>());
            Paint(go, mat);
            return go;
        }

        public static void SetupLighting(Camera cam, Color sky)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = sky;
            cam.farClipPlane = 900f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = Color.Lerp(sky, Color.white, 0.15f);
            RenderSettings.fogStartDistance = 180f;
            RenderSettings.fogEndDistance = 620f;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = sky;
            RenderSettings.ambientEquatorColor = new Color(0.72f, 0.78f, 0.7f);
            RenderSettings.ambientGroundColor = new Color(0.28f, 0.24f, 0.18f);
            RenderSettings.subtractiveShadowColor = new Color(0.25f, 0.22f, 0.3f);
            DirLight("Sun", new Color(1f, 0.95f, 0.86f), 1.15f, new Vector3(50f, 30f, 0f), true);
            DirLight("Fill", Color.black, 0f, Vector3.zero, false);
            DirLight("Rim", Color.black, 0f, Vector3.zero, false);
        }

        /// <summary>Harbor afternoon: warm key, cool fill, gold rim. Not a default Directional Light.</summary>
        public static void RigAfternoon(Camera cam)
        {
            var sky = new Color(0.52f, 0.70f, 0.88f);
            SetupLighting(cam, sky);
            cam.backgroundColor = sky;
            RenderSettings.fogColor = new Color(0.78f, 0.80f, 0.72f);
            RenderSettings.fogStartDistance = 240f;
            RenderSettings.fogEndDistance = 760f;
            RenderSettings.ambientSkyColor = new Color(0.58f, 0.74f, 0.90f);
            RenderSettings.ambientEquatorColor = new Color(0.86f, 0.74f, 0.52f);
            RenderSettings.ambientGroundColor = new Color(0.30f, 0.24f, 0.16f);
            DirLight("Sun", new Color(1f, 0.91f, 0.72f), 1.55f, new Vector3(38f, 42f, 0f), true);
            DirLight("Fill", new Color(0.52f, 0.66f, 0.85f), 0.32f, new Vector3(58f, -78f, 0f), false);
            DirLight("Rim", new Color(1f, 0.78f, 0.48f), 0.42f, new Vector3(16f, 168f, 0f), false);
        }

        static void DirLight(string name, Color color, float intensity, Vector3 euler, bool shadows)
        {
            var go = GameObject.Find(name);
            if (go == null) go = new GameObject(name);
            var light = go.GetComponent<Light>();
            if (light == null) light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = color;
            light.intensity = intensity;
            light.shadows = shadows ? LightShadows.Soft : LightShadows.None;
            go.transform.rotation = Quaternion.Euler(euler);
        }

        static Texture2D Load(string file, bool repeat)
        {
            var fromRes = Resources.Load<Texture2D>("Art/" + Path.GetFileNameWithoutExtension(file));
            if (fromRes == null) return null;
            fromRes.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            fromRes.filterMode = FilterMode.Bilinear;
            return fromRes;
        }
    }
}

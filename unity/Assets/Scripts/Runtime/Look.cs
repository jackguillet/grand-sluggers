using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace GrandSluggers.UnityClient
{
    public static class Look
    {
        static Shader _lit, _toon;
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

        public static bool HasPortrait(string id)
        {
            switch (id)
            {
                case "rio":
                case "vale":
                case "zig":
                case "brondo":
                case "konga":
                case "ashlord":
                    return true;
                default:
                    return false;
            }
        }

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

        public static Shader ToonShader
        {
            get
            {
                if (_toon == null)
                    _toon = Shader.Find("GrandSluggers/ToonFill");
                return _toon;
            }
        }

        /// <summary>Two-tone ramp fill — Harbor trim and bodies. Falls back to matte Lit.</summary>
        public static Material Toon(Color color)
        {
            var c = Color.Lerp(color, Color.white, 0.08f);
            c = new Color(Mathf.Min(1f, c.r * 1.18f), Mathf.Min(1f, c.g * 1.12f), Mathf.Min(1f, c.b * 1.08f), 1f);
            var shadow = Color.Lerp(c, new Color(0.16f, 0.12f, 0.22f), 0.42f);
            var sh = ToonShader;
            if (sh != null)
            {
                var m = new Material(sh);
                if (m.HasProperty("_Color")) m.SetColor("_Color", c);
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
                if (m.HasProperty("_ShadowTint")) m.SetColor("_ShadowTint", shadow);
                if (m.HasProperty("_Rim")) m.SetColor("_Rim", new Color(0.07f, 0.06f, 0.09f, 1f));
                return m;
            }
            return Lit(c, smooth: 0.04f);
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

        /// <summary>Crystal ice garden: cool key, cool fill, violet rim. Not Harbor afternoon.</summary>
        public static void RigIceGarden(Camera cam)
        {
            var sky = new Color(0.56f, 0.70f, 0.82f);
            SetupLighting(cam, sky);
            cam.backgroundColor = sky;
            RenderSettings.fogColor = new Color(0.70f, 0.82f, 0.90f);
            RenderSettings.fogStartDistance = 200f;
            RenderSettings.fogEndDistance = 700f;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.76f, 0.90f);
            RenderSettings.ambientEquatorColor = new Color(0.52f, 0.66f, 0.80f);
            RenderSettings.ambientGroundColor = new Color(0.30f, 0.36f, 0.44f);
            DirLight("Sun", new Color(0.82f, 0.91f, 1f), 1.12f, new Vector3(48f, 22f, 0f), true);
            DirLight("Fill", new Color(0.48f, 0.66f, 0.88f), 0.36f, new Vector3(55f, -95f, 0f), false);
            DirLight("Rim", new Color(0.78f, 0.70f, 0.95f), 0.24f, new Vector3(18f, 155f, 0f), false);
        }

        /// <summary>Funfair carnival: amber key, rose fill, gold rim. Not Harbor afternoon, not Crystal ice.</summary>
        public static void RigCarnival(Camera cam)
        {
            var sky = new Color(0.78f, 0.50f, 0.58f);
            SetupLighting(cam, sky);
            cam.backgroundColor = sky;
            RenderSettings.fogColor = new Color(0.88f, 0.60f, 0.46f);
            RenderSettings.fogStartDistance = 220f;
            RenderSettings.fogEndDistance = 720f;
            RenderSettings.ambientSkyColor = new Color(0.90f, 0.56f, 0.46f);
            RenderSettings.ambientEquatorColor = new Color(0.94f, 0.46f, 0.56f);
            RenderSettings.ambientGroundColor = new Color(0.32f, 0.18f, 0.14f);
            DirLight("Sun", new Color(1f, 0.76f, 0.40f), 1.38f, new Vector3(42f, 48f, 0f), true);
            DirLight("Fill", new Color(0.92f, 0.36f, 0.58f), 0.40f, new Vector3(52f, -108f, 0f), false);
            DirLight("Rim", new Color(1f, 0.86f, 0.30f), 0.50f, new Vector3(20f, 158f, 0f), false);
        }

        /// <summary>Rooftop dusk/neon: low amber key, cyan fill, gold rim. Not Harbor afternoon, not carnival.</summary>
        public static void RigNeon(Camera cam)
        {
            var sky = new Color(0.22f, 0.16f, 0.38f);
            SetupLighting(cam, sky);
            cam.backgroundColor = sky;
            RenderSettings.fogColor = new Color(0.42f, 0.22f, 0.48f);
            RenderSettings.fogStartDistance = 180f;
            RenderSettings.fogEndDistance = 640f;
            RenderSettings.ambientSkyColor = new Color(0.38f, 0.24f, 0.62f);
            RenderSettings.ambientEquatorColor = new Color(0.86f, 0.42f, 0.28f);
            RenderSettings.ambientGroundColor = new Color(0.16f, 0.12f, 0.18f);
            DirLight("Sun", new Color(1f, 0.52f, 0.28f), 1.05f, new Vector3(18f, 52f, 0f), true);
            DirLight("Fill", new Color(0.22f, 0.72f, 0.95f), 0.48f, new Vector3(55f, -110f, 0f), false);
            DirLight("Rim", new Color(1f, 0.78f, 0.22f), 0.55f, new Vector3(12f, 165f, 0f), false);
        }

        /// <summary>Jungle canopy: dappled green key, moss fill, gold-leaf rim. Not Harbor afternoon.</summary>
        public static void RigCanopy(Camera cam)
        {
            var sky = new Color(0.38f, 0.58f, 0.42f);
            SetupLighting(cam, sky);
            cam.backgroundColor = sky;
            RenderSettings.fogColor = new Color(0.32f, 0.48f, 0.28f);
            RenderSettings.fogStartDistance = 160f;
            RenderSettings.fogEndDistance = 580f;
            RenderSettings.ambientSkyColor = new Color(0.42f, 0.62f, 0.38f);
            RenderSettings.ambientEquatorColor = new Color(0.36f, 0.48f, 0.22f);
            RenderSettings.ambientGroundColor = new Color(0.18f, 0.16f, 0.10f);
            DirLight("Sun", new Color(0.92f, 0.95f, 0.62f), 1.18f, new Vector3(62f, 28f, 0f), true);
            DirLight("Fill", new Color(0.28f, 0.52f, 0.32f), 0.38f, new Vector3(48f, -88f, 0f), false);
            DirLight("Rim", new Color(0.72f, 0.88f, 0.38f), 0.32f, new Vector3(22f, 148f, 0f), false);
        }

        /// <summary>Ember courtyard: fire key, warm fill, gold rim. Night-ready even in day. Not Harbor afternoon.</summary>
        public static void RigCourtyard(Camera cam)
        {
            var sky = new Color(0.28f, 0.14f, 0.16f);
            SetupLighting(cam, sky);
            cam.backgroundColor = sky;
            RenderSettings.fogColor = new Color(0.42f, 0.18f, 0.12f);
            RenderSettings.fogStartDistance = 160f;
            RenderSettings.fogEndDistance = 620f;
            RenderSettings.ambientSkyColor = new Color(0.48f, 0.22f, 0.16f);
            RenderSettings.ambientEquatorColor = new Color(0.72f, 0.32f, 0.14f);
            RenderSettings.ambientGroundColor = new Color(0.14f, 0.08f, 0.08f);
            DirLight("Sun", new Color(1f, 0.58f, 0.28f), 1.22f, new Vector3(32f, 38f, 0f), true);
            DirLight("Fill", new Color(0.95f, 0.28f, 0.12f), 0.44f, new Vector3(58f, -96f, 0f), false);
            DirLight("Rim", new Color(1f, 0.72f, 0.22f), 0.52f, new Vector3(18f, 162f, 0f), false);
        }

        /// <summary>Harbor night: moon key, cool fill, stadium rim. Fireworks are ParkView, not a skybox.</summary>
        public static void RigHarborNight(Camera cam)
        {
            var sky = new Color(0.07f, 0.10f, 0.20f);
            SetupLighting(cam, sky);
            cam.backgroundColor = sky;
            RenderSettings.fogColor = new Color(0.10f, 0.14f, 0.22f);
            RenderSettings.fogStartDistance = 200f;
            RenderSettings.fogEndDistance = 700f;
            RenderSettings.ambientSkyColor = new Color(0.14f, 0.18f, 0.32f);
            RenderSettings.ambientEquatorColor = new Color(0.28f, 0.26f, 0.22f);
            RenderSettings.ambientGroundColor = new Color(0.10f, 0.10f, 0.08f);
            DirLight("Sun", new Color(0.72f, 0.80f, 1f), 0.42f, new Vector3(28f, 50f, 0f), true);
            DirLight("Fill", new Color(0.35f, 0.42f, 0.62f), 0.22f, new Vector3(58f, -78f, 0f), false);
            DirLight("Rim", new Color(1f, 0.78f, 0.42f), 0.28f, new Vector3(16f, 168f, 0f), false);
        }

        /// <summary>Crystal night blackout: almost no key. Follow-spot on the ball is the light.</summary>
        public static void RigIceGardenNight(Camera cam)
        {
            var sky = new Color(0.03f, 0.04f, 0.08f);
            SetupLighting(cam, sky);
            cam.backgroundColor = sky;
            RenderSettings.fogColor = new Color(0.04f, 0.05f, 0.08f);
            RenderSettings.fogStartDistance = 70f;
            RenderSettings.fogEndDistance = 380f;
            RenderSettings.ambientSkyColor = new Color(0.05f, 0.07f, 0.12f);
            RenderSettings.ambientEquatorColor = new Color(0.06f, 0.08f, 0.12f);
            RenderSettings.ambientGroundColor = new Color(0.03f, 0.04f, 0.06f);
            DirLight("Sun", new Color(0.30f, 0.38f, 0.52f), 0.06f, new Vector3(48f, 22f, 0f), false);
            DirLight("Fill", new Color(0.18f, 0.24f, 0.38f), 0.04f, new Vector3(55f, -95f, 0f), false);
            DirLight("Rim", new Color(0.40f, 0.50f, 0.70f), 0.05f, new Vector3(18f, 155f, 0f), false);
        }

        /// <summary>Funfair night: dark carnival, amber key, rose fill. Chompers are ParkView.</summary>
        public static void RigCarnivalNight(Camera cam)
        {
            var sky = new Color(0.12f, 0.06f, 0.14f);
            SetupLighting(cam, sky);
            cam.backgroundColor = sky;
            RenderSettings.fogColor = new Color(0.28f, 0.10f, 0.16f);
            RenderSettings.fogStartDistance = 180f;
            RenderSettings.fogEndDistance = 640f;
            RenderSettings.ambientSkyColor = new Color(0.32f, 0.12f, 0.22f);
            RenderSettings.ambientEquatorColor = new Color(0.55f, 0.18f, 0.28f);
            RenderSettings.ambientGroundColor = new Color(0.14f, 0.08f, 0.08f);
            DirLight("Sun", new Color(1f, 0.55f, 0.22f), 0.55f, new Vector3(42f, 48f, 0f), true);
            DirLight("Fill", new Color(0.92f, 0.22f, 0.48f), 0.32f, new Vector3(52f, -108f, 0f), false);
            DirLight("Rim", new Color(1f, 0.82f, 0.22f), 0.46f, new Vector3(20f, 158f, 0f), false);
        }

        /// <summary>Ember night: hotter fire key, gold rim. Amp of the day courtyard, not a skybox.</summary>
        public static void RigCourtyardNight(Camera cam)
        {
            var sky = new Color(0.12f, 0.04f, 0.06f);
            SetupLighting(cam, sky);
            cam.backgroundColor = sky;
            RenderSettings.fogColor = new Color(0.48f, 0.16f, 0.08f);
            RenderSettings.fogStartDistance = 140f;
            RenderSettings.fogEndDistance = 560f;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.22f, 0.10f);
            RenderSettings.ambientEquatorColor = new Color(0.90f, 0.32f, 0.10f);
            RenderSettings.ambientGroundColor = new Color(0.12f, 0.05f, 0.04f);
            DirLight("Sun", new Color(1f, 0.42f, 0.12f), 1.55f, new Vector3(32f, 38f, 0f), true);
            DirLight("Fill", new Color(1f, 0.22f, 0.06f), 0.62f, new Vector3(58f, -96f, 0f), false);
            DirLight("Rim", new Color(1f, 0.78f, 0.18f), 0.72f, new Vector3(18f, 162f, 0f), false);
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

using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace GrandSluggers.UnityClient
{
    public static class Look
    {
        static Shader _lit;
        static Texture2D _grass, _dirt, _crowd, _rio;

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

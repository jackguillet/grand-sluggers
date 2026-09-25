using System.Collections.Generic;
using System.IO;
using GrandSluggers.Sim;
using UnityEngine;
using UnityEngine.Rendering;

namespace GrandSluggers.UnityClient
{
    public static class Look
    {
        static Shader _lit, _toon;
        static Texture2D _grass, _dirt, _crowd, _white;
        static readonly Dictionary<string, Texture2D> _generated = new Dictionary<string, Texture2D>();

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

        /// <summary>The captain's portrait the skins catalog names (data/art/skins.json <c>portrait</c>); none is null.</summary>
        public static bool HasPortrait(string id) => !string.IsNullOrEmpty(id) && ArtBinder.HasPortrait(id);

        public static Texture2D Portrait(string id) => LoadPortraitFile(id);

        /// <summary>Every drafted head gets a face. Role players reuse the faction captain.</summary>
        public static Texture2D Portrait(Character who)
        {
            if (who == null) return null;
            var own = LoadPortraitFile(who.Id);
            if (own != null) return own;
            var cap = LoadPortraitFile(Silhouette.PortraitId(who));
            if (cap != null) return cap;
            return GeneratedHead(who);
        }

        static Texture2D LoadPortraitFile(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            var tex = ArtBinder.LoadPortrait(id);
            if (tex == null) return null;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            return tex;
        }

        static Texture2D GeneratedHead(Character who)
        {
            var key = who.Faction + ":" + Silhouette.PortraitId(who);
            if (_generated.TryGetValue(key, out var tex) && tex != null) return tex;
            var spec = Silhouette.Proportions(who);
            const int n = 64;
            tex = new Texture2D(n, n, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var body = Colors.Body(who.Faction);
            var skin = Colors.SkinTone(who.Faction);
            var ink = new Color(0.12f, 0.10f, 0.12f, 1f);
            var cx = 31.5f;
            var cy = 34f;
            var rx = 16f * Mathf.Clamp(spec.Width, 0.7f, 1.6f);
            var ry = 18f * Mathf.Clamp(spec.Head, 0.9f, 1.8f);
            var pixels = new Color[n * n];
            for (var y = 0; y < n; y++)
            {
                for (var x = 0; x < n; x++)
                {
                    var dx = (x - cx) / rx;
                    var dy = (y - cy) / ry;
                    var col = body;
                    if (dx * dx + dy * dy <= 1f)
                    {
                        col = skin;
                        var eyeY = cy + ry * 0.12f;
                        var eyeX = rx * 0.32f;
                        var el = Dist2(x, y, cx - eyeX, eyeY) < 6.5f;
                        var er = Dist2(x, y, cx + eyeX, eyeY) < 6.5f;
                        if (el || er) col = ink;
                    }
                    pixels[y * n + x] = col;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            _generated[key] = tex;
            return tex;
        }

        static float Dist2(float x, float y, float ax, float ay)
        {
            var dx = x - ax;
            var dy = y - ay;
            return dx * dx + dy * dy;
        }

        public static Material Lit(Color color, Texture tex = null, float tile = 1f, float smooth = 0.22f)
        {
            var m = new Material(LitShader);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            else m.color = color;
            // URP Lit on llvmpipe treats a missing base map as white and
            // drops _BaseColor — Harbor town became four untextured boxes.
            var map = tex != null ? tex : White;
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", map);
            m.mainTexture = map;
            m.mainTextureScale = new Vector2(tile, tile);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
            return m;
        }

        static Texture2D White
        {
            get
            {
                if (_white == null)
                {
                    _white = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                    _white.SetPixel(0, 0, Color.white);
                    _white.Apply();
                    _white.wrapMode = TextureWrapMode.Repeat;
                    _white.filterMode = FilterMode.Point;
                    _white.name = "LookWhite";
                }
                return _white;
            }
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

        /// <summary>Kit props (Harbor wood, roofs, crowd figures, toys): the brightened fill in matte Lit. Character bodies draw in <see cref="Body"/>.</summary>
        public static Material Toon(Color color)
        {
            var c = Color.Lerp(color, Color.white, 0.08f);
            c = new Color(Mathf.Min(1f, c.r * 1.18f), Mathf.Min(1f, c.g * 1.12f), Mathf.Min(1f, c.b * 1.08f), 1f);
            // ToonFill is a built-in CG shader with a single SRPDefaultUnlit pass: no
            // DepthOnly, DepthNormals or ShadowCaster pass. The kit keeps matte Lit;
            // characters use ToonRim, the real URP toon (Body).
            return Lit(c, smooth: 0.04f);
        }

        static Shader _toonRim;
        static bool _toonRimMissing;

        /// <summary>The character toon (CF-7), shipped in Resources so a player build carries it.</summary>
        public static Shader ToonRimShader
        {
            get
            {
                if (_toonRim != null || _toonRimMissing) return _toonRim;
                _toonRim = Resources.Load<Shader>("Shaders/ToonRim") ?? Shader.Find("GrandSluggers/ToonRim");
                if (_toonRim == null || !_toonRim.isSupported)
                {
                    Debug.LogWarning("ToonRim shader is missing or unsupported; character bodies draw in matte Lit");
                    _toonRim = null;
                    _toonRimMissing = true;
                }
                return _toonRim;
            }
        }

        /// <summary>
        /// A character's jersey, trim or flesh (CF-7, CH-14): the ToonRim bands and rim, every number from
        /// <c>data/art/toon.json</c>. With no catalog bound or no shader it is the matte Lit the body drew before.
        /// </summary>
        public static Material Body(Color color)
        {
            var toon = ArtBinder.Art?.Toon;
            var sh = ToonRimShader;
            if (toon == null || sh == null) return Toon(color);
            var (r, g, b) = toon.Fill.Of(color.r, color.g, color.b);
            var m = new Material(sh) { name = "toon-body" };
            m.SetColor("_BaseColor", new Color((float)r, (float)g, (float)b, 1f));
            var l = toon.Light;
            m.SetColor("_ShadeTint", Of(l.Shade));
            m.SetFloat("_Wrap", (float)l.Wrap);
            m.SetFloat("_BandAt", (float)l.BandAt);
            m.SetFloat("_BandSoft", (float)l.BandSoft);
            m.SetFloat("_ShadowWeight", (float)l.Shadow);
            m.SetFloat("_SunFull", (float)l.SunFull);
            m.SetFloat("_SunTint", (float)l.SunTint);
            m.SetFloat("_Ambient", (float)l.Ambient);
            var rim = toon.Rim;
            m.SetColor("_RimColor", Of(rim.Color));
            m.SetFloat("_RimAt", (float)rim.At);
            m.SetFloat("_RimSoft", (float)rim.Soft);
            m.SetFloat("_RimStrength", (float)rim.Strength);
            m.SetFloat("_RimUp", (float)rim.Up);
            return m;
        }

        public static Material Unlit(Color color, Texture2D tex = null)
        {
            var sh = Shader.Find("Universal Render Pipeline/Unlit") ?? LitShader;
            var m = new Material(sh);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            else m.color = color;
            var map = tex != null ? tex : White;
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", map);
            m.mainTexture = map;
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

        public static GameObject Solid(string name, Transform parent, Vector3[] verts, int[] tris, Material mat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var mf = go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            var mesh = new Mesh { name = name };
            mesh.vertices = verts;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mf.sharedMesh = mesh;
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return go;
        }

        /// <summary>Annulus you can stand in. A scaled cylinder is a pancake.</summary>
        public static GameObject Torus(string name, Transform parent, float major, float minor, Material mat, int seg = 28, int sides = 10)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var mf = go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            mf.sharedMesh = BuildTorus(major, minor, seg, sides);
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return go;
        }

        static Mesh BuildTorus(float major, float minor, int seg, int sides)
        {
            seg = Mathf.Max(8, seg);
            sides = Mathf.Max(6, sides);
            var verts = new Vector3[(seg + 1) * (sides + 1)];
            var tris = new int[seg * sides * 6];
            for (var i = 0; i <= seg; i++)
            {
                var theta = i * Mathf.PI * 2f / seg;
                var ct = Mathf.Cos(theta);
                var st = Mathf.Sin(theta);
                for (var j = 0; j <= sides; j++)
                {
                    var phi = j * Mathf.PI * 2f / sides;
                    var cp = Mathf.Cos(phi);
                    var sp = Mathf.Sin(phi);
                    verts[i * (sides + 1) + j] =
                        new Vector3(ct * (major + minor * cp), minor * sp, st * (major + minor * cp));
                }
            }
            var t = 0;
            for (var i = 0; i < seg; i++)
            {
                for (var j = 0; j < sides; j++)
                {
                    var a = i * (sides + 1) + j;
                    var b = a + sides + 1;
                    // Clockwise from outside: Unity culls CCW, which hid the tube
                    // from the plate 3/4 (only the hole's inner wall was front-facing).
                    tris[t++] = a;
                    tris[t++] = b;
                    tris[t++] = a + 1;
                    tris[t++] = a + 1;
                    tris[t++] = b;
                    tris[t++] = b + 1;
                }
            }
            var mesh = new Mesh { name = "Torus" };
            mesh.vertices = verts;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
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

        /// <summary>
        /// A park's sky and light from data (FD-16, FR-04; F6-c): the rows its kit names in data/art/looks.json, at night the
        /// night row where there is one. What SetupLighting sets first, each row then overrides, as every rig did. An empty
        /// slot keeps the greybox default: the plain sky and SetupLighting's sun.
        /// </summary>
        public static void Apply(Camera cam, SkyLook sky, LightLook light, bool night)
        {
            var s = sky?.At(night);
            SetupLighting(cam, s != null ? Of(s.Color) : Colors.Sky);
            if (s != null)
            {
                cam.backgroundColor = Of(s.Color);
                RenderSettings.fogColor = Of(s.Fog.Color);
                RenderSettings.fogStartDistance = (float)s.Fog.Start;
                RenderSettings.fogEndDistance = (float)s.Fog.End;
            }
            var l = light?.At(night);
            if (l == null) return;
            RenderSettings.ambientSkyColor = Of(l.AmbientSky);
            RenderSettings.ambientEquatorColor = Of(l.AmbientEquator);
            RenderSettings.ambientGroundColor = Of(l.AmbientGround);
            DirLight("Sun", l.Sun);
            DirLight("Fill", l.Fill);
            DirLight("Rim", l.Rim);
        }

        /// <summary>A data color as a Unity color: a hex the way Colors.Hex reads it, numbers as written.</summary>
        public static Color Of(LookColor c) =>
            c.Hex is int hex ? Colors.Hex(hex) : new Color((float)c.R, (float)c.G, (float)c.B, 1f);

        /// <summary>A greybox surface of a park's palette: its color, its tiled texture if it names one, its smoothness.</summary>
        public static Material Lit(LookSurface s) =>
            Lit(Of(s.Color), s.Texture == "grass" ? Grass : s.Texture == "dirt" ? Dirt : null, (float)s.Tile, (float)s.Smooth);

        static void DirLight(string name, LookLight l) =>
            DirLight(name, Of(l.Color), (float)l.Intensity, new Vector3((float)l.EulerX, (float)l.EulerY, (float)l.EulerZ), l.Shadows);

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

using UnityEngine;

namespace GrandSluggers.UnityClient
{
    /// <summary>
    /// World-space title sticker. Block letters so the player and still-gate
    /// both read GRAND SLUGGERS — TextMesh is editor-only on this URP path.
    /// Faces the title camera, not the park.
    /// </summary>
    public sealed class LogoToy : MonoBehaviour
    {
        bool _built;

        public static LogoToy Attach(Transform parent)
        {
            var go = new GameObject("TitleSticker");
            go.transform.SetParent(parent, false);
            return go.AddComponent<LogoToy>();
        }

        public void Show(string copy, Vector3 at, Vector3 cameraAt)
        {
            if (!_built) Build(copy);
            transform.position = at;
            var toCam = cameraAt - at;
            toCam.y = 0f;
            if (toCam.sqrMagnitude < 0.01f) toCam = Vector3.back;
            transform.rotation = Quaternion.LookRotation(toCam.normalized, Vector3.up);
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (gameObject != null) gameObject.SetActive(false);
        }

        void Build(string copy)
        {
            _built = true;
            var gold = Look.Unlit(Colors.Gold);
            var ink = Look.Unlit(new Color(0.12f, 0.08f, 0.04f));
            Look.Prim(PrimitiveType.Cube, "Board", transform, Vector3.zero, new Vector3(7.2f, 2.55f, 0.14f), gold);
            Look.Prim(PrimitiveType.Cube, "Ink", transform, new Vector3(0f, 0f, 0.05f), new Vector3(6.8f, 2.2f, 0.08f), ink);
            Look.Prim(PrimitiveType.Cube, "Star", transform, new Vector3(-3.15f, 0.55f, 0.11f), new Vector3(0.42f, 0.42f, 0.1f), gold);
            Stamp(copy ?? "", gold);
        }

        void Stamp(string copy, Material gold)
        {
            var lines = copy.Split(' ');
            const float px = 0.11f;
            const float gap = 0.08f;
            var step = 5f * px + gap;
            var lineH = 5f * px + 0.2f;
            var top = (lines.Length - 1) * 0.5f * lineH;
            for (var li = 0; li < lines.Length; li++)
            {
                var line = lines[li];
                if (string.IsNullOrEmpty(line)) continue;
                var w = line.Length * step;
                var x0 = -w * 0.5f + 2f * px;
                var y = top - li * lineH;
                for (var i = 0; i < line.Length; i++)
                    Glyph(line[i], new Vector3(x0 + i * step, y, 0.11f), px, gold);
            }
        }

        void Glyph(char c, Vector3 origin, float px, Material mat)
        {
            var bits = Bits(c);
            if (bits == 0) return;
            for (var row = 0; row < 5; row++)
            {
                for (var col = 0; col < 5; col++)
                {
                    if ((bits & (1 << (24 - row * 5 - col))) == 0) continue;
                    var p = origin + new Vector3((col - 2) * px, (2 - row) * px, 0f);
                    Look.Prim(PrimitiveType.Cube, "Px", transform, p, new Vector3(px * 0.88f, px * 0.88f, 0.08f), mat);
                }
            }
        }

        static int Bits(char c) => char.ToUpperInvariant(c) switch
        {
            'A' => 0b01110_10001_11111_10001_10001,
            'D' => 0b11110_10001_10001_10001_11110,
            'E' => 0b11111_10000_11110_10000_11111,
            'G' => 0b01111_10000_10111_10001_01110,
            'L' => 0b10000_10000_10000_10000_11111,
            'N' => 0b10001_11001_10101_10011_10001,
            'R' => 0b11110_10001_11110_10100_10010,
            'S' => 0b01111_10000_01110_00001_11110,
            'U' => 0b10001_10001_10001_10001_01110,
            _ => 0
        };
    }
}

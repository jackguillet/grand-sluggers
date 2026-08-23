using System.Collections.Generic;
using System.IO;
using GrandSluggers.Sim;
using UnityEditor;
using UnityEngine;

namespace GrandSluggers.EditorTools
{
    public static class ArtRailsValidate
    {
        [MenuItem("Grand Sluggers/Validate Art Rails")]
        public static void Run()
        {
            var data = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "data"));
            var content = ContentCatalog.Load(data);
            var created = EnsureFolders(content.Art.Folders);
            AssetDatabase.Refresh();
            var errors = content.Art.Validate(content);
            if (created.Count > 0)
            {
                Debug.Log("Grand Sluggers art rails: created " + created.Count + " folders\n" + string.Join("\n", created));
            }
            if (errors.Count == 0)
            {
                Debug.Log("Grand Sluggers art rails OK. " + content.Art.Clips.Count + " clips, "
                    + content.Art.Skins.Count + " captain skins, " + content.Art.Parks.Count + " park kits.");
                return;
            }
            foreach (var e in errors)
                Debug.LogError("Art rails: " + e);
        }

        static List<string> EnsureFolders(IReadOnlyList<string> folders)
        {
            var created = new List<string>();
            var assets = Path.GetFullPath(Application.dataPath);
            var unityRoot = Path.GetDirectoryName(assets);
            foreach (var rel in folders)
            {
                var abs = Path.GetFullPath(Path.Combine(unityRoot, rel));
                if (Directory.Exists(abs)) continue;
                Directory.CreateDirectory(abs);
                created.Add(rel);
            }
            return created;
        }
    }
}

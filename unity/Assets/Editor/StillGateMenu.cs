using System.IO;
using GrandSluggers.Sim;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GrandSluggers.EditorTools
{
    public static class StillGateMenu
    {
        const string ScenePath = "Assets/Scenes/HarborDiamond.unity";

        [MenuItem("Grand Sluggers/Capture Still Gate")]
        public static void Capture()
        {
            var temp = Path.Combine(Directory.GetParent(Application.dataPath)!.FullName, "Temp");
            Directory.CreateDirectory(temp);
            var path = StillRequest.RequestPath(temp);
            if (!File.Exists(path))
            {
                File.WriteAllText(path,
                    "{\"shots\":[\"title\",\"plate\",\"mound\",\"diamond-grounder\",\"smash\"],\"home\":\"rio\",\"away\":\"ashlord\",\"hudOff\":true,\"charge01\":1}");
            }
            try { File.Delete(StillRequest.DonePath(temp)); }
            catch { /* first run */ }
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                EditorApplication.delayCall += () => EditorApplication.isPlaying = true;
                Debug.Log("Grand Sluggers still gate: restarting Play. Request at " + path);
                return;
            }
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath)
                EditorSceneManager.OpenScene(ScenePath);
            EditorApplication.isPlaying = true;
            Debug.Log("Grand Sluggers still gate: Play. PNGs → " + Path.Combine(temp, StillRequest.DefaultOutFolder));
        }
    }
}

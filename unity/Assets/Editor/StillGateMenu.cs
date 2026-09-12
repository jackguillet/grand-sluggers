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
        const string RequestFileEnvironment = "GS_STILL_REQUEST_FILE";

        [MenuItem("Grand Sluggers/Capture Request File")]
        public static void CaptureRequestFile()
        {
            var source = System.Environment.GetEnvironmentVariable(RequestFileEnvironment);
            try
            {
                var json = StillRequest.ReadValidatedJsonFile(source);
                var temp = Path.Combine(Directory.GetParent(Application.dataPath)!.FullName, "Temp");
                Directory.CreateDirectory(temp);
                var staged = StillRequest.RequestPath(temp);
                File.WriteAllText(staged, json);
                Debug.Log("Grand Sluggers still gate: validated " + source + " → " + staged);
                Capture();
            }
            catch (System.Exception ex)
            {
                Debug.LogError("Grand Sluggers still gate: " + RequestFileEnvironment + " is invalid: " + ex.Message);
            }
        }

        [MenuItem("Grand Sluggers/Capture Still Gate")]
        public static void Capture()
        {
            var temp = Path.Combine(Directory.GetParent(Application.dataPath)!.FullName, "Temp");
            Directory.CreateDirectory(temp);
            var path = StillRequest.RequestPath(temp);
            if (!File.Exists(path))
            {
                File.WriteAllText(path,
                    "{\"shots\":[\"title\",\"select\",\"lineup\",\"plate\",\"pitch\",\"mound\",\"diamond-grounder\",\"smash\"],\"home\":\"rio\",\"away\":\"ashlord\",\"hudOff\":true,\"charge01\":1}");
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

        [MenuItem("Grand Sluggers/Capture Character Stills")]
        public static void CaptureCharacters()
        {
            var temp = Path.Combine(Directory.GetParent(Application.dataPath)!.FullName, "Temp");
            Directory.CreateDirectory(temp);
            File.WriteAllText(StillRequest.RequestPath(temp),
                "{\"shots\":[\"char-rest\",\"char-pose\"],\"home\":\"fenn\",\"away\":\"rio\",\"hudOff\":true,\"width\":1920,\"height\":1080}");
            Capture();
        }

        [MenuItem("Grand Sluggers/Capture Swing Matrix")]
        public static void CaptureSwings()
        {
            var temp = Path.Combine(Directory.GetParent(Application.dataPath)!.FullName, "Temp");
            Directory.CreateDirectory(temp);
            var outDir = System.Environment.GetEnvironmentVariable("GS_SWING_STILLS");
            var outJson = string.IsNullOrWhiteSpace(outDir)
                ? ""
                : ",\"outDir\":\"" + outDir.Replace("\\", "/").Replace("\"", "'") + "\"";
            File.WriteAllText(StillRequest.RequestPath(temp),
                "{\"shots\":[\"swing-matrix\"],\"hudOff\":true,\"width\":1920,\"height\":1080"
                + outJson + "}");
            Capture();
        }
    }
}

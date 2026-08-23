using System.IO;
using GrandSluggers.UnityClient;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;


namespace GrandSluggers.EditorTools
{
    public static class ProjectBootstrap
    {
        const string ScenePath = "Assets/Scenes/HarborDiamond.unity";
        const string PipelinePath = "Assets/Settings/URP.asset";
        const string RendererPath = "Assets/Settings/URPRenderer.asset";

        [MenuItem("Grand Sluggers/Bootstrap Scene")]
        public static void Run()
        {
            Directory.CreateDirectory("Assets/Settings");
            Directory.CreateDirectory("Assets/Scenes");

            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, RendererPath);
            }

            var urp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (urp == null)
            {
                urp = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(urp, PipelinePath);
            }
            GraphicsSettings.defaultRenderPipeline = urp;
            QualitySettings.renderPipeline = urp;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var lightGo = new GameObject("Sun");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.15f;
            lightGo.transform.rotation = Quaternion.Euler(50f, 30f, 0f);

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.46f, 0.73f, 0.91f);
            cam.fieldOfView = 48f;
            camGo.transform.position = new Vector3(0, 95, -40);
            camGo.transform.LookAt(new Vector3(0, 0, 140));
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<UniversalAdditionalCameraData>();

            var director = new GameObject("MatchDirector");
            director.AddComponent<MatchDirector>();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("Grand Sluggers: HarborDiamond scene + URP ready. Press Play.");
        }

        // Invoked from batchmode: -executeMethod GrandSluggers.EditorTools.ProjectBootstrap.RunAndQuit
        public static void RunAndQuit()
        {
            Run();
            EditorApplication.Exit(0);
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GrandSluggers.Sim;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GrandSluggers.EditorTools
{
    /// <summary>Configured-runner import and Harbor scene gate. It does not play the game.</summary>
    public static class ValidationGate
    {
        const string ScenePath = "Assets/Scenes/HarborDiamond.unity";

        public static void Run()
        {
            var revision = Environment.GetEnvironmentVariable("GS_VALIDATION_REVISION") ?? "";
            var evidencePath = Environment.GetEnvironmentVariable("GS_VALIDATION_EVIDENCE") ??
                Path.Combine(Path.GetDirectoryName(Application.dataPath)!, "Temp", "unity-validation.json");
            var evidence = new Evidence
            {
                revision = revision,
                unityVersion = Application.unityVersion,
                scene = ScenePath,
                utc = DateTime.UtcNow.ToString("O")
            };

            try
            {
                if (revision.Length != 40 || revision.Any(c => !Uri.IsHexDigit(c)))
                    throw new BuildFailedException("GS_VALIDATION_REVISION must be the validated 40-character Git revision.");

                var expectedVersion = Environment.GetEnvironmentVariable("GS_VALIDATION_UNITY_VERSION") ?? "";
                if (string.IsNullOrWhiteSpace(expectedVersion)
                    || !Application.unityVersion.Equals(expectedVersion, StringComparison.Ordinal))
                    throw new BuildFailedException("Unity " + Application.unityVersion
                        + " does not match the tracked project editor version " + expectedVersion + ".");

                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                RequireCompiledSources(evidence);

                var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
                if (sceneAsset == null)
                    throw new BuildFailedException("Harbor validation scene did not import: " + ScenePath);
                var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                if (!scene.IsValid() || !scene.isLoaded)
                    throw new BuildFailedException("Harbor validation scene did not open: " + ScenePath);

                var data = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "data"));
                var content = ContentCatalog.Load(data);
                var artErrors = content.Art.Validate(content);
                evidence.artErrors = artErrors.ToArray();
                if (artErrors.Count > 0)
                    throw new BuildFailedException("Art validation failed:\n" + string.Join("\n", artErrors));

                var packageErrors = CharacterPackageImportValidation.Validate(content);
                evidence.packageErrors = packageErrors.ToArray();
                if (packageErrors.Count > 0)
                    throw new BuildFailedException("Character package import validation failed:\n"
                        + string.Join("\n", packageErrors));

                evidence.ok = true;
                WriteEvidence(evidencePath, evidence);
                Debug.Log("Grand Sluggers Unity validation OK for " + revision + " at " + ScenePath);
            }
            catch (Exception ex)
            {
                evidence.ok = false;
                evidence.error = ex.Message;
                WriteEvidence(evidencePath, evidence);
                throw new BuildFailedException("Grand Sluggers Unity validation failed for " + revision + ": " + ex.Message);
            }
        }

        static void RequireCompiledSources(Evidence evidence)
        {
            var assemblies = CompilationPipeline.GetAssemblies(AssembliesType.Editor);
            evidence.assemblies = assemblies.Select(a => a.name).OrderBy(x => x).ToArray();
            var required = new Dictionary<string, string[]>
            {
                ["GrandSluggers.Sim"] = new[]
                {
                    "src/GrandSluggers.Sim/StealThrow.cs",
                    "Packages/com.grandsluggers.sim/StealThrow.cs"
                },
                ["GrandSluggers.Runtime"] = new[] { "Assets/Scripts/Runtime/MatchDirector.cs" },
                ["Assembly-CSharp"] = new[] { "Assets/Scripts/MatchBootstrap.cs" },
                ["GrandSluggers.Editor"] = new[] { "Assets/Editor/ValidationGate.cs" }
            };
            foreach (var pair in required)
            {
                var assembly = assemblies.FirstOrDefault(a => a.name == pair.Key);
                if (assembly == null)
                    throw new BuildFailedException("Unity did not compile assembly " + pair.Key);
                var found = assembly.sourceFiles.Any(path => SourceMatches(path, pair.Value));
                if (!found)
                    throw new BuildFailedException(pair.Key + " omitted tracked source (expected one of "
                        + string.Join(", ", pair.Value) + ")");
            }
        }

        static bool SourceMatches(string path, IEnumerable<string> candidates)
        {
            var normalized = path.Replace('\\', '/').TrimStart('/');
            foreach (var candidate in candidates)
            {
                if (normalized.Equals(candidate, StringComparison.Ordinal)
                    || normalized.EndsWith("/" + candidate, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        static void WriteEvidence(string path, Evidence evidence)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonUtility.ToJson(evidence, true));
        }

        [Serializable]
        sealed class Evidence
        {
            public bool ok;
            public string revision = "";
            public string unityVersion = "";
            public string scene = "";
            public string utc = "";
            public string[] assemblies = Array.Empty<string>();
            public string[] artErrors = Array.Empty<string>();
            public string[] packageErrors = Array.Empty<string>();
            public string error = "";
        }
    }
}

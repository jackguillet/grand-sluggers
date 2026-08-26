using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace GrandSluggers.EditorTools
{
    /// <summary>
    /// File-drop Linux player build. Write unity/Temp/gs-player-request.json
    /// while the editor is open (edit mode). No Play, no -batchmode, no keystrokes.
    /// tools/build-player.sh is the agent entry.
    /// </summary>
    [InitializeOnLoad]
    public static class PlayerBuildGate
    {
        const string ScenePath = "Assets/Scenes/HarborDiamond.unity";
        const string RequestFile = "gs-player-request.json";
        const string DoneFile = "gs-player-done.json";
        const string RelOut = "Builds/linux/GrandSluggers.x86_64";

        static bool _busy;
        static double _next;

        static PlayerBuildGate()
        {
            EditorApplication.update += Tick;
        }

        [MenuItem("Grand Sluggers/Build Linux Player")]
        public static void MenuBuild()
        {
            var temp = TempDir();
            Directory.CreateDirectory(temp);
            var req = Path.Combine(temp, RequestFile);
            if (!File.Exists(req))
                File.WriteAllText(req, "{\"target\":\"linux\",\"width\":1280,\"height\":800,\"development\":true}");
            try { File.Delete(Path.Combine(temp, DoneFile)); } catch { /* first run */ }
        }

        static void Tick()
        {
            if (_busy) return;
            if (EditorApplication.timeSinceStartup < _next) return;
            _next = EditorApplication.timeSinceStartup + 0.4;

            var temp = TempDir();
            var req = Path.Combine(temp, RequestFile);
            if (!File.Exists(req)) return;

            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                Debug.Log("Grand Sluggers player gate: leaving Play so the Linux build can run.");
                return;
            }

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                return;

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneLinux64)
            {
                Debug.Log("Grand Sluggers player gate: switching to StandaloneLinux64.");
                EditorUserBuildSettings.SwitchActiveBuildTarget(NamedBuildTarget.Standalone, BuildTarget.StandaloneLinux64);
                return;
            }

            _busy = true;
            try { Build(req, temp); }
            catch (Exception ex)
            {
                WriteDone(temp, false, "", ex.Message);
                try { File.Delete(req); } catch { /* leftover is ok */ }
            }
            finally { _busy = false; }
        }

        static void Build(string reqPath, string temp)
        {
            var json = File.ReadAllText(reqPath);

            var width = JsonInt(json, "width", 1280);
            var height = JsonInt(json, "height", 800);
            var development = JsonBool(json, "development", true);
            if (width < 640) width = 1280;
            if (height < 360) height = 800;

            var unityRoot = Directory.GetParent(Application.dataPath)!.FullName;
            var exe = Path.Combine(unityRoot, RelOut);
            Directory.CreateDirectory(Path.GetDirectoryName(exe)!);

            var prevMode = PlayerSettings.fullScreenMode;
            var prevW = PlayerSettings.defaultScreenWidth;
            var prevH = PlayerSettings.defaultScreenHeight;
            var prevResizable = PlayerSettings.resizableWindow;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = width;
            PlayerSettings.defaultScreenHeight = height;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            EditorUserBuildSettings.buildScriptsOnly = false;

            var opts = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = exe,
                target = BuildTarget.StandaloneLinux64,
                options = BuildOptions.CompressWithLz4
            };
            if (development) opts.options |= BuildOptions.Development;
            opts.options |= BuildOptions.CleanBuildCache;

            Debug.Log("Grand Sluggers player gate: building " + exe);
            var report = BuildPipeline.BuildPlayer(opts);
            var size = File.Exists(exe) ? new FileInfo(exe).Length : 0;
            var ok = report.summary.result == BuildResult.Succeeded && size > 1_000_000;
            var err = ok ? "" : report.summary.result + " errors=" + report.summary.totalErrors;
            if (!ok && size > 0 && size < 1_000_000)
                err = (err + " stub exe " + size + " bytes (scripts-only / Linux postprocess skipped)").Trim();
            if (!ok && !File.Exists(exe))
                err = (err + " missing exe (Unity postprocess skipped the Linux binary)").Trim();
            if (!ok && EditorApplication.isCompiling)
            {
                Debug.Log("Grand Sluggers player gate: scripts still compiling, will retry.");
                return;
            }
            try { File.Delete(reqPath); } catch { /* consumed */ }

            PlayerSettings.fullScreenMode = prevMode;
            PlayerSettings.defaultScreenWidth = prevW;
            PlayerSettings.defaultScreenHeight = prevH;
            PlayerSettings.resizableWindow = prevResizable;

            WriteDone(temp, ok, exe, err);
            Debug.Log("Grand Sluggers player gate: " + (ok ? "ok " + exe : err));
        }

        static void WriteDone(string temp, bool ok, string exe, string error)
        {
            Directory.CreateDirectory(temp);
            var json = "{\"ok\":" + (ok ? "true" : "false")
                + ",\"exe\":\"" + (exe ?? "").Replace("\\", "/")
                + "\",\"error\":\"" + (error ?? "").Replace("\"", "'") + "\"}";
            File.WriteAllText(Path.Combine(temp, DoneFile), json);
        }

        static string TempDir()
        {
            var unity = Path.GetDirectoryName(Application.dataPath);
            return Path.Combine(unity ?? Application.dataPath, "Temp");
        }

        static int JsonInt(string json, string key, int fallback)
        {
            var needle = "\"" + key + "\"";
            var i = json.IndexOf(needle, StringComparison.OrdinalIgnoreCase);
            if (i < 0) return fallback;
            var c = json.IndexOf(':', i);
            if (c < 0) return fallback;
            var s = c + 1;
            while (s < json.Length && (json[s] == ' ' || json[s] == '\t')) s++;
            var e = s;
            while (e < json.Length && (char.IsDigit(json[e]) || json[e] == '-')) e++;
            return int.TryParse(json.Substring(s, e - s), out var n) ? n : fallback;
        }

        static bool JsonBool(string json, string key, bool fallback)
        {
            var needle = "\"" + key + "\"";
            var i = json.IndexOf(needle, StringComparison.OrdinalIgnoreCase);
            if (i < 0) return fallback;
            var c = json.IndexOf(':', i);
            if (c < 0) return fallback;
            var rest = json.Substring(c + 1).TrimStart();
            if (rest.StartsWith("true", StringComparison.OrdinalIgnoreCase)) return true;
            if (rest.StartsWith("false", StringComparison.OrdinalIgnoreCase)) return false;
            return fallback;
        }
    }
}

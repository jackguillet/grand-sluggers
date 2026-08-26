using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace GrandSluggers.EditorTools
{
    /// <summary>
    /// File-drop player build. Write unity/Temp/gs-player-request.json
    /// while the editor is open (edit mode). No Play, no -batchmode, no keystrokes.
    /// tools/build-player.sh is the agent entry. target: linux | mac.
    /// </summary>
    [InitializeOnLoad]
    public static class PlayerBuildGate
    {
        const string ScenePath = "Assets/Scenes/HarborDiamond.unity";
        const string RequestFile = "gs-player-request.json";
        const string DoneFile = "gs-player-done.json";
        const string RelOutLinux = "Builds/linux/GrandSluggers.x86_64";
        const string RelOutMac = "Builds/osx/GrandSluggers.app";

        static bool _busy;
        static double _next;

        static PlayerBuildGate()
        {
            EditorApplication.update += Tick;
        }

        [MenuItem("Grand Sluggers/Build Linux Player")]
        public static void MenuBuildLinux() => WriteRequest("linux", development: true);

        [MenuItem("Grand Sluggers/Build Mac Player")]
        public static void MenuBuildMac() => WriteRequest("mac", development: false);

        static void WriteRequest(string target, bool development)
        {
            var temp = TempDir();
            Directory.CreateDirectory(temp);
            var req = Path.Combine(temp, RequestFile);
            if (!File.Exists(req))
                File.WriteAllText(req, "{\"target\":\"" + target + "\",\"width\":1280,\"height\":800,\"development\":" + (development ? "true" : "false") + "}");
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
                Debug.Log("Grand Sluggers player gate: leaving Play so the player build can run.");
                return;
            }

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                return;

            var json = File.ReadAllText(req);
            var mac = Mac(json);
            var want = mac ? BuildTarget.StandaloneOSX : BuildTarget.StandaloneLinux64;
            if (EditorUserBuildSettings.activeBuildTarget != want)
            {
                Debug.Log("Grand Sluggers player gate: switching to " + want + ".");
                EditorUserBuildSettings.SwitchActiveBuildTarget(NamedBuildTarget.Standalone, want);
                return;
            }

            _busy = true;
            try { Build(req, temp, json, mac); }
            catch (Exception ex)
            {
                WriteDone(temp, false, "", ex.Message);
                try { File.Delete(req); } catch { /* leftover is ok */ }
            }
            finally { _busy = false; }
        }

        static void Build(string reqPath, string temp, string json, bool mac)
        {
            var width = JsonInt(json, "width", 1280);
            var height = JsonInt(json, "height", 800);
            var development = JsonBool(json, "development", !mac);
            if (width < 640) width = 1280;
            if (height < 360) height = 800;

            var unityRoot = Directory.GetParent(Application.dataPath)!.FullName;
            var exe = Path.Combine(unityRoot, mac ? RelOutMac : RelOutLinux);
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
            if (mac)
                PlayerSettings.SetArchitecture(NamedBuildTarget.Standalone, 1); // Apple silicon

            var opts = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = exe,
                target = mac ? BuildTarget.StandaloneOSX : BuildTarget.StandaloneLinux64,
                options = BuildOptions.CompressWithLz4
            };
            if (development) opts.options |= BuildOptions.Development;
            opts.options |= BuildOptions.CleanBuildCache;

            Debug.Log("Grand Sluggers player gate: building " + exe);
            var report = BuildPipeline.BuildPlayer(opts);
            var ok = report.summary.result == BuildResult.Succeeded && PlayerLooksReal(exe, mac);
            var err = ok ? "" : report.summary.result + " errors=" + report.summary.totalErrors;
            if (!ok && !PlayerLooksReal(exe, mac))
                err = (err + " missing player at " + exe).Trim();
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

        static bool Mac(string json)
        {
            var t = JsonString(json, "target", "linux");
            return t.Equals("mac", StringComparison.OrdinalIgnoreCase)
                || t.Equals("osx", StringComparison.OrdinalIgnoreCase)
                || t.Equals("macos", StringComparison.OrdinalIgnoreCase);
        }

        static bool PlayerLooksReal(string exe, bool mac)
        {
            if (mac)
            {
                var bin = Path.Combine(exe, "Contents", "MacOS");
                return Directory.Exists(bin) && Directory.GetFiles(bin).Length > 0;
            }
            return File.Exists(exe);
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

        static string JsonString(string json, string key, string fallback)
        {
            var needle = "\"" + key + "\"";
            var i = json.IndexOf(needle, StringComparison.OrdinalIgnoreCase);
            if (i < 0) return fallback;
            var c = json.IndexOf(':', i);
            if (c < 0) return fallback;
            var q = json.IndexOf('"', c + 1);
            if (q < 0) return fallback;
            var e = json.IndexOf('"', q + 1);
            if (e < 0) return fallback;
            return json.Substring(q + 1, e - q - 1);
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

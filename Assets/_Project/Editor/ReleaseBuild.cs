using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Overworked.EditorTools
{
    /// <summary>
    /// Релизная сборка для игроков (Windows, .exe): без отладки — не Development Build, поэтому нет DebugHotkeys
    /// (F1–F8, пропуск этажей), HorrorDebugKeys, отладочного HUD Белой комнаты и консоли; отладка Tiny UFO — только
    /// в редакторе (#if UNITY_EDITOR). Сцены — включённые в Build Settings, первая — MainMenu (игра стартует с меню).
    /// Меню: Overworked → Build → Windows Release. Из командной строки (Unity закрыт):
    /// Unity.exe -batchmode -quit -projectPath . -executeMethod Overworked.EditorTools.ReleaseBuild.BuildWindows
    /// </summary>
    public static class ReleaseBuild
    {
        const string OutputDir = "Builds/Windows";
        const string ExeName = "Overworked.exe";
        const string FirstScene = "Assets/_Project/Scenes/MainMenu.unity";

        [MenuItem("Overworked/Build/Windows Release")]
        public static void BuildWindows()
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToList();
            // Игра всегда начинается с главного меню, даже если порядок в Build Settings сбит.
            scenes.Remove(FirstScene);
            scenes.Insert(0, FirstScene);

            // Песочницы в релиз не идут — там тестовые клавиши и сборщики.
            scenes.RemoveAll(s => s.Contains("/Sandbox/"));

            Directory.CreateDirectory(OutputDir);
            var options = new BuildPlayerOptions
            {
                scenes = scenes.ToArray(),
                locationPathName = Path.Combine(OutputDir, ExeName),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,   // не Development: Debug.isDebugBuild = false
            };
            EditorUserBuildSettings.development = false;
            EditorUserBuildSettings.allowDebugging = false;
            EditorUserBuildSettings.connectProfiler = false;

            Debug.Log("[Build] сцены: " + string.Join(", ", scenes));
            var report = BuildPipeline.BuildPlayer(options);
            var s = report.summary;
            Debug.Log($"[Build] {s.result}: {s.outputPath}, {s.totalSize / (1024 * 1024)} МБ, {s.totalErrors} ошибок, {s.totalTime}");
            if (Application.isBatchMode && s.result != BuildResult.Succeeded) EditorApplication.Exit(1);
        }
    }
}

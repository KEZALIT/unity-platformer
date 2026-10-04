using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Сборка игры в обычную программу для Windows — её можно запускать без Unity.
/// Меню Platformer → «Собрать игру для Windows»: папка Build/UnityPlatformer и архив
/// UnityPlatformer-Windows.zip в корне проекта.
/// </summary>
public static class BuildGame
{
    const string BuildFolder = "Build/UnityPlatformer";
    const string ExeName = "UnityPlatformer.exe";
    const string ZipPath = "UnityPlatformer-Windows.zip";

    [MenuItem("Platformer/Собрать игру для Windows")]
    public static void BuildOnly()
    {
        Build(false);
    }

    [MenuItem("Platformer/Собрать игру для Windows и запустить")]
    public static void BuildAndRun()
    {
        Build(true);
    }

    static void Build(bool run)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Сначала остановите Play, потом запускайте сборку.");
            return;
        }

        var scenes = new List<string>();
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled) scenes.Add(scene.path);
        }
        if (scenes.Count == 0)
        {
            Debug.LogError("В Build Settings нет ни одной сцены. Сначала выполните Platformer → Создать сцену Main.");
            return;
        }

        // Игра открывается в обычном окне 1280x720; развернуть на весь экран — Alt+Enter.
        PlayerSettings.productName = "Unity Platformer";
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 720;
        PlayerSettings.resizableWindow = true;

        // Старую сборку убираем целиком, чтобы в архив не попали устаревшие файлы.
        if (Directory.Exists(BuildFolder)) Directory.Delete(BuildFolder, true);

        var options = new BuildPlayerOptions();
        options.scenes = scenes.ToArray();
        options.locationPathName = BuildFolder + "/" + ExeName;
        options.target = BuildTarget.StandaloneWindows64;
        options.options = BuildOptions.None;

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
        {
            Debug.LogError("Сборка не удалась: " + report.summary.result + ", ошибок: " + report.summary.totalErrors);
            return;
        }

        File.WriteAllText(BuildFolder + "/README.txt",
            "Unity Platformer\r\n\r\n" +
            "Запуск: UnityPlatformer.exe (Unity для этого не нужна).\r\n" +
            "Папку нужно держать целиком: файлы рядом с .exe нужны игре.\r\n\r\n" +
            "Управление: A / D или стрелки - бег, Пробел - прыжок, R - начать заново.\r\n" +
            "Alt+Enter - на весь экран и обратно.\r\n\r\n" +
            "Графика: Pixel Platformer, автор Kenney (kenney.nl), лицензия CC0.\r\n",
            new System.Text.UTF8Encoding(true));

        if (File.Exists(ZipPath)) File.Delete(ZipPath);
        ZipFile.CreateFromDirectory(BuildFolder, ZipPath, System.IO.Compression.CompressionLevel.Optimal, true);

        long zipBytes = new FileInfo(ZipPath).Length;
        Debug.Log("Игра собрана: " + Path.GetFullPath(BuildFolder) + ". Архив: " + Path.GetFullPath(ZipPath)
            + " (" + (zipBytes / 1048576f).ToString("0.0") + " МБ).");

        if (run)
        {
            System.Diagnostics.Process.Start(Path.GetFullPath(BuildFolder + "/" + ExeName));
        }
    }
}

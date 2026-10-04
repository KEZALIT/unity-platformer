using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// При первом открытии проекта создаёт пустую сцену Assets/Scenes/Main.unity
/// и добавляет её в Build Settings, чтобы игру можно было сразу запустить и собрать.
/// </summary>
[InitializeOnLoad]
public static class PlatformerSceneSetup
{
    const string SceneFolder = "Assets/Scenes";
    const string ScenePath = "Assets/Scenes/Main.unity";

    static PlatformerSceneSetup()
    {
        EditorApplication.delayCall += EnsureScene;
    }

    static void EnsureScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        if (File.Exists(ScenePath))
        {
            AddToBuildSettings();
            return;
        }

        // Автоматически создаём сцену, только если открыта новая несохранённая сцена без правок.
        Scene active = SceneManager.GetActiveScene();
        if (!string.IsNullOrEmpty(active.path) || active.isDirty) return;
        CreateScene();
    }

    [MenuItem("Platformer/Создать сцену Main")]
    public static void CreateSceneFromMenu()
    {
        if (EditorApplication.isPlaying) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (File.Exists(ScenePath))
        {
            EditorSceneManager.OpenScene(ScenePath);
            AddToBuildSettings();
            return;
        }
        CreateScene();
    }

    static void CreateScene()
    {
        if (!Directory.Exists(SceneFolder))
        {
            Directory.CreateDirectory(SceneFolder);
            AssetDatabase.Refresh();
        }
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddToBuildSettings();
    }

    static void AddToBuildSettings()
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (EditorBuildSettingsScene existing in scenes)
        {
            if (existing.path == ScenePath) return;
        }
        scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}

using UnityEditor;
using UnityEngine;

/// <summary>
/// Настройки импорта для картинок из Assets/Resources/Kenney: пиксель-арт должен быть спрайтом
/// без сглаживания и сжатия, иначе он будет размытым. Настройки применяются автоматически
/// при импорте и ещё раз проверяются при каждом открытии проекта.
/// </summary>
public class KenneyImportSettings : AssetPostprocessor
{
    const string Folder = "Assets/Resources/Kenney";

    void OnPreprocessTexture()
    {
        string path = assetPath.Replace('\\', '/');
        if (!path.StartsWith(Folder + "/")) return;
        Apply((TextureImporter)assetImporter, path);
    }

    /// <summary>Возвращает true, если настройки пришлось поменять.</summary>
    static bool Apply(TextureImporter importer, string path)
    {
        // Фон нарисован крупнее: 6 пикселей на юнит, остальное — 18 (один тайл = один юнит).
        float pixelsPerUnit = path.EndsWith("/backdrop.png") ? 6f : 18f;

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);

        bool correct = importer.textureType == TextureImporterType.Sprite
            && importer.spriteImportMode == SpriteImportMode.Single
            && Mathf.Approximately(importer.spritePixelsPerUnit, pixelsPerUnit)
            && importer.filterMode == FilterMode.Point
            && importer.textureCompression == TextureImporterCompression.Uncompressed
            && !importer.mipmapEnabled
            && settings.spriteMeshType == SpriteMeshType.FullRect;
        if (correct) return false;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;

        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;   // нужно, чтобы спрайт можно было повторять плиткой
        settings.spriteAlignment = (int)SpriteAlignment.Center;
        importer.SetTextureSettings(settings);
        return true;
    }

    [InitializeOnLoadMethod]
    static void CheckOnLoad()
    {
        EditorApplication.delayCall += FixAll;
    }

    [MenuItem("Platformer/Настроить импорт спрайтов")]
    public static void FixAll()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!AssetDatabase.IsValidFolder(Folder)) return;

        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Folder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null && Apply(importer, path))
            {
                importer.SaveAndReimport();
            }
        }
    }
}

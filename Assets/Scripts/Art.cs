using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Спрайты из набора «Pixel Platformer» (Kenney, лицензия CC0), лежат в Assets/Resources/Kenney.
/// Если картинку не удалось загрузить, вместо неё подставляется белый квадрат и в консоль пишется предупреждение.
/// </summary>
public static class Art
{
    const string Folder = "Kenney/";

    /// <summary>Сколько пикселей картинки приходится на один юнит (один тайл 18x18 = 1 юнит).</summary>
    public const float PixelsPerUnit = 18f;

    static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

    public static Sprite Get(string name)
    {
        Sprite sprite;
        if (cache.TryGetValue(name, out sprite) && sprite != null) return sprite;

        sprite = Resources.Load<Sprite>(Folder + name);
        if (sprite == null)
        {
            Debug.LogWarning("Не найден спрайт Resources/" + Folder + name + ". Проверьте, что картинка импортирована как Sprite (меню Platformer → Настроить импорт спрайтов).");
            sprite = SpriteFactory.Square;
        }
        cache[name] = sprite;
        return sprite;
    }
}

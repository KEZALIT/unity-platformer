using UnityEngine;

/// <summary>
/// Рисует простые спрайты прямо в коде, чтобы игре не нужны были файлы с графикой.
/// </summary>
public static class SpriteFactory
{
    static Sprite square;
    static Sprite circle;
    static Sprite triangle;

    /// <summary>Белый квадрат 1x1 юнит. Размер и цвет задаются через Transform и SpriteRenderer.</summary>
    public static Sprite Square
    {
        get
        {
            if (square == null)
            {
                var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                tex.SetPixel(0, 0, Color.white);
                tex.Apply();
                tex.filterMode = FilterMode.Point;
                tex.wrapMode = TextureWrapMode.Clamp;
                square = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
                square.name = "Square";
            }
            return square;
        }
    }

    /// <summary>Белый круг диаметром 1 юнит со сглаженным краем.</summary>
    public static Sprite Circle
    {
        get
        {
            if (circle == null)
            {
                const int size = 64;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                var pixels = new Color[size * size];
                float radius = size / 2f;
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dx = x + 0.5f - radius;
                        float dy = y + 0.5f - radius;
                        float dist = Mathf.Sqrt(dx * dx + dy * dy);
                        float alpha = Mathf.Clamp01(radius - dist);
                        pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                    }
                }
                tex.SetPixels(pixels);
                tex.Apply();
                tex.filterMode = FilterMode.Bilinear;
                tex.wrapMode = TextureWrapMode.Clamp;
                circle = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
                circle.name = "Circle";
            }
            return circle;
        }
    }

    /// <summary>Белый треугольник 1x1 юнит остриём вверх (для шипов).</summary>
    public static Sprite Triangle
    {
        get
        {
            if (triangle == null)
            {
                const int size = 32;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                var pixels = new Color[size * size];
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float nx = (x + 0.5f) / size;      // 0..1 слева направо
                        float ny = (y + 0.5f) / size;      // 0..1 снизу вверх
                        float limit = 1f - Mathf.Abs(2f * nx - 1f);
                        pixels[y * size + x] = ny <= limit ? Color.white : new Color(1f, 1f, 1f, 0f);
                    }
                }
                tex.SetPixels(pixels);
                tex.Apply();
                tex.filterMode = FilterMode.Point;
                tex.wrapMode = TextureWrapMode.Clamp;
                triangle = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
                triangle.name = "Triangle";
            }
            return triangle;
        }
    }
}

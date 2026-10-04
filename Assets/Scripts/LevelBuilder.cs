using UnityEngine;

/// <summary>
/// «Конструктор» уровня: создаёт отдельные объекты (землю, платформы, монеты, врагов...).
/// Что и где ставить, решает EndlessLevel. Координаты заданы в юнитах, «top» — высота верхней грани.
/// Картинки берутся из набора Kenney через Art.Get(), размеры коллайдеров от картинок не зависят.
/// </summary>
public static class LevelBuilder
{
    /// <summary>Цвет неба — совпадает с верхом фоновой картинки.</summary>
    public static readonly Color Sky = new Color32(223, 246, 245, 255);
    static readonly Color Haze = new Color32(194, 227, 232, 255);   // низ фоновой картинки

    /// <summary>Нижний край земли: ниже камера не опускается, так что «дна» не видно.</summary>
    public const float GroundBottom = -7f;

    /// <summary>Ниже этой высоты герой считается упавшим в пропасть.</summary>
    public const float KillY = -8f;

    public const float EnemyWidth = 0.9f;
    public const float EnemyHeight = 0.8f;
    public const float SpikeSize = 0.5f;
    const float PlatformThickness = 0.5f;

    // ---------- Общие помощники ----------

    static GameObject MakeEmpty(string name, Transform parent, Vector2 position)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(position.x, position.y, 0f);
        return go;
    }

    static SpriteRenderer MakeSprite(string name, Transform parent, Sprite sprite, Vector2 position, int order)
    {
        var go = MakeEmpty(name, parent, position);
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = order;
        return renderer;
    }

    /// <summary>Один спрайт, повторённый плиткой на прямоугольник size (в юнитах).</summary>
    static SpriteRenderer MakeTiled(string name, Transform parent, Sprite sprite, Vector2 center, Vector2 size, int order)
    {
        var renderer = MakeSprite(name, parent, sprite, center, order);
        renderer.drawMode = SpriteDrawMode.Tiled;
        renderer.size = size;
        return renderer;
    }

    /// <summary>Масштабирует объект так, чтобы спрайт стал нужной высоты (пропорции сохраняются).</summary>
    static void FitHeight(Transform target, Sprite sprite, float height)
    {
        float spriteHeight = sprite.bounds.size.y;
        float scale = spriteHeight > 0.0001f ? height / spriteHeight : 1f;
        target.localScale = new Vector3(scale, scale, 1f);
    }

    static BoxCollider2D AddBox(GameObject go, Vector2 size, bool trigger)
    {
        var box = go.AddComponent<BoxCollider2D>();
        box.offset = Vector2.zero;
        box.size = size;
        box.isTrigger = trigger;
        return box;
    }

    static SpriteAnimator Animate(SpriteRenderer renderer, float fps, params Sprite[] frames)
    {
        var animator = renderer.gameObject.AddComponent<SpriteAnimator>();
        animator.target = renderer;
        animator.frames = frames;
        animator.fps = fps;
        return animator;
    }

    // ---------- Поверхности ----------

    /// <summary>Участок земли. Ширина должна быть целым числом юнитов (не меньше 2), чтобы тайлы легли ровно.</summary>
    public static void AddGround(Transform parent, float left, float right, float top)
    {
        float width = right - left;
        int rows = Mathf.Max(2, Mathf.CeilToInt(top - GroundBottom));   // верхний ряд с травой + ряды земли
        float centerX = (left + right) * 0.5f;

        var body = MakeEmpty("Ground", parent, new Vector2(centerX, top - rows * 0.5f));
        AddBox(body, new Vector2(width, rows), false);

        float inner = Mathf.Max(0f, width - 2f);
        float topY = top - 0.5f;
        MakeSprite("GrassLeft", parent, Art.Get("ground_top_left"), new Vector2(left + 0.5f, topY), 0);
        MakeSprite("GrassRight", parent, Art.Get("ground_top_right"), new Vector2(right - 0.5f, topY), 0);
        if (inner > 0f) MakeTiled("Grass", parent, Art.Get("ground_top_mid"), new Vector2(centerX, topY), new Vector2(inner, 1f), 0);

        float dirtRows = rows - 1;
        float dirtY = top - 1f - dirtRows * 0.5f;
        MakeTiled("DirtLeft", parent, Art.Get("ground_left"), new Vector2(left + 0.5f, dirtY), new Vector2(1f, dirtRows), 0);
        MakeTiled("DirtRight", parent, Art.Get("ground_right"), new Vector2(right - 0.5f, dirtY), new Vector2(1f, dirtRows), 0);
        if (inner > 0f) MakeTiled("Dirt", parent, Art.Get("ground_mid"), new Vector2(centerX, dirtY), new Vector2(inner, dirtRows), 0);
    }

    static GameObject MakePlank(string name, Transform parent, string spriteName, Vector2 center, int width)
    {
        var go = MakeEmpty(name, parent, center);
        AddBox(go, new Vector2(width, PlatformThickness), false);

        // Доска чуть толще коллайдера; выравниваем её по верхней грани.
        Sprite plank = Art.Get(spriteName);
        float height = plank.bounds.size.y;
        float offsetY = PlatformThickness * 0.5f - height * 0.5f;
        MakeTiled("Visual", go.transform, plank, new Vector2(0f, offsetY), new Vector2(width, height), 1);
        return go;
    }

    /// <summary>Деревянная платформа. Ширина — целое число юнитов.</summary>
    public static void AddPlatform(Transform parent, float left, float top, int width)
    {
        MakePlank("Platform", parent, "plank_wood", new Vector2(left + width * 0.5f, top - PlatformThickness * 0.5f), width);
    }

    /// <summary>Платформа ездит между centerA и centerB (это координаты её центра).</summary>
    public static void AddMovingPlatform(Transform parent, Vector2 centerA, Vector2 centerB, int width, float speed)
    {
        var go = MakePlank("MovingPlatform", parent, "plank_metal", centerA, width);
        var moving = go.AddComponent<MovingPlatform>();
        moving.pointA = centerA;
        moving.pointB = centerB;
        moving.speed = speed;
    }

    /// <summary>Невидимая стена, которая не даёт уйти назад за край уровня.</summary>
    public static Transform AddWall(Transform parent)
    {
        var go = MakeEmpty("LeftWall", parent, Vector2.zero);
        AddBox(go, new Vector2(2f, 80f), false);
        return go.transform;
    }

    // ---------- Предметы и опасности ----------

    public static void AddCoin(Transform parent, float x, float y)
    {
        var go = MakeEmpty("Coin", parent, new Vector2(x, y));
        var circle = go.AddComponent<CircleCollider2D>();
        circle.radius = 0.4f;
        circle.isTrigger = true;

        Sprite front = Art.Get("coin_a");
        Sprite side = Art.Get("coin_b");
        var visual = MakeSprite("Visual", go.transform, front, Vector2.zero, 6);
        FitHeight(visual.transform, front, 0.6f);
        Animate(visual, 5f, front, side);

        var coin = go.AddComponent<Coin>();
        coin.visual = visual.transform;
    }

    /// <summary>Враг стоит на земле с верхом groundTop и ходит между leftX и rightX (координаты его центра).</summary>
    public static void AddEnemy(Transform parent, float leftX, float rightX, float groundTop, float speed)
    {
        var go = MakeEmpty("Enemy", parent, new Vector2(leftX, groundTop + EnemyHeight * 0.5f));
        AddBox(go, new Vector2(EnemyWidth, EnemyHeight), true);

        Sprite stepA = Art.Get("enemy_a");
        Sprite stepB = Art.Get("enemy_b");
        var visual = MakeSprite("Visual", go.transform, stepA, Vector2.zero, 5);
        FitHeight(visual.transform, stepA, EnemyHeight);
        Animate(visual, 5f, stepA, stepB);

        var enemy = go.AddComponent<Enemy>();
        enemy.leftX = leftX;
        enemy.rightX = rightX;
        enemy.speed = speed;
    }

    public static void AddSpikes(Transform parent, float left, float top, int count)
    {
        float width = count * SpikeSize;
        var go = MakeEmpty("Spikes", parent, new Vector2(left + width * 0.5f, top + 0.2f));
        AddBox(go, new Vector2(width - 0.2f, 0.3f), true);   // зона чуть меньше картинки — так честнее
        go.AddComponent<Hazard>();

        Sprite spike = Art.Get("spike");
        for (int i = 0; i < count; i++)
        {
            float x = left + SpikeSize * 0.5f + i * SpikeSize;
            var visual = MakeSprite("Spike", parent, spike, new Vector2(x, top + SpikeSize * 0.5f), 2);
            FitHeight(visual.transform, spike, SpikeSize);
        }
    }

    /// <summary>Невидимая зона в начале участка земли: после неё герой возрождается здесь.</summary>
    public static void AddCheckpoint(Transform parent, float groundLeft, float top)
    {
        var go = MakeEmpty("Checkpoint", parent, new Vector2(groundLeft + 1.5f, top + 5f));
        AddBox(go, new Vector2(2f, 10f), true);
        var checkpoint = go.AddComponent<Checkpoint>();
        checkpoint.respawnPoint = new Vector2(groundLeft + 1.5f, top + 0.6f);
    }

    /// <summary>Флаг-веха с сердцем: даёт дополнительную жизнь.</summary>
    public static void AddMilestone(Transform parent, float x, float top)
    {
        Sprite pole = Art.Get("pole");
        MakeSprite("MilestonePole", parent, pole, new Vector2(x, top + 0.5f), 2);
        MakeSprite("MilestonePole", parent, pole, new Vector2(x, top + 1.5f), 2);

        // На картинке флага древко занимает левые 6 пикселей из 18 — сдвигаем, чтобы оно встало на шест.
        float flagX = x + 6f / Art.PixelsPerUnit;
        Sprite flagA = Art.Get("flag_a");
        Sprite flagB = Art.Get("flag_b");
        var flag = MakeSprite("MilestoneFlag", parent, flagA, new Vector2(flagX, top + 2.5f), 2);
        Animate(flag, 4f, flagA, flagB);

        var heart = MakeSprite("MilestoneHeart", parent, Art.Get("heart_full"), new Vector2(flagX, top + 3.6f), 3);

        var go = MakeEmpty("Milestone", parent, new Vector2(x, top + 2f));
        AddBox(go, new Vector2(1.2f, 4f), true);
        var goal = go.AddComponent<Goal>();
        goal.heart = heart.gameObject;
    }

    // ---------- Герой и украшения ----------

    public static PlayerController AddPlayer(Transform parent, Vector2 spawn)
    {
        var go = MakeEmpty("Player", parent, spawn);
        AddBox(go, new Vector2(0.8f, 1f), false);

        // Картинка героя — в дочернем объекте: его удобно разворачивать и скрывать.
        Sprite idle = Art.Get("player_idle");
        Sprite walk = Art.Get("player_walk");
        var visual = MakeSprite("Visual", go.transform, idle, Vector2.zero, 10);
        FitHeight(visual.transform, idle, 1f);
        var animator = Animate(visual, 8f, idle, walk);
        animator.playing = false;

        var player = go.AddComponent<PlayerController>();
        player.visual = visual.transform;
        player.walkAnimation = animator;
        player.killY = KillY;
        return player;
    }

    public static void AddCloud(Transform parent, float x, float y, float scale)
    {
        var go = MakeEmpty("Cloud", parent, new Vector2(x, y));
        go.transform.localScale = new Vector3(scale, scale, 1f);
        MakeSprite("Left", go.transform, Art.Get("cloud_left"), new Vector2(-1f, 0f), -10);
        MakeSprite("Mid", go.transform, Art.Get("cloud_mid"), new Vector2(0f, 0f), -10);
        MakeSprite("Right", go.transform, Art.Get("cloud_right"), new Vector2(1f, 0f), -10);
    }

    /// <summary>Украшение на земле (росток, куст, ёлка, гриб): только картинка, без столкновений.</summary>
    public static void AddDecor(Transform parent, string spriteName, float x, float top)
    {
        MakeSprite("Decor", parent, Art.Get(spriteName), new Vector2(x, top + 0.5f), -1);
    }

    /// <summary>Фон с холмами, который едет за камерой.</summary>
    public static void AddBackdrop(Transform parent, Transform cam)
    {
        var go = MakeEmpty("Backdrop", parent, Vector2.zero);
        Sprite strip = Art.Get("backdrop");
        float period = strip.bounds.size.x;
        float height = strip.bounds.size.y;

        MakeTiled("Hills", go.transform, strip, Vector2.zero, new Vector2(period * 6f, height), -30);

        // всё, что ниже холмов, закрашиваем их нижним цветом
        var haze = MakeSprite("Haze", go.transform, SpriteFactory.Square, new Vector2(0f, -height * 0.5f - 15f), -30);
        haze.color = Haze;
        haze.transform.localScale = new Vector3(period * 6f, 30f, 1f);

        var backdrop = go.AddComponent<Backdrop>();
        backdrop.cam = cam;
        backdrop.period = period;
    }
}

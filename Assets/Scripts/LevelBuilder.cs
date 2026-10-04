using UnityEngine;

/// <summary>Что получилось после сборки уровня.</summary>
public class LevelInfo
{
    public Transform root;
    public PlayerController player;
    public Vector2 spawn;
    public int totalCoins;
    public float minX;
    public float maxX;
}

/// <summary>
/// Собирает весь уровень из кода. Чтобы изменить уровень, правьте метод Build():
/// координаты заданы в юнитах, «top» — это высота верхней грани.
/// </summary>
public static class LevelBuilder
{
    public static readonly Color Sky = new Color(0.53f, 0.81f, 0.92f);
    static readonly Color Dirt = new Color(0.47f, 0.33f, 0.23f);
    static readonly Color Grass = new Color(0.36f, 0.72f, 0.33f);
    static readonly Color Wood = new Color(0.72f, 0.52f, 0.30f);
    static readonly Color WoodDark = new Color(0.52f, 0.36f, 0.20f);
    static readonly Color Metal = new Color(0.45f, 0.52f, 0.62f);
    static readonly Color PlayerColor = new Color(0.20f, 0.45f, 0.90f);
    static readonly Color EnemyColor = new Color(0.85f, 0.25f, 0.30f);
    static readonly Color CoinColor = new Color(1.00f, 0.82f, 0.15f);
    static readonly Color SpikeColor = new Color(0.75f, 0.76f, 0.80f);
    static readonly Color FlagIdle = new Color(0.70f, 0.70f, 0.70f);
    static readonly Color GoalColor = new Color(1.00f, 0.55f, 0.10f);

    const float KillY = -8f;

    static Transform root;
    static int coinCount;

    public static LevelInfo Build()
    {
        root = new GameObject("Level").transform;
        coinCount = 0;

        var info = new LevelInfo();
        info.root = root;
        info.minX = -8f;
        info.maxX = 78f;
        info.spawn = new Vector2(-5f, 0.6f);

        AddClouds(info.minX - 4f, info.maxX + 4f);

        // Невидимые за кадром стены по краям уровня.
        AddWall(-10f, -8f);
        AddWall(78f, 80f);

        // --- Участок 1: разминка ---
        AddGround(-8f, 12f, 0f);
        AddCoin(0f, 1f); AddCoin(2f, 1f); AddCoin(4f, 1f);
        // монеты дугой над первой ямой
        AddCoin(12.5f, 2f); AddCoin(13.5f, 2.6f); AddCoin(14.5f, 2f);

        // --- Участок 2: первый враг и платформы-ступеньки ---
        AddGround(15f, 27f, 0f);
        AddEnemy(16.5f, 25.5f, 0f);
        AddPlatform(19f, 2f, 3f);
        AddCoin(20.5f, 3f);
        AddPlatform(24f, 4f, 3f);
        AddCoin(25f, 5f); AddCoin(26f, 5f);

        // --- Участок 3: шипы и движущаяся платформа над пропастью ---
        AddGround(30f, 36f, 1f);
        AddSpikes(32.5f, 1f, 3);
        AddCoin(33.25f, 3.4f);
        AddMovingPlatform(new Vector2(38f, 0.75f), new Vector2(43.5f, 0.75f), 3f);
        AddCoin(40.75f, 2.2f);

        // --- Участок 4: чекпоинт, два врага и лестница из платформ ---
        AddGround(46f, 64f, 0f);
        AddCheckpoint(47.5f, 0f);
        AddEnemy(50f, 56f, 0f);
        AddEnemy(57f, 63f, 0f);
        AddPlatform(50f, 2f, 2f);
        AddCoin(51f, 3f);
        AddPlatform(54f, 4f, 2f);
        AddCoin(55f, 5f);
        AddPlatform(58f, 6f, 3f);
        AddCoin(59f, 7f); AddCoin(60f, 7f);
        AddCoin(65.5f, 2.2f);

        // --- Участок 5: финиш ---
        AddGround(67f, 78f, 0f);
        AddCoin(69f, 1f); AddCoin(70.5f, 1f);
        AddEnemy(69f, 73.5f, 0f);
        AddGoal(75.5f, 0f);

        info.player = AddPlayer(info.spawn);
        info.totalCoins = coinCount;
        return info;
    }

    // ---------- Вспомогательные методы ----------

    static GameObject MakeSprite(string name, Transform parent, Sprite sprite, Vector2 position, Vector2 size, Color color, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(position.x, position.y, 0f);
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = order;
        return go;
    }

    static BoxCollider2D AddBox(GameObject go, Vector2 size, bool trigger)
    {
        var box = go.AddComponent<BoxCollider2D>();
        box.offset = Vector2.zero;
        box.size = size;
        box.isTrigger = trigger;
        return box;
    }

    static void AddGround(float left, float right, float top)
    {
        const float depth = 6f;
        float width = right - left;
        float centerX = (left + right) * 0.5f;
        var body = MakeSprite("Ground", root, SpriteFactory.Square, new Vector2(centerX, top - depth * 0.5f), new Vector2(width, depth), Dirt, 0);
        AddBox(body, Vector2.one, false);
        MakeSprite("Grass", root, SpriteFactory.Square, new Vector2(centerX, top - 0.15f), new Vector2(width, 0.3f), Grass, 1);
    }

    static void AddWall(float left, float right)
    {
        float width = right - left;
        var wall = MakeSprite("Wall", root, SpriteFactory.Square, new Vector2((left + right) * 0.5f, 5f), new Vector2(width, 30f), Dirt, 0);
        AddBox(wall, Vector2.one, false);
    }

    static void AddPlatform(float left, float top, float width)
    {
        const float thickness = 0.5f;
        float centerX = left + width * 0.5f;
        var body = MakeSprite("Platform", root, SpriteFactory.Square, new Vector2(centerX, top - thickness * 0.5f), new Vector2(width, thickness), Wood, 0);
        AddBox(body, Vector2.one, false);
        MakeSprite("PlatformEdge", root, SpriteFactory.Square, new Vector2(centerX, top - thickness + 0.06f), new Vector2(width, 0.12f), WoodDark, 1);
    }

    static void AddMovingPlatform(Vector2 centerA, Vector2 centerB, float width)
    {
        const float thickness = 0.5f;
        var body = MakeSprite("MovingPlatform", root, SpriteFactory.Square, centerA, new Vector2(width, thickness), Metal, 0);
        AddBox(body, Vector2.one, false);
        var moving = body.AddComponent<MovingPlatform>();
        moving.pointA = centerA;
        moving.pointB = centerB;
        moving.speed = 2.5f;
    }

    static void AddCoin(float x, float y)
    {
        var go = new GameObject("Coin");
        go.transform.SetParent(root, false);
        go.transform.localPosition = new Vector3(x, y, 0f);
        var circle = go.AddComponent<CircleCollider2D>();
        circle.radius = 0.4f;
        circle.isTrigger = true;
        var visual = MakeSprite("Visual", go.transform, SpriteFactory.Circle, Vector2.zero, new Vector2(0.6f, 0.6f), CoinColor, 6);
        var coin = go.AddComponent<Coin>();
        coin.visual = visual.transform;
        coinCount++;
    }

    static void AddEnemy(float leftX, float rightX, float groundTop)
    {
        const float width = 0.9f;
        const float height = 0.8f;
        var go = MakeSprite("Enemy", root, SpriteFactory.Square, new Vector2(leftX, groundTop + height * 0.5f), new Vector2(width, height), EnemyColor, 5);
        AddBox(go, Vector2.one, true);
        var enemy = go.AddComponent<Enemy>();
        enemy.leftX = leftX;
        enemy.rightX = rightX;
        enemy.speed = 2f;

        // глаза (координаты и размеры — в долях от размера врага)
        MakeSprite("EyeL", go.transform, SpriteFactory.Square, new Vector2(-0.2f, 0.15f), new Vector2(0.22f, 0.28f), Color.white, 6);
        MakeSprite("EyeR", go.transform, SpriteFactory.Square, new Vector2(0.2f, 0.15f), new Vector2(0.22f, 0.28f), Color.white, 6);
        MakeSprite("PupilL", go.transform, SpriteFactory.Square, new Vector2(-0.2f, 0.1f), new Vector2(0.1f, 0.14f), Color.black, 7);
        MakeSprite("PupilR", go.transform, SpriteFactory.Square, new Vector2(0.2f, 0.1f), new Vector2(0.1f, 0.14f), Color.black, 7);
    }

    static void AddSpikes(float left, float top, int count)
    {
        const float spikeSize = 0.5f;
        float width = count * spikeSize;
        var go = new GameObject("Spikes");
        go.transform.SetParent(root, false);
        go.transform.localPosition = new Vector3(left + width * 0.5f, top + 0.2f, 0f);
        AddBox(go, new Vector2(width - 0.2f, 0.3f), true);   // зона чуть меньше картинки — так честнее
        go.AddComponent<Hazard>();

        for (int i = 0; i < count; i++)
        {
            float x = left + spikeSize * 0.5f + i * spikeSize;
            MakeSprite("Spike", root, SpriteFactory.Triangle, new Vector2(x, top + spikeSize * 0.5f), new Vector2(spikeSize, spikeSize), SpikeColor, 2);
        }
    }

    static void AddCheckpoint(float x, float top)
    {
        MakeSprite("CheckpointPole", root, SpriteFactory.Square, new Vector2(x, top + 1f), new Vector2(0.12f, 2f), Metal, 2);
        var flag = MakeSprite("CheckpointFlag", root, SpriteFactory.Square, new Vector2(x + 0.36f, top + 1.75f), new Vector2(0.6f, 0.4f), FlagIdle, 2);

        var go = new GameObject("Checkpoint");
        go.transform.SetParent(root, false);
        go.transform.localPosition = new Vector3(x, top + 1.5f, 0f);
        AddBox(go, new Vector2(1f, 3f), true);
        var checkpoint = go.AddComponent<Checkpoint>();
        checkpoint.flag = flag.GetComponent<SpriteRenderer>();
        checkpoint.respawnPoint = new Vector2(x, top + 0.6f);
    }

    static void AddGoal(float x, float top)
    {
        MakeSprite("GoalPole", root, SpriteFactory.Square, new Vector2(x, top + 1.6f), new Vector2(0.15f, 3.2f), Metal, 2);
        MakeSprite("GoalFlag", root, SpriteFactory.Square, new Vector2(x + 0.55f, top + 2.8f), new Vector2(0.95f, 0.7f), GoalColor, 2);
        MakeSprite("GoalStar", root, SpriteFactory.Circle, new Vector2(x + 0.55f, top + 2.8f), new Vector2(0.35f, 0.35f), Color.white, 3);

        var go = new GameObject("Goal");
        go.transform.SetParent(root, false);
        go.transform.localPosition = new Vector3(x, top + 2f, 0f);
        AddBox(go, new Vector2(1.2f, 4f), true);
        go.AddComponent<Goal>();
    }

    static PlayerController AddPlayer(Vector2 spawn)
    {
        var go = new GameObject("Player");
        go.transform.SetParent(root, false);
        go.transform.localPosition = new Vector3(spawn.x, spawn.y, 0f);
        AddBox(go, new Vector2(0.8f, 1f), false);

        // Вся картинка героя лежит в дочернем объекте: его удобно разворачивать и скрывать.
        var visual = new GameObject("Visual");
        visual.transform.SetParent(go.transform, false);
        visual.transform.localScale = new Vector3(0.8f, 1f, 1f);
        MakeSprite("Body", visual.transform, SpriteFactory.Square, Vector2.zero, Vector2.one, PlayerColor, 10);
        MakeSprite("EyeBack", visual.transform, SpriteFactory.Square, new Vector2(0.0f, 0.2f), new Vector2(0.22f, 0.24f), Color.white, 11);
        MakeSprite("EyeFront", visual.transform, SpriteFactory.Square, new Vector2(0.3f, 0.2f), new Vector2(0.22f, 0.24f), Color.white, 11);
        MakeSprite("PupilBack", visual.transform, SpriteFactory.Square, new Vector2(0.05f, 0.18f), new Vector2(0.1f, 0.12f), Color.black, 12);
        MakeSprite("PupilFront", visual.transform, SpriteFactory.Square, new Vector2(0.35f, 0.18f), new Vector2(0.1f, 0.12f), Color.black, 12);

        var player = go.AddComponent<PlayerController>();
        player.visual = visual.transform;
        player.killY = KillY;
        return player;
    }

    static void AddClouds(float fromX, float toX)
    {
        var cloudColor = new Color(1f, 1f, 1f, 0.85f);
        int index = 0;
        for (float x = fromX; x < toX; x += 8.5f)
        {
            float y = 7f + (index * 37 % 5) * 0.55f;
            float scale = 1f + (index * 53 % 4) * 0.2f;
            MakeSprite("Cloud", root, SpriteFactory.Circle, new Vector2(x, y), new Vector2(1.6f, 1.1f) * scale, cloudColor, -10);
            MakeSprite("Cloud", root, SpriteFactory.Circle, new Vector2(x + 0.9f * scale, y - 0.1f), new Vector2(1.9f, 1.3f) * scale, cloudColor, -10);
            MakeSprite("Cloud", root, SpriteFactory.Circle, new Vector2(x + 1.9f * scale, y), new Vector2(1.5f, 1f) * scale, cloudColor, -10);
            index++;
        }
    }
}

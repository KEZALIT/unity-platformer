using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Бесконечный уровень: достраивает случайные участки впереди героя и убирает пройденные.
///
/// Проходимость гарантируется правилами, а не удачей:
///  * каждая следующая поверхность ставится через NextSurface(), который не даёт сделать
///    яму шире и ступеньку выше, чем герой способен перепрыгнуть (с запасом);
///  * враги ставятся только на землю и ходят строго в пределах своего участка;
///  * первые 3,5 юнита каждого участка земли всегда свободны — туда безопасно приземляться
///    и там же герой возрождается;
///  * шипы не шире 1,5 юнита и стоят не ближе 4 юнитов к краям участка.
/// </summary>
public class EndlessLevel : MonoBehaviour
{
    // Герой прыгает на ~2,7 юнита вверх и на ~5,5 в длину; здесь пределы взяты с запасом.
    const float MaxRise = 2f;
    const float MinTop = 0f;
    const float MaxTop = 5f;

    const float GenerateAhead = 45f;     // на сколько юнитов вперёд держим готовый уровень
    const float KeepBehind = 25f;        // сколько пройденного уровня оставляем позади
    const float MilestoneStep = 100f;    // флаг с дополнительной жизнью каждые 100 м
    const float LandingZone = 3.5f;      // свободное место в начале каждого участка земли

    static readonly string[] DecorSprites = { "decor_sprout", "decor_plant", "decor_tree", "decor_mushroom" };

    class Chunk
    {
        public GameObject root;
        public float right;
    }

    public PlayerController Player { get; private set; }
    public Vector2 RespawnPoint { get; private set; }
    public float StartX { get; private set; }
    public float FurthestX { get; private set; }
    public float LeftLimit { get; private set; }

    System.Random rng;
    readonly List<Chunk> chunks = new List<Chunk>();
    Transform current;        // участок, который сейчас строится
    Transform wall;
    float cursorX;            // правый край последней построенной поверхности
    float cursorTop;          // высота её верха
    float nextMilestone = MilestoneStep;
    int sectionIndex;

    /// <summary>Самая широкая яма, которую разрешено ставить при подъёме на rise юнитов.</summary>
    public static float MaxGap(float rise)
    {
        return rise <= 0f ? 3.5f : 3.5f - 0.5f * rise;
    }

    public void Begin(int seed)
    {
        rng = new System.Random(seed);

        Vector2 spawn = new Vector2(-5f, 0.6f);
        StartX = spawn.x;
        FurthestX = spawn.x;
        RespawnPoint = spawn;
        LeftLimit = -8f;

        // Стартовая площадка: без врагов и шипов.
        current = NewChunk();
        LevelBuilder.AddGround(current, -8f, 12f, 0f);
        for (int i = 0; i < 4; i++) LevelBuilder.AddCoin(current, 1f + i * 1.5f, 1f);
        LevelBuilder.AddDecor(current, "decor_tree", -2.5f, 0f);
        LevelBuilder.AddDecor(current, "decor_sprout", 8.5f, 0f);
        LevelBuilder.AddCloud(current, -3f, 6.5f, 1.2f);
        LevelBuilder.AddCloud(current, 7f, 7.5f, 1f);
        cursorX = 12f;
        cursorTop = 0f;
        CloseChunk();

        wall = LevelBuilder.AddWall(transform);
        PlaceWall();

        Player = LevelBuilder.AddPlayer(transform, spawn);
        Fill();
    }

    /// <summary>Запомнить новую точку возрождения (назад она не откатывается).</summary>
    public void SetRespawn(Vector2 point)
    {
        if (point.x > RespawnPoint.x) RespawnPoint = point;
    }

    void Update()
    {
        if (Player == null) return;

        float playerX = Player.transform.position.x;
        if (playerX > FurthestX) FurthestX = playerX;

        Fill();

        // Левая граница ползёт за героем, но никогда не обгоняет точку возрождения.
        float limit = Mathf.Min(FurthestX - KeepBehind, RespawnPoint.x - 3f);
        if (limit > LeftLimit + 0.5f)
        {
            LeftLimit = limit;
            PlaceWall();
            RemoveOldChunks();
        }
    }

    void Fill()
    {
        int guard = 0;
        while (cursorX < FurthestX + GenerateAhead && guard < 50)
        {
            GenerateSection();
            guard++;
        }
    }

    void PlaceWall()
    {
        wall.position = new Vector3(LeftLimit - 1f, 10f, 0f);
    }

    void RemoveOldChunks()
    {
        for (int i = chunks.Count - 1; i >= 0; i--)
        {
            if (chunks[i].right < LeftLimit - 12f)
            {
                Destroy(chunks[i].root);
                chunks.RemoveAt(i);
            }
        }
    }

    // ---------- Случайные числа ----------

    float Range(float min, float max)
    {
        if (max <= min) return min;
        return min + (float)rng.NextDouble() * (max - min);
    }

    /// <summary>Целое от min до max включительно.</summary>
    int RangeInt(int min, int max)
    {
        return rng.Next(min, max + 1);
    }

    bool Chance(float probability)
    {
        return rng.NextDouble() < probability;
    }

    /// <summary>От 0 в начале до 1 примерно через 400 м: уровень постепенно усложняется.</summary>
    float Difficulty()
    {
        return Mathf.Clamp01((cursorX - StartX) / 400f);
    }

    // ---------- Участки ----------

    Transform NewChunk()
    {
        var go = new GameObject("Chunk");
        go.transform.SetParent(transform, false);
        return go.transform;
    }

    void CloseChunk()
    {
        var chunk = new Chunk();
        chunk.root = current.gameObject;
        chunk.right = cursorX;
        chunks.Add(chunk);
        current = null;
    }

    /// <summary>Один участок = переход (яма, цепочка платформ или движущаяся платформа) + земля.</summary>
    void GenerateSection()
    {
        current = NewChunk();
        float startX = cursorX;
        float difficulty = Difficulty();

        float left;
        float top;
        float roll = Range(0f, 1f);
        if (sectionIndex == 0 || roll < 0.5f - 0.15f * difficulty)
        {
            SimpleGap(difficulty, out left, out top);
        }
        else if (roll < 0.8f)
        {
            PlatformChain(difficulty, out left, out top);
        }
        else
        {
            MovingPlatformCrossing(difficulty, out left, out top);
        }

        Ground(left, top, difficulty);

        int clouds = RangeInt(1, 2);
        for (int i = 0; i < clouds; i++)
        {
            LevelBuilder.AddCloud(current, Range(startX, cursorX), cursorTop + Range(5f, 8.5f), Range(0.8f, 1.4f));
        }

        CloseChunk();
        sectionIndex++;
    }

    /// <summary>
    /// Выбирает место для следующей поверхности так, чтобы с текущей до неё точно можно было допрыгнуть:
    /// подъём не больше MaxRise, яма не шире MaxGap(подъём).
    /// </summary>
    void NextSurface(float minGap, float minStep, float maxStep, float difficulty, out float left, out float top)
    {
        top = Mathf.Clamp(cursorTop + Range(minStep, maxStep), MinTop, MaxTop);
        top = Mathf.Round(top * 2f) / 2f;

        float rise = top - cursorTop;
        if (rise > MaxRise)
        {
            top = cursorTop + MaxRise;
            rise = MaxRise;
        }

        float maxGap = MaxGap(rise);
        float low = Mathf.Min(minGap, maxGap);
        float high = Mathf.Lerp(low, maxGap, 0.55f + 0.45f * difficulty);
        left = cursorX + Range(low, high);
    }

    void SimpleGap(float difficulty, out float left, out float top)
    {
        float fromX = cursorX;
        float fromTop = cursorTop;
        // в самом начале ступеньки пониже, чтобы игрок успел освоиться
        float maxStep = sectionIndex < 2 ? 1f : 2f;
        NextSurface(1.5f, -maxStep, maxStep, difficulty, out left, out top);

        if (Chance(0.6f))
        {
            // монеты дугой над ямой — на высоте, до которой герой достаёт в прыжке
            float gap = left - fromX;
            float y = Mathf.Max(fromTop + 1.8f, top + 1f);
            LevelBuilder.AddCoin(current, fromX + gap * 0.25f, y);
            LevelBuilder.AddCoin(current, fromX + gap * 0.5f, y + 0.5f);
            LevelBuilder.AddCoin(current, fromX + gap * 0.75f, y);
        }
    }

    void PlatformChain(float difficulty, out float left, out float top)
    {
        int count = RangeInt(2, 4);
        for (int i = 0; i < count; i++)
        {
            float platformLeft;
            float platformTop;
            NextSurface(1.5f, -1.5f, 1.5f, difficulty, out platformLeft, out platformTop);

            int width = RangeInt(3, 4);
            LevelBuilder.AddPlatform(current, platformLeft, platformTop, width);
            if (Chance(0.7f)) LevelBuilder.AddCoin(current, platformLeft + width * 0.5f, platformTop + 1f);

            cursorX = platformLeft + width;
            cursorTop = platformTop;
        }

        NextSurface(1.5f, -2f, 1.5f, difficulty, out left, out top);
    }

    void MovingPlatformCrossing(float difficulty, out float left, out float top)
    {
        const int width = 3;
        const float edgeGap = 0.6f;   // зазор между платформой в крайней точке и землёй

        float platformTop = cursorTop;
        float travel = Range(4f, 6f + 3f * difficulty);
        float startCenter = cursorX + edgeGap + width * 0.5f;
        float endCenter = startCenter + travel;
        float y = platformTop - 0.25f;

        LevelBuilder.AddMovingPlatform(current, new Vector2(startCenter, y), new Vector2(endCenter, y), width, Range(2f, 2.5f + difficulty));

        int coins = RangeInt(1, 3);
        for (int i = 0; i < coins; i++)
        {
            float t = (i + 1f) / (coins + 1f);
            LevelBuilder.AddCoin(current, Mathf.Lerp(startCenter, endCenter, t), platformTop + 1.3f);
        }

        cursorX = endCenter + width * 0.5f;
        cursorTop = platformTop;

        // Земля сразу за крайним положением платформы, не выше одной ступеньки.
        int step = RangeInt(-1, 2);
        if (step == 2) step = 0;
        left = cursorX + edgeGap;
        top = Mathf.Clamp(platformTop + step, MinTop, MaxTop);
    }

    void Ground(float left, float top, float difficulty)
    {
        int length = RangeInt(9, 17);
        float right = left + length;

        LevelBuilder.AddGround(current, left, right, top);
        LevelBuilder.AddCheckpoint(current, left, top);

        if (right - StartX >= nextMilestone)
        {
            LevelBuilder.AddMilestone(current, left + 2.5f, top);
            while (right - StartX >= nextMilestone) nextMilestone += MilestoneStep;
        }

        // Первые два участка — только монеты, чтобы игрок успел освоиться.
        bool easy = sectionIndex < 2;
        bool spikes = !easy && length >= 10 && Chance(0.3f + 0.25f * difficulty);
        bool enemy = !easy && Chance(0.45f + 0.3f * difficulty);
        if (spikes && enemy && length < 16)
        {
            // на коротком участке место есть только для чего-то одного
            if (Chance(0.5f)) spikes = false;
            else enemy = false;
        }

        float enemyFrom = left + LandingZone;
        float enemyTo = right - 1.5f;
        float spikesCenter = float.NegativeInfinity;

        if (spikes)
        {
            int count = RangeInt(1, difficulty > 0.4f ? 3 : 2);
            float width = count * LevelBuilder.SpikeSize;
            float minX = left + 4f;
            float maxX = enemy ? left + 6f : right - 4f - width;
            float spikesLeft = Range(minX, Mathf.Max(minX, maxX));
            LevelBuilder.AddSpikes(current, spikesLeft, top, count);
            spikesCenter = spikesLeft + width * 0.5f;
            enemyFrom = spikesLeft + width + LandingZone;   // после шипов тоже нужно место для приземления
        }

        if (enemy && enemyTo - enemyFrom >= 3f)
        {
            float half = LevelBuilder.EnemyWidth * 0.5f;
            float speed = Range(1.5f, 2.2f + difficulty);
            if (enemyTo - enemyFrom >= 9f && Chance(0.3f + 0.4f * difficulty))
            {
                // двое врагов, каждый на своей половине
                float middle = (enemyFrom + enemyTo) * 0.5f;
                LevelBuilder.AddEnemy(current, enemyFrom + half, middle - 0.5f - half, top, speed);
                LevelBuilder.AddEnemy(current, middle + 0.5f + half, enemyTo - half, top, Range(1.5f, 2.2f + difficulty));
            }
            else
            {
                LevelBuilder.AddEnemy(current, enemyFrom + half, enemyTo - half, top, speed);
            }
        }

        if (!spikes && length >= 10 && Chance(0.3f))
        {
            // бонусная платформа над землёй (идти под ней можно свободно)
            float platformLeft = Range(left + 4f, right - 6.5f);
            LevelBuilder.AddPlatform(current, platformLeft, top + 2f, 3);
            LevelBuilder.AddCoin(current, platformLeft + 1f, top + 3f);
            LevelBuilder.AddCoin(current, platformLeft + 2f, top + 3f);
        }
        else if (Chance(0.75f))
        {
            int count = RangeInt(3, 5);
            float rowLeft = Range(left + 2f, right - 2f - count);
            for (int i = 0; i < count; i++) LevelBuilder.AddCoin(current, rowLeft + i, top + 1f);
        }

        // украшения: только картинки, на проходимость не влияют
        int decorCount = RangeInt(0, 3);
        for (int i = 0; i < decorCount; i++)
        {
            float x = Range(left + 0.5f, right - 0.5f);
            string sprite = DecorSprites[rng.Next(DecorSprites.Length)];
            if (Mathf.Abs(x - spikesCenter) < 1.5f) continue;   // не ставим поверх шипов
            LevelBuilder.AddDecor(current, sprite, x, top);
        }

        cursorX = right;
        cursorTop = top;
    }
}

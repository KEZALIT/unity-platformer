using UnityEngine;

public enum GameState
{
    Playing,
    GameOver
}

/// <summary>
/// Главный объект игры: создаётся сам при запуске, запускает бесконечный уровень,
/// считает дистанцию, очки и жизни, рисует интерфейс.
/// </summary>
public class GameManager : MonoBehaviour
{
    public const int StartLives = 3;
    public const int MaxLives = 5;
    public const int CoinScore = 10;
    public const int EnemyScore = 50;
    const string BestKey = "Platformer.BestDistance";
    static readonly Color HudText = new Color32(61, 71, 94, 255);   // тёмно-синий, как контуры в наборе Kenney

    public static GameManager Instance { get; private set; }

    public GameState State { get; private set; }
    public int Coins { get; private set; }
    public int Stomps { get; private set; }
    public int Lives { get; private set; }
    public int Distance { get; private set; }
    public int Best { get; private set; }
    public float Elapsed { get; private set; }

    public int Score
    {
        get { return Distance + Coins * CoinScore + Stomps * EnemyScore; }
    }

    EndlessLevel level;
    CameraFollow cameraFollow;
    bool newRecord;
    GUIStyle hudStyle;
    GUIStyle titleStyle;
    GUIStyle hintStyle;

    /// <summary>Игра запускается в любой сцене — ничего расставлять в редакторе не нужно.</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null) return;
        var go = new GameObject("GameManager");
        go.AddComponent<GameManager>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        var source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        Sfx.Init(source);

        StartLevel();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void StartLevel()
    {
        if (level != null)
        {
            level.gameObject.SetActive(false);
            Destroy(level.gameObject);
        }

        var go = new GameObject("Level");
        level = go.AddComponent<EndlessLevel>();
        level.Begin(System.Environment.TickCount);   // каждый запуск — новый случайный уровень

        Coins = 0;
        Stomps = 0;
        Distance = 0;
        Lives = StartLives;
        Elapsed = 0f;
        newRecord = false;
        Best = PlayerPrefs.GetInt(BestKey, 0);
        State = GameState.Playing;

        SetupCamera();
    }

    void SetupCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            cam = go.AddComponent<Camera>();
        }
        if (cam.GetComponent<AudioListener>() == null)
        {
            cam.gameObject.AddComponent<AudioListener>();
        }

        cam.orthographic = true;
        cam.orthographicSize = 6f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = LevelBuilder.Sky;
        Vector3 position = cam.transform.position;
        position.z = -10f;
        cam.transform.position = position;
        cam.transform.rotation = Quaternion.identity;

        cameraFollow = cam.GetComponent<CameraFollow>();
        if (cameraFollow == null) cameraFollow = cam.gameObject.AddComponent<CameraFollow>();
        cameraFollow.target = level.Player.transform;
        cameraFollow.minX = level.LeftLimit;
        cameraFollow.maxX = 10000000f;   // справа уровень не кончается
        cameraFollow.Snap();

        LevelBuilder.AddBackdrop(level.transform, cam.transform);
    }

    void Update()
    {
        if (State == GameState.Playing && level != null)
        {
            Elapsed += Time.deltaTime;
            int meters = Mathf.FloorToInt(level.FurthestX - level.StartX);
            if (meters > Distance) Distance = meters;
            if (cameraFollow != null) cameraFollow.minX = level.LeftLimit;
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            StartLevel();
        }
    }

    // ---------- События игры ----------

    public void OnCoinCollected()
    {
        Coins++;
        Sfx.Coin();
    }

    public void OnEnemyStomped()
    {
        Stomps++;
        Sfx.Stomp();
    }

    public void OnCheckpoint(Vector2 point)
    {
        if (level != null) level.SetRespawn(point);
    }

    public void OnMilestone()
    {
        if (Lives < MaxLives) Lives++;
        Sfx.Milestone();
    }

    public void OnPlayerHurt(PlayerController player)
    {
        if (State != GameState.Playing) return;
        Lives--;
        Sfx.Hurt();

        if (Lives <= 0)
        {
            Lives = 0;
            State = GameState.GameOver;
            player.gameObject.SetActive(false);
            if (Distance > Best)
            {
                Best = Distance;
                newRecord = true;
                PlayerPrefs.SetInt(BestKey, Best);
                PlayerPrefs.Save();
            }
        }
        else
        {
            player.Respawn(level.RespawnPoint);
            if (cameraFollow != null) cameraFollow.Snap();
        }
    }

    // ---------- Интерфейс ----------

    void OnGUI()
    {
        EnsureStyles();
        float w = Screen.width;
        float h = Screen.height;
        float line = hudStyle.fontSize * 1.5f;
        float pad = hudStyle.fontSize * 0.6f;

        string hud = "Дистанция: " + Distance + " м    Рекорд: " + Mathf.Max(Best, Distance) + " м    Монеты: " + Coins
            + "    Очки: " + Score;
        hudStyle.alignment = TextAnchor.UpperLeft;
        Outlined(new Rect(pad, pad, w - pad * 2f, line), hud, hudStyle);

        // жизни — сердечками в правом верхнем углу
        float heart = hudStyle.fontSize * 1.8f;
        Texture full = Art.Get("heart_full").texture;
        Texture empty = Art.Get("heart_empty").texture;
        for (int i = 0; i < MaxLives; i++)
        {
            var rect = new Rect(w - pad - (MaxLives - i) * heart, pad * 0.5f, heart, heart);
            if (i < Lives) GUI.DrawTexture(rect, full);
            else if (i < StartLives) GUI.DrawTexture(rect, empty);
        }

        // подсказка внизу — на тёмной полупрозрачной плашке, чтобы читалась и на земле, и на небе
        const string hint = "A / D или стрелки — бег     Пробел — прыжок     R — начать заново";
        hintStyle.alignment = TextAnchor.MiddleCenter;
        Vector2 hintSize = hintStyle.CalcSize(new GUIContent(hint));
        var hintRect = new Rect((w - hintSize.x) * 0.5f - pad, h - hintSize.y - pad * 1.5f, hintSize.x + pad * 2f, hintSize.y + pad * 0.5f);
        Color before = GUI.color;
        GUI.color = new Color(HudText.r, HudText.g, HudText.b, 0.7f);
        GUI.DrawTexture(hintRect, Texture2D.whiteTexture);
        GUI.color = before;
        hintStyle.normal.textColor = Color.white;
        GUI.Label(hintRect, hint, hintStyle);

        if (State == GameState.Playing) return;

        // затемнение и сообщение по центру
        Color old = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.55f);
        GUI.DrawTexture(new Rect(0f, 0f, w, h), Texture2D.whiteTexture);
        GUI.color = old;

        string title = newRecord ? "Новый рекорд!" : "Игра окончена";
        string details = "Дистанция: " + Distance + " м   Монеты: " + Coins + "   Очки: " + Score + "   Время: " + FormatTime(Elapsed);

        float titleHeight = titleStyle.fontSize * 1.6f;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        Shadowed(new Rect(0f, h * 0.5f - titleHeight, w, titleHeight), title, titleStyle, newRecord ? new Color(1f, 0.85f, 0.2f) : new Color(1f, 0.4f, 0.4f));
        hudStyle.alignment = TextAnchor.MiddleCenter;
        Shadowed(new Rect(0f, h * 0.5f + pad, w, line), details, hudStyle, Color.white);
        Shadowed(new Rect(0f, h * 0.5f + pad + line, w, line), "Нажмите R, чтобы сыграть ещё раз", hudStyle, Color.white);
    }

    void EnsureStyles()
    {
        if (hudStyle == null)
        {
            hudStyle = new GUIStyle(GUI.skin.label);
            hudStyle.fontStyle = FontStyle.Bold;
            titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.fontStyle = FontStyle.Bold;
            hintStyle = new GUIStyle(GUI.skin.label);
        }
        int size = Mathf.Max(14, Screen.height / 30);
        hudStyle.fontSize = size;
        titleStyle.fontSize = size * 3;
        hintStyle.fontSize = Mathf.Max(12, Mathf.RoundToInt(size * 0.75f));
    }

    /// <summary>Тёмный текст со светлой подложкой — читается на светлом небе.</summary>
    static void Outlined(Rect rect, string text, GUIStyle style)
    {
        style.normal.textColor = new Color(1f, 1f, 1f, 0.75f);
        GUI.Label(new Rect(rect.x + 1f, rect.y + 2f, rect.width, rect.height), text, style);
        style.normal.textColor = HudText;
        GUI.Label(rect, text, style);
    }

    /// <summary>Светлый текст с тенью — для затемнённого экрана конца игры.</summary>
    static void Shadowed(Rect rect, string text, GUIStyle style, Color color)
    {
        style.normal.textColor = new Color(0f, 0f, 0f, 0.6f);
        GUI.Label(new Rect(rect.x + 2f, rect.y + 2f, rect.width, rect.height), text, style);
        style.normal.textColor = color;
        GUI.Label(rect, text, style);
    }

    static string FormatTime(float seconds)
    {
        int total = Mathf.FloorToInt(seconds);
        return (total / 60).ToString("00") + ":" + (total % 60).ToString("00");
    }
}

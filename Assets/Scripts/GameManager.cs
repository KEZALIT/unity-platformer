using UnityEngine;

public enum GameState
{
    Playing,
    Won,
    GameOver
}

/// <summary>
/// Главный объект игры: создаётся сам при запуске, собирает уровень,
/// считает очки и жизни, рисует интерфейс.
/// </summary>
public class GameManager : MonoBehaviour
{
    public const int StartLives = 3;
    public const int CoinScore = 10;
    public const int EnemyScore = 50;
    public const int LifeBonus = 100;

    public static GameManager Instance { get; private set; }

    public GameState State { get; private set; }
    public int Score { get; private set; }
    public int Coins { get; private set; }
    public int TotalCoins { get; private set; }
    public int Lives { get; private set; }
    public float Elapsed { get; private set; }

    LevelInfo level;
    Vector2 respawnPoint;
    CameraFollow cameraFollow;
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
        if (level != null && level.root != null)
        {
            level.root.gameObject.SetActive(false);
            Destroy(level.root.gameObject);
        }

        level = LevelBuilder.Build();
        respawnPoint = level.spawn;
        TotalCoins = level.totalCoins;
        Coins = 0;
        Score = 0;
        Lives = StartLives;
        Elapsed = 0f;
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
        cameraFollow.target = level.player.transform;
        cameraFollow.minX = level.minX;
        cameraFollow.maxX = level.maxX;
        cameraFollow.Snap();
    }

    void Update()
    {
        if (State == GameState.Playing) Elapsed += Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.R))
        {
            StartLevel();
        }
    }

    // ---------- События игры ----------

    public void OnCoinCollected()
    {
        Coins++;
        Score += CoinScore;
        Sfx.Coin();
    }

    public void OnEnemyStomped()
    {
        Score += EnemyScore;
        Sfx.Stomp();
    }

    public void OnCheckpoint(Vector2 point)
    {
        respawnPoint = point;
        Sfx.Checkpoint();
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
        }
        else
        {
            player.Respawn(respawnPoint);
            if (cameraFollow != null) cameraFollow.Snap();
        }
    }

    public void OnGoalReached()
    {
        if (State != GameState.Playing) return;
        State = GameState.Won;
        Score += Lives * LifeBonus;
        Sfx.Win();
    }

    // ---------- Интерфейс ----------

    void OnGUI()
    {
        EnsureStyles();
        float w = Screen.width;
        float h = Screen.height;
        float line = hudStyle.fontSize * 1.5f;
        float pad = hudStyle.fontSize * 0.6f;

        string hud = "Монеты: " + Coins + "/" + TotalCoins + "    Очки: " + Score + "    Жизни: " + Lives + "    Время: " + FormatTime(Elapsed);
        hudStyle.alignment = TextAnchor.UpperLeft;
        Shadowed(new Rect(pad, pad, w - pad * 2f, line), hud, hudStyle, Color.white);

        hintStyle.alignment = TextAnchor.LowerCenter;
        Shadowed(new Rect(0f, h - line - pad, w, line), "A / D или стрелки — бег     Пробел — прыжок     R — начать заново", hintStyle, Color.white);

        if (State == GameState.Playing) return;

        // затемнение и сообщение по центру
        Color old = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.55f);
        GUI.DrawTexture(new Rect(0f, 0f, w, h), Texture2D.whiteTexture);
        GUI.color = old;

        bool won = State == GameState.Won;
        string title = won ? "Уровень пройден!" : "Игра окончена";
        string details = won
            ? "Очки: " + Score + "   Монеты: " + Coins + "/" + TotalCoins + "   Время: " + FormatTime(Elapsed)
            : "Очки: " + Score + "   Монеты: " + Coins + "/" + TotalCoins;

        float titleHeight = titleStyle.fontSize * 1.6f;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        Shadowed(new Rect(0f, h * 0.5f - titleHeight, w, titleHeight), title, titleStyle, won ? new Color(1f, 0.85f, 0.2f) : new Color(1f, 0.4f, 0.4f));
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

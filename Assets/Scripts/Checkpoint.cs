using UnityEngine;

/// <summary>Чекпоинт: после него герой возрождается здесь.</summary>
public class Checkpoint : MonoBehaviour
{
    public SpriteRenderer flag;
    public Vector2 respawnPoint;
    public Color activeColor = new Color(0.3f, 0.85f, 0.4f);
    bool activated;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (activated || other.GetComponent<PlayerController>() == null) return;
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.State != GameState.Playing) return;
        activated = true;
        if (flag != null) flag.color = activeColor;
        gm.OnCheckpoint(respawnPoint);
    }
}

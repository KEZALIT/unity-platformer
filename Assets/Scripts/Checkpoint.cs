using UnityEngine;

/// <summary>
/// Невидимая зона в начале каждого участка земли: как только герой до неё добрался,
/// после потери жизни он возрождается здесь.
/// </summary>
public class Checkpoint : MonoBehaviour
{
    public Vector2 respawnPoint;
    bool activated;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (activated || other.GetComponent<PlayerController>() == null) return;
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.State != GameState.Playing) return;
        activated = true;
        gm.OnCheckpoint(respawnPoint);
    }
}

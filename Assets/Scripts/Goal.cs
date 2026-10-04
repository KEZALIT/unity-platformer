using UnityEngine;

/// <summary>Финиш уровня.</summary>
public class Goal : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<PlayerController>() == null) return;
        GameManager gm = GameManager.Instance;
        if (gm != null) gm.OnGoalReached();
    }
}

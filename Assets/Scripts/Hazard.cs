using UnityEngine;

/// <summary>Опасная зона (шипы).</summary>
public class Hazard : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other) { Touch(other); }
    void OnTriggerStay2D(Collider2D other) { Touch(other); }

    void Touch(Collider2D other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null) player.Hurt(false);
    }
}

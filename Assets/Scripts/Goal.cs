using UnityEngine;

/// <summary>Флаг-веха (стоит каждые 100 м): даёт дополнительную жизнь, сердце над флагом при этом исчезает.</summary>
public class Goal : MonoBehaviour
{
    public GameObject heart;
    bool reached;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (reached || other.GetComponent<PlayerController>() == null) return;
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.State != GameState.Playing) return;
        reached = true;
        if (heart != null) heart.SetActive(false);
        gm.OnMilestone();
    }
}

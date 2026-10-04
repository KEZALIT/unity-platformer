using UnityEngine;

/// <summary>Монета: подбирается касанием.</summary>
public class Coin : MonoBehaviour
{
    public Transform visual;
    float phase;

    void Start()
    {
        phase = transform.position.x * 0.7f;   // чтобы соседние монеты качались вразнобой
    }

    void Update()
    {
        if (visual == null) return;
        float t = Time.time * 3f + phase;
        visual.localPosition = new Vector3(0f, Mathf.Sin(t) * 0.12f, 0f);
        Vector3 scale = visual.localScale;
        scale.x = 0.6f * Mathf.Abs(Mathf.Cos(t * 0.8f)) + 0.08f;   // имитация вращения
        visual.localScale = scale;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<PlayerController>() == null) return;
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.State != GameState.Playing) return;
        gm.OnCoinCollected();
        Destroy(gameObject);
    }
}

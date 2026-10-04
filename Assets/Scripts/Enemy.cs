using UnityEngine;

/// <summary>Враг: ходит туда-сюда. Прыжок сверху убивает его, касание сбоку ранит героя.</summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class Enemy : MonoBehaviour
{
    public float leftX;
    public float rightX;
    public float speed = 2f;

    Rigidbody2D rb;
    BoxCollider2D box;
    int direction = 1;
    bool dead;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        box = GetComponent<BoxCollider2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        box.isTrigger = true;
    }

    void FixedUpdate()
    {
        if (dead) return;
        Vector2 position = rb.position;
        position.x += direction * speed * Time.fixedDeltaTime;
        if (position.x >= rightX) { position.x = rightX; direction = -1; }
        else if (position.x <= leftX) { position.x = leftX; direction = 1; }
        rb.MovePosition(position);
    }

    void OnTriggerEnter2D(Collider2D other) { Touch(other); }
    void OnTriggerStay2D(Collider2D other) { Touch(other); }

    void Touch(Collider2D other)
    {
        if (dead) return;
        PlayerController player = other.GetComponent<PlayerController>();
        if (player == null) return;
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.State != GameState.Playing) return;

        // Герой падает и его ноги выше середины врага — это прыжок сверху.
        bool falling = player.Velocity.y < -0.5f;
        bool above = other.bounds.min.y > box.bounds.center.y - 0.05f;
        if (falling && above)
        {
            dead = true;
            player.Bounce();
            gm.OnEnemyStomped();
            Destroy(gameObject);
        }
        else
        {
            player.Hurt(false);
        }
    }
}

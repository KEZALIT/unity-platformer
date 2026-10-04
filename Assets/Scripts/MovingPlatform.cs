using UnityEngine;

/// <summary>Платформа, которая ездит между двумя точками.</summary>
[RequireComponent(typeof(Rigidbody2D))]
public class MovingPlatform : MonoBehaviour
{
    public Vector2 pointA;
    public Vector2 pointB;
    public float speed = 2.5f;

    Rigidbody2D rb;
    bool towardsB = true;

    public Vector2 Velocity { get; private set; }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        Vector2 target = towardsB ? pointB : pointA;
        Vector2 current = rb.position;
        Vector2 next = Vector2.MoveTowards(current, target, speed * dt);
        Velocity = (next - current) / dt;
        rb.MovePosition(next);
        if ((next - target).sqrMagnitude < 0.0001f) towardsB = !towardsB;
    }
}

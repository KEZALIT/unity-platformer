using UnityEngine;

/// <summary>
/// Управление героем: бег, прыжок с «временем койота» и буфером нажатия,
/// высота прыжка зависит от того, как долго держат кнопку.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Движение")]
    public float moveSpeed = 7f;
    public float groundAcceleration = 70f;
    public float airAcceleration = 45f;

    [Header("Прыжок")]
    public float jumpVelocity = 13f;
    public float coyoteTime = 0.1f;
    public float jumpBufferTime = 0.12f;
    public float gravityScale = 3f;
    public float fallGravityMultiplier = 1.5f;
    public float lowJumpGravityMultiplier = 2.2f;
    public float maxFallSpeed = 20f;

    [Header("Прочее")]
    public float invulnerableTime = 1.5f;
    public float killY = -8f;
    public Transform visual;
    public SpriteAnimator walkAnimation;

    Rigidbody2D rb;
    BoxCollider2D box;
    readonly Collider2D[] groundHits = new Collider2D[8];
    ContactFilter2D groundFilter;

    float inputX;
    bool jumpHeld;
    float jumpBufferTimer;
    float coyoteTimer;
    float invulnerableTimer;
    float visualWidth = 1f;

    public bool Grounded { get; private set; }
    public Vector2 Velocity { get { return rb.GetVel(); } }
    public bool Invulnerable { get { return invulnerableTimer > 0f; } }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        box = GetComponent<BoxCollider2D>();

        rb.gravityScale = gravityScale;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        // Нулевое трение, чтобы герой не «прилипал» к стенам в прыжке.
        var material = new PhysicsMaterial2D("PlayerNoFriction");
        material.friction = 0f;
        material.bounciness = 0f;
        box.sharedMaterial = material;

        groundFilter = new ContactFilter2D();
        groundFilter.useTriggers = false;
    }

    void Start()
    {
        if (visual != null) visualWidth = Mathf.Abs(visual.localScale.x);
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        bool playing = gm == null || gm.State == GameState.Playing;

        inputX = 0f;
        jumpHeld = false;
        if (playing)
        {
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) inputX -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) inputX += 1f;

            jumpHeld = Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            {
                jumpBufferTimer = jumpBufferTime;
            }
        }

        // Разворот в сторону движения.
        if (visual != null && inputX != 0f)
        {
            Vector3 scale = visual.localScale;
            scale.x = visualWidth * Mathf.Sign(inputX);
            visual.localScale = scale;
        }

        // Кадры ходьбы сменяются только когда герой бежит по земле.
        if (walkAnimation != null) walkAnimation.playing = inputX != 0f && Grounded;

        // Мигание после получения урона.
        if (invulnerableTimer > 0f)
        {
            invulnerableTimer -= Time.deltaTime;
            if (visual != null)
            {
                bool show = invulnerableTimer <= 0f || Mathf.FloorToInt(invulnerableTimer * 12f) % 2 == 0;
                visual.gameObject.SetActive(show);
            }
        }

        // Падение в пропасть.
        if (playing && transform.position.y < killY)
        {
            Hurt(true);
        }
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        Vector2 velocity = rb.GetVel();

        MovingPlatform platform;
        bool onGround = CheckGround(out platform);
        if (onGround && velocity.y > 0.5f) onGround = false;   // только что прыгнули — землёй это не считаем
        Grounded = onGround;

        if (Grounded) coyoteTimer = coyoteTime;
        else coyoteTimer -= dt;
        jumpBufferTimer -= dt;

        // Горизонтальное движение (на движущейся платформе едем вместе с ней).
        float platformSpeed = platform != null ? platform.Velocity.x : 0f;
        float targetSpeed = inputX * moveSpeed + platformSpeed;
        float acceleration = Grounded ? groundAcceleration : airAcceleration;
        velocity.x = Mathf.MoveTowards(velocity.x, targetSpeed, acceleration * dt);

        // Прыжок.
        if (jumpBufferTimer > 0f && coyoteTimer > 0f)
        {
            velocity.y = jumpVelocity;
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
            Grounded = false;
            Sfx.Jump();
        }

        // Быстрее падаем, чем взлетаем; короткое нажатие — низкий прыжок.
        if (velocity.y < 0f) rb.gravityScale = gravityScale * fallGravityMultiplier;
        else if (velocity.y > 0f && !jumpHeld) rb.gravityScale = gravityScale * lowJumpGravityMultiplier;
        else rb.gravityScale = gravityScale;

        if (velocity.y < -maxFallSpeed) velocity.y = -maxFallSpeed;

        rb.SetVel(velocity);
    }

    bool CheckGround(out MovingPlatform platform)
    {
        platform = null;
        Bounds bounds = box.bounds;
        Vector2 center = new Vector2(bounds.center.x, bounds.min.y - 0.04f);
        Vector2 size = new Vector2(bounds.size.x * 0.9f, 0.08f);

        int count = Physics2D.OverlapBox(center, size, 0f, groundFilter, groundHits);
        bool found = false;
        for (int i = 0; i < count; i++)
        {
            Collider2D hit = groundHits[i];
            if (hit == null || hit == box) continue;
            found = true;
            MovingPlatform moving = hit.GetComponent<MovingPlatform>();
            if (moving != null) platform = moving;
        }
        return found;
    }

    /// <summary>Отскок после прыжка на врага.</summary>
    public void Bounce()
    {
        Vector2 velocity = rb.GetVel();
        velocity.y = jumpVelocity * 0.8f;
        rb.SetVel(velocity);
        coyoteTimer = 0f;
    }

    /// <summary>Герой получил урон. force = true игнорирует неуязвимость (падение в пропасть).</summary>
    public void Hurt(bool force)
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.State != GameState.Playing) return;
        if (!force && Invulnerable) return;
        // сразу после возрождения урон не засчитываем вообще — защита от двойного срабатывания
        if (invulnerableTimer > invulnerableTime - 0.3f) return;
        gm.OnPlayerHurt(this);
    }

    public void Respawn(Vector2 position)
    {
        transform.position = new Vector3(position.x, position.y, 0f);
        rb.position = position;
        rb.SetVel(Vector2.zero);
        jumpBufferTimer = 0f;
        coyoteTimer = 0f;
        invulnerableTimer = invulnerableTime;
    }
}

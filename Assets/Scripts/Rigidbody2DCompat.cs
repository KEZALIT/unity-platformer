using UnityEngine;

/// <summary>
/// В Unity 6 свойство Rigidbody2D.velocity переименовали в linearVelocity.
/// Эти методы позволяют коду собираться без предупреждений в обеих версиях.
/// </summary>
public static class Rigidbody2DCompat
{
    public static Vector2 GetVel(this Rigidbody2D rb)
    {
#if UNITY_6000_0_OR_NEWER
        return rb.linearVelocity;
#else
        return rb.velocity;
#endif
    }

    public static void SetVel(this Rigidbody2D rb, Vector2 value)
    {
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = value;
#else
        rb.velocity = value;
#endif
    }
}

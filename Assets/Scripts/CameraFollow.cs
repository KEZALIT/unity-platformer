using UnityEngine;

/// <summary>Камера плавно следует за героем и не выходит за края уровня.</summary>
[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float minX;
    public float maxX;
    public float baseY = 2.5f;
    public float smoothTime = 0.15f;

    Camera cam;
    Vector3 velocity;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    Vector3 DesiredPosition()
    {
        float halfWidth = cam.orthographicSize * cam.aspect;
        float left = minX + halfWidth;
        float right = maxX - halfWidth;
        float x = left <= right ? Mathf.Clamp(target.position.x, left, right) : (minX + maxX) * 0.5f;
        float y = Mathf.Max(baseY, target.position.y - 1f);
        return new Vector3(x, y, transform.position.z);
    }

    /// <summary>Мгновенно переставить камеру (при старте и возрождении).</summary>
    public void Snap()
    {
        if (target == null) return;
        if (cam == null) cam = GetComponent<Camera>();
        transform.position = DesiredPosition();
        velocity = Vector3.zero;
    }

    void LateUpdate()
    {
        if (target == null) return;
        transform.position = Vector3.SmoothDamp(transform.position, DesiredPosition(), ref velocity, smoothTime);
    }
}

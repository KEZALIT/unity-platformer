using UnityEngine;

/// <summary>
/// Фон с холмами: едет за камерой, но чуть медленнее неё (параллакс), и повторяется по кругу.
/// Выполняется после CameraFollow, чтобы не отставать от камеры на кадр.
/// </summary>
[DefaultExecutionOrder(100)]
public class Backdrop : MonoBehaviour
{
    public Transform cam;
    public float period = 16f;      // ширина повторяющегося рисунка в юнитах
    public float parallax = 0.3f;   // 0 — фон приклеен к камере, 1 — стоит на месте, как уровень
    public float offsetY = -1f;

    void LateUpdate()
    {
        if (cam == null) return;
        float x = cam.position.x - Mathf.Repeat(cam.position.x * parallax, period);
        transform.position = new Vector3(x, cam.position.y + offsetY, 0f);
    }
}

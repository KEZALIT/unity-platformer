using UnityEngine;

/// <summary>Простая покадровая анимация: по очереди показывает спрайты из списка.</summary>
public class SpriteAnimator : MonoBehaviour
{
    public SpriteRenderer target;
    public Sprite[] frames;
    public float fps = 6f;
    public bool playing = true;   // когда выключено, показывается первый кадр

    float timer;
    int index;

    void Update()
    {
        if (target == null || frames == null || frames.Length == 0) return;

        if (!playing)
        {
            if (index != 0)
            {
                index = 0;
                target.sprite = frames[0];
            }
            timer = 0f;
            return;
        }

        timer += Time.deltaTime;
        float frameTime = 1f / Mathf.Max(fps, 0.01f);
        if (timer >= frameTime)
        {
            timer -= frameTime;
            index = (index + 1) % frames.Length;
            target.sprite = frames[index];
        }
    }
}

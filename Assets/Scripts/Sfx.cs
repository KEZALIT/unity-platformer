using UnityEngine;

/// <summary>
/// Короткие звуки, сгенерированные в коде (без аудиофайлов).
/// </summary>
public static class Sfx
{
    static AudioSource source;
    static AudioClip jump, coin, stomp, hurt, win, checkpoint;

    public static void Init(AudioSource audioSource)
    {
        source = audioSource;
        if (jump == null) jump = Tone("Jump", 330f, 660f, 0.15f, false);
        if (coin == null) coin = Tone("Coin", 990f, 1480f, 0.12f, false);
        if (stomp == null) stomp = Tone("Stomp", 300f, 90f, 0.15f, true);
        if (hurt == null) hurt = Tone("Hurt", 220f, 70f, 0.35f, true);
        if (win == null) win = Tone("Win", 520f, 1040f, 0.6f, false);
        if (checkpoint == null) checkpoint = Tone("Checkpoint", 660f, 880f, 0.25f, false);
    }

    public static void Jump() { Play(jump, 0.35f); }
    public static void Coin() { Play(coin, 0.4f); }
    public static void Stomp() { Play(stomp, 0.4f); }
    public static void Hurt() { Play(hurt, 0.45f); }
    public static void Win() { Play(win, 0.5f); }
    public static void Checkpoint() { Play(checkpoint, 0.4f); }

    static void Play(AudioClip clip, float volume)
    {
        if (source != null && clip != null)
        {
            source.PlayOneShot(clip, volume);
        }
    }

    static AudioClip Tone(string name, float startHz, float endHz, float seconds, bool square)
    {
        const int rate = 44100;
        int count = Mathf.Max(1, Mathf.RoundToInt(rate * seconds));
        var data = new float[count];
        float phase = 0f;
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)count;
            float hz = Mathf.Lerp(startHz, endHz, t);
            phase += 2f * Mathf.PI * hz / rate;
            float wave = Mathf.Sin(phase);
            if (square) wave = wave >= 0f ? 0.6f : -0.6f;
            float envelope = Mathf.Min(1f, i / 200f) * (1f - t);   // плавный старт и затухание
            data[i] = wave * envelope;
        }
        var clip = AudioClip.Create(name, count, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }
}

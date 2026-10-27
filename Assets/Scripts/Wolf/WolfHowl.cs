using UnityEngine;

public class WolfHowl : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] private float volume = 0.25f;
    private AudioSource source;
    private AudioClip clip;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        if (source == null)
            source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 1f;
        source.minDistance = 3f;
        source.maxDistance = 45f;
        const int sampleRate = 22050;
        float[] samples = new float[sampleRate];
        float phase = 0f;
        for (int i = 0; i < samples.Length; i++)
        {
            float t = (float)i / sampleRate;
            float frequency = Mathf.Lerp(480f, 260f, t) + Mathf.Sin(t * 32f) * 12f;
            phase += 2f * Mathf.PI * frequency / sampleRate;
            float envelope = Mathf.Sin(t * Mathf.PI);
            samples[i] = (Mathf.Sin(phase) + Mathf.Sin(phase * 2f) * 0.2f) * envelope * 0.5f;
        }
        clip = AudioClip.Create("Wolf howl", samples.Length, 1, sampleRate, false);
        clip.SetData(samples, 0);
    }

    public void Play() => source.PlayOneShot(clip, volume);
    private void OnDestroy()
    {
        if (clip != null)
            Destroy(clip);
    }
}
using UnityEngine;

[CreateAssetMenu(fileName = "SFX_Event_", menuName = "NeoSurvive/Audio/Game Sound Event")]
public class GameSoundEventDefinition : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string eventKey = "ui.hover";

    [Header("Clips")]
    [SerializeField] private AudioClip[] clips;

    [Header("Playback")]
    [SerializeField, Range(0f, 1f)] private float volume = 1f;
    [SerializeField] private bool randomPitch = false;
    [SerializeField, Range(0.5f, 2f)] private float pitchMin = 0.95f;
    [SerializeField, Range(0.5f, 2f)] private float pitchMax = 1.05f;

    public string EventKey => eventKey;
    public float Volume => volume;

    public bool HasClip
    {
        get
        {
            return clips != null && clips.Length > 0 && clips[0] != null;
        }
    }

    public AudioClip PickClip()
    {
        if (clips == null || clips.Length == 0) return null;
        if (clips.Length == 1) return clips[0];
        return clips[Random.Range(0, clips.Length)];
    }

    public float PickPitch()
    {
        if (!randomPitch) return 1f;
        return Random.Range(pitchMin, pitchMax);
    }
}

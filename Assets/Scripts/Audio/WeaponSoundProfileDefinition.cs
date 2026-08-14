using UnityEngine;

[CreateAssetMenu(fileName = "SFX_Weapon_", menuName = "NeoSurvive/Audio/Weapon Sound Profile")]
public class WeaponSoundProfileDefinition : ScriptableObject
{
    public enum WeaponSoundType
    {
        Equip,
        LevelUp,
        Use,
        Hit,
        SustainLoop
    }

    [Header("Identity")]
    [Min(1)]
    [SerializeField] private int weaponId = 1;
    [SerializeField] private string weaponName;

    [Header("Per-Weapon Clips")]
    [SerializeField] private AudioClip[] equipClips;
    [SerializeField] private AudioClip[] levelUpClips;
    [SerializeField] private AudioClip[] useClips;
    [SerializeField] private AudioClip[] hitClips;
    [SerializeField] private AudioClip[] sustainLoopClips;

    [Header("Playback")]
    [SerializeField, Range(0f, 1f)] private float volume = 1f;
    [SerializeField] private bool randomPitch = true;
    [SerializeField, Range(0.5f, 2f)] private float pitchMin = 0.95f;
    [SerializeField, Range(0.5f, 2f)] private float pitchMax = 1.05f;

    public int WeaponId => weaponId;
    public float Volume => volume;

    public AudioClip PickClip(WeaponSoundType type)
    {
        AudioClip[] source = null;

        switch (type)
        {
            case WeaponSoundType.Equip: source = equipClips; break;
            case WeaponSoundType.LevelUp: source = levelUpClips; break;
            case WeaponSoundType.Use: source = useClips; break;
            case WeaponSoundType.Hit: source = hitClips; break;
            case WeaponSoundType.SustainLoop: source = sustainLoopClips; break;
        }

        if (source == null || source.Length == 0) return null;
        if (source.Length == 1) return source[0];
        return source[Random.Range(0, source.Length)];
    }

    public float PickPitch()
    {
        if (!randomPitch) return 1f;
        return Random.Range(pitchMin, pitchMax);
    }
}

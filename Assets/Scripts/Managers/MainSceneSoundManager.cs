using System;
using System.Collections.Generic;
using NeoSurvive.Weapon;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public class MainSceneSoundManager : MonoBehaviour
{
    public static MainSceneSoundManager Instance { get; private set; }

    public enum MainSfxEvent
    {
        PlayerHit,
        PlayerDeath,
        EnemyHit,
        EnemyDeath,

        ChestOpened,
        ItemPickup,
        GoldPickup,
        ExpPickup,
        DataChipPickup,
        PsychoCorruptionPickup,

        WeaponEquipGeneric,
        WeaponLevelUpGeneric,
        WeaponUseGeneric,
        WeaponHitGeneric,

        Error
    }

    [Serializable]
    public class SfxSlot
    {
        public MainSfxEvent eventType;
        public AudioClip[] clips;

        [Range(0f, 1f)]
        public float volume = 1f;

        public bool randomPitch = false;

        [Range(0.5f, 2f)]
        public float pitchMin = 0.95f;

        [Range(0.5f, 2f)]
        public float pitchMax = 1.05f;

        public AudioClip PickClip()
        {
            if (clips == null || clips.Length == 0) return null;
            if (clips.Length == 1) return clips[0];
            return clips[UnityEngine.Random.Range(0, clips.Length)];
        }

        public float PickPitch()
        {
            if (!randomPitch) return 1f;
            return UnityEngine.Random.Range(pitchMin, pitchMax);
        }
    }

    [Serializable]
    public class WeaponSfxSlot
    {
        [Header("Weapon Key")]
        [Min(1)] public int weaponId = 1;
        public string weaponName;

        [Header("Per-Weapon Clips")]
        public AudioClip[] equipClips;
        public AudioClip[] levelUpClips;
        public AudioClip[] useClips;
        public AudioClip[] hitClips;

        [Range(0f, 1f)]
        public float volume = 1f;

        public bool randomPitch = true;

        [Range(0.5f, 2f)]
        public float pitchMin = 0.95f;

        [Range(0.5f, 2f)]
        public float pitchMax = 1.05f;

        public AudioClip PickClip(WeaponSfxType type)
        {
            AudioClip[] source = null;
            switch (type)
            {
                case WeaponSfxType.Equip: source = equipClips; break;
                case WeaponSfxType.LevelUp: source = levelUpClips; break;
                case WeaponSfxType.Use: source = useClips; break;
                case WeaponSfxType.Hit: source = hitClips; break;
            }

            if (source == null || source.Length == 0) return null;
            if (source.Length == 1) return source[0];
            return source[UnityEngine.Random.Range(0, source.Length)];
        }

        public float PickPitch()
        {
            if (!randomPitch) return 1f;
            return UnityEngine.Random.Range(pitchMin, pitchMax);
        }
    }

    public enum WeaponSfxType
    {
        Equip,
        LevelUp,
        Use,
        Hit
    }

    [Header("Audio Output")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField, Range(0f, 1f)] private float masterSfxVolume = 1f;

    [Header("MainScene Generic SFX")]
    [SerializeField] private List<SfxSlot> sfxSlots = new List<SfxSlot>();

    [Header("MainScene Weapon SFX (Hacker/Cyborg)")]
    [Tooltip("무기별 슬롯. 무기 ID 기준으로 8~10개씩(해커/사이보그) 채우면 됩니다.")]
    [SerializeField] private List<WeaponSfxSlot> weaponSfxSlots = new List<WeaponSfxSlot>();

    private readonly Dictionary<MainSfxEvent, SfxSlot> slotMap = new Dictionary<MainSfxEvent, SfxSlot>();
    private readonly Dictionary<int, WeaponSfxSlot> weaponSlotMap = new Dictionary<int, WeaponSfxSlot>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (sfxSource == null)
            sfxSource = GetComponent<AudioSource>();

        if (sfxSource != null)
        {
            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
            sfxSource.spatialBlend = 0f;
        }

        RebuildMaps();
    }

    private void OnValidate()
    {
        RebuildMaps();
    }

    private void RebuildMaps()
    {
        slotMap.Clear();
        for (int i = 0; i < sfxSlots.Count; i++)
        {
            var slot = sfxSlots[i];
            if (slot == null) continue;
            slotMap[slot.eventType] = slot;
        }

        weaponSlotMap.Clear();
        for (int i = 0; i < weaponSfxSlots.Count; i++)
        {
            var slot = weaponSfxSlots[i];
            if (slot == null || slot.weaponId <= 0) continue;
            weaponSlotMap[slot.weaponId] = slot;
        }
    }

    public void Play(MainSfxEvent eventType)
    {
        if (sfxSource == null) return;
        if (!slotMap.TryGetValue(eventType, out var slot) || slot == null) return;

        AudioClip clip = slot.PickClip();
        if (clip == null) return;

        PlayOneShot(clip, slot.volume, slot.PickPitch());
    }

    public void PlayWeaponEquip(WeaponBase weapon)
    {
        if (!TryPlayWeapon(weapon?.weaponId ?? 0, WeaponSfxType.Equip))
            Play(MainSfxEvent.WeaponEquipGeneric);
    }

    public void PlayWeaponLevelUp(WeaponBase weapon)
    {
        if (!TryPlayWeapon(weapon?.weaponId ?? 0, WeaponSfxType.LevelUp))
            Play(MainSfxEvent.WeaponLevelUpGeneric);
    }

    public void PlayWeaponUse(WeaponBase weapon)
    {
        if (!TryPlayWeapon(weapon?.weaponId ?? 0, WeaponSfxType.Use))
            Play(MainSfxEvent.WeaponUseGeneric);
    }

    public void PlayWeaponUseById(int weaponId)
    {
        if (!TryPlayWeapon(weaponId, WeaponSfxType.Use))
            Play(MainSfxEvent.WeaponUseGeneric);
    }

    public void PlayWeaponHit(WeaponBase weapon)
    {
        if (!TryPlayWeapon(weapon?.weaponId ?? 0, WeaponSfxType.Hit))
            Play(MainSfxEvent.WeaponHitGeneric);
    }

    private bool TryPlayWeapon(int weaponId, WeaponSfxType type)
    {
        if (sfxSource == null || weaponId <= 0) return false;
        if (!weaponSlotMap.TryGetValue(weaponId, out var slot) || slot == null) return false;

        AudioClip clip = slot.PickClip(type);
        if (clip == null) return false;

        PlayOneShot(clip, slot.volume, slot.PickPitch());
        return true;
    }

    private void PlayOneShot(AudioClip clip, float slotVolume, float pitch)
    {
        float settingsSfx = SettingsManager.Instance != null ? SettingsManager.Instance.sfxVolume : 1f;

        float prevPitch = sfxSource.pitch;
        sfxSource.pitch = pitch;
        sfxSource.PlayOneShot(clip, slotVolume * masterSfxVolume * settingsSfx);
        sfxSource.pitch = prevPitch;
    }

    // Inspector wrappers (generic)
    public void PlayPlayerHit() => Play(MainSfxEvent.PlayerHit);
    public void PlayPlayerDeath() => Play(MainSfxEvent.PlayerDeath);
    public void PlayEnemyHit() => Play(MainSfxEvent.EnemyHit);
    public void PlayEnemyDeath() => Play(MainSfxEvent.EnemyDeath);
    public void PlayChestOpened() => Play(MainSfxEvent.ChestOpened);
    public void PlayItemPickup() => Play(MainSfxEvent.ItemPickup);
    public void PlayGoldPickup() => Play(MainSfxEvent.GoldPickup);
    public void PlayExpPickup() => Play(MainSfxEvent.ExpPickup);
    public void PlayDataChipPickup() => Play(MainSfxEvent.DataChipPickup);
    public void PlayPsychoCorruptionPickup() => Play(MainSfxEvent.PsychoCorruptionPickup);
}

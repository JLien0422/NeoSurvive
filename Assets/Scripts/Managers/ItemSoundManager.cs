using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public class ItemSoundManager : MonoBehaviour
{
    public static ItemSoundManager Instance { get; private set; }

    public enum ItemSfxEvent
    {
        ChestOpened,
        ItemPickup,
        GoldPickup,
        ExpPickup,
        DataChipPickup,
        PsychoCorruptionPickup
    }

    [Serializable]
    public class SfxSlot
    {
        public ItemSfxEvent eventType;
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

    [Header("Audio Output")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField, Range(0f, 1f)] private float masterSfxVolume = 1f;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;

    [Header("Item Pickup SFX Catalog")]
    [SerializeField] private List<SfxSlot> sfxSlots = new List<SfxSlot>();

    private readonly Dictionary<ItemSfxEvent, SfxSlot> slotMap = new Dictionary<ItemSfxEvent, SfxSlot>();

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

        RebuildMap();
    }

    private void OnValidate()
    {
        RebuildMap();
    }

    private void RebuildMap()
    {
        slotMap.Clear();
        for (int i = 0; i < sfxSlots.Count; i++)
        {
            var slot = sfxSlots[i];
            if (slot == null) continue;
            slotMap[slot.eventType] = slot;
        }
    }

    public void Play(ItemSfxEvent eventType)
    {
        if (sfxSource == null) return;

        if (!slotMap.TryGetValue(eventType, out var slot) || slot == null)
        {
            if (enableDebugLogs)
                Debug.LogWarning($"[ItemSoundManager] Slot not found for event: {eventType}");
            return;
        }

        AudioClip clip = slot.PickClip();
        if (clip == null)
        {
            if (enableDebugLogs)
                Debug.LogWarning($"[ItemSoundManager] Clip not set for event: {eventType}");
            return;
        }

        float prevPitch = sfxSource.pitch;
        sfxSource.pitch = slot.PickPitch();
        sfxSource.PlayOneShot(clip, slot.volume * masterSfxVolume);
        sfxSource.pitch = prevPitch;
    }

    public void PlayChestOpened() => Play(ItemSfxEvent.ChestOpened);
    public void PlayItemPickup() => Play(ItemSfxEvent.ItemPickup);
    public void PlayGoldPickup() => Play(ItemSfxEvent.GoldPickup);
    public void PlayExpPickup() => Play(ItemSfxEvent.ExpPickup);
    public void PlayDataChipPickup() => Play(ItemSfxEvent.DataChipPickup);
    public void PlayPsychoCorruptionPickup() => Play(ItemSfxEvent.PsychoCorruptionPickup);
}

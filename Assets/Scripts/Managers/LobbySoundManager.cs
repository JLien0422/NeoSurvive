using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public class LobbySoundManager : MonoBehaviour
{
    public static LobbySoundManager Instance { get; private set; }

    public enum LobbySfxEvent
    {
        // 공통 UI 이벤트
        UIHover,
        UIClick,
        UICancel,
        UIConfirm,
        UIBack,

        TabSwitch,
        PanelOpen,
        PanelClose,

        // 캐릭터 선택 탭 관련 이벤트
        CharacterHover,
        CharacterSelect,
        CharacterDeselect,

        // 메인 로비 탭 관련 이벤트
        LobbyCreate,
        LobbyJoin,
        LobbyLeave,
        LobbyReadyOn,
        LobbyReadyOff,
        LobbyStart,

        // 설정 탭 관련 이벤트
        SettingsOpen,
        SettingsClose,
        SettingsTabSwitch,
        SettingsApply,
        UpgradeBuy,

        원하는거근데한글은안됨,

        Error
    }

    [Serializable]
    public class SfxSlot
    {
        public LobbySfxEvent eventType;
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
    [SerializeField] private AudioSource uiSfxSource;
    [SerializeField, Range(0f, 1f)] private float masterUiVolume = 1f;

    [Header("Lobby UI SFX Catalog")]
    [Tooltip("디자이너가 이벤트별 AudioClip을 넣는 슬롯 목록")]
    [SerializeField] private List<SfxSlot> sfxSlots = new List<SfxSlot>();

    private readonly Dictionary<LobbySfxEvent, SfxSlot> slotMap = new Dictionary<LobbySfxEvent, SfxSlot>();

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

        if (uiSfxSource == null)
            uiSfxSource = GetComponent<AudioSource>();

        if (uiSfxSource != null)
        {
            uiSfxSource.playOnAwake = false;
            uiSfxSource.loop = false;
            uiSfxSource.spatialBlend = 0f;
        }

        RebuildSlotMap();
    }

    private void OnValidate()
    {
        RebuildSlotMap();
    }

    private void RebuildSlotMap()
    {
        slotMap.Clear();
        for (int i = 0; i < sfxSlots.Count; i++)
        {
            var slot = sfxSlots[i];
            if (slot == null) continue;
            slotMap[slot.eventType] = slot;
        }
    }

    public void Play(LobbySfxEvent eventType)
    {
        if (uiSfxSource == null) return;

        if (!slotMap.TryGetValue(eventType, out var slot) || slot == null)
            return;

        AudioClip clip = slot.PickClip();
        if (clip == null) return;

        float prevPitch = uiSfxSource.pitch;
        uiSfxSource.pitch = slot.PickPitch();
        uiSfxSource.PlayOneShot(clip, slot.volume * masterUiVolume);
        uiSfxSource.pitch = prevPitch;
    }

    // Inspector-friendly wrappers
    public void PlayHover() => Play(LobbySfxEvent.UIHover);
    public void PlayClick() => Play(LobbySfxEvent.UIClick);
    public void PlayCancel() => Play(LobbySfxEvent.UICancel);
    public void PlayConfirm() => Play(LobbySfxEvent.UIConfirm);
    public void PlayBack() => Play(LobbySfxEvent.UIBack);
    public void PlayTabSwitch() => Play(LobbySfxEvent.TabSwitch);
    public void PlayPanelOpen() => Play(LobbySfxEvent.PanelOpen);
    public void PlayPanelClose() => Play(LobbySfxEvent.PanelClose);
    public void PlayCharacterHover() => Play(LobbySfxEvent.CharacterHover);
    public void PlayCharacterSelect() => Play(LobbySfxEvent.CharacterSelect);
    public void PlayCharacterDeselect() => Play(LobbySfxEvent.CharacterDeselect);
    public void PlayLobbyCreate() => Play(LobbySfxEvent.LobbyCreate);
    public void PlayLobbyJoin() => Play(LobbySfxEvent.LobbyJoin);
    public void PlayLobbyLeave() => Play(LobbySfxEvent.LobbyLeave);
    public void PlayLobbyReadyOn() => Play(LobbySfxEvent.LobbyReadyOn);
    public void PlayLobbyReadyOff() => Play(LobbySfxEvent.LobbyReadyOff);
    public void PlayLobbyStart() => Play(LobbySfxEvent.LobbyStart);
    public void PlaySettingsOpen() => Play(LobbySfxEvent.SettingsOpen);
    public void PlaySettingsClose() => Play(LobbySfxEvent.SettingsClose);
    public void PlaySettingsTabSwitch() => Play(LobbySfxEvent.SettingsTabSwitch);
    public void PlaySettingsApply() => Play(LobbySfxEvent.SettingsApply);
    public void PlayUpgradeBuy() => Play(LobbySfxEvent.UpgradeBuy);
    public void PlayError() => Play(LobbySfxEvent.Error);

    public bool HasClip(LobbySfxEvent eventType)
    {
        if (!slotMap.TryGetValue(eventType, out var slot) || slot == null)
            return false;
        return slot.clips != null && slot.clips.Length > 0 && slot.clips[0] != null;
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using NeoSurvive.Weapon;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public class InGameSoundManager : MonoBehaviour
{
  public static InGameSoundManager Instance { get; private set; }

  public enum InGameSfxEvent
  {
    WeaponEquipGeneric,
    WeaponLevelUpGeneric,
    WeaponUseGeneric,
    WeaponHitGeneric,

    LinkPistolFire,
    PlasmaRifleFire,
    AIDroneFire,
    DataScramblerFire,
    TacticalTurretFire,
    EMPPulseGeneratorFire,

    // Cyborg
    LaserSwordFire,
    BoltLauncherFire,
    BoosterKnuckleFire,
    BlastBreathFire,
    ChainSawFire,
    GravityHammerFire,
    LightningStrikeFire,
    PlasmaPhotonGunFire,
    SurgeBladeFire,
    TeslaCoilArmorFire,
    EnergyShieldFire
  }

  [Serializable]
  public class SfxSlot
  {
    public InGameSfxEvent eventType;
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
  [SerializeField] private bool enableDebugLogs = false;

  [Header("InGame Weapon SFX Catalog")]
  [Tooltip("무기 이벤트별 AudioClip 슬롯")]
  [SerializeField] private List<SfxSlot> sfxSlots = new List<SfxSlot>();

  private readonly Dictionary<InGameSfxEvent, SfxSlot> slotMap = new Dictionary<InGameSfxEvent, SfxSlot>();

  private void Awake()
  {
    if (Instance == null)
    {
      Instance = this;
      DontDestroyOnLoad(transform.root.gameObject);
      if (enableDebugLogs)
      {
        Debug.Log($"[InGameSoundManager] Instance set: {name} (root: {transform.root.name})");
      }
    }
    else if (Instance != this)
    {
      Debug.LogWarning($"[InGameSoundManager] Duplicate instance detected. Existing={Instance.name}, New={name}. New object will be destroyed.");
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

  public void Play(InGameSfxEvent eventType)
  {
    if (sfxSource == null)
    {
      if (enableDebugLogs) Debug.LogWarning("[InGameSoundManager] sfxSource is null.");
      return;
    }

    if (!slotMap.TryGetValue(eventType, out var slot) || slot == null)
    {
      if (enableDebugLogs) Debug.LogWarning($"[InGameSoundManager] No slot mapped for event: {eventType}");
      return;
    }

    AudioClip clip = slot.PickClip();
    if (clip == null)
    {
      if (enableDebugLogs) Debug.LogWarning($"[InGameSoundManager] Slot has no valid clip: {eventType}");
      return;
    }

    float settingsSfx = SettingsManager.Instance != null ? SettingsManager.Instance.sfxVolume : 1f;
    if (enableDebugLogs)
    {
      Debug.Log($"[InGameSoundManager] Play {eventType} clip={clip.name} slotVol={slot.volume} masterVol={masterSfxVolume} settingsSfx={settingsSfx} listenerVol={AudioListener.volume}");
    }

    float prevPitch = sfxSource.pitch;
    sfxSource.pitch = slot.PickPitch();
    sfxSource.PlayOneShot(clip, slot.volume * masterSfxVolume * settingsSfx);
    sfxSource.pitch = prevPitch;
  }

  [ContextMenu("Debug/Test LinkPistolFire")]
  private void DebugTestLinkPistolFire()
  {
    Play(InGameSfxEvent.LinkPistolFire);
  }

  // Weapon wrappers
  public void PlayWeaponEquip(WeaponBase weapon) => Play(InGameSfxEvent.WeaponEquipGeneric);
  public void PlayWeaponLevelUp(WeaponBase weapon) => Play(InGameSfxEvent.WeaponLevelUpGeneric);
  public void PlayWeaponUse(WeaponBase weapon) => Play(InGameSfxEvent.WeaponUseGeneric);
  public void PlayWeaponUseById(int weaponId) => Play(InGameSfxEvent.WeaponUseGeneric);
  public void PlayWeaponHit(WeaponBase weapon) => Play(InGameSfxEvent.WeaponHitGeneric);

  public void PlayWeaponEquipGeneric() => Play(InGameSfxEvent.WeaponEquipGeneric);
  public void PlayWeaponLevelUpGeneric() => Play(InGameSfxEvent.WeaponLevelUpGeneric);
  public void PlayWeaponUseGeneric() => Play(InGameSfxEvent.WeaponUseGeneric);
  public void PlayWeaponHitGeneric() => Play(InGameSfxEvent.WeaponHitGeneric);

  public void PlayLinkPistolFire() => Play(InGameSfxEvent.LinkPistolFire);
  public void PlayPlasmaRifleFire() => Play(InGameSfxEvent.PlasmaRifleFire);
  public void PlayAIDroneFire() => Play(InGameSfxEvent.AIDroneFire);
  public void PlayDataScramblerFire() => Play(InGameSfxEvent.DataScramblerFire);
  public void PlayTacticalTurretFire() => Play(InGameSfxEvent.TacticalTurretFire);
  public void PlayEMPPulseGeneratorFire() => Play(InGameSfxEvent.EMPPulseGeneratorFire);

  // Cyborg
  public void PlayLaserSwordFire() => Play(InGameSfxEvent.LaserSwordFire);
  public void PlayBoltLauncherFire() => Play(InGameSfxEvent.BoltLauncherFire);
  public void PlayBoosterKnuckleFire() => Play(InGameSfxEvent.BoosterKnuckleFire);
  public void PlayBlastBreathFire() => Play(InGameSfxEvent.BlastBreathFire);
  public void PlayChainSawFire() => Play(InGameSfxEvent.ChainSawFire);
  public void PlayGravityHammerFire() => Play(InGameSfxEvent.GravityHammerFire);
  public void PlayLightningStrikeFire() => Play(InGameSfxEvent.LightningStrikeFire);
  public void PlayPlasmaPhotonGunFire() => Play(InGameSfxEvent.PlasmaPhotonGunFire);
  public void PlaySurgeBladeFire() => Play(InGameSfxEvent.SurgeBladeFire);
  public void PlayTeslaCoilArmorFire() => Play(InGameSfxEvent.TeslaCoilArmorFire);
  public void PlayEnergyShieldFire() => Play(InGameSfxEvent.EnergyShieldFire);

  public bool HasClip(InGameSfxEvent eventType)
  {
    if (!slotMap.TryGetValue(eventType, out var slot) || slot == null)
      return false;
    return slot.clips != null && slot.clips.Length > 0 && slot.clips[0] != null;
  }
}

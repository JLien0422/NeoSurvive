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

    // Hacker
    AIDroneFire,
    AIDroneSpawn,
    AIDroneDestroy,
    DataOptimizationSpawn,
    DataOptimizationAttack,
    DataOptimizationDestroy,
    DataScramblerAttack,
    DigitalShieldSpawn,
    DigitalShieldAttack,
    EMPPulseAttack,
    EMPGrenadeAttack,
    LinkPistolAttack,
    PlasmaRifleAttack,
    TacticalTurretSpawn,
    TacticalTurretAttack,

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
    EnergyShieldFire,

    // New additions
    EnergyShieldSpawn,
    ProtectiveShieldSpawn,
    Error
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
  
  // (추가) 같은 사운드가 짧은 시간 내에 중첩되어 볼륨이 커지는 현상 방지
  private readonly Dictionary<InGameSfxEvent, float> lastPlayTimes = new Dictionary<InGameSfxEvent, float>();

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

    // 중첩 방지 (0.05초 이내에 동일한 사운드가 여러 번 재생되어 볼륨이 폭증하는 것 방지)
    if (lastPlayTimes.TryGetValue(eventType, out float lastTime))
    {
        if (Time.unscaledTime - lastTime < 0.05f)
        {
            return; 
        }
    }
    lastPlayTimes[eventType] = Time.unscaledTime;

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
    Play(InGameSfxEvent.LinkPistolAttack);
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

  public void PlayAIDroneFire() => Play(InGameSfxEvent.AIDroneFire);
  public void PlayAIDroneSpawn() => Play(InGameSfxEvent.AIDroneSpawn);
  public void PlayAIDroneDestroy() => Play(InGameSfxEvent.AIDroneDestroy);

  public void PlayDataOptimizationSpawn() => Play(InGameSfxEvent.DataOptimizationSpawn);
  public void PlayDataOptimizationAttack() => Play(InGameSfxEvent.DataOptimizationAttack);
  public void PlayDataOptimizationDestroy() => Play(InGameSfxEvent.DataOptimizationDestroy);

  public void PlayDataScramblerAttack() => Play(InGameSfxEvent.DataScramblerAttack);

  public void PlayDigitalShieldSpawn() => Play(InGameSfxEvent.DigitalShieldSpawn);
  public void PlayDigitalShieldAttack() => Play(InGameSfxEvent.DigitalShieldAttack);

  public void PlayEMPPulseAttack() => Play(InGameSfxEvent.EMPPulseAttack);

  public void PlayEMPGrenadeAttack() => Play(InGameSfxEvent.EMPGrenadeAttack);

  public void PlayLinkPistolAttack() => Play(InGameSfxEvent.LinkPistolAttack);

  public void PlayPlasmaRifleAttack() => Play(InGameSfxEvent.PlasmaRifleAttack);

  public void PlayTacticalTurretSpawn() => Play(InGameSfxEvent.TacticalTurretSpawn);
  public void PlayTacticalTurretAttack() => Play(InGameSfxEvent.TacticalTurretAttack);

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

  public void PlayEnergyShieldSpawn() => Play(InGameSfxEvent.EnergyShieldSpawn);
  public void PlayProtectiveShieldSpawn() => Play(InGameSfxEvent.ProtectiveShieldSpawn);
  public void PlayError() => Play(InGameSfxEvent.Error);

  public bool HasClip(InGameSfxEvent eventType)
  {
    if (!slotMap.TryGetValue(eventType, out var slot) || slot == null)
      return false;
    return slot.clips != null && slot.clips.Length > 0 && slot.clips[0] != null;
  }
}

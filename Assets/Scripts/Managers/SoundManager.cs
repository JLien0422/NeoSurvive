using System.Collections.Generic;
using NeoSurvive.Weapon;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    public const string KeyUiHover = "ui.hover";
    public const string KeyUiClick = "ui.click";
    public const string KeyUiCancel = "ui.cancel";
    public const string KeyUiConfirm = "ui.confirm";
    public const string KeyUiBack = "ui.back";
    public const string KeyUiTabSwitch = "ui.tab.switch";
    public const string KeyUiPanelOpen = "ui.panel.open";
    public const string KeyUiPanelClose = "ui.panel.close";

    public const string KeyPlayerHit = "player.hit";
    public const string KeyPlayerDeath = "player.death";
    public const string KeyGameOver = "game.over";
    public const string KeyEnemyHit = "enemy.hit";
    public const string KeyEnemyDeath = "enemy.death";
    public const string KeyChestOpened = "pickup.chest.opened";
    public const string KeyItemPickup = "pickup.item";
    public const string KeyGoldPickup = "pickup.gold";
    public const string KeyExpPickup = "pickup.exp";
    public const string KeyDataChipPickup = "pickup.datachip";
    public const string KeyPsychoCorruptionPickup = "pickup.psycho";

    public const string KeyWeaponEquipGeneric = "weapon.generic.equip";
    public const string KeyWeaponLevelUpGeneric = "weapon.generic.levelup";
    public const string KeyWeaponUseGeneric = "weapon.generic.use";
    public const string KeyWeaponHitGeneric = "weapon.generic.hit";

    public const string KeyError = "system.error";

    [Header("BGM")]
    [SerializeField] private AudioClip[] lobbyBgmClips;
    [SerializeField] private AudioClip[] inGameBgmClips;
    private int currentBgmIndex = 0;
    private AudioSource bgmSource;
    private string lastSceneType = "";
    private int currentPhaseBgm = 0;

    [Header("SFX")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField, Range(0f, 1f)] private float masterSfxVolume = 1f;

    [Header("Game SFX Catalog (ScriptableObject)")]
    [SerializeField] private List<GameSoundEventDefinition> eventDefinitions = new List<GameSoundEventDefinition>();
    [SerializeField] private bool autoLoadEventDefinitionsFromAssets = true;
    [SerializeField] private string eventDefinitionsAssetsPath = "Assets/Data/Sounds/Ingame";

    [Header("Weapon SFX Catalog (ScriptableObject)")]
    [SerializeField] private List<WeaponSoundProfileDefinition> weaponSoundProfiles = new List<WeaponSoundProfileDefinition>();
    [SerializeField] private bool autoLoadWeaponProfilesFromAssets = true;
    [SerializeField] private string weaponProfilesAssetsPath = "Assets/Data/Sounds/Ingame";

    private readonly Dictionary<string, GameSoundEventDefinition> eventMap = new Dictionary<string, GameSoundEventDefinition>();
    private readonly Dictionary<int, WeaponSoundProfileDefinition> weaponProfileMap = new Dictionary<int, WeaponSoundProfileDefinition>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(transform.root.gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        bgmSource = GetComponent<AudioSource>();
        bgmSource.spatialBlend = 0f;
        bgmSource.playOnAwake = false;
        bgmSource.loop = false;

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
        }

        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
        sfxSource.spatialBlend = 0f;

        PreloadBgmClips();
        RebuildMaps();
    }

    private void OnValidate()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            RefreshCatalogsFromAssets();
        }
#endif
        RebuildMaps();
    }

    private void Start()
    {
        UpdateVolume();

        SceneManager.sceneLoaded += OnSceneLoaded;
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string sceneType = scene.name.Contains("Lobby") ? "Lobby" : "InGame";
        if (lastSceneType != sceneType)
        {
            lastSceneType = sceneType;
            currentBgmIndex = 0;
            currentPhaseBgm = 0;

            if (sceneType == "Lobby")
            {
                StopBgm();

                if (lobbyBgmClips != null && lobbyBgmClips.Length > 0)
                {
                    PlayNextBgm();
                }
            }
            else
            {
                PlayPhaseBgm(1);
            }
        }
    }

    private void Update()
    {
        if (lastSceneType != "Lobby") return;

        AudioClip[] activeClips = lastSceneType == "Lobby" ? lobbyBgmClips : inGameBgmClips;
        if (bgmSource != null && !bgmSource.isPlaying && activeClips != null && activeClips.Length > 0)
        {
            PlayNextBgm();
        }
    }

    private void RebuildMaps()
    {
        eventMap.Clear();
        RegisterEventDefinitions(eventDefinitions);

        weaponProfileMap.Clear();
        RegisterWeaponProfiles(weaponSoundProfiles);
    }

    private void PreloadBgmClips()
    {
        PreloadClips(lobbyBgmClips);
        PreloadClips(inGameBgmClips);
    }

    private static void PreloadClips(AudioClip[] clips)
    {
        if (clips == null) return;

        for (int i = 0; i < clips.Length; i++)
        {
            AudioClip clip = clips[i];
            if (clip == null) continue;

            clip.LoadAudioData();
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Refresh Sound Catalogs From Assets Path")]
    private void RefreshCatalogsFromAssets()
    {
        bool changed = false;

        if (autoLoadEventDefinitionsFromAssets)
        {
            var loadedEvents = LoadAssetsInFolder<GameSoundEventDefinition>(eventDefinitionsAssetsPath, nameof(eventDefinitionsAssetsPath));
            if (!ReferenceEquals(loadedEvents, null))
            {
                eventDefinitions = loadedEvents;
                changed = true;
            }
        }

        if (autoLoadWeaponProfilesFromAssets)
        {
            var loadedProfiles = LoadAssetsInFolder<WeaponSoundProfileDefinition>(weaponProfilesAssetsPath, nameof(weaponProfilesAssetsPath));
            if (!ReferenceEquals(loadedProfiles, null))
            {
                weaponSoundProfiles = loadedProfiles;
                changed = true;
            }
        }

        if (changed)
        {
            EditorUtility.SetDirty(this);
        }
    }

    private static List<T> LoadAssetsInFolder<T>(string rawPath, string fieldName) where T : UnityEngine.Object
    {
        if (string.IsNullOrWhiteSpace(rawPath))
        {
            Debug.LogWarning($"[SoundManager] {fieldName}가 비어 있습니다. Assets 기준 폴더 경로를 입력하세요.");
            return new List<T>();
        }

        string path = rawPath.Trim().Replace('\\', '/');
        if (!path.StartsWith("Assets/"))
        {
            Debug.LogWarning($"[SoundManager] {fieldName}는 Assets/로 시작해야 합니다: {rawPath}");
            return new List<T>();
        }

        if (!AssetDatabase.IsValidFolder(path))
        {
            Debug.LogWarning($"[SoundManager] 경로가 유효한 폴더가 아닙니다: {path}");
            return new List<T>();
        }

        string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { path });
        var list = new List<T>(guids.Length);

        for (int i = 0; i < guids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
            T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset != null)
            {
                list.Add(asset);
            }
        }

        list.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        return list;
    }
#endif

    private void RegisterEventDefinitions(IList<GameSoundEventDefinition> definitions)
    {
        if (definitions == null) return;

        for (int i = 0; i < definitions.Count; i++)
        {
            GameSoundEventDefinition def = definitions[i];
            if (def == null) continue;

            string key = NormalizeKey(def.EventKey);
            if (string.IsNullOrEmpty(key)) continue;

            eventMap[key] = def;
        }
    }

    private void RegisterWeaponProfiles(IList<WeaponSoundProfileDefinition> profiles)
    {
        if (profiles == null) return;

        for (int i = 0; i < profiles.Count; i++)
        {
            WeaponSoundProfileDefinition profile = profiles[i];
            if (profile == null || profile.WeaponId <= 0) continue;
            weaponProfileMap[profile.WeaponId] = profile;
        }
    }

    private static string NormalizeKey(string key)
    {
        return string.IsNullOrWhiteSpace(key) ? string.Empty : key.Trim().ToLowerInvariant();
    }

    private void PlayNextBgm()
    {
        if (bgmSource == null) return;
        AudioClip[] activeClips = lastSceneType == "Lobby" ? lobbyBgmClips : inGameBgmClips;

        if (activeClips == null || activeClips.Length == 0) return;

        if (currentBgmIndex >= activeClips.Length)
        {
            currentBgmIndex = 0;
        }

        bgmSource.clip = activeClips[currentBgmIndex];
        bgmSource.Play();
        currentBgmIndex++;
    }

    public void PlayPhaseBgm(int phase)
    {
        if (bgmSource == null) return;
        if (phase < 1 || phase > 5) return;

        if (currentPhaseBgm == phase)
            return;

        if (inGameBgmClips == null || inGameBgmClips.Length == 0)
            return;

        int startIndex = (phase - 1) * 2;
        if (startIndex >= inGameBgmClips.Length)
            return;

        int variantCount = Mathf.Min(2, inGameBgmClips.Length - startIndex);
        int selectedIndex = startIndex + Random.Range(0, variantCount);
        AudioClip selectedClip = inGameBgmClips[selectedIndex];

        if (selectedClip == null)
            return;

        if (bgmSource.isPlaying)
        {
            bgmSource.Stop();
        }

        lastSceneType = "InGame";
        currentPhaseBgm = phase;
        currentBgmIndex = selectedIndex + 1;
        bgmSource.clip = selectedClip;
        UpdateVolume();
        bgmSource.Play();
    }

    private void StopBgm()
    {
        if (bgmSource == null) return;

        bgmSource.Stop();
        bgmSource.clip = null;
    }

    public void UpdateVolume()
    {
        if (bgmSource == null) return;

        if (SettingsManager.Instance != null)
        {
            bgmSource.volume = SettingsManager.Instance.bgmVolume;
        }
        else
        {
            bgmSource.volume = 1f;
        }
    }

    public void Play(string eventKey)
    {
        if (sfxSource == null) return;

        string normalizedKey = NormalizeKey(eventKey);
        if (string.IsNullOrEmpty(normalizedKey))
            return;

        if (!eventMap.TryGetValue(normalizedKey, out var eventDef) || eventDef == null)
            return;

        AudioClip clip = eventDef.PickClip();
        if (clip == null) return;

        PlayOneShot(clip, eventDef.Volume, eventDef.PickPitch());
    }

    public void Play(GameSoundEventDefinition eventDefinition)
    {
        if (eventDefinition == null)
            return;

        Play(eventDefinition.EventKey);
    }

    public void PlayWeaponEquip(WeaponBase weapon)
    {
        if (!TryPlayWeapon(weapon?.weaponId ?? 0, WeaponSoundProfileDefinition.WeaponSoundType.Equip))
            Play(KeyWeaponEquipGeneric);
    }

    public void PlayWeaponLevelUp(WeaponBase weapon)
    {
        if (!TryPlayWeapon(weapon?.weaponId ?? 0, WeaponSoundProfileDefinition.WeaponSoundType.LevelUp))
            Play(KeyWeaponLevelUpGeneric);
    }

    public void PlayWeaponUse(WeaponBase weapon)
    {
        if (!TryPlayWeapon(weapon?.weaponId ?? 0, WeaponSoundProfileDefinition.WeaponSoundType.Use))
            Play(KeyWeaponUseGeneric);
    }

    public void PlayWeaponUseById(int weaponId)
    {
        if (!TryPlayWeapon(weaponId, WeaponSoundProfileDefinition.WeaponSoundType.Use))
            Play(KeyWeaponUseGeneric);
    }

    public void PlayWeaponHit(WeaponBase weapon)
    {
        if (!TryPlayWeapon(weapon?.weaponId ?? 0, WeaponSoundProfileDefinition.WeaponSoundType.Hit))
            Play(KeyWeaponHitGeneric);
    }

    public bool ConfigureWeaponSustainLoop(AudioSource targetSource, WeaponBase weapon)
    {
        if (targetSource == null || weapon == null || weapon.weaponId <= 0) return false;
        if (!weaponProfileMap.TryGetValue(weapon.weaponId, out var profile) || profile == null) return false;

        AudioClip clip = profile.PickClip(WeaponSoundProfileDefinition.WeaponSoundType.SustainLoop);
        if (clip == null) return false;

        float settingsSfx = SettingsManager.Instance != null ? SettingsManager.Instance.sfxVolume : 1f;

        targetSource.playOnAwake = false;
        targetSource.loop = true;
        targetSource.clip = clip;
        targetSource.pitch = profile.PickPitch();
        targetSource.volume = profile.Volume * masterSfxVolume * settingsSfx;
        targetSource.Play();
        return true;
    }

    private bool TryPlayWeapon(int weaponId, WeaponSoundProfileDefinition.WeaponSoundType type)
    {
        if (sfxSource == null || weaponId <= 0) return false;
        if (!weaponProfileMap.TryGetValue(weaponId, out var profile) || profile == null) return false;

        AudioClip clip = profile.PickClip(type);
        if (clip == null) return false;

        PlayOneShot(clip, profile.Volume, profile.PickPitch());
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

    // UI wrappers
    public void PlayHover() => Play(KeyUiHover);
    public void PlayClick() => Play(KeyUiClick);
    public void PlayCancel() => Play(KeyUiCancel);
    public void PlayConfirm() => Play(KeyUiConfirm);
    public void PlayBack() => Play(KeyUiBack);
    public void PlayTabSwitch() => Play(KeyUiTabSwitch);
    public void PlayPanelOpen() => Play(KeyUiPanelOpen);
    public void PlayPanelClose() => Play(KeyUiPanelClose);

    // Game wrappers
    public void PlayPlayerHit() => Play(KeyPlayerHit);
    public void PlayPlayerDeath() => Play(KeyPlayerDeath);
    public void PlayGameOver() => Play(KeyGameOver);
    public void PlayEnemyHit() => Play(KeyEnemyHit);
    public void PlayEnemyDeath() => Play(KeyEnemyDeath);
    public void PlayChestOpened() => Play(KeyChestOpened);
    public void PlayItemPickup() => Play(KeyItemPickup);
    public void PlayGoldPickup() => Play(KeyGoldPickup);
    public void PlayExpPickup() => Play(KeyExpPickup);
    public void PlayDataChipPickup() => Play(KeyDataChipPickup);
    public void PlayPsychoCorruptionPickup() => Play(KeyPsychoCorruptionPickup);
    public void PlayError() => Play(KeyError);

    public bool HasClip(string eventKey)
    {
        string normalizedKey = NormalizeKey(eventKey);
        if (string.IsNullOrEmpty(normalizedKey))
            return false;

        if (!eventMap.TryGetValue(normalizedKey, out var eventDef) || eventDef == null)
            return false;

        return eventDef.HasClip;
    }
}

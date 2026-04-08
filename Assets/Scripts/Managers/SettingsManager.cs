using UnityEngine;
using System.Collections.Generic; // For List
using System.Linq; // For LINQ

// SettingsManager 클래스는 게임의 모든 설정을 관리합니다 (비디오, 오디오 등).
// 싱글톤으로 구현되어 어디서든 쉽게 접근할 수 있습니다.
public class SettingsManager : MonoBehaviour
{
  public static SettingsManager Instance { get; private set; }

  // 설정값 저장을 위한 Easy Save 키
  private const string KEY_RESOLUTION = "Settings_ResolutionIndex";
  private const string KEY_FULLSCREEN = "Settings_Fullscreen";
  private const string KEY_QUALITY = "Settings_QualityIndex";
  private const string KEY_VSYNC = "Settings_VSync";
  private const string KEY_TARGET_FPS = "Settings_TargetFPS";
  private const string KEY_BACKGROUND_FPS = "Settings_BackgroundFPS";
  private const string KEY_POST_PROCESSING = "Settings_PostProcessing";
  private const string KEY_SHOW_DAMAGE_NUMBERS = "Settings_ShowDamageNumbers";
  private const string KEY_EFFECT_OPACITY = "Settings_EffectOpacity";
  private const string KEY_SCREEN_SHAKE = "Settings_ScreenShake";
  private const string KEY_MASTER_VOLUME = "Settings_MasterVolume";
  private const string KEY_BGM_VOLUME = "Settings_BgmVolume";
  private const string KEY_SFX_VOLUME = "Settings_SfxVolume";
  private const string KEY_MUTE_ON_FOCUS_LOST = "Settings_MuteOnFocusLost";
  private const string KEY_PAUSE_ON_FOCUS_LOST = "Settings_PauseOnFocusLost";

  // 설정값 변수
  // 비디오
  public int resolutionIndex; // 해상도 선택
  public bool isFullscreen; // 전체화면
  public int qualityIndex; // 그래픽품질
  public bool vSyncEnabled; // 수직동기화
  public int targetFPS = 60; // 포그라운드 프레임 제한
  public int backgroundFPS = 30; // 백그라운드 프레임 제한
  public bool postProcessingEnabled = true; // 포스트 프로세싱
  public bool showDamageNumbers = true; // 데미지 숫자 표시
  [Range(0f, 1f)]
  public float effectOpacity = 1f; // 이펙트 투명도
  public bool screenShakeEnabled = true; // 화면 흔들림

  // 오디오
  public float masterVolume;
  public float bgmVolume;
  public float sfxVolume;
  public bool muteOnFocusLost = false; // 백그라운드 음소거

  // 게임플레이
  public bool pauseOnFocusLost = true; // 백그라운드 시 자동 일시정지

  // 사용 가능한 해상도 목록
  public Resolution[] resolutions;

  private void Awake()
  {
    // 싱글톤 패턴
    if (Instance == null)
    {
      Instance = this;
      // Managers가 루트가 아닌 경우 부모(Managers)를 유지시킴
      DontDestroyOnLoad(transform.root.gameObject);

      // 중복을 제거하고 해상도 목록을 가져옵니다.
      resolutions = Screen.resolutions.Select(resolution => new Resolution { width = resolution.width, height = resolution.height }).Distinct().ToArray();

      LoadSettingsAndApply(); // 저장된 설정 불러오기 및 적용
    }
    else
    {
      Destroy(gameObject);
    }
  }

  private void Start()
  {
    // FPS 설정이 첫 프레임에서 제대로 안 먹히는 경우가 있어서
    // Start()에서 한 번 더 강제 적용
    if (Instance == this)
    {
      StartCoroutine(ReapplyFPSSettings());
    }
  }

  /// <summary>
  /// FPS 설정을 한 프레임 지연 후 다시 적용 (초기화 버그 방지)
  /// </summary>
  private System.Collections.IEnumerator ReapplyFPSSettings()
  {
    yield return null; // 1프레임 대기
    Application.targetFrameRate = targetFPS;
    Debug.Log($"[SettingsManager] FPS 재적용 완료: {targetFPS}");
  }

  // 설정을 불러오고 즉시 적용하는 메서드
  public void LoadSettingsAndApply()
  {
    // Easy Save에서 값 불러오기 (기본값 설정 포함)
    resolutionIndex = ES3.Load(KEY_RESOLUTION, resolutions.Length - 1); // 기본값: 가장 높은 해상도
    isFullscreen = ES3.Load(KEY_FULLSCREEN, true); // 기본값: 전체화면
    qualityIndex = ES3.Load(KEY_QUALITY, QualitySettings.names.Length - 1); // 기본값: 가장 높은 품질
    vSyncEnabled = ES3.Load(KEY_VSYNC, false);
    targetFPS = ES3.Load(KEY_TARGET_FPS, 300);
    backgroundFPS = ES3.Load(KEY_BACKGROUND_FPS, 30);
    postProcessingEnabled = ES3.Load(KEY_POST_PROCESSING, true);
    showDamageNumbers = ES3.Load(KEY_SHOW_DAMAGE_NUMBERS, true);
    effectOpacity = ES3.Load(KEY_EFFECT_OPACITY, 1f);
    screenShakeEnabled = ES3.Load(KEY_SCREEN_SHAKE, true);
    masterVolume = ES3.Load(KEY_MASTER_VOLUME, 0.8f);
    bgmVolume = ES3.Load(KEY_BGM_VOLUME, 1f);
    sfxVolume = ES3.Load(KEY_SFX_VOLUME, 1f);
    muteOnFocusLost = ES3.Load(KEY_MUTE_ON_FOCUS_LOST, false);
    pauseOnFocusLost = ES3.Load(KEY_PAUSE_ON_FOCUS_LOST, true);

    Debug.Log("설정값을 불러왔습니다.");

    // 불러온 설정을 게임에 즉시 적용
    ApplyAllSettings();
  }

  // 모든 현재 설정값을 저장하는 메서드
  public void SaveSettings()
  {
    ES3.Save(KEY_RESOLUTION, resolutionIndex);
    ES3.Save(KEY_FULLSCREEN, isFullscreen);
    ES3.Save(KEY_QUALITY, qualityIndex);
    ES3.Save(KEY_VSYNC, vSyncEnabled);
    ES3.Save(KEY_TARGET_FPS, targetFPS);
    ES3.Save(KEY_BACKGROUND_FPS, backgroundFPS);
    ES3.Save(KEY_POST_PROCESSING, postProcessingEnabled);
    ES3.Save(KEY_SHOW_DAMAGE_NUMBERS, showDamageNumbers);
    ES3.Save(KEY_EFFECT_OPACITY, effectOpacity);
    ES3.Save(KEY_SCREEN_SHAKE, screenShakeEnabled);
    ES3.Save(KEY_MASTER_VOLUME, masterVolume);
    ES3.Save(KEY_BGM_VOLUME, bgmVolume);
    ES3.Save(KEY_SFX_VOLUME, sfxVolume);
    ES3.Save(KEY_MUTE_ON_FOCUS_LOST, muteOnFocusLost);
    ES3.Save(KEY_PAUSE_ON_FOCUS_LOST, pauseOnFocusLost);

    Debug.Log("설정값을 저장했습니다.");
  }

  // 현재 변수에 저장된 설정값들을 게임에 실제로 적용하는 메서드
  public void ApplyAllSettings()
  {
    // 비디오 설정 적용
    if (resolutionIndex < resolutions.Length)
    {
      Resolution resolution = resolutions[resolutionIndex];
      Screen.SetResolution(resolution.width, resolution.height, isFullscreen);
    }
    QualitySettings.SetQualityLevel(qualityIndex);

    // 수직동기화 설정
    QualitySettings.vSyncCount = vSyncEnabled ? 1 : 0;

    // 프레임 제한 설정
    Application.targetFrameRate = targetFPS;

    // 포스트 프로세싱은 씬의 PostProcessVolume 컴포넌트로 제어 (UI에서 처리)

    // 오디오 설정 적용 (마스터 볼륨)
    // AudioListener는 씬에 있는 모든 소리에 영향을 줍니다.
    AudioListener.volume = masterVolume;

    // SoundManager가 존재하면 BGM 볼륨도 적용합니다.
    if (SoundManager.Instance != null)
    {
      SoundManager.Instance.UpdateVolume();
    }

    Debug.Log("불러온 설정값을 게임에 모두 적용했습니다.");

    // BGM, SFX 볼륨은 각 오디오 소스나 AudioMixer에 개별적으로 적용해야 합니다.
    // 이 부분은 SoundManager 등과 연동 시 처리합니다.
  }

  // --- UI에서 호출할 개별 설정 변경 메서드들 ---

  public void SetResolution(int index)
  {
    resolutionIndex = index;
    ApplyAllSettings();
  }

  public void SetFullscreen(bool fullscreen)
  {
    isFullscreen = fullscreen;
    ApplyAllSettings();
  }

  public void SetQuality(int index)
  {
    qualityIndex = index;
    ApplyAllSettings();
  }

  public void SetMasterVolume(float volume)
  {
    masterVolume = volume;
    ApplyAllSettings();
  }

  public void SetBgmVolume(float volume)
  {
    bgmVolume = volume;
    // SoundManager가 있다면 볼륨을 즉시 업데이트
    if (SoundManager.Instance != null)
    {
      SoundManager.Instance.UpdateVolume();
    }
  }

  public void SetSfxVolume(float volume)
  {
    sfxVolume = volume;
    // TODO: 효과음 매니저와 연동
  }

  // 비디오 설정 메서드
  public void SetVSync(bool enabled)
  {
    vSyncEnabled = enabled;
    QualitySettings.vSyncCount = enabled ? 1 : 0;
  }

  public void SetTargetFPS(int fps)
  {
    targetFPS = fps;
    Application.targetFrameRate = fps;
  }

  public void SetBackgroundFPS(int fps)
  {
    backgroundFPS = fps;
    // 백그라운드 FPS는 OnApplicationFocus에서 처리
  }

  public void SetPostProcessing(bool enabled)
  {
    postProcessingEnabled = enabled;
    
    // PostProcessVolume 컴포넌트를 찾아서 활성화/비활성화
    #if UNITY_POST_PROCESSING_STACK_V2
    UnityEngine.Rendering.PostProcessing.PostProcessVolume volume = FindObjectOfType<UnityEngine.Rendering.PostProcessing.PostProcessVolume>();
    if (volume != null)
    {
      volume.enabled = enabled;
      Debug.Log($"[SettingsManager] Post Processing {(enabled ? "활성화" : "비활성화")}");
    }
    else
    {
      Debug.LogWarning("[SettingsManager] PostProcessVolume을 찾을 수 없습니다. 씬에 추가해 주세요.");
    }
    #else
    Debug.LogWarning("[SettingsManager] Post Processing Stack V2가 설치되지 않았습니다.");
    #endif
  }

  public void SetShowDamageNumbers(bool show)
  {
    showDamageNumbers = show;
    // UIManager에서 처리
  }

  public void SetEffectOpacity(float opacity)
  {
    effectOpacity = Mathf.Clamp01(opacity);
    // 이펙트 매니저에서 처리
  }

  public void SetScreenShake(bool enabled)
  {
    screenShakeEnabled = enabled;
    // 카메라 쉐이크 매니저에서 처리
  }

  // 오디오 설정 메서드
  public void SetMuteOnFocusLost(bool mute)
  {
    muteOnFocusLost = mute;
  }

  // 게임플레이 설정 메서드
  public void SetPauseOnFocusLost(bool pause)
  {
    pauseOnFocusLost = pause;
  }

  // 백그라운드 처리
  private void OnApplicationFocus(bool hasFocus)
  {
    if (!hasFocus)
    {
      // 백그라운드로 전환
      if (pauseOnFocusLost && GameManager.Instance != null)
      {
        // GameManager에 일시정지 요청
        // TODO: GameManager에 일시정지 메서드 추가 필요
      }

      if (muteOnFocusLost)
      {
        AudioListener.volume = 0f;
      }

      Application.targetFrameRate = backgroundFPS;
    }
    else
    {
      // 포그라운드로 복귀
      AudioListener.volume = masterVolume;
      Application.targetFrameRate = targetFPS;
    }
  }
}

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 설정 UI 관리 스크립트
/// 기획서에 따라 비디오, 오디오, 게임플레이, 계정 탭을 관리합니다.
/// </summary>
public class SettingsUI : MonoBehaviour
{
    [Header("탭 버튼")]
    public Button videoTabButton;
    public Button audioTabButton;
    public Button gameplayTabButton;
    public Button accountTabButton;

    [Header("탭 패널")]
    public GameObject videoTabPanel;
    public GameObject audioTabPanel;
    public GameObject gameplayTabPanel;
    public GameObject accountTabPanel;

    [Header("비디오 설정 UI")]
    public TMP_Dropdown resolutionDropdown;
    public Toggle fullscreenToggle;
    public Toggle vSyncToggle;
    public TMP_Dropdown targetFPSDropdown;
    public TMP_Dropdown backgroundFPSDropdown;
    public Toggle postProcessingToggle;
    public Toggle showDamageNumbersToggle;
    public Slider effectOpacitySlider;
    public TextMeshProUGUI effectOpacityText;
    public Toggle screenShakeToggle;

    [Header("오디오 설정 UI")]
    public Slider masterVolumeSlider;
    public TextMeshProUGUI masterVolumeText;
    public Slider bgmVolumeSlider;
    public TextMeshProUGUI bgmVolumeText;
    public Slider sfxVolumeSlider;
    public TextMeshProUGUI sfxVolumeText;
    public Toggle muteOnFocusLostToggle;

    [Header("게임플레이 설정 UI")]
    public Toggle pauseOnFocusLostToggle;

    [Header("계정 탭")]
    // 계정 탭은 UI만 있고 기능은 이현승이 구현

    private SettingsManager settingsManager;

    private void Awake()
    {
        settingsManager = SettingsManager.Instance;
        if (settingsManager == null)
        {
            Debug.LogError("[SettingsUI] SettingsManager를 찾을 수 없습니다!");
        }
    }

    private void Start()
    {
        InitializeTabs();
        InitializeVideoSettings();
        InitializeAudioSettings();
        InitializeGameplaySettings();
        
        // 기본적으로 비디오 탭 표시
        ShowTab(0);
    }

    /// <summary>
    /// 탭 초기화
    /// </summary>
    private void InitializeTabs()
    {
        if (videoTabButton != null)
            videoTabButton.onClick.AddListener(() => ShowTab(0));
        if (audioTabButton != null)
            audioTabButton.onClick.AddListener(() => ShowTab(1));
        if (gameplayTabButton != null)
            gameplayTabButton.onClick.AddListener(() => ShowTab(2));
        if (accountTabButton != null)
            accountTabButton.onClick.AddListener(() => ShowTab(3));
    }

    /// <summary>
    /// 탭 표시
    /// </summary>
    private void ShowTab(int tabIndex)
    {
        // 모든 탭 패널 비활성화
        if (videoTabPanel != null) videoTabPanel.SetActive(false);
        if (audioTabPanel != null) audioTabPanel.SetActive(false);
        if (gameplayTabPanel != null) gameplayTabPanel.SetActive(false);
        if (accountTabPanel != null) accountTabPanel.SetActive(false);

        // 선택된 탭만 활성화
        switch (tabIndex)
        {
            case 0: // 비디오
                if (videoTabPanel != null) videoTabPanel.SetActive(true);
                break;
            case 1: // 오디오
                if (audioTabPanel != null) audioTabPanel.SetActive(true);
                break;
            case 2: // 게임플레이
                if (gameplayTabPanel != null) gameplayTabPanel.SetActive(true);
                break;
            case 3: // 계정
                if (accountTabPanel != null) accountTabPanel.SetActive(true);
                break;
        }
    }

    /// <summary>
    /// 비디오 설정 초기화
    /// </summary>
    private void InitializeVideoSettings()
    {
        if (settingsManager == null) return;

        // 해상도 드롭다운
        if (resolutionDropdown != null)
        {
            resolutionDropdown.ClearOptions();
            List<string> options = new List<string>();
            foreach (var res in settingsManager.resolutions)
            {
                options.Add($"{res.width} x {res.height}");
            }
            resolutionDropdown.AddOptions(options);
            resolutionDropdown.value = settingsManager.resolutionIndex;
            resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
        }

        // 전체화면 토글
        if (fullscreenToggle != null)
        {
            fullscreenToggle.isOn = settingsManager.isFullscreen;
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
        }

        // 수직동기화 토글
        if (vSyncToggle != null)
        {
            vSyncToggle.isOn = settingsManager.vSyncEnabled;
            vSyncToggle.onValueChanged.AddListener(OnVSyncChanged);
        }

        // 프레임 제한 드롭다운
        if (targetFPSDropdown != null)
        {
            targetFPSDropdown.ClearOptions();
            targetFPSDropdown.AddOptions(new List<string> { "30", "60", "120", "144", "240", "무제한" });
            int fpsIndex = GetFPSIndex(settingsManager.targetFPS);
            targetFPSDropdown.value = fpsIndex;
            targetFPSDropdown.onValueChanged.AddListener(OnTargetFPSChanged);
        }

        if (backgroundFPSDropdown != null)
        {
            backgroundFPSDropdown.ClearOptions();
            backgroundFPSDropdown.AddOptions(new List<string> { "10", "15", "30", "60" });
            int fpsIndex = GetBackgroundFPSIndex(settingsManager.backgroundFPS);
            backgroundFPSDropdown.value = fpsIndex;
            backgroundFPSDropdown.onValueChanged.AddListener(OnBackgroundFPSChanged);
        }

        // 포스트 프로세싱 토글
        if (postProcessingToggle != null)
        {
            postProcessingToggle.isOn = settingsManager.postProcessingEnabled;
            postProcessingToggle.onValueChanged.AddListener(OnPostProcessingChanged);
        }

        // 데미지 숫자 표시 토글
        if (showDamageNumbersToggle != null)
        {
            showDamageNumbersToggle.isOn = settingsManager.showDamageNumbers;
            showDamageNumbersToggle.onValueChanged.AddListener(OnShowDamageNumbersChanged);
        }

        // 이펙트 투명도 슬라이더
        if (effectOpacitySlider != null)
        {
            effectOpacitySlider.value = settingsManager.effectOpacity;
            effectOpacitySlider.onValueChanged.AddListener(OnEffectOpacityChanged);
            UpdateEffectOpacityText();
        }

        // 화면 흔들림 토글
        if (screenShakeToggle != null)
        {
            screenShakeToggle.isOn = settingsManager.screenShakeEnabled;
            screenShakeToggle.onValueChanged.AddListener(OnScreenShakeChanged);
        }
    }

    /// <summary>
    /// 오디오 설정 초기화
    /// </summary>
    private void InitializeAudioSettings()
    {
        if (settingsManager == null) return;

        // 마스터 볼륨
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.value = settingsManager.masterVolume;
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            UpdateMasterVolumeText();
        }

        // BGM 볼륨
        if (bgmVolumeSlider != null)
        {
            bgmVolumeSlider.value = settingsManager.bgmVolume;
            bgmVolumeSlider.onValueChanged.AddListener(OnBgmVolumeChanged);
            UpdateBgmVolumeText();
        }

        // SFX 볼륨
        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.value = settingsManager.sfxVolume;
            sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
            UpdateSfxVolumeText();
        }

        // 백그라운드 음소거 토글
        if (muteOnFocusLostToggle != null)
        {
            muteOnFocusLostToggle.isOn = settingsManager.muteOnFocusLost;
            muteOnFocusLostToggle.onValueChanged.AddListener(OnMuteOnFocusLostChanged);
        }
    }

    /// <summary>
    /// 게임플레이 설정 초기화
    /// </summary>
    private void InitializeGameplaySettings()
    {
        if (settingsManager == null) return;

        // 백그라운드 자동 일시정지 토글
        if (pauseOnFocusLostToggle != null)
        {
            pauseOnFocusLostToggle.isOn = settingsManager.pauseOnFocusLost;
            pauseOnFocusLostToggle.onValueChanged.AddListener(OnPauseOnFocusLostChanged);
        }
    }

    // ===================== 비디오 설정 이벤트 핸들러 =====================

    private void OnResolutionChanged(int index)
    {
        if (settingsManager != null)
        {
            settingsManager.SetResolution(index);
            settingsManager.SaveSettings();
        }
    }

    private void OnFullscreenChanged(bool isOn)
    {
        if (settingsManager != null)
        {
            settingsManager.SetFullscreen(isOn);
            settingsManager.SaveSettings();
        }
    }

    private void OnVSyncChanged(bool isOn)
    {
        if (settingsManager != null)
        {
            settingsManager.SetVSync(isOn);
            settingsManager.SaveSettings();
        }
    }

    private void OnTargetFPSChanged(int index)
    {
        if (settingsManager != null)
        {
            int[] fpsValues = { 30, 60, 120, 144, 240, -1 }; // -1은 무제한
            int fps = fpsValues[index];
            if (fps == -1)
            {
                Application.targetFrameRate = -1;
                settingsManager.targetFPS = -1;
            }
            else
            {
                settingsManager.SetTargetFPS(fps);
            }
            settingsManager.SaveSettings();
        }
    }

    private void OnBackgroundFPSChanged(int index)
    {
        if (settingsManager != null)
        {
            int[] fpsValues = { 10, 15, 30, 60 };
            settingsManager.SetBackgroundFPS(fpsValues[index]);
            settingsManager.SaveSettings();
        }
    }

    private void OnPostProcessingChanged(bool isOn)
    {
        if (settingsManager != null)
        {
            settingsManager.SetPostProcessing(isOn);
            settingsManager.SaveSettings();
        }
    }

    private void OnShowDamageNumbersChanged(bool isOn)
    {
        if (settingsManager != null)
        {
            settingsManager.SetShowDamageNumbers(isOn);
            settingsManager.SaveSettings();
        }
    }

    private void OnEffectOpacityChanged(float value)
    {
        if (settingsManager != null)
        {
            settingsManager.SetEffectOpacity(value);
            UpdateEffectOpacityText();
            settingsManager.SaveSettings();
        }
    }

    private void OnScreenShakeChanged(bool isOn)
    {
        if (settingsManager != null)
        {
            settingsManager.SetScreenShake(isOn);
            settingsManager.SaveSettings();
        }
    }

    // ===================== 오디오 설정 이벤트 핸들러 =====================

    private void OnMasterVolumeChanged(float value)
    {
        if (settingsManager != null)
        {
            settingsManager.SetMasterVolume(value);
            UpdateMasterVolumeText();
            settingsManager.SaveSettings();
        }
    }

    private void OnBgmVolumeChanged(float value)
    {
        if (settingsManager != null)
        {
            settingsManager.SetBgmVolume(value);
            UpdateBgmVolumeText();
            settingsManager.SaveSettings();
        }
    }

    private void OnSfxVolumeChanged(float value)
    {
        if (settingsManager != null)
        {
            settingsManager.SetSfxVolume(value);
            UpdateSfxVolumeText();
            settingsManager.SaveSettings();
        }
    }

    private void OnMuteOnFocusLostChanged(bool isOn)
    {
        if (settingsManager != null)
        {
            settingsManager.SetMuteOnFocusLost(isOn);
            settingsManager.SaveSettings();
        }
    }

    // ===================== 게임플레이 설정 이벤트 핸들러 =====================

    private void OnPauseOnFocusLostChanged(bool isOn)
    {
        if (settingsManager != null)
        {
            settingsManager.SetPauseOnFocusLost(isOn);
            settingsManager.SaveSettings();
        }
    }

    // ===================== UI 텍스트 업데이트 =====================

    private void UpdateEffectOpacityText()
    {
        if (effectOpacityText != null && effectOpacitySlider != null)
        {
            effectOpacityText.text = $"{(int)(effectOpacitySlider.value * 100)}%";
        }
    }

    private void UpdateMasterVolumeText()
    {
        if (masterVolumeText != null && masterVolumeSlider != null)
        {
            masterVolumeText.text = $"{(int)(masterVolumeSlider.value * 100)}%";
        }
    }

    private void UpdateBgmVolumeText()
    {
        if (bgmVolumeText != null && bgmVolumeSlider != null)
        {
            bgmVolumeText.text = $"{(int)(bgmVolumeSlider.value * 100)}%";
        }
    }

    private void UpdateSfxVolumeText()
    {
        if (sfxVolumeText != null && sfxVolumeSlider != null)
        {
            sfxVolumeText.text = $"{(int)(sfxVolumeSlider.value * 100)}%";
        }
    }

    // ===================== 헬퍼 메서드 =====================

    private int GetFPSIndex(int fps)
    {
        int[] fpsValues = { 30, 60, 120, 144, 240, -1 };
        for (int i = 0; i < fpsValues.Length; i++)
        {
            if (fpsValues[i] == fps || (fps == -1 && i == 5))
                return i;
        }
        return 1; // 기본값: 60
    }

    private int GetBackgroundFPSIndex(int fps)
    {
        int[] fpsValues = { 10, 15, 30, 60 };
        for (int i = 0; i < fpsValues.Length; i++)
        {
            if (fpsValues[i] == fps)
                return i;
        }
        return 2; // 기본값: 30
    }
}

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

    [Header("Settings Panel")]
    public GameObject settingsPanel; // 메인 설정 패널 (자동 생성됨)

    [Header("공통 토글 크기 설정")]
    [Tooltip("Start 시점에 SettingsPanel 하위 모든 Toggle의 크기를 이 값으로 맞춥니다.")]
    [SerializeField] private bool applyToggleSizeOnStart = true;
    [SerializeField] private Vector2 commonToggleSize = new Vector2(40f, 40f);

    private SettingsManager settingsManager;
    private bool isSettingsOpen = false;
    public System.Action onSettingsClosed;

    private void Awake()
    {
        settingsManager = SettingsManager.Instance;
        if (settingsManager == null)
        {
            var existing = FindObjectOfType<SettingsManager>();
            if (existing != null)
                settingsManager = existing;
            else
            {
                var go = GameObject.Find("SettingsManager");
                if (go == null) go = new GameObject("SettingsManager");
                if (go.GetComponent<SettingsManager>() == null)
                    go.AddComponent<SettingsManager>();
                settingsManager = SettingsManager.Instance;
            }
            if (settingsManager == null)
                Debug.LogError("[SettingsUI] SettingsManager를 찾을 수 없습니다!");
        }

        // SettingsUI GameObject는 항상 활성화 (Update 실행을 위해)
        gameObject.SetActive(true);
    }

    // 자동 생성 기능은 이제 사용하지 않습니다.
    // SettingsUI 및 SettingsPanel은 씬에서 디자이너가 직접 배치합니다.

    private void Start()
    {
        // 수동 배치 모드: 디자이너가 씬에서 직접 만든 UI를 사용합니다.
        // settingsPanel, 탭 버튼, 각 탭 패널, 세부 UI들은 인스펙터에서 연결해야 합니다.

        // Awake 시점에 SettingsManager.Instance가 아직 준비되지 않았을 수 있으므로
        // Start에서 한 번 더 참조를 갱신해준다.
        if (settingsManager == null)
        {
            settingsManager = SettingsManager.Instance ?? FindObjectOfType<SettingsManager>();
            if (settingsManager == null)
            {
                Debug.LogError("[SettingsUI] Start 시점에도 SettingsManager를 찾을 수 없습니다. 비디오/오디오/게임플레이 설정 초기화가 동작하지 않습니다.");
                return;
            }
        }

        InitializeTabs();
        InitializeVideoSettings();
        InitializeAudioSettings();
        InitializeGameplaySettings();

        // 토글 공통 크기 적용 (해상도 바뀌어도 클릭 영역이 충분히 크게)
        ApplyCommonToggleSize();

        // 기본적으로 비디오 탭 표시
        ShowTab(0);

        // ESC 키로 설정 열기/닫기
        // Update에서 처리
    }

    /// <summary>
    /// SettingsPanel 하위에 있는 모든 Toggle의 RectTransform 크기를 공통 값으로 맞춥니다.
    /// </summary>
    private void ApplyCommonToggleSize()
    {
        if (!applyToggleSizeOnStart) return;
        if (settingsPanel == null) return;

        var toggles = settingsPanel.GetComponentsInChildren<Toggle>(true);
        foreach (var t in toggles)
        {
            var rt = t.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.sizeDelta = commonToggleSize;
            }
        }
    }

    private void Update()
    {
        // ESC는 PauseMenuController에서 처리 (재개/설정/종료 메뉴 → 설정 버튼으로만 설정 열기)
    }

    /// <summary>
    /// 설정 패널이 현재 열려 있는지. PauseMenuController에서 ESC 처리 시 사용.
    /// </summary>
    public bool IsSettingsOpen() => isSettingsOpen;

    /// <summary>
    /// 설정 UI 열기/닫기
    /// </summary>
    public void ToggleSettings()
    {
        Debug.Log($"[SettingsUI] ToggleSettings 호출됨. 현재 isSettingsOpen: {isSettingsOpen}, settingsPanel: {(settingsPanel != null ? settingsPanel.name : "NULL")}");

        isSettingsOpen = !isSettingsOpen;

        if (isSettingsOpen)
            LobbySoundManager.Instance?.PlaySettingsOpen();
        else
            LobbySoundManager.Instance?.PlaySettingsClose();

        // settingsPanel이 없으면 오류 출력 후 종료
        if (settingsPanel == null)
        {
            Debug.LogError("[SettingsUI] settingsPanel이 null입니다. 씬에서 SettingsPanel을 만들고 SettingsUI에 연결해 주세요.");
            return;
        }

        settingsPanel.SetActive(isSettingsOpen);
        Debug.Log($"[SettingsUI] Settings 패널 {(isSettingsOpen ? "열림" : "닫힘")}, settingsPanel.activeSelf: {settingsPanel.activeSelf}");

        // 설정 패널이 보이도록 Canvas Sort Order를 최상위로 설정
        if (isSettingsOpen)
        {
            Canvas canvas = settingsPanel.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                canvas.sortingOrder = 100; // 최상위로 설정
                Debug.Log($"[SettingsUI] Canvas Sort Order를 {canvas.sortingOrder}로 설정했습니다.");
            }
        }
        else
        {
            // 설정을 버튼 등으로 닫을 때 일시정지 메뉴로 복귀 (ESC로 닫을 때는 PauseMenuController가 직접 ShowPauseMenu 호출)
            var pauseMenu = FindObjectOfType<PauseMenuController>();
            if (pauseMenu != null && !pauseMenu.IsPauseMenuOpen)
                pauseMenu.ShowPauseMenu();

            // 로비 등 외부에서 등록한 닫기 콜백 호출
            onSettingsClosed?.Invoke();
        }

        // 설정이 열려있을 때 게임 일시정지
        if (isSettingsOpen)
        {
            Time.timeScale = 0f;
        }
        else
        {
            Time.timeScale = 1f;
            // 설정 저장
            if (settingsManager != null)
            {
                settingsManager.SaveSettings();
            }
        }
    }

    /// <summary>
    /// 설정 UI 열기
    /// </summary>
    public void OpenSettings()
    {
        if (!isSettingsOpen)
        {
            ToggleSettings();
        }
    }

    /// <summary>
    /// 설정 UI 닫기
    /// </summary>
    public void CloseSettings()
    {
        if (isSettingsOpen)
        {
            ToggleSettings();
        }
    }

    /// <summary>
    /// 탭 초기화
    /// </summary>
    private void InitializeTabs()
    {
        if (videoTabButton != null)
        {
            videoTabButton.onClick.AddListener(() => { Debug.Log("[SettingsUI] Video 탭 버튼 클릭"); ShowTab(0); });
        }
        else
        {
            Debug.LogWarning("[SettingsUI] videoTabButton이 null입니다!");
        }

        if (audioTabButton != null)
        {
            audioTabButton.onClick.AddListener(() => { Debug.Log("[SettingsUI] Audio 탭 버튼 클릭"); ShowTab(1); });
        }
        else
        {
            Debug.LogWarning("[SettingsUI] audioTabButton이 null입니다!");
        }

        if (gameplayTabButton != null)
        {
            gameplayTabButton.onClick.AddListener(() => { Debug.Log("[SettingsUI] Gameplay 탭 버튼 클릭"); ShowTab(2); });
        }
        else
        {
            Debug.LogWarning("[SettingsUI] gameplayTabButton이 null입니다!");
        }

        if (accountTabButton != null)
        {
            accountTabButton.onClick.AddListener(() => { Debug.Log("[SettingsUI] Account 탭 버튼 클릭"); ShowTab(3); });
        }
        else
        {
            Debug.LogWarning("[SettingsUI] accountTabButton이 null입니다!");
        }
    }

    /// <summary>
    /// 탭 표시
    /// </summary>
    private void ShowTab(int tabIndex)
    {
        Debug.Log($"[SettingsUI] ShowTab({tabIndex}) 호출됨");
        LobbySoundManager.Instance?.PlaySettingsTabSwitch();

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
            effectOpacityText.text = $"{(int)(effectOpacitySlider.value * 100)}";
        }
    }

    private void UpdateMasterVolumeText()
    {
        if (masterVolumeText != null && masterVolumeSlider != null)
        {
            masterVolumeText.text = $"{(int)(masterVolumeSlider.value * 100)}";
        }
    }

    private void UpdateBgmVolumeText()
    {
        if (bgmVolumeText != null && bgmVolumeSlider != null)
        {
            bgmVolumeText.text = $"{(int)(bgmVolumeSlider.value * 100)}";
        }
    }

    private void UpdateSfxVolumeText()
    {
        if (sfxVolumeText != null && sfxVolumeSlider != null)
        {
            sfxVolumeText.text = $"{(int)(sfxVolumeSlider.value * 100)}";
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

    // ===================== UI 자동 생성 =====================

    /// <summary>
    /// 누락된 UI 요소들을 자동으로 생성합니다.
    /// </summary>
    private void AutoCreateMissingUI()
    {
        // Canvas 찾기
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            // CanvasScaler 설정
            UnityEngine.UI.CanvasScaler scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            Debug.Log("[SettingsUI] Canvas가 없어 새로 생성했습니다.");
        }

        // Settings 패널이 없으면 생성 (Canvas의 직접 자식으로)
        if (this.settingsPanel == null)
        {
            GameObject panelObj = new GameObject("SettingsPanel");
            panelObj.transform.SetParent(canvas.transform, false);
            this.settingsPanel = panelObj;
        }

        RectTransform panelRect = settingsPanel.GetComponent<RectTransform>();
        if (panelRect == null)
        {
            panelRect = settingsPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(800, 600);
            panelRect.anchoredPosition = Vector2.zero;
        }

        // 배경 이미지 추가
        if (settingsPanel.GetComponent<Image>() == null)
        {
            Image bgImg = settingsPanel.AddComponent<Image>();
            bgImg.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);
        }

        // Settings 패널 초기 비활성화 (SettingsUI GameObject는 활성화 상태 유지)
        settingsPanel.SetActive(false);

        // 탭 버튼 영역 생성
        CreateTabButtons(settingsPanel.transform);

        // 탭 패널들 생성
        CreateTabPanels(settingsPanel.transform);

        Debug.Log("[SettingsUI] 누락된 UI 요소 자동 생성 완료");
    }

    /// <summary>
    /// 탭 버튼 영역 생성
    /// </summary>
    private void CreateTabButtons(Transform parent)
    {
        // 탭 버튼 컨테이너
        GameObject tabButtonContainer = new GameObject("TabButtonContainer");
        tabButtonContainer.transform.SetParent(parent, false);
        RectTransform containerRect = tabButtonContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0, 1);
        containerRect.anchorMax = new Vector2(1, 1);
        containerRect.anchoredPosition = new Vector2(0, -20);
        containerRect.sizeDelta = new Vector2(0, 50);

        // Horizontal Layout Group 추가
        HorizontalLayoutGroup layoutGroup = tabButtonContainer.AddComponent<HorizontalLayoutGroup>();
        layoutGroup.spacing = 10;
        layoutGroup.padding = new RectOffset(10, 10, 10, 10);
        layoutGroup.childControlWidth = false;
        layoutGroup.childControlHeight = true;
        layoutGroup.childForceExpandWidth = false;
        layoutGroup.childForceExpandHeight = true;

        // 탭 버튼들 생성
        if (videoTabButton == null)
            videoTabButton = CreateTabButton(tabButtonContainer.transform, "Video", 0);
        if (audioTabButton == null)
            audioTabButton = CreateTabButton(tabButtonContainer.transform, "Audio", 1);
        if (gameplayTabButton == null)
            gameplayTabButton = CreateTabButton(tabButtonContainer.transform, "Gameplay", 2);
        if (accountTabButton == null)
            accountTabButton = CreateTabButton(tabButtonContainer.transform, "Account", 3);
    }

    /// <summary>
    /// 개별 탭 버튼 생성
    /// </summary>
    private Button CreateTabButton(Transform parent, string label, int tabIndex)
    {
        GameObject buttonObj = new GameObject($"{label}TabButton");
        buttonObj.transform.SetParent(parent, false);

        RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
        buttonRect.sizeDelta = new Vector2(150, 40);

        Image buttonImg = buttonObj.AddComponent<Image>();
        buttonImg.color = new Color(0.3f, 0.3f, 0.3f, 1f);

        Button button = buttonObj.AddComponent<Button>();

        // 텍스트 추가
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = 18;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;

        return button;
    }

    /// <summary>
    /// 탭 패널들 생성
    /// </summary>
    private void CreateTabPanels(Transform parent)
    {
        // 콘텐츠 영역 (탭 패널들이 들어갈 공간)
        GameObject contentArea = new GameObject("ContentArea");
        contentArea.transform.SetParent(parent, false);
        RectTransform contentRect = contentArea.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 0);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(-20, -70); // 상단 탭 버튼 영역 제외

        // 비디오 탭 패널 생성
        if (videoTabPanel == null)
            videoTabPanel = CreateVideoTabPanel(contentArea.transform);

        // 오디오 탭 패널 생성
        if (audioTabPanel == null)
            audioTabPanel = CreateAudioTabPanel(contentArea.transform);

        // 게임플레이 탭 패널 생성
        if (gameplayTabPanel == null)
            gameplayTabPanel = CreateGameplayTabPanel(contentArea.transform);

        // 계정 탭 패널 생성
        if (accountTabPanel == null)
            accountTabPanel = CreateAccountTabPanel(contentArea.transform);
    }

    /// <summary>
    /// 비디오 탭 패널 생성
    /// </summary>
    private GameObject CreateVideoTabPanel(Transform parent)
    {
        GameObject panel = new GameObject("VideoTabPanel");
        panel.transform.SetParent(parent, false);
        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;

        // Vertical Layout Group
        VerticalLayoutGroup layoutGroup = panel.AddComponent<VerticalLayoutGroup>();
        layoutGroup.spacing = 15;
        layoutGroup.padding = new RectOffset(20, 20, 20, 20);
        layoutGroup.childControlWidth = true;
        layoutGroup.childControlHeight = false;
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.childForceExpandHeight = false;

        // Scroll View 추가 (내용이 많을 수 있으므로)
        GameObject scrollView = new GameObject("ScrollView");
        scrollView.transform.SetParent(panel.transform, false);
        RectTransform scrollRect = scrollView.AddComponent<RectTransform>();
        scrollRect.anchorMin = Vector2.zero;
        scrollRect.anchorMax = Vector2.one;
        scrollRect.sizeDelta = Vector2.zero;

        ScrollRect scroll = scrollView.AddComponent<ScrollRect>();
        Image scrollImg = scrollView.AddComponent<Image>();
        scrollImg.color = new Color(0.15f, 0.15f, 0.15f, 1f);

        // Viewport
        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollView.transform, false);
        RectTransform viewportRect = viewport.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.sizeDelta = Vector2.zero;
        viewport.AddComponent<Mask>();
        Image viewportImg = viewport.AddComponent<Image>();
        viewportImg.color = Color.clear;

        // Content
        GameObject content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        RectTransform contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.sizeDelta = new Vector2(0, 0);

        VerticalLayoutGroup contentLayout = content.AddComponent<VerticalLayoutGroup>();
        contentLayout.spacing = 15;
        contentLayout.padding = new RectOffset(10, 10, 10, 10);
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = false;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;

        ContentSizeFitter sizeFitter = content.AddComponent<ContentSizeFitter>();
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.content = contentRect;
        scroll.viewport = viewportRect;
        scroll.horizontal = false;
        scroll.vertical = true;

        // 비디오 설정 UI 요소들 생성
        CreateVideoSettingsUI(content.transform);

        panel.SetActive(false); // 초기 비활성화
        return panel;
    }

    /// <summary>
    /// 비디오 설정 UI 요소들 생성
    /// </summary>
    private void CreateVideoSettingsUI(Transform parent)
    {
        // 해상도
        CreateSettingRow(parent, "Resolution", out GameObject resolutionObj);
        resolutionDropdown = CreateDropdown(resolutionObj.transform);

        // 전체화면
        CreateSettingRow(parent, "Fullscreen", out GameObject fullscreenObj);
        fullscreenToggle = CreateToggle(fullscreenObj.transform);

        // VSync
        CreateSettingRow(parent, "VSync", out GameObject vSyncObj);
        vSyncToggle = CreateToggle(vSyncObj.transform);

        // Target FPS
        CreateSettingRow(parent, "Target FPS", out GameObject targetFPSObj);
        targetFPSDropdown = CreateDropdown(targetFPSObj.transform);

        // Background FPS
        CreateSettingRow(parent, "Background FPS", out GameObject bgFPSObj);
        backgroundFPSDropdown = CreateDropdown(bgFPSObj.transform);

        // Post Processing
        CreateSettingRow(parent, "Post Processing", out GameObject postProcObj);
        postProcessingToggle = CreateToggle(postProcObj.transform);

        // Show Damage Numbers
        CreateSettingRow(parent, "Show Damage Numbers", out GameObject damageNumObj);
        showDamageNumbersToggle = CreateToggle(damageNumObj.transform);

        // Effect Opacity
        CreateSettingRow(parent, "Effect Opacity", out GameObject effectOpacityObj);
        GameObject sliderContainer = new GameObject("SliderContainer");
        sliderContainer.transform.SetParent(effectOpacityObj.transform, false);
        RectTransform sliderRect = sliderContainer.AddComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0.5f, 0);
        sliderRect.anchorMax = new Vector2(1, 1);
        sliderRect.sizeDelta = new Vector2(-10, 0);

        effectOpacitySlider = CreateSlider(sliderContainer.transform);
        effectOpacityText = CreateText(sliderContainer.transform, "100%");
        RectTransform textRect = effectOpacityText.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(1, 0);
        textRect.anchorMax = new Vector2(1, 1);
        textRect.sizeDelta = new Vector2(60, 0);
        textRect.anchoredPosition = new Vector2(70, 0);

        // Screen Shake
        CreateSettingRow(parent, "Screen Shake", out GameObject screenShakeObj);
        screenShakeToggle = CreateToggle(screenShakeObj.transform);
    }

    /// <summary>
    /// 오디오 탭 패널 생성
    /// </summary>
    private GameObject CreateAudioTabPanel(Transform parent)
    {
        GameObject panel = new GameObject("AudioTabPanel");
        panel.transform.SetParent(parent, false);
        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;

        VerticalLayoutGroup layoutGroup = panel.AddComponent<VerticalLayoutGroup>();
        layoutGroup.spacing = 20;
        layoutGroup.padding = new RectOffset(20, 20, 20, 20);

        // 오디오 설정 UI 요소들 생성
        CreateSettingRow(panel.transform, "Master Volume", out GameObject masterVolObj);
        GameObject masterSliderContainer = new GameObject("SliderContainer");
        masterSliderContainer.transform.SetParent(masterVolObj.transform, false);
        RectTransform masterSliderRect = masterSliderContainer.AddComponent<RectTransform>();
        masterSliderRect.anchorMin = new Vector2(0.5f, 0);
        masterSliderRect.anchorMax = new Vector2(1, 1);
        masterSliderRect.sizeDelta = new Vector2(-10, 0);
        masterVolumeSlider = CreateSlider(masterSliderContainer.transform);
        masterVolumeText = CreateText(masterSliderContainer.transform, "100%");
        RectTransform masterTextRect = masterVolumeText.GetComponent<RectTransform>();
        masterTextRect.anchorMin = new Vector2(1, 0);
        masterTextRect.anchorMax = new Vector2(1, 1);
        masterTextRect.sizeDelta = new Vector2(60, 0);
        masterTextRect.anchoredPosition = new Vector2(70, 0);

        CreateSettingRow(panel.transform, "BGM Volume", out GameObject bgmVolObj);
        GameObject bgmSliderContainer = new GameObject("SliderContainer");
        bgmSliderContainer.transform.SetParent(bgmVolObj.transform, false);
        RectTransform bgmSliderRect = bgmSliderContainer.AddComponent<RectTransform>();
        bgmSliderRect.anchorMin = new Vector2(0.5f, 0);
        bgmSliderRect.anchorMax = new Vector2(1, 1);
        bgmSliderRect.sizeDelta = new Vector2(-10, 0);
        bgmVolumeSlider = CreateSlider(bgmSliderContainer.transform);
        bgmVolumeText = CreateText(bgmSliderContainer.transform, "100%");
        RectTransform bgmTextRect = bgmVolumeText.GetComponent<RectTransform>();
        bgmTextRect.anchorMin = new Vector2(1, 0);
        bgmTextRect.anchorMax = new Vector2(1, 1);
        bgmTextRect.sizeDelta = new Vector2(60, 0);
        bgmTextRect.anchoredPosition = new Vector2(70, 0);

        CreateSettingRow(panel.transform, "SFX Volume", out GameObject sfxVolObj);
        GameObject sfxSliderContainer = new GameObject("SliderContainer");
        sfxSliderContainer.transform.SetParent(sfxVolObj.transform, false);
        RectTransform sfxSliderRect = sfxSliderContainer.AddComponent<RectTransform>();
        sfxSliderRect.anchorMin = new Vector2(0.5f, 0);
        sfxSliderRect.anchorMax = new Vector2(1, 1);
        sfxSliderRect.sizeDelta = new Vector2(-10, 0);
        sfxVolumeSlider = CreateSlider(sfxSliderContainer.transform);
        sfxVolumeText = CreateText(sfxSliderContainer.transform, "100%");
        RectTransform sfxTextRect = sfxVolumeText.GetComponent<RectTransform>();
        sfxTextRect.anchorMin = new Vector2(1, 0);
        sfxTextRect.anchorMax = new Vector2(1, 1);
        sfxTextRect.sizeDelta = new Vector2(60, 0);
        sfxTextRect.anchoredPosition = new Vector2(70, 0);

        CreateSettingRow(panel.transform, "Mute on Focus Lost", out GameObject muteObj);
        muteOnFocusLostToggle = CreateToggle(muteObj.transform);

        panel.SetActive(false);
        return panel;
    }

    /// <summary>
    /// 게임플레이 탭 패널 생성
    /// </summary>
    private GameObject CreateGameplayTabPanel(Transform parent)
    {
        GameObject panel = new GameObject("GameplayTabPanel");
        panel.transform.SetParent(parent, false);
        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;

        VerticalLayoutGroup layoutGroup = panel.AddComponent<VerticalLayoutGroup>();
        layoutGroup.spacing = 20;
        layoutGroup.padding = new RectOffset(20, 20, 20, 20);

        // CreateSettingRow(panel.transform, "Pause on Focus Lost", out GameObject pauseObj);
        // pauseOnFocusLostToggle = CreateToggle(pauseObj.transform);

        panel.SetActive(false);
        return panel;
    }

    /// <summary>
    /// 계정 탭 패널 생성
    /// </summary>
    private GameObject CreateAccountTabPanel(Transform parent)
    {
        GameObject panel = new GameObject("AccountTabPanel");
        panel.transform.SetParent(parent, false);
        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;

        VerticalLayoutGroup layoutGroup = panel.AddComponent<VerticalLayoutGroup>();
        layoutGroup.spacing = 20;
        layoutGroup.padding = new RectOffset(20, 20, 20, 20);

        // 계정 탭은 UI만 있고 기능은 이현승이 구현
        GameObject textObj = new GameObject("InfoText");
        textObj.transform.SetParent(panel.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(0, 100);
        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = "Account settings will be implemented by 이현승";
        text.fontSize = 20;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;

        panel.SetActive(false);
        return panel;
    }

    // ===================== UI 요소 생성 헬퍼 메서드 =====================

    /// <summary>
    /// 설정 행 생성 (라벨 + 컨트롤)
    /// </summary>
    private void CreateSettingRow(Transform parent, string labelText, out GameObject rowObj)
    {
        rowObj = new GameObject($"{labelText}Row");
        rowObj.transform.SetParent(parent, false);
        RectTransform rowRect = rowObj.AddComponent<RectTransform>();
        rowRect.sizeDelta = new Vector2(0, 40);

        HorizontalLayoutGroup layoutGroup = rowObj.AddComponent<HorizontalLayoutGroup>();
        layoutGroup.spacing = 10;
        layoutGroup.childControlWidth = false;
        layoutGroup.childControlHeight = true;
        layoutGroup.childForceExpandWidth = false;
        layoutGroup.childForceExpandHeight = true;

        // 라벨
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(rowObj.transform, false);
        RectTransform labelRect = labelObj.AddComponent<RectTransform>();
        labelRect.sizeDelta = new Vector2(200, 0);
        TextMeshProUGUI label = labelObj.AddComponent<TextMeshProUGUI>();
        label.text = labelText;
        label.fontSize = 16;
        label.alignment = TextAlignmentOptions.Left;
        label.color = Color.white;
    }

    /// <summary>
    /// 드롭다운 생성
    /// </summary>
    private TMP_Dropdown CreateDropdown(Transform parent)
    {
        GameObject dropdownObj = new GameObject("Dropdown");
        dropdownObj.transform.SetParent(parent, false);
        RectTransform dropdownRect = dropdownObj.AddComponent<RectTransform>();
        dropdownRect.anchorMin = new Vector2(0.5f, 0);
        dropdownRect.anchorMax = new Vector2(1, 1);
        dropdownRect.sizeDelta = new Vector2(-10, 0);

        Image dropdownImg = dropdownObj.AddComponent<Image>();
        dropdownImg.color = new Color(0.2f, 0.2f, 0.2f, 1f);

        TMP_Dropdown dropdown = dropdownObj.AddComponent<TMP_Dropdown>();

        // Label
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(dropdownObj.transform, false);
        RectTransform labelRect = labelObj.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.sizeDelta = new Vector2(-30, 0);
        TextMeshProUGUI label = labelObj.AddComponent<TextMeshProUGUI>();
        label.text = "Option";
        label.fontSize = 14;
        label.alignment = TextAlignmentOptions.Left;
        label.color = Color.white;
        dropdown.captionText = label;

        // Arrow
        GameObject arrowObj = new GameObject("Arrow");
        arrowObj.transform.SetParent(dropdownObj.transform, false);
        RectTransform arrowRect = arrowObj.AddComponent<RectTransform>();
        arrowRect.anchorMin = new Vector2(1, 0.5f);
        arrowRect.anchorMax = new Vector2(1, 0.5f);
        arrowRect.sizeDelta = new Vector2(20, 20);
        arrowRect.anchoredPosition = new Vector2(-15, 0);
        Image arrowImg = arrowObj.AddComponent<Image>();
        arrowImg.color = Color.white;
        dropdown.captionImage = arrowImg;

        // Template
        GameObject templateObj = new GameObject("Template");
        templateObj.transform.SetParent(dropdownObj.transform, false);
        templateObj.SetActive(false);
        RectTransform templateRect = templateObj.AddComponent<RectTransform>();
        templateRect.anchorMin = new Vector2(0, 0);
        templateRect.anchorMax = new Vector2(1, 0);
        templateRect.pivot = new Vector2(0.5f, 1);
        templateRect.sizeDelta = new Vector2(0, 150);
        Image templateImg = templateObj.AddComponent<Image>();
        templateImg.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        ScrollRect templateScroll = templateObj.AddComponent<ScrollRect>();
        dropdown.template = templateRect;

        // Viewport
        GameObject viewportObj = new GameObject("Viewport");
        viewportObj.transform.SetParent(templateObj.transform, false);
        RectTransform viewportRect = viewportObj.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.sizeDelta = Vector2.zero;
        viewportObj.AddComponent<Mask>();
        Image viewportImg = viewportObj.AddComponent<Image>();
        viewportImg.color = Color.clear;
        templateScroll.viewport = viewportRect;

        // Content
        GameObject contentObj = new GameObject("Content");
        contentObj.transform.SetParent(viewportObj.transform, false);
        RectTransform contentRect = contentObj.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.sizeDelta = new Vector2(0, 0);
        templateScroll.content = contentRect;

        // Item
        GameObject itemObj = new GameObject("Item");
        itemObj.transform.SetParent(contentObj.transform, false);
        RectTransform itemRect = itemObj.AddComponent<RectTransform>();
        itemRect.sizeDelta = new Vector2(0, 30);
        Toggle itemToggle = itemObj.AddComponent<Toggle>();

        // Item Label
        GameObject itemLabelObj = new GameObject("Item Label");
        itemLabelObj.transform.SetParent(itemObj.transform, false);
        RectTransform itemLabelRect = itemLabelObj.AddComponent<RectTransform>();
        itemLabelRect.anchorMin = Vector2.zero;
        itemLabelRect.anchorMax = Vector2.one;
        itemLabelRect.sizeDelta = Vector2.zero;
        TextMeshProUGUI itemLabel = itemLabelObj.AddComponent<TextMeshProUGUI>();
        itemLabel.text = "Option";
        itemLabel.fontSize = 14;
        itemLabel.alignment = TextAlignmentOptions.Left;
        itemLabel.color = Color.white;
        dropdown.itemText = itemLabel;

        return dropdown;
    }

    /// <summary>
    /// 토글 생성
    /// </summary>
    private Toggle CreateToggle(Transform parent)
    {
        GameObject toggleObj = new GameObject("Toggle");
        toggleObj.transform.SetParent(parent, false);
        RectTransform toggleRect = toggleObj.AddComponent<RectTransform>();
        toggleRect.anchorMin = new Vector2(0.5f, 0);
        toggleRect.anchorMax = new Vector2(1, 1);
        toggleRect.sizeDelta = new Vector2(-10, 0);

        Toggle toggle = toggleObj.AddComponent<Toggle>();

        // Background
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(toggleObj.transform, false);
        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        toggle.targetGraphic = bgImg;

        // Checkmark
        GameObject checkObj = new GameObject("Checkmark");
        checkObj.transform.SetParent(bgObj.transform, false);
        RectTransform checkRect = checkObj.AddComponent<RectTransform>();
        checkRect.anchorMin = Vector2.zero;
        checkRect.anchorMax = Vector2.one;
        checkRect.sizeDelta = Vector2.zero;
        Image checkImg = checkObj.AddComponent<Image>();
        checkImg.color = Color.white;
        toggle.graphic = checkImg;

        return toggle;
    }

    /// <summary>
    /// 슬라이더 생성
    /// </summary>
    private Slider CreateSlider(Transform parent)
    {
        GameObject sliderObj = new GameObject("Slider");
        sliderObj.transform.SetParent(parent, false);
        RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
        sliderRect.anchorMin = Vector2.zero;
        sliderRect.anchorMax = Vector2.one;
        sliderRect.sizeDelta = Vector2.zero;

        Slider slider = sliderObj.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;

        // Background
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(sliderObj.transform, false);
        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        slider.targetGraphic = bgImg;

        // Fill Area
        GameObject fillAreaObj = new GameObject("Fill Area");
        fillAreaObj.transform.SetParent(sliderObj.transform, false);
        RectTransform fillAreaRect = fillAreaObj.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.sizeDelta = Vector2.zero;

        // Fill
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(fillAreaObj.transform, false);
        RectTransform fillRect = fillObj.AddComponent<RectTransform>();
        fillRect.sizeDelta = Vector2.zero;
        Image fillImg = fillObj.AddComponent<Image>();
        fillImg.color = new Color(0.2f, 0.6f, 1f, 1f);
        slider.fillRect = fillRect;

        // Handle
        GameObject handleObj = new GameObject("Handle Slide Area");
        handleObj.transform.SetParent(sliderObj.transform, false);
        RectTransform handleAreaRect = handleObj.AddComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.sizeDelta = Vector2.zero;

        GameObject handle = new GameObject("Handle");
        handle.transform.SetParent(handleObj.transform, false);
        RectTransform handleRect = handle.AddComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(20, 20);
        Image handleImg = handle.AddComponent<Image>();
        handleImg.color = Color.white;
        slider.handleRect = handleRect;

        return slider;
    }

    /// <summary>
    /// 텍스트 생성
    /// </summary>
    private TextMeshProUGUI CreateText(Transform parent, string text)
    {
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(parent, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
        textComponent.text = text;
        textComponent.fontSize = 14;
        textComponent.alignment = TextAlignmentOptions.Center;
        textComponent.color = Color.white;
        return textComponent;
    }
}

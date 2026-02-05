using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// ESC 입력 시 일시정지 메뉴를 띄웁니다.
/// - 좌측 상단: 재개 / 설정 / 종료 버튼
/// - 중앙: WeaponStatsPanel (무기별 대미지 통계)
/// 설정 버튼을 누르면 SettingUI의 설정 패널이 열리고, ESC로 설정을 닫으면 다시 이 메뉴로 돌아옵니다.
///
/// 씬 설정:
/// 1. 빈 GameObject에 이 스크립트 추가 (예: PauseMenuController).
/// 2. 일시정지 시 보일 패널 생성 (Panel) → Pause Menu Panel. 앵커 좌상단, 전체 화면 등.
/// 3. 그 안에 재개/설정/종료 버튼 3개를 좌측 상단에 배치. 각각 Resume/Settings/Quit 버튼에 연결.
/// 4. WeaponStatsPanel은 씬에 이미 있으면 자동 탐색. 없으면 인스펙터에서 할당.
/// 5. SettingsUI, pauseMenuPanel, resumeButton, settingsButton, quitButton 인스펙터 할당.
/// </summary>
public class PauseMenuController : MonoBehaviour
{
    [Header("일시정지 메뉴")]
    [Tooltip("일시정지 시 보일 전체 패널 (재개/설정/종료 버튼 + WeaponStatsPanel 부모)")]
    public GameObject pauseMenuPanel;

    [Tooltip("재개 버튼")]
    public Button resumeButton;

    [Tooltip("설정 버튼 (클릭 시 설정 패널 열림)")]
    public Button settingsButton;

    [Tooltip("종료 버튼")]
    public Button quitButton;

    [Header("무기 통계 패널")]
    [Tooltip("ESC 시 중앙에 배치할 WeaponStatsPanel. 비어 있으면 씬에서 WeaponStatsPanelUI 찾음")]
    public RectTransform weaponStatsPanel;

    [Header("설정 연동")]
    [Tooltip("비어 있으면 씬에서 SettingsUI 찾음")]
    public SettingsUI settingsUI;

    [Header("종료 동작")]
    [Tooltip("비어 있으면 Application.Quit(), 있으면 해당 씬으로 이동 (예: 로비)")]
    public string quitSceneName = "";

    private bool isPauseMenuOpen;
    private Canvas pauseCanvas;
    private RectTransform weaponStatsOriginalParent;
    private Vector2 weaponStatsOriginalAnchorMin, weaponStatsOriginalAnchorMax;
    private Vector2 weaponStatsOriginalAnchoredPos;
    private int weaponStatsOriginalSiblingIndex;

    private void Awake()
    {
        Debug.Log($"[PauseMenu] Awake on {gameObject.name}");

        if (settingsUI == null)
            settingsUI = FindObjectOfType<SettingsUI>();

        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
            pauseCanvas = pauseMenuPanel.GetComponentInParent<Canvas>();
        }

        if (weaponStatsPanel == null)
        {
            var w = FindObjectOfType<WeaponStatsPanelUI>();
            if (w != null)
                weaponStatsPanel = w.GetComponent<RectTransform>();
        }

        if (resumeButton != null)
            resumeButton.onClick.AddListener(OnResume);
        if (settingsButton != null)
            settingsButton.onClick.AddListener(OnOpenSettings);
        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuit);
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape)) return;

        bool settingsOpen = settingsUI != null && settingsUI.IsSettingsOpen();
        Debug.Log($"[PauseMenu] ESC pressed. isPauseMenuOpen={isPauseMenuOpen}, settingsOpen={settingsOpen}");

        // 설정 패널이 열려 있으면 먼저 설정만 닫고 일시정지 메뉴로 복귀
        if (settingsUI != null && settingsUI.IsSettingsOpen())
        {
            settingsUI.CloseSettings();
            ShowPauseMenu();
            return;
        }

        if (isPauseMenuOpen)
        {
            ClosePauseMenu();
            return;
        }

        ShowPauseMenu();
    }

    /// <summary>
    /// 설정 패널이 열려 있는지 (SettingsUI 내부 상태)
    /// </summary>
    public bool IsSettingsOpen()
    {
        return settingsUI != null && settingsUI.IsSettingsOpen();
    }

    public bool IsPauseMenuOpen => isPauseMenuOpen;

    public void ShowPauseMenu()
    {
        if (pauseMenuPanel == null)
        {
            Debug.LogError("[PauseMenu] pauseMenuPanel is NULL, cannot show pause menu.");
            return;
        }

        isPauseMenuOpen = true;
        pauseMenuPanel.SetActive(true);
        Time.timeScale = 0f;

        if (pauseCanvas != null)
            pauseCanvas.sortingOrder = 99;

        PlaceWeaponStatsPanelInCenter();
    }

    public void ClosePauseMenu()
    {
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);

        isPauseMenuOpen = false;
        Time.timeScale = 1f;

        RestoreWeaponStatsPanel();
    }

    private void PlaceWeaponStatsPanelInCenter()
    {
        if (weaponStatsPanel == null || pauseMenuPanel == null) return;

        var rt = weaponStatsPanel;
        // 원래 부모는 최초 1회만 저장 (이미 일시정지 패널 자식이면 덮어쓰지 않음)
        if (rt.parent != pauseMenuPanel.transform)
        {
            weaponStatsOriginalParent = rt.parent as RectTransform;
            weaponStatsOriginalAnchorMin = rt.anchorMin;
            weaponStatsOriginalAnchorMax = rt.anchorMax;
            weaponStatsOriginalAnchoredPos = rt.anchoredPosition;
            weaponStatsOriginalSiblingIndex = rt.GetSiblingIndex();
        }

        rt.SetParent(pauseMenuPanel.transform, true);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.SetAsLastSibling();

        var ui = weaponStatsPanel.GetComponent<WeaponStatsPanelUI>();
        if (ui != null)
            ui.Refresh();
    }

    private void RestoreWeaponStatsPanel()
    {
        if (weaponStatsPanel == null || weaponStatsOriginalParent == null) return;

        var rt = weaponStatsPanel;
        rt.SetParent(weaponStatsOriginalParent, true);
        rt.anchorMin = weaponStatsOriginalAnchorMin;
        rt.anchorMax = weaponStatsOriginalAnchorMax;
        rt.anchoredPosition = weaponStatsOriginalAnchoredPos;
        rt.SetSiblingIndex(weaponStatsOriginalSiblingIndex);
    }

    private void OnResume()
    {
        ClosePauseMenu();
    }

    private void OnOpenSettings()
    {
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);
        isPauseMenuOpen = false;
        // 설정 패널이 열리면 Time.timeScale은 SettingsUI에서 0으로 둠
        if (settingsUI != null)
            settingsUI.OpenSettings();
    }

    private void OnQuit()
    {
        Time.timeScale = 1f;
        if (!string.IsNullOrEmpty(quitSceneName))
            SceneManager.LoadScene(quitSceneName);
        else
            Application.Quit();
    }
}

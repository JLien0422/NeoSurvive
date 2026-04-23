using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;

/// <summary>
/// 보스 처치 후 게임 클리어 UI를 표시합니다.
/// - WeaponStatsPanelUI로 무기별 DPM 표 표시
/// - 왼쪽 하단 "로비로 돌아가기" 버튼
/// Boss.OnBossDefeated 이벤트를 구독합니다.
/// </summary>
public class GameClearUI : MonoBehaviour
{
    [Header("패널 참조")]
    [Tooltip("게임 클리어 전체 패널 (루트 오브젝트)")]
    [SerializeField] private GameObject clearPanel;

    [Header("DPM 표")]
    [Tooltip("기존 WeaponStatsPanelUI를 그대로 참조 - None이면 씬에서 자동 탐색")]
    [SerializeField] private WeaponStatsPanelUI weaponStatsPanel;

    [Header("버튼")]
    [Tooltip("로비로 돌아가기 버튼")]
    [SerializeField] private Button lobbyButton;

    [Header("DOTween 연출")]
    [Tooltip("클리어 패널 페이드인 시간")]
    [SerializeField] private float panelFadeInDuration = 0.8f;

    [Tooltip("클리어 전 딜레이 (보스 사망 연출 후 표시)")]
    [SerializeField] private float showDelay = 1.5f;

    private CanvasGroup canvasGroup;

    private void Awake()
    {
        // CanvasGroup이 없으면 추가 (DOTween 페이드에 필요)
        canvasGroup = clearPanel != null ? clearPanel.GetComponent<CanvasGroup>() : null;
        if (canvasGroup == null && clearPanel != null)
            canvasGroup = clearPanel.AddComponent<CanvasGroup>();

        // 시작 시 숨김
        if (clearPanel != null)
            clearPanel.SetActive(false);

        // 로비 버튼 이벤트 연결
        if (lobbyButton != null)
            lobbyButton.onClick.AddListener(OnLobbyButtonClicked);
    }

    private void OnEnable()
    {
        Boss.OnBossDefeated += OnBossDefeated;
    }

    private void OnDisable()
    {
        Boss.OnBossDefeated -= OnBossDefeated;
    }

    private void OnDestroy()
    {
        Boss.OnBossDefeated -= OnBossDefeated;
    }

    /// <summary>
    /// 보스 처치 이벤트 수신 - 딜레이 후 클리어 UI 표시
    /// </summary>
    private void OnBossDefeated()
    {
        StartCoroutine(ShowClearUIDelayed());
    }

    private IEnumerator ShowClearUIDelayed()
    {
        // 게임 일시 정지
        Time.timeScale = 0f;

        yield return new WaitForSecondsRealtime(showDelay);

        ShowClearUI();
    }

    /// <summary>
    /// 클리어 UI를 표시하고 WeaponStatsPanelUI로 DPM 표를 갱신합니다.
    /// </summary>
    private void ShowClearUI()
    {
        if (clearPanel == null) return;

        // 공용 WeaponStatsPanel을 쓰는 경우를 위해 먼저 클리어 패널을 켭니다.
        clearPanel.SetActive(true);

        // WeaponStatsPanelUI 갱신 (None이면 씬에서 자동 탐색)
        if (weaponStatsPanel == null)
            weaponStatsPanel = FindObjectOfType<WeaponStatsPanelUI>(true);

        if (weaponStatsPanel != null)
        {

            weaponStatsPanel.gameObject.SetActive(true);
            weaponStatsPanel.Refresh();
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.DOFade(1f, panelFadeInDuration)
                       .SetEase(Ease.OutCubic)
                       .SetUpdate(true); // TimeScale 0에서도 동작
        }
    }

    /// <summary>
    /// 로비로 돌아가기 버튼 클릭
    /// </summary>
    private void OnLobbyButtonClicked()
    {
        Time.timeScale = 1f;

        if (weaponStatsPanel != null)
            weaponStatsPanel.gameObject.SetActive(false);

        SceneManager.LoadScene("LobbyScene");
    }
}

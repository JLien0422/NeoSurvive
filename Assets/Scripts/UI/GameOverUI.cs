using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;

/// <summary>
/// 플레이어 사망 후 게임 오버 UI를 표시합니다.
/// GameClearUI와 동일한 UI/연출이며, Player.OnPlayerDied 이벤트만 구독합니다.
/// </summary>
public class GameOverUI : MonoBehaviour
{
    [Header("패널 참조")]
    [Tooltip("게임 오버 전체 패널 (루트 오브젝트)")]
    [SerializeField] private GameObject overPanel;

    [Header("DPS 표")]
    [Tooltip("기존 WeaponStatsPanelUI를 그대로 참조 - None이면 씬에서 자동 탐색")]
    [SerializeField] private WeaponStatsPanelUI weaponStatsPanel;

    [Header("버튼")]
    [Tooltip("로비로 돌아가기 버튼")]
    [SerializeField] private Button lobbyButton;

    [Header("DOTween 연출")]
    [Tooltip("게임 오버 패널 페이드인 시간")]
    [SerializeField] private float panelFadeInDuration = 0.8f;

    [Tooltip("게임 오버 UI 표시 전 딜레이")]
    [SerializeField] private float showDelay = 1.5f;

    private CanvasGroup canvasGroup;

    private void Awake()
    {
        canvasGroup = overPanel != null ? overPanel.GetComponent<CanvasGroup>() : null;
        if (canvasGroup == null && overPanel != null)
            canvasGroup = overPanel.AddComponent<CanvasGroup>();

        if (overPanel != null)
            overPanel.SetActive(false);

        if (lobbyButton != null)
            lobbyButton.onClick.AddListener(OnLobbyButtonClicked);
    }

    private void OnEnable()
    {
        Player.OnPlayerDied += OnPlayerDied;
    }

    private void OnDisable()
    {
        Player.OnPlayerDied -= OnPlayerDied;
    }

    private void OnDestroy()
    {
        Player.OnPlayerDied -= OnPlayerDied;
    }

    private void OnPlayerDied()
    {
        StartCoroutine(ShowOverUIDelayed());
    }

    private IEnumerator ShowOverUIDelayed()
    {
        Time.timeScale = 0f;

        yield return new WaitForSecondsRealtime(showDelay);

        ShowOverUI();
    }

    private void ShowOverUI()
    {
        if (overPanel == null) return;

        overPanel.SetActive(true);

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
                       .SetUpdate(true);
        }
    }

    private void OnLobbyButtonClicked()
    {
        Time.timeScale = 1f;

        if (weaponStatsPanel != null)
            weaponStatsPanel.gameObject.SetActive(false);

        SceneManager.LoadScene("LobbyScene");
    }
}

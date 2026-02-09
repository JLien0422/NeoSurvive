using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using NeoSurvive.Network;

namespace NeoSurvive.UI
{
  public class LeaderboardUI : MonoBehaviour
  {
    [Header("UI References")]
    [SerializeField] private GameObject leaderboardPanel;
    [SerializeField] private Transform contentRoot;
    [SerializeField] private GameObject entryPrefab;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button refreshButton;

    private GameServerAPI serverAPI;

    private void Start()
    {
      InitializeAPI();

      if (closeButton != null)
        closeButton.onClick.AddListener(Close);

      if (refreshButton != null)
        refreshButton.onClick.AddListener(RefreshLeaderboard);

      // 시작 시엔 비활성화
      if (leaderboardPanel != null)
        leaderboardPanel.SetActive(false);
    }

    private void InitializeAPI()
    {
      if (GameManager.Instance != null)
      {
        serverAPI = GameManager.Instance.GetComponent<GameServerAPI>();
      }

      if (serverAPI == null)
      {
        serverAPI = FindObjectOfType<GameServerAPI>();
      }
    }

    public void Open()
    {
      if (leaderboardPanel != null)
      {
        leaderboardPanel.SetActive(true);
        RefreshLeaderboard();
      }
    }

    public void Close()
    {
      if (leaderboardPanel != null)
        leaderboardPanel.SetActive(false);
    }

    public void RefreshLeaderboard()
    {
      if (serverAPI == null) InitializeAPI();
      if (serverAPI == null) return;

      // 기존 목록 삭제
      foreach (Transform child in contentRoot)
      {
        Destroy(child.gameObject);
      }

      // 서버 요청 (Top 10)
      StartCoroutine(serverAPI.GetLeaderboard(10, (entries) =>
      {
        if (entries != null)
        {
          foreach (var entry in entries)
          {
            CreateEntry(entry);
          }
        }
      }));
    }

    private void CreateEntry(LeaderboardEntry entry)
    {
      if (entryPrefab == null) return;

      GameObject itemObj = Instantiate(entryPrefab, contentRoot);
      itemObj.SetActive(true);

      // 간단하게 TextMeshProUGUI 찾아서 설정
      // 프리팹 구조가 확실하지 않으므로 자식들 중 TextMeshProUGUI를 순서대로 찾거나 이름으로 찾음
      var texts = itemObj.GetComponentsInChildren<TextMeshProUGUI>();

      // 예상 구조: 0:Rank, 1:Name, 2:Level, 3:Time
      if (texts.Length > 0) texts[0].text = $"{entry.rank}";
      if (texts.Length > 1) texts[1].text = entry.nickname;
      if (texts.Length > 2) texts[2].text = $"Lv.{entry.level}";
      if (texts.Length > 3) texts[3].text = FormatTime(entry.bestSurvivalTime);
    }

    private string FormatTime(int seconds)
    {
      int m = seconds / 60;
      int s = seconds % 60;
      return $"{m:00}:{s:00}";
    }

    private void OnDestroy()
    {
      if (closeButton != null) closeButton.onClick.RemoveAllListeners();
      if (refreshButton != null) refreshButton.onClick.RemoveAllListeners();
    }
  }
}
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NeoSurvive.Characters;

namespace NeoSurvive.UI
{
  using System;
  using UnityEngine.SceneManagement;

  /// <summary>
  /// 게임 시작 시 캐릭터 선택 UI를 관리합니다.
  /// 자동으로 UI 요소들을 찾아서 연결합니다.
  /// </summary>
  public class CharacterSelector : MonoBehaviour
  {
    public event Action OnGameStartRequested;
    public string gameSceneName;

    [Header("UI References")]
    [SerializeField] private GameObject selectionPanel;
    [SerializeField] private Button hackerButton;
    [SerializeField] private Button cyborgButton;
    [SerializeField] private Button startGameButton;


    [Header("Character Info Display")]
    [SerializeField] private TextMeshProUGUI characterNameText;
    [SerializeField] private TextMeshProUGUI characterDescriptionText;
    [SerializeField] private Image characterPortrait;

    [Header("Character Data")]
    [SerializeField] private CharacterData hackerData;
    [SerializeField] private CharacterData cyborgData;

    private CharacterType selectedCharacter = CharacterType.Hacker;

    private void Awake()
    {
      // UI 요소 자동 찾기
      AutoFindUIElements();

      // 캐릭터 데이터 자동 로드 (없으면)
      AutoLoadCharacterData();
    }

    private void Start()
    {

      // 버튼 이벤트 연결
      if (hackerButton != null)
        hackerButton.onClick.AddListener(() => SelectCharacter(CharacterType.Hacker));

      if (cyborgButton != null)
        cyborgButton.onClick.AddListener(() => SelectCharacter(CharacterType.Cyborg));

      if (startGameButton != null)
      {
        startGameButton.onClick.AddListener(StartGame);
      }
      // 캐릭터 선택 패널 초기화 (숨김)
      if (selectionPanel != null) selectionPanel.SetActive(false);

      // 기본 선택 (해커)
      SelectCharacter(CharacterType.Hacker);
    }

    /// <summary>
    /// UI 요소들을 자동으로 찾습니다.
    /// </summary>
    private void AutoFindUIElements()
    {
      if (characterNameText == null)
      {
        var nameObj = GameObject.Find("CharacterNameText");
        if (nameObj != null) characterNameText = nameObj.GetComponent<TextMeshProUGUI>();
      }

      if (characterDescriptionText == null)
      {
        var descObj = GameObject.Find("CharacterDescriptionText");
        if (descObj != null) characterDescriptionText = descObj.GetComponent<TextMeshProUGUI>();
      }

      if (characterPortrait == null)
      {
        var portraitObj = GameObject.Find("CharacterPortrait");
        if (portraitObj != null) characterPortrait = portraitObj.GetComponent<Image>();
      }

      Debug.Log($"[CharacterSelector] UI 요소 자동 연결 완료");
    }

    /// <summary>
    /// 캐릭터 데이터를 자동으로 로드합니다.
    /// </summary>
    private void AutoLoadCharacterData()
    {
      if (hackerData == null)
      {
        hackerData = Resources.Load<CharacterData>("Characters/HackerData");
        if (hackerData == null)
        {
          // Asset 경로에서 찾기
#if UNITY_EDITOR
          var guids = UnityEditor.AssetDatabase.FindAssets("HackerData t:CharacterData");
          if (guids.Length > 0)
          {
              var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
              hackerData = UnityEditor.AssetDatabase.LoadAssetAtPath<CharacterData>(path);
          }
#endif
        }
      }

      if (cyborgData == null)
      {
        cyborgData = Resources.Load<CharacterData>("Characters/CyborgData");
        if (cyborgData == null)
        {
#if UNITY_EDITOR
          var guids = UnityEditor.AssetDatabase.FindAssets("CyborgData t:CharacterData");
          if (guids.Length > 0)
          {
              var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
              cyborgData = UnityEditor.AssetDatabase.LoadAssetAtPath<CharacterData>(path);
          }
#endif
        }
      }

      Debug.Log($"[CharacterSelector] 캐릭터 데이터 로드: Hacker={hackerData != null}, Cyborg={cyborgData != null}");
    }

    /// <summary>
    /// 캐릭터 선택 패널을 표시합니다.
    /// </summary>
    public void ShowCharacterSelection()
    {
      if (selectionPanel != null)
      {
        selectionPanel.SetActive(true);
        Time.timeScale = 0f; // 게임 시간 정지
      }
    }

    /// <summary>
    /// 캐릭터를 선택합니다.
    /// </summary>
    private void SelectCharacter(CharacterType characterType)
    {
      selectedCharacter = characterType;

      // 선택한 캐릭터 데이터 가져오기
      CharacterData characterData = characterType == CharacterType.Hacker ? hackerData : cyborgData;

      if (characterData != null)
      {
        // UI 업데이트
        if (characterNameText != null)
        {
          characterNameText.text = characterData.characterName;
          characterNameText.fontSize = 48;
          characterNameText.alignment = TextAlignmentOptions.Center;
        }

        if (characterDescriptionText != null)
        {
          string stats = $"{characterData.description}\n\n" +
                         $"<b>기본 스탯:</b>\n" +
                         $"체력: {characterData.baseHealth}\n" +
                         $"공격력: {characterData.baseAttackDamage}\n" +
                         $"공격 범위: {characterData.baseAttackRange}\n" +
                         $"공격 속도: {characterData.baseAttackSpeed * 100}%\n" +
                         $"이동 속도: {characterData.baseMoveSpeed}\n\n" +
                         $"<b>특수 능력:</b>\n{characterData.specialAbilityDescription}";
          characterDescriptionText.text = stats;
          characterDescriptionText.fontSize = 24;
          characterDescriptionText.alignment = TextAlignmentOptions.Left;
        }

        if (characterPortrait != null && characterData.characterPortrait != null)
          characterPortrait.sprite = characterData.characterPortrait;
      }

      // 버튼 하이라이트 효과 (선택된 버튼 강조)
      UpdateButtonHighlight();

      Debug.Log($"캐릭터 선택: {characterType}");
    }

    /// <summary>
    /// 선택된 버튼을 시각적으로 강조합니다.
    /// </summary>
    private void UpdateButtonHighlight()
    {
      if (hackerButton != null && cyborgButton != null)
      {
        // 선택된 버튼의 스케일을 약간 키워서 강조
        hackerButton.transform.localScale = selectedCharacter == CharacterType.Hacker ? Vector3.one * 1.1f : Vector3.one;
        cyborgButton.transform.localScale = selectedCharacter == CharacterType.Cyborg ? Vector3.one * 1.1f : Vector3.one;
      }
    }

    /// <summary>
    /// 게임을 시작합니다.
    /// </summary>
    private void StartGame()
    {
      // GameManager에 선택된 캐릭터 전달
      if (GameManager.Instance != null)
      {
        GameManager.Instance.SetSelectedCharacter(selectedCharacter);
      }

      OnGameStartRequested?.Invoke();

      // 선택 패널 숨기기
      if (selectionPanel != null)
      {
        selectionPanel.SetActive(false);
        Time.timeScale = 1f; // 게임 시간 재개
      }

      Debug.Log($"게임 시작! 선택된 캐릭터: {selectedCharacter}");
      SceneManager.LoadScene(gameSceneName);
    }

    private void OnDestroy()
    {
      // 이벤트 리스너 정리
      if (hackerButton != null)
        hackerButton.onClick.RemoveAllListeners();

      if (cyborgButton != null)
        cyborgButton.onClick.RemoveAllListeners();

      if (startGameButton != null)
        startGameButton.onClick.RemoveAllListeners();
    }
  }
}

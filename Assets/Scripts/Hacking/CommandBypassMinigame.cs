using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 커맨드 바이패스 미니게임
/// 화면에 나타나는 화살표 방향키 배열을 보고 빠르게 타이핑합니다.
/// </summary>
public class CommandBypassMinigame : HackingMinigameBase
{
    [Header("UI 설정")]
    [SerializeField]
    private Transform commandParent; // 커맨드가 표시될 부모

    [SerializeField]
    private TextMeshProUGUI statusText; // 상태 텍스트

    [SerializeField]
    private GameObject arrowPrefab; // 화살표 프리팹 (선택사항)

    private List<GameObject> commandArrows = new List<GameObject>();
    private List<KeyCode> currentSequence = new List<KeyCode>(); // 현재 입력해야 할 시퀀스
    private int currentInputIndex = 0; // 현재 입력해야 할 인덱스
    private int totalSequences = 5; // 총 시퀀스 개수
    private int completedSequences = 0;

    private float timeLimit = 20f; // 제한 시간 (초)
    private float remainingTime = 0f;
    private bool isActive = false;

    // 방향키 매핑
    private KeyCode[] arrowKeys = { KeyCode.UpArrow, KeyCode.DownArrow, KeyCode.LeftArrow, KeyCode.RightArrow };
    private string[] arrowSymbols = { "↑", "↓", "←", "→" };

    public override void Initialize(System.Action onSuccessCallback, System.Action onFailureCallback)
    {
        base.Initialize(onSuccessCallback, onFailureCallback);

        // UI 부모가 없으면 자동 생성
        if (commandParent == null)
        {
            CreateUIParent();
        }

        ResetMinigame();
    }

    /// <summary>
    /// UI 부모 오브젝트 자동 생성
    /// </summary>
    private void CreateUIParent()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("HackingCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }

        GameObject panel = new GameObject("CommandBypassPanel");
        panel.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(500, 300);
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;

        UnityEngine.UI.Image panelImg = panel.AddComponent<UnityEngine.UI.Image>();
        panelImg.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);

        commandParent = panel.transform;

        // 상태 텍스트 생성
        GameObject statusObj = new GameObject("StatusText");
        statusObj.transform.SetParent(panel.transform, false);

        RectTransform statusRect = statusObj.AddComponent<RectTransform>();
        statusRect.sizeDelta = new Vector2(450, 50);
        statusRect.anchorMin = new Vector2(0.5f, 1f);
        statusRect.anchorMax = new Vector2(0.5f, 1f);
        statusRect.anchoredPosition = new Vector2(0, -20);

        statusText = statusObj.AddComponent<TextMeshProUGUI>();
        statusText.text = "커맨드 바이패스";
        statusText.fontSize = 24;
        statusText.alignment = TextAlignmentOptions.Center;
        statusText.color = Color.white;
    }

    /// <summary>
    /// 미니게임 리셋
    /// </summary>
    private void ResetMinigame()
    {
        completedSequences = 0;
        currentInputIndex = 0;
        remainingTime = timeLimit;
        isActive = true;

        ClearCommandArrows();
        GenerateNewSequence();
    }

    /// <summary>
    /// 새로운 시퀀스 생성
    /// </summary>
    private void GenerateNewSequence()
    {
        currentSequence.Clear();
        currentInputIndex = 0;

        // 랜덤한 방향키 시퀀스 생성 (5~8개)
        int sequenceLength = Random.Range(5, 9);
        for (int i = 0; i < sequenceLength; i++)
        {
            int randomIndex = Random.Range(0, arrowKeys.Length);
            currentSequence.Add(arrowKeys[randomIndex]);
        }

        DisplaySequence();
    }

    /// <summary>
    /// 시퀀스를 화면에 표시
    /// </summary>
    private void DisplaySequence()
    {
        ClearCommandArrows();

        float arrowSize = 60f;
        float spacing = 10f;
        float totalWidth = (arrowSize * currentSequence.Count) + (spacing * (currentSequence.Count - 1));
        float startX = -totalWidth / 2f + arrowSize / 2f;

        for (int i = 0; i < currentSequence.Count; i++)
        {
            GameObject arrowObj = new GameObject($"Arrow_{i}");
            arrowObj.transform.SetParent(commandParent, false);

            RectTransform rect = arrowObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(arrowSize, arrowSize);
            rect.anchoredPosition = new Vector2(startX + i * (arrowSize + spacing), 0);

            UnityEngine.UI.Text text = arrowObj.AddComponent<UnityEngine.UI.Text>();
            int keyIndex = System.Array.IndexOf(arrowKeys, currentSequence[i]);
            text.text = arrowSymbols[keyIndex];
            text.fontSize = 48;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = i < currentInputIndex ? Color.green : Color.white;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            commandArrows.Add(arrowObj);
        }

        UpdateStatusText();
    }

    /// <summary>
    /// 커맨드 화살표 정리
    /// </summary>
    private void ClearCommandArrows()
    {
        foreach (var arrow in commandArrows)
        {
            if (arrow != null)
                Destroy(arrow);
        }
        commandArrows.Clear();
    }

    private void Update()
    {
        if (!isActive) return;

        // 시간 제한 체크
        remainingTime -= Time.deltaTime;
        if (remainingTime <= 0f)
        {
            OnFailure();
            return;
        }

        // 입력 처리
        if (currentInputIndex < currentSequence.Count)
        {
            KeyCode expectedKey = currentSequence[currentInputIndex];
            
            if (Input.GetKeyDown(expectedKey))
            {
                // 올바른 입력
                currentInputIndex++;
                DisplaySequence();

                // 시퀀스 완료
                if (currentInputIndex >= currentSequence.Count)
                {
                    completedSequences++;
                    
                    // 모든 시퀀스 완료
                    if (completedSequences >= totalSequences)
                    {
                        OnSuccess();
                        return;
                    }

                    // 다음 시퀀스 생성
                    GenerateNewSequence();
                }
            }
            else if (Input.anyKeyDown && !Input.GetKeyDown(KeyCode.Escape))
            {
                // 잘못된 입력 체크 (방향키가 아닌 다른 키는 무시)
                bool isArrowKey = false;
                foreach (var key in arrowKeys)
                {
                    if (Input.GetKeyDown(key) && key != expectedKey)
                    {
                        isArrowKey = true;
                        break;
                    }
                }

                if (isArrowKey)
                {
                    // 잘못된 방향키 입력
                    OnFailure();
                }
            }
        }
    }

    /// <summary>
    /// 상태 텍스트 업데이트
    /// </summary>
    private void UpdateStatusText()
    {
        if (statusText != null)
        {
            statusText.text = $"시퀀스 {completedSequences + 1}/{totalSequences} - 다음: {arrowSymbols[System.Array.IndexOf(arrowKeys, currentSequence[currentInputIndex])]}";
        }
    }

    public override void OnSuccess()
    {
        isActive = false;
        if (statusText != null)
        {
            statusText.text = "성공!";
        }
        base.OnSuccess();
    }

    public override void OnFailure()
    {
        isActive = false;
        if (statusText != null)
        {
            statusText.text = "실패!";
        }
        base.OnFailure();
    }
}

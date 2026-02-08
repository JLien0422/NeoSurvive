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

    private Transform preMadeArrowContainer;  // 손으로 만든 화살표 컨테이너
    private List<Component> preMadeArrowTexts = new List<Component>(); // 8칸 모드용
    private Component singleArrowText;        // ArrowContainer에 Text 하나만 붙인 모드
    private bool usePreMadeUI = false;

    private Slider progressSlider;   // 진행도 게이지 (Slider)
    private Image progressImage;     // 진행도 게이지 (Image Fill)

    private List<GameObject> commandArrows = new List<GameObject>();
    private List<KeyCode> currentSequence = new List<KeyCode>(); // 현재 입력해야 할 시퀀스
    private int currentInputIndex = 0; // 현재 입력해야 할 인덱스
    private int totalCorrectKeysPressed = 0;  // 지금까지 맞춘 화살표 개수
    private int totalKeysNeeded = 20;         // 기획: 10초 안에 20개 맞추면 성공

    private float timeLimit = 10f; // 기획: 미니게임 10초 제한
    private float remainingTime = 0f;
    private bool isActive = false;

    // 방향키 매핑
    private KeyCode[] arrowKeys = { KeyCode.UpArrow, KeyCode.DownArrow, KeyCode.LeftArrow, KeyCode.RightArrow };
    private string[] arrowSymbols = { "↑", "↓", "←", "→" };

    /// <summary>
    /// 손으로 만든 UI 사용 (HackingSystem에서 호출)
    /// </summary>
    public void SetPreMadeUI(Transform arrowContainer, TextMeshProUGUI status, Slider progress = null, Image progressFill = null)
    {
        preMadeArrowContainer = arrowContainer;
        statusText = status;
        progressSlider = progress;
        progressImage = (progressFill != null && progressFill.type == Image.Type.Filled) ? progressFill : null;
        singleArrowText = null;

        if (arrowContainer == null) { commandParent = null; return; }
        commandParent = arrowContainer;

        // ArrowContainer에 Text/TMP 하나만 붙인 경우
        var tmp = arrowContainer.GetComponent<TextMeshProUGUI>();
        if (tmp != null) { singleArrowText = tmp; usePreMadeUI = true; return; }
        var txt = arrowContainer.GetComponent<UnityEngine.UI.Text>();
        if (txt != null) { singleArrowText = txt; usePreMadeUI = true; return; }
        tmp = arrowContainer.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmp != null) { singleArrowText = tmp; usePreMadeUI = true; return; }
        txt = arrowContainer.GetComponentInChildren<UnityEngine.UI.Text>(true);
        if (txt != null) { singleArrowText = txt; usePreMadeUI = true; return; }

        // 자식 8개(Arrow_0~7) 모드
        usePreMadeUI = arrowContainer.childCount >= 8;
        if (usePreMadeUI) CachePreMadeArrowTexts();
    }

    /// <summary>
    /// 손으로 만든 화살표 요소들의 Text/TextMeshProUGUI 캐싱
    /// </summary>
    private void CachePreMadeArrowTexts()
    {
        preMadeArrowTexts.Clear();
        if (preMadeArrowContainer == null) return;
        for (int i = 0; i < 8 && i < preMadeArrowContainer.childCount; i++)
        {
            Transform child = preMadeArrowContainer.GetChild(i);
            var tmp = child.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmp != null) preMadeArrowTexts.Add(tmp);
            else
            {
                var txt = child.GetComponentInChildren<UnityEngine.UI.Text>(true);
                if (txt != null) preMadeArrowTexts.Add(txt);
            }
        }
    }

    public override void Initialize(System.Action onSuccessCallback, System.Action onFailureCallback)
    {
        base.Initialize(onSuccessCallback, onFailureCallback);

        if (usePreMadeUI && preMadeArrowContainer != null)
        {
            // 손으로 만든 UI 사용
            commandParent = preMadeArrowContainer;
        }
        else if (commandParent == null)
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
        totalCorrectKeysPressed = 0;
        currentInputIndex = 0;
        remainingTime = timeLimit;
        isActive = true;

        ClearCommandArrows();
        GenerateNewSequence();
        UpdateProgressGauge();
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
        if (usePreMadeUI && preMadeArrowTexts.Count >= 8)
        {
            DisplaySequencePreMade();
            return;
        }

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
    /// 손으로 만든 화살표 요소로 시퀀스 표시
    /// </summary>
    private void DisplaySequencePreMade()
    {
        // ArrowContainer에 Text 하나만 붙인 경우: 시퀀스 전체를 한 문자열로
        if (singleArrowText != null)
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < currentSequence.Count; i++)
            {
                int idx = System.Array.IndexOf(arrowKeys, currentSequence[i]);
                if (i > 0) sb.Append("  ");
                sb.Append(arrowSymbols[idx]);
            }
            string s = sb.ToString();
            if (singleArrowText is TextMeshProUGUI t) t.text = s;
            else if (singleArrowText is UnityEngine.UI.Text u) u.text = s;
            UpdateStatusText();
            return;
        }

        // 자식 8칸(Arrow_0~7) 모드
        int maxSlots = Mathf.Min(8, preMadeArrowContainer.childCount, preMadeArrowTexts.Count);
        for (int i = 0; i < maxSlots; i++)
        {
            Transform child = preMadeArrowContainer.GetChild(i);
            child.gameObject.SetActive(i < currentSequence.Count);

            if (i < currentSequence.Count && i < preMadeArrowTexts.Count)
            {
                int keyIndex = System.Array.IndexOf(arrowKeys, currentSequence[i]);
                string symbol = arrowSymbols[keyIndex];
                Color c = i < currentInputIndex ? Color.green : Color.white;

                var tmp = preMadeArrowTexts[i] as TextMeshProUGUI;
                if (tmp != null) { tmp.text = symbol; tmp.color = c; }
                else if (preMadeArrowTexts[i] is UnityEngine.UI.Text tx) { tx.text = symbol; tx.color = c; }
            }
        }
        UpdateStatusText();
    }

    /// <summary>
    /// 커맨드 화살표 정리
    /// </summary>
    private void ClearCommandArrows()
    {
        if (usePreMadeUI && preMadeArrowContainer != null)
        {
            if (singleArrowText != null)
            {
                if (singleArrowText is TextMeshProUGUI t) t.text = "";
                else if (singleArrowText is UnityEngine.UI.Text u) u.text = "";
            }
            else
            {
                for (int i = 0; i < preMadeArrowContainer.childCount; i++)
                    preMadeArrowContainer.GetChild(i).gameObject.SetActive(false);
            }
            return;
        }
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
                // 올바른 입력 → 화살표 한 개당 진행도 증가
                currentInputIndex++;
                totalCorrectKeysPressed++;
                DisplaySequence();

                // 목표 개수 도달 (10초 안에 20개)
                if (totalCorrectKeysPressed >= totalKeysNeeded)
                {
                    OnSuccess();
                    return;
                }

                // 시퀀스 끝나면 다음 시퀀스
                if (currentInputIndex >= currentSequence.Count)
                    GenerateNewSequence();
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
        if (statusText != null) statusText.text = ""; // 텍스트 없음, 게이지만 사용
        UpdateProgressGauge();
    }

    /// <summary>
    /// 진행도 게이지 업데이트 (0~1) - 화살표 한 개 맞출 때마다 1/totalKeysNeeded 씩 증가
    /// </summary>
    private void UpdateProgressGauge()
    {
        float progress = (float)totalCorrectKeysPressed / totalKeysNeeded;
        if (progressSlider != null) progressSlider.value = progress;
        if (progressImage != null) progressImage.fillAmount = progress;
    }

    public override void OnSuccess()
    {
        isActive = false;
        if (progressSlider != null) progressSlider.value = 1f;
        if (progressImage != null) progressImage.fillAmount = 1f;
        base.OnSuccess();
    }

    public override void OnFailure()
    {
        isActive = false;
        base.OnFailure();
    }
}

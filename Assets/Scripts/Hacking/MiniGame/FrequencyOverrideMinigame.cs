using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 주파수 오버라이드 미니게임
/// 방향키로 플레이어 파형을 조절하여 목표 파형과 3초간 일치시킵니다.
/// LineRenderer 대신 UI Image 점들로 파형을 그립니다.
/// </summary>
public class FrequencyOverrideMinigame : HackingMinigameBase
{
    [Header("UI 설정")]
    [SerializeField]
    private Transform gameParent;

    [SerializeField]
    private GameObject targetWaveGraph; // 목표 파형 컨테이너 (녹색)
    [SerializeField]
    private GameObject playerWaveGraph; // 플레이어 파형 컨테이너 (빨간)

    private Slider timeGauge;
    private Slider matchGauge;
    private Slider holdGauge;

    private float targetFrequency = 1f;
    private float targetAmplitude = 1f;
    private float playerFrequency = 1f;
    private float playerAmplitude = 1f;

    private float matchThreshold = 0.1f;
    private float matchDuration = 3f;
    private float currentMatchTime = 0f;

    private float timeLimit = 15f;
    private float remainingTime = 0f;
    private bool isActive = false;

    private const int WAVE_POINTS = 50;
    private RectTransform[] targetDots;
    private RectTransform[] playerDots;

    public void SetPreMadeUI(GameObject targetWave, GameObject playerWave,
                              Slider timeSlider, Slider matchSlider, Slider holdSlider)
    {
        targetWaveGraph = targetWave;
        playerWaveGraph = playerWave;
        timeGauge = timeSlider;
        matchGauge = matchSlider;
        holdGauge = holdSlider;

        if (gameParent == null && targetWave != null)
            gameParent = targetWave.transform.parent;
    }

    public override void Initialize(System.Action onSuccessCallback, System.Action onFailureCallback)
    {
        base.Initialize(onSuccessCallback, onFailureCallback);

        if (gameParent == null)
            CreateUIParent();

        if (targetWaveGraph == null || playerWaveGraph == null)
            CreateWaveContainers();

        InitializeWaveDots();
        ResetMinigame();
    }

    private void CreateUIParent()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("HackingCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        GameObject panel = new GameObject("FrequencyOverridePanel");
        panel.transform.SetParent(canvas.transform, false);
        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(600, 400);
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        Image panelImg = panel.AddComponent<Image>();
        panelImg.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        gameParent = panel.transform;
    }

    private void CreateWaveContainers()
    {
        if (targetWaveGraph == null)
        {
            targetWaveGraph = new GameObject("TargetWave");
            targetWaveGraph.transform.SetParent(gameParent, false);
            RectTransform r = targetWaveGraph.AddComponent<RectTransform>();
            r.anchoredPosition = new Vector2(0, 60f);
            r.sizeDelta = new Vector2(300f, 80f);
        }
        if (playerWaveGraph == null)
        {
            playerWaveGraph = new GameObject("PlayerWave");
            playerWaveGraph.transform.SetParent(gameParent, false);
            RectTransform r = playerWaveGraph.AddComponent<RectTransform>();
            r.anchoredPosition = new Vector2(0, -60f);
            r.sizeDelta = new Vector2(300f, 80f);
        }
    }

    private void InitializeWaveDots()
    {
        // 두 파형 컨테이너를 같은 위치(중앙)에 겹치도록 설정
        RectTransform targetRect = targetWaveGraph.GetComponent<RectTransform>();
        RectTransform playerRect = playerWaveGraph.GetComponent<RectTransform>();
        if (targetRect != null) targetRect.anchoredPosition = Vector2.zero;
        if (playerRect != null) playerRect.anchoredPosition = Vector2.zero;

        // 기존 점 정리
        foreach (Transform child in targetWaveGraph.transform) Destroy(child.gameObject);
        foreach (Transform child in playerWaveGraph.transform) Destroy(child.gameObject);

        targetDots = CreateDots(targetWaveGraph.transform, Color.green);
        playerDots = CreateDots(playerWaveGraph.transform, Color.red);
    }

    private RectTransform[] CreateDots(Transform parent, Color color)
    {
        RectTransform[] dots = new RectTransform[WAVE_POINTS];
        for (int i = 0; i < WAVE_POINTS; i++)
        {
            GameObject dot = new GameObject($"Dot_{i}");
            dot.transform.SetParent(parent, false);
            RectTransform rect = dot.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(5f, 5f);
            Image img = dot.AddComponent<Image>();
            img.color = color;
            dots[i] = rect;
        }
        return dots;
    }

    private void ResetMinigame()
    {
        targetFrequency = Random.Range(0.5f, 2f);
        targetAmplitude = Random.Range(0.5f, 1.5f);
        playerFrequency = Random.Range(0.3f, 2.5f);
        playerAmplitude = Random.Range(0.3f, 1.8f);

        currentMatchTime = 0f;
        remainingTime = timeLimit;
        isActive = true;

        UpdateWaveGraphs();
        UpdateTimeGauge();
        UpdateMatchGauge();
        UpdateHoldGauge();
    }

    private void UpdateWaveGraphs()
    {
        if (targetDots == null || playerDots == null) return;

        float graphWidth = 280f;
        float graphHeight = 35f;

        for (int i = 0; i < WAVE_POINTS; i++)
        {
            float t = i / (float)(WAVE_POINTS - 1);
            float x = (t - 0.5f) * graphWidth;

            float ty = Mathf.Sin(t * targetFrequency * Mathf.PI * 6f) * targetAmplitude * graphHeight;
            targetDots[i].anchoredPosition = new Vector2(x, ty);

            float py = Mathf.Sin(t * playerFrequency * Mathf.PI * 6f) * playerAmplitude * graphHeight;
            playerDots[i].anchoredPosition = new Vector2(x, py);
        }
    }

    private void UpdateTimeGauge()
    {
        if (timeGauge != null)
            timeGauge.value = remainingTime / timeLimit; // 1→0
    }

    private void UpdateMatchGauge()
    {
        // 일치도: 두 파형이 얼마나 가까운지 (0→1)
        float freqDiff = Mathf.Abs(playerFrequency - targetFrequency);
        float ampDiff = Mathf.Abs(playerAmplitude - targetAmplitude);
        float maxDiff = Mathf.Max(freqDiff, ampDiff);
        float matchValue = Mathf.Clamp01(1f - maxDiff / matchThreshold);
        if (matchGauge != null)
            matchGauge.value = matchValue;
    }

    private void UpdateHoldGauge()
    {
        // 유지 시간: 일치 상태를 3초 유지 (0→1)
        if (holdGauge != null)
            holdGauge.value = currentMatchTime / matchDuration;
    }

    private void Update()
    {
        if (!isActive) return;

        remainingTime -= Time.unscaledDeltaTime;
        UpdateTimeGauge();

        if (remainingTime <= 0f)
        {
            OnFailure();
            return;
        }

        // 방향키 입력으로 파형 조절
        float freqChange = 0f;
        float ampChange = 0f;

        if (Input.GetKey(KeyCode.LeftArrow))  freqChange -= Time.unscaledDeltaTime * 0.5f;
        if (Input.GetKey(KeyCode.RightArrow)) freqChange += Time.unscaledDeltaTime * 0.5f;
        if (Input.GetKey(KeyCode.UpArrow))    ampChange  += Time.unscaledDeltaTime * 0.5f;
        if (Input.GetKey(KeyCode.DownArrow))  ampChange  -= Time.unscaledDeltaTime * 0.5f;

        playerFrequency = Mathf.Clamp(playerFrequency + freqChange, 0.1f, 3f);
        playerAmplitude = Mathf.Clamp(playerAmplitude + ampChange, 0.1f, 2f);

        UpdateWaveGraphs();

        // 일치 판정
        float freqDiff = Mathf.Abs(playerFrequency - targetFrequency);
        float ampDiff = Mathf.Abs(playerAmplitude - targetAmplitude);

        UpdateMatchGauge();

        if (freqDiff <= matchThreshold && ampDiff <= matchThreshold)
        {
            currentMatchTime += Time.unscaledDeltaTime;
            UpdateHoldGauge();
            if (currentMatchTime >= matchDuration)
            {
                OnSuccess();
                return;
            }
        }
        else
        {
            currentMatchTime = 0f;
            UpdateHoldGauge();
        }
    }

    public override void OnSuccess()
    {
        isActive = false;
        if (timeGauge != null) timeGauge.value = 1f;
        if (matchGauge != null) matchGauge.value = 1f;
        if (holdGauge != null) holdGauge.value = 1f;
        // 성공 시 플레이어 파형 녹색으로 변경
        if (playerDots != null)
            foreach (var dot in playerDots)
                if (dot != null) dot.GetComponent<Image>().color = Color.green;
        base.OnSuccess();
    }

    public override void OnFailure()
    {
        isActive = false;
        if (timeGauge != null) timeGauge.value = 0f;
        base.OnFailure();
    }
}

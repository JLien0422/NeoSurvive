using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 주파수 오버라이드 미니게임
/// 방향키로 플레이어 사각파를 조절하여 목표 사각파와 3초간 일치시킵니다.
/// 
/// 변경점:
/// - 기존 Dot 방식 제거
/// - 가로선/세로선 Segment Image 방식으로 사각파 표시
/// </summary>
public class FrequencyOverrideMinigame : HackingMinigameBase
{
    [Header("UI 설정")]
    [SerializeField] private Transform gameParent;

    [SerializeField] private GameObject targetWaveGraph;
    [SerializeField] private GameObject playerWaveGraph;

    private Slider timeGauge;
    private Slider matchGauge;
    private Slider holdGauge;

    [Header("게임 설정")]
    [SerializeField] private float timeLimit = 15f;
    [SerializeField] private float matchThreshold = 0.1f;
    [SerializeField] private float matchDuration = 3f;

    [Header("파형 표시 설정")]
    [SerializeField] private float graphWidth = 300f;
    [SerializeField] private float graphHeight = 35f;
    [SerializeField] private float lineThickness = 6f;
    [SerializeField] private int sampleCount = 48;

    [Header("색상")]
    [SerializeField] private Color targetColor = Color.green;
    [SerializeField] private Color playerColor = Color.red;
    [SerializeField] private Color successColor = Color.green;

    private float targetFrequency = 1f;
    private float targetAmplitude = 1f;
    private float playerFrequency = 1f;
    private float playerAmplitude = 1f;

    private float currentMatchTime = 0f;
    private float remainingTime = 0f;
    private bool isActive = false;

    private readonly List<GameObject> targetSegments = new List<GameObject>();
    private readonly List<GameObject> playerSegments = new List<GameObject>();

    public void SetPreMadeUI(
        GameObject targetWave,
        GameObject playerWave,
        Slider timeSlider,
        Slider matchSlider,
        Slider holdSlider)
    {
        targetWaveGraph = targetWave;
        playerWaveGraph = playerWave;
        timeGauge = timeSlider;
        matchGauge = matchSlider;
        holdGauge = holdSlider;

        if (gameParent == null && targetWave != null)
            gameParent = targetWave.transform.parent;
    }

    public override void Initialize(
        System.Action onSuccessCallback,
        System.Action onFailureCallback)
    {
        base.Initialize(onSuccessCallback, onFailureCallback);

        if (gameParent == null)
            CreateUIParent();

        if (targetWaveGraph == null || playerWaveGraph == null)
            CreateWaveContainers();

        PrepareWaveContainers();
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
            r.anchoredPosition = Vector2.zero;
            r.sizeDelta = new Vector2(graphWidth, graphHeight * 2f);
        }

        if (playerWaveGraph == null)
        {
            playerWaveGraph = new GameObject("PlayerWave");
            playerWaveGraph.transform.SetParent(gameParent, false);

            RectTransform r = playerWaveGraph.AddComponent<RectTransform>();
            r.anchoredPosition = Vector2.zero;
            r.sizeDelta = new Vector2(graphWidth, graphHeight * 2f);
        }
    }

    private void PrepareWaveContainers()
    {
        RectTransform targetRect = targetWaveGraph.GetComponent<RectTransform>();
        RectTransform playerRect = playerWaveGraph.GetComponent<RectTransform>();

        if (targetRect != null)
            targetRect.anchoredPosition = Vector2.zero;

        if (playerRect != null)
            playerRect.anchoredPosition = Vector2.zero;

        ClearSegments(targetWaveGraph.transform);
        ClearSegments(playerWaveGraph.transform);

        targetSegments.Clear();
        playerSegments.Clear();
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

    private void ClearSegments(Transform parent)
    {
        if (parent == null)
            return;

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Destroy(parent.GetChild(i).gameObject);
        }
    }

    private void UpdateWaveGraphs()
    {
        if (targetWaveGraph == null || playerWaveGraph == null)
            return;

        RedrawSquareWave(
            targetWaveGraph.transform,
            targetSegments,
            targetFrequency,
            targetAmplitude,
            targetColor
        );

        RedrawSquareWave(
            playerWaveGraph.transform,
            playerSegments,
            playerFrequency,
            playerAmplitude,
            playerColor
        );
    }

    /// <summary>
    /// 사각파를 가로/세로 Image 막대로 그림
    /// </summary>
    private void RedrawSquareWave(
        Transform parent,
        List<GameObject> segmentList,
        float frequency,
        float amplitude,
        Color color)
    {
        ClearSegmentObjects(segmentList);

        List<Vector2> points = BuildSquareWavePoints(frequency, amplitude);

        if (points.Count < 2)
            return;

        for (int i = 0; i < points.Count - 1; i++)
        {
            CreateSegment(
                parent,
                segmentList,
                points[i],
                points[i + 1],
                color
            );
        }
    }

    private void ClearSegmentObjects(List<GameObject> segmentList)
    {
        for (int i = segmentList.Count - 1; i >= 0; i--)
        {
            if (segmentList[i] != null)
                Destroy(segmentList[i]);
        }

        segmentList.Clear();
    }

    /// <summary>
    /// 사각파의 꺾이는 점 목록 생성
    /// </summary>
    private List<Vector2> BuildSquareWavePoints(float frequency, float amplitude)
    {
        List<Vector2> points = new List<Vector2>();

        float halfWidth = graphWidth * 0.5f;
        float yHigh = amplitude * graphHeight;
        float yLow = -amplitude * graphHeight;

        bool isHigh = Mathf.Sin(0f) >= 0f;
        float currentY = isHigh ? yHigh : yLow;

        points.Add(new Vector2(-halfWidth, currentY));

        float previousY = currentY;

        for (int i = 1; i <= sampleCount; i++)
        {
            float t = i / (float)sampleCount;
            float x = Mathf.Lerp(-halfWidth, halfWidth, t);

            float sinValue = Mathf.Sin(t * frequency * Mathf.PI * 6f);
            bool high = sinValue >= 0f;
            float y = high ? yHigh : yLow;

            if (!Mathf.Approximately(y, previousY))
            {
                float prevT = (i - 1) / (float)sampleCount;
                float prevX = Mathf.Lerp(-halfWidth, halfWidth, prevT);

                // 이전 높이의 가로선 끝
                points.Add(new Vector2(prevX, previousY));

                // 같은 X에서 세로 전환
                points.Add(new Vector2(prevX, y));

                previousY = y;
            }
        }

        points.Add(new Vector2(halfWidth, previousY));

        return points;
    }

    private void CreateSegment(
        Transform parent,
        List<GameObject> segmentList,
        Vector2 from,
        Vector2 to,
        Color color)
    {
        GameObject segment = new GameObject("WaveSegment");
        segment.transform.SetParent(parent, false);

        RectTransform rect = segment.AddComponent<RectTransform>();
        Image img = segment.AddComponent<Image>();

        img.color = color;
        img.raycastTarget = false;

        Vector2 direction = to - from;
        float distance = direction.magnitude;

        if (distance <= 0.01f)
        {
            Destroy(segment);
            return;
        }

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);

        rect.sizeDelta = new Vector2(distance, lineThickness);
        rect.anchoredPosition = (from + to) * 0.5f;
        rect.localRotation = Quaternion.Euler(0f, 0f, angle);

        segmentList.Add(segment);
    }

    private void UpdateTimeGauge()
    {
        if (timeGauge != null)
            timeGauge.value = remainingTime / timeLimit;
    }

    private void UpdateMatchGauge()
    {
        float freqDiff = Mathf.Abs(playerFrequency - targetFrequency);
        float ampDiff = Mathf.Abs(playerAmplitude - targetAmplitude);

        float maxDiff = Mathf.Max(freqDiff, ampDiff);
        float matchValue = Mathf.Clamp01(1f - maxDiff / matchThreshold);

        if (matchGauge != null)
            matchGauge.value = matchValue;
    }

    private void UpdateHoldGauge()
    {
        if (holdGauge != null)
            holdGauge.value = currentMatchTime / matchDuration;
    }

    private void Update()
    {
        if (!isActive)
            return;

        remainingTime -= Time.unscaledDeltaTime;
        UpdateTimeGauge();

        if (remainingTime <= 0f)
        {
            OnFailure();
            return;
        }

        HandleInput();
        UpdateWaveGraphs();
        CheckMatch();
    }

    private void HandleInput()
    {
        float freqChange = 0f;
        float ampChange = 0f;

        if (Input.GetKey(KeyCode.LeftArrow))
            freqChange -= Time.unscaledDeltaTime * 0.5f;

        if (Input.GetKey(KeyCode.RightArrow))
            freqChange += Time.unscaledDeltaTime * 0.5f;

        if (Input.GetKey(KeyCode.UpArrow))
            ampChange += Time.unscaledDeltaTime * 0.5f;

        if (Input.GetKey(KeyCode.DownArrow))
            ampChange -= Time.unscaledDeltaTime * 0.5f;

        playerFrequency = Mathf.Clamp(playerFrequency + freqChange, 0.1f, 3f);
        playerAmplitude = Mathf.Clamp(playerAmplitude + ampChange, 0.1f, 2f);
    }

    private void CheckMatch()
    {
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

        if (timeGauge != null)
            timeGauge.value = 1f;

        if (matchGauge != null)
            matchGauge.value = 1f;

        if (holdGauge != null)
            holdGauge.value = 1f;

        SetSegmentsColor(playerSegments, successColor);

        base.OnSuccess();
    }

    public override void OnFailure()
    {
        isActive = false;

        if (timeGauge != null)
            timeGauge.value = 0f;

        base.OnFailure();
    }

    private void SetSegmentsColor(List<GameObject> segments, Color color)
    {
        foreach (GameObject segment in segments)
        {
            if (segment == null)
                continue;

            Image img = segment.GetComponent<Image>();

            if (img != null)
                img.color = color;
        }
    }
}
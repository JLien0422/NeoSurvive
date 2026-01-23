using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 주파수 오버라이드 미니게임
/// 물결 모양의 파형 그래프를 조절하여 목표 그래프와 완전히 겹치게 만듭니다.
/// </summary>
public class FrequencyOverrideMinigame : HackingMinigameBase
{
    [Header("UI 설정")]
    [SerializeField]
    private Transform gameParent; // 게임이 표시될 부모

    [SerializeField]
    private TextMeshProUGUI statusText; // 상태 텍스트

    [SerializeField]
    private GameObject targetWaveGraph; // 목표 파형 그래프
    [SerializeField]
    private GameObject playerWaveGraph; // 플레이어가 조절하는 파형 그래프

    private float targetFrequency = 1f; // 목표 주파수
    private float targetAmplitude = 1f; // 목표 진폭
    private float playerFrequency = 1f; // 플레이어 주파수
    private float playerAmplitude = 1f; // 플레이어 진폭

    private float matchThreshold = 0.1f; // 일치 판정 임계값
    private float matchDuration = 3f; // 일치 유지 시간
    private float currentMatchTime = 0f;

    private float timeLimit = 30f;
    private float remainingTime = 0f;
    private bool isActive = false;

    private LineRenderer targetLineRenderer;
    private LineRenderer playerLineRenderer;

    public override void Initialize(System.Action onSuccessCallback, System.Action onFailureCallback)
    {
        base.Initialize(onSuccessCallback, onFailureCallback);

        if (gameParent == null)
        {
            CreateUIParent();
        }

        CreateWaveGraphs();
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
            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }

        GameObject panel = new GameObject("FrequencyOverridePanel");
        panel.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(600, 400);
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;

        UnityEngine.UI.Image panelImg = panel.AddComponent<UnityEngine.UI.Image>();
        panelImg.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);

        gameParent = panel.transform;

        // 상태 텍스트
        GameObject statusObj = new GameObject("StatusText");
        statusObj.transform.SetParent(panel.transform, false);
        RectTransform statusRect = statusObj.AddComponent<RectTransform>();
        statusRect.sizeDelta = new Vector2(550, 50);
        statusRect.anchorMin = new Vector2(0.5f, 1f);
        statusRect.anchorMax = new Vector2(0.5f, 1f);
        statusRect.anchoredPosition = new Vector2(0, -20);
        statusText = statusObj.AddComponent<TextMeshProUGUI>();
        statusText.text = "주파수 오버라이드";
        statusText.fontSize = 24;
        statusText.alignment = TextAlignmentOptions.Center;
        statusText.color = Color.white;
    }

    private void CreateWaveGraphs()
    {
        // 목표 파형 그래프
        if (targetWaveGraph == null)
        {
            targetWaveGraph = new GameObject("TargetWave");
            targetWaveGraph.transform.SetParent(gameParent, false);
            targetWaveGraph.transform.localPosition = new Vector3(0, 100, 0);

            targetLineRenderer = targetWaveGraph.AddComponent<LineRenderer>();
            targetLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            targetLineRenderer.material.color = Color.green;
            targetLineRenderer.startWidth = 3f;
            targetLineRenderer.endWidth = 3f;
            targetLineRenderer.positionCount = 100;
        }

        // 플레이어 파형 그래프
        if (playerWaveGraph == null)
        {
            playerWaveGraph = new GameObject("PlayerWave");
            playerWaveGraph.transform.SetParent(gameParent, false);
            playerWaveGraph.transform.localPosition = new Vector3(0, -100, 0);

            playerLineRenderer = playerWaveGraph.AddComponent<LineRenderer>();
            playerLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            playerLineRenderer.material.color = Color.red;
            playerLineRenderer.startWidth = 3f;
            playerLineRenderer.endWidth = 3f;
            playerLineRenderer.positionCount = 100;
        }
    }

    private void ResetMinigame()
    {
        // 랜덤 목표 주파수/진폭 생성
        targetFrequency = Random.Range(0.5f, 2f);
        targetAmplitude = Random.Range(0.5f, 1.5f);

        // 플레이어 초기값 (목표와 다르게)
        playerFrequency = Random.Range(0.3f, 2.5f);
        playerAmplitude = Random.Range(0.3f, 1.8f);

        currentMatchTime = 0f;
        remainingTime = timeLimit;
        isActive = true;

        UpdateWaveGraphs();
        UpdateStatusText();
    }

    private void UpdateWaveGraphs()
    {
        if (targetLineRenderer == null || playerLineRenderer == null) return;

        float graphWidth = 400f;
        float graphHeight = 100f;

        // 목표 파형 그리기
        for (int i = 0; i < 100; i++)
        {
            float x = (i / 99f) * graphWidth - graphWidth / 2f;
            float y = Mathf.Sin(x * targetFrequency * 0.1f) * targetAmplitude * graphHeight;
            targetLineRenderer.SetPosition(i, new Vector3(x, y, 0));
        }

        // 플레이어 파형 그리기
        for (int i = 0; i < 100; i++)
        {
            float x = (i / 99f) * graphWidth - graphWidth / 2f;
            float y = Mathf.Sin(x * playerFrequency * 0.1f) * playerAmplitude * graphHeight;
            playerLineRenderer.SetPosition(i, new Vector3(x, y, 0));
        }
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
        float freqChange = 0f;
        float ampChange = 0f;

        if (Input.GetKey(KeyCode.LeftArrow))
            freqChange -= Time.deltaTime * 0.5f;
        if (Input.GetKey(KeyCode.RightArrow))
            freqChange += Time.deltaTime * 0.5f;
        if (Input.GetKey(KeyCode.UpArrow))
            ampChange += Time.deltaTime * 0.5f;
        if (Input.GetKey(KeyCode.DownArrow))
            ampChange -= Time.deltaTime * 0.5f;

        playerFrequency = Mathf.Clamp(playerFrequency + freqChange, 0.1f, 3f);
        playerAmplitude = Mathf.Clamp(playerAmplitude + ampChange, 0.1f, 2f);

        UpdateWaveGraphs();

        // 일치 판정
        float freqDiff = Mathf.Abs(playerFrequency - targetFrequency);
        float ampDiff = Mathf.Abs(playerAmplitude - targetAmplitude);

        if (freqDiff <= matchThreshold && ampDiff <= matchThreshold)
        {
            currentMatchTime += Time.deltaTime;
            if (currentMatchTime >= matchDuration)
            {
                OnSuccess();
                return;
            }
        }
        else
        {
            currentMatchTime = 0f;
        }

        UpdateStatusText();
    }

    private void UpdateStatusText()
    {
        if (statusText != null)
        {
            float freqDiff = Mathf.Abs(playerFrequency - targetFrequency);
            float ampDiff = Mathf.Abs(playerAmplitude - targetAmplitude);
            float matchPercent = (1f - Mathf.Max(freqDiff, ampDiff) / matchThreshold) * 100f;
            matchPercent = Mathf.Clamp(matchPercent, 0f, 100f);

            statusText.text = $"일치도: {(int)matchPercent}% - 좌우: 주파수, 상하: 진폭 ({(int)currentMatchTime}/{(int)matchDuration}초 유지)";
        }
    }

    public override void OnSuccess()
    {
        isActive = false;
        if (statusText != null)
        {
            statusText.text = "성공! Access Granted!";
        }
        
        // 성공 이펙트 (색상 변경)
        if (playerLineRenderer != null && playerLineRenderer.material != null)
        {
            playerLineRenderer.material.color = Color.green;
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

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 시냅스 동기화 미니게임
/// 안으로 좁혀져 들어오는 원이 중앙의 고정 원과 겹치는 찰나의 순간에 버튼을 누릅니다.
/// </summary>
public class SynapseSyncMinigame : HackingMinigameBase
{
    [Header("UI 설정")]
    [SerializeField]
    private Transform gameParent; // 게임이 표시될 부모

    [SerializeField]
    private TextMeshProUGUI statusText; // 상태 텍스트

    [SerializeField]
    private GameObject centerCircle; // 중앙 고정 원
    [SerializeField]
    private GameObject outerCircle; // 안으로 좁혀지는 원

    private float circleSpeed = 2f; // 원이 좁혀지는 속도
    private float currentRadius = 200f; // 현재 외부 원의 반지름
    private float targetRadius = 50f; // 목표 반지름 (중앙 원 크기)
    private float perfectTimingRange = 10f; // 완벽한 타이밍 범위

    private int requiredHits = 5; // 필요한 성공 횟수
    private int currentHits = 0;
    private int currentAttempt = 0;

    private float timeLimit = 30f;
    private float remainingTime = 0f;
    private bool isActive = false;
    private bool waitingForInput = false;

    public override void Initialize(System.Action onSuccessCallback, System.Action onFailureCallback)
    {
        base.Initialize(onSuccessCallback, onFailureCallback);

        if (gameParent == null)
        {
            CreateUIParent();
        }

        CreateCircles();
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

        GameObject panel = new GameObject("SynapseSyncPanel");
        panel.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(500, 500);
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
        statusRect.sizeDelta = new Vector2(450, 50);
        statusRect.anchorMin = new Vector2(0.5f, 1f);
        statusRect.anchorMax = new Vector2(0.5f, 1f);
        statusRect.anchoredPosition = new Vector2(0, -20);
        statusText = statusObj.AddComponent<TextMeshProUGUI>();
        statusText.text = "시냅스 동기화";
        statusText.fontSize = 24;
        statusText.alignment = TextAlignmentOptions.Center;
        statusText.color = Color.white;
    }

    private void CreateCircles()
    {
        // 중앙 고정 원
        if (centerCircle == null)
        {
            centerCircle = new GameObject("CenterCircle");
            centerCircle.transform.SetParent(gameParent, false);

            RectTransform rect = centerCircle.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(targetRadius * 2, targetRadius * 2);
            rect.anchoredPosition = Vector2.zero;

            Image img = centerCircle.AddComponent<Image>();
            img.color = new Color(0f, 1f, 1f, 0.8f); // 시안색

            // 원형으로 만들기 위해 마스크 사용 (간단하게 Image로 대체)
        }

        // 외부 원
        if (outerCircle == null)
        {
            outerCircle = new GameObject("OuterCircle");
            outerCircle.transform.SetParent(gameParent, false);

            RectTransform rect = outerCircle.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(currentRadius * 2, currentRadius * 2);
            rect.anchoredPosition = Vector2.zero;

            Image img = outerCircle.AddComponent<Image>();
            img.color = new Color(1f, 0.5f, 0f, 0.6f); // 주황색
        }
    }

    private void ResetMinigame()
    {
        currentHits = 0;
        currentAttempt = 0;
        remainingTime = timeLimit;
        isActive = true;
        waitingForInput = false;

        ResetCircle();
        UpdateStatusText();
    }

    private void ResetCircle()
    {
        currentRadius = 200f;
        waitingForInput = true;
        UpdateCircleSize();
    }

    private void UpdateCircleSize()
    {
        if (outerCircle != null)
        {
            RectTransform rect = outerCircle.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(currentRadius * 2, currentRadius * 2);
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

        if (waitingForInput)
        {
            // 원이 좁혀짐
            currentRadius -= circleSpeed * Time.deltaTime * 50f;

            if (currentRadius <= 0f)
            {
                // 타이밍 놓침
                OnFailure();
                return;
            }

            UpdateCircleSize();

            // 입력 체크
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            {
                CheckTiming();
            }
        }
    }

    private void CheckTiming()
    {
        float distance = Mathf.Abs(currentRadius - targetRadius);

        if (distance <= perfectTimingRange)
        {
            // 성공!
            currentHits++;
            currentAttempt++;
            UpdateStatusText();

            if (currentHits >= requiredHits)
            {
                OnSuccess();
                return;
            }

            // 다음 시도
            StartCoroutine(SuccessRoutine());
        }
        else
        {
            // 실패
            OnFailure();
        }
    }

    private IEnumerator SuccessRoutine()
    {
        waitingForInput = false;
        
        // 성공 이펙트 (원 색상 변경)
        if (outerCircle != null)
        {
            Image img = outerCircle.GetComponent<Image>();
            img.color = Color.green;
        }

        yield return new WaitForSeconds(0.5f);

        // 원 리셋
        ResetCircle();
    }

    private void UpdateStatusText()
    {
        if (statusText != null)
        {
            statusText.text = $"동기화: {currentHits}/{requiredHits} - 스페이스바 또는 클릭으로 타이밍 맞추기";
        }
    }

    public override void OnSuccess()
    {
        isActive = false;
        waitingForInput = false;
        if (statusText != null)
        {
            statusText.text = "성공!";
        }
        base.OnSuccess();
    }

    public override void OnFailure()
    {
        isActive = false;
        waitingForInput = false;
        if (statusText != null)
        {
            statusText.text = "실패!";
        }
        base.OnFailure();
    }
}

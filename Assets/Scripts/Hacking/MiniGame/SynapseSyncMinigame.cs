using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 시냅스 동기화 미니게임
/// 안으로 좁혀져 들어오는 사각형이 중앙의 고정 사각형과 겹치는 찰나의 순간에 버튼을 누릅니다.
/// </summary>
public class SynapseSyncMinigame : HackingMinigameBase
{
    [Header("UI 설정")]
    [SerializeField]
    private Transform gameParent; // 게임이 표시될 부모

    [SerializeField]
    private GameObject centerCircle; // 중앙 고정 사각형

    [SerializeField]
    private GameObject outerCircle; // 안으로 좁혀지는 사각형

    private Slider timeGauge;       // 시간 게이지 (빨강, 상단)
    private Slider successGauge;    // 성공 횟수 게이지 (녹색, 하단)

    private float circleSpeed = 2f;
    private float currentRadius = 200f;     // OuterCircle 현재 반크기
    private float initialOuterRadius = 200f; // OuterCircle 초기 반크기 (리셋용)
    private float targetRadius = 50f;        // CenterCircle 반크기
    private float perfectTimingRange = 15f;  // 허용 범위

    private int requiredHits = 5;
    private int currentHits = 0;

    private float timeLimit = 15f;
    private float remainingTime = 0f;
    private bool isActive = false;
    private bool waitingForInput = false;

    /// <summary>
    /// 손으로 만든 UI 전달 (HackingSystem에서 호출)
    /// </summary>
    public void SetPreMadeUI(GameObject center, GameObject outer, Slider timeSlider, Slider successSlider)
    {
        centerCircle = center;
        outerCircle = outer;
        timeGauge = timeSlider;
        successGauge = successSlider;

        // targetRadius는 CenterCircle 실제 크기의 절반
        if (centerCircle != null)
        {
            var rect = centerCircle.GetComponent<RectTransform>();
            if (rect != null) targetRadius = rect.sizeDelta.x / 2f;
        }

        // currentRadius는 OuterCircle 실제 크기의 절반으로 시작 (초기값도 함께 저장)
        if (outerCircle != null)
        {
            var rect = outerCircle.GetComponent<RectTransform>();
            if (rect != null)
            {
                currentRadius = rect.sizeDelta.x / 2f;
                initialOuterRadius = currentRadius;
            }
        }

        if (gameParent == null && outerCircle != null)
            gameParent = outerCircle.transform.parent;
    }

    public override void Initialize(System.Action onSuccessCallback, System.Action onFailureCallback)
    {
        base.Initialize(onSuccessCallback, onFailureCallback);

        if (gameParent == null)
            CreateUIParent();

        if (centerCircle == null || outerCircle == null)
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
    }

    private void CreateCircles()
    {
        if (centerCircle == null)
        {
            centerCircle = new GameObject("CenterCircle");
            centerCircle.transform.SetParent(gameParent, false);
            RectTransform rect = centerCircle.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(targetRadius * 2, targetRadius * 2);
            rect.anchoredPosition = Vector2.zero;
            Image img = centerCircle.AddComponent<Image>();
            img.color = new Color(0f, 1f, 1f, 0.8f);
        }

        if (outerCircle == null)
        {
            outerCircle = new GameObject("OuterCircle");
            outerCircle.transform.SetParent(gameParent, false);
            RectTransform rect = outerCircle.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(currentRadius * 2, currentRadius * 2);
            rect.anchoredPosition = Vector2.zero;
            Image img = outerCircle.AddComponent<Image>();
            img.color = new Color(1f, 0.5f, 0f, 0.6f);
        }
    }

    private void ResetMinigame()
    {
        currentHits = 0;
        remainingTime = timeLimit;
        isActive = true;
        waitingForInput = false;

        ResetCircle();
        UpdateTimeGauge();
        UpdateSuccessGauge();
    }

    private void ResetCircle()
    {
        // 저장해둔 초기 반크기로 복원 (현재 크기 읽으면 이미 줄어든 값이라 버그 발생)
        currentRadius = initialOuterRadius > 0f ? initialOuterRadius : 200f;

        waitingForInput = true;
        UpdateCircleSize();

        // OuterCircle 색상 복원 (주황색)
        if (outerCircle != null)
        {
            Image img = outerCircle.GetComponent<Image>();
            if (img != null) img.color = new Color(1f, 0.5f, 0f, 0.6f);
        }
    }

    private void UpdateCircleSize()
    {
        if (outerCircle != null)
        {
            RectTransform rect = outerCircle.GetComponent<RectTransform>();
            if (rect != null)
                rect.sizeDelta = new Vector2(currentRadius * 2, currentRadius * 2);
        }
    }

    private void UpdateTimeGauge()
    {
        if (timeGauge != null)
            timeGauge.value = remainingTime / timeLimit; // 1 → 0
    }

    private void UpdateSuccessGauge()
    {
        if (successGauge != null)
            successGauge.value = (float)currentHits / requiredHits; // 0 → 1
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

        if (waitingForInput)
        {
            // OuterCircle이 CenterCircle 방향으로 좁혀짐
            currentRadius -= circleSpeed * Time.unscaledDeltaTime * 50f;

            if (currentRadius <= 0f)
            {
                OnFailure();
                return;
            }

            UpdateCircleSize();

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
            currentHits++;
            UpdateSuccessGauge();

            if (currentHits >= requiredHits)
            {
                OnSuccess();
                return;
            }

            StartCoroutine(SuccessRoutine());
        }
        else
        {
            OnFailure();
        }
    }

    private IEnumerator SuccessRoutine()
    {
        waitingForInput = false;

        // 성공 이펙트 (녹색으로 변경)
        if (outerCircle != null)
        {
            Image img = outerCircle.GetComponent<Image>();
            if (img != null) img.color = Color.green;
        }

        // 해킹 중 timeScale=0이어도 다음 라운드로 정상 진행되도록 실시간 대기 사용
        yield return new WaitForSecondsRealtime(0.5f);

        ResetCircle();
    }

    public override void OnSuccess()
    {
        isActive = false;
        waitingForInput = false;
        if (timeGauge != null) timeGauge.value = 1f;
        if (successGauge != null) successGauge.value = 1f;
        base.OnSuccess();
    }

    public override void OnFailure()
    {
        isActive = false;
        waitingForInput = false;
        if (timeGauge != null) timeGauge.value = 0f;
        base.OnFailure();
    }
}

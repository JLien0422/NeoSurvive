using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 휠로 아래 막대 줄을 16픽셀씩 옮겨 위 정답 구간과 맞춥니다.
/// </summary>
public class WheelBarMatchMinigame : HackingMinigameBase
{
    private const int SlotCount = 20;
    private const float SlotWidth = 64f;
    private const float Step = 16f;
    private const float StripWidth = 1280f;
    private const float MaxOffset = 320f;
    private const float TimeLimit = 5f;

    private RectTransform answerStrip;
    private RectTransform solutionStrip;
    private RectTransform[] barTemplates;
    private Slider timeGauge;
    private float pendingNotches;
    private float remainingTime;
    private bool running;

    public override void Initialize(System.Action onSuccessCallback, System.Action onFailureCallback)
    {
        base.Initialize(onSuccessCallback, onFailureCallback);
        running = false;

        if (!Bind())
        {
            OnFailure();
            return;
        }

        BuildMatchingStrips();
        remainingTime = TimeLimit;
        running = true;
        UpdateTimeGauge();
    }

    private void Update()
    {
        if (!running)
            return;

        remainingTime -= Time.unscaledDeltaTime;
        UpdateTimeGauge();
        if (remainingTime <= 0f)
        {
            running = false;
            OnFailure();
            return;
        }

        float notches = ReadWheelNotches();
        if (Mathf.Approximately(notches, 0f))
            return;

        pendingNotches += notches;
        while (running && pendingNotches >= 1f)
        {
            pendingNotches -= 1f;
            MoveSolution(Step);
        }

        while (running && pendingNotches <= -1f)
        {
            pendingNotches += 1f;
            MoveSolution(-Step);
        }
    }

    private bool Bind()
    {
        Transform answerBox = transform.Find("AnswerImgBox");
        Transform solutionBox = transform.Find("SolutionImgBox");
        Transform templateRoot = transform.Find("GreenBarTemplate");
        if (answerBox == null || solutionBox == null || templateRoot == null)
        {
            Debug.LogWarning("[WheelBarMatch] 휠 막대 UI를 찾지 못했습니다.");
            return false;
        }

        answerStrip = answerBox.Find("BackImg") as RectTransform;
        solutionStrip = solutionBox.Find("BackImg") as RectTransform;
        if (answerStrip == null || solutionStrip == null)
        {
            Debug.LogWarning("[WheelBarMatch] BackImg가 없습니다.");
            return false;
        }

        Transform gauge = transform.Find("TimeGauge");
        timeGauge = gauge != null ? gauge.GetComponent<Slider>() : null;

        int templateCount = templateRoot.childCount;
        if (templateCount == 0)
        {
            Debug.LogWarning("[WheelBarMatch] 초록 막대 원본이 없습니다.");
            return false;
        }

        barTemplates = new RectTransform[templateCount];
        for (int i = 0; i < templateCount; i++)
            barTemplates[i] = templateRoot.GetChild(i) as RectTransform;

        for (int i = 0; i < barTemplates.Length - 1; i++)
        {
            for (int j = i + 1; j < barTemplates.Length; j++)
            {
                if (barTemplates[j].sizeDelta.x < barTemplates[i].sizeDelta.x)
                {
                    RectTransform swap = barTemplates[i];
                    barTemplates[i] = barTemplates[j];
                    barTemplates[j] = swap;
                }
            }
        }

        templateRoot.gameObject.SetActive(false);
        return true;
    }

    private void BuildMatchingStrips()
    {
        int[] sizes = new int[SlotCount];
        int[] alignments = new int[SlotCount];
        for (int i = 0; i < SlotCount; i++)
        {
            sizes[i] = Random.Range(0, barTemplates.Length);
            alignments[i] = Random.Range(0, 3);
        }

        ClearGeneratedBars(answerStrip);
        ClearGeneratedBars(solutionStrip);
        PlaceBars(answerStrip, sizes, alignments);
        PlaceBars(solutionStrip, sizes, alignments);

        int stepCount = Mathf.RoundToInt(MaxOffset / Step);
        int answerStep = Random.Range(-stepCount, stepCount + 1);
        int solutionStep = answerStep;
        int guard = 0;
        while (solutionStep == answerStep && guard < 20)
        {
            solutionStep = Random.Range(-stepCount, stepCount + 1);
            guard++;
        }

        SetStripX(answerStrip, answerStep * Step);
        SetStripX(solutionStrip, solutionStep * Step);
        pendingNotches = 0f;
    }

    private void PlaceBars(RectTransform strip, int[] sizes, int[] alignments)
    {
        float left = -StripWidth * 0.5f;
        for (int i = 0; i < SlotCount; i++)
        {
            RectTransform source = barTemplates[sizes[i]];
            RectTransform bar = Instantiate(source, strip);
            bar.gameObject.name = "WheelBar";
            bar.gameObject.SetActive(true);
            bar.anchorMin = new Vector2(0.5f, 0.5f);
            bar.anchorMax = new Vector2(0.5f, 0.5f);
            bar.pivot = new Vector2(0.5f, 0.5f);

            float width = source.sizeDelta.x;
            float slotLeft = left + i * SlotWidth;
            float x = slotLeft + SlotWidth * 0.5f;
            if (alignments[i] == 0)
                x = slotLeft + width * 0.5f;
            else if (alignments[i] == 2)
                x = slotLeft + SlotWidth - width * 0.5f;

            bar.sizeDelta = new Vector2(width, source.sizeDelta.y);
            bar.anchoredPosition = new Vector2(x, 0f);

            Image image = bar.GetComponent<Image>();
            if (image != null)
                image.raycastTarget = false;
        }
    }

    private static void ClearGeneratedBars(RectTransform strip)
    {
        for (int i = strip.childCount - 1; i >= 0; i--)
        {
            Transform child = strip.GetChild(i);
            if (child.name == "WheelBar")
                Destroy(child.gameObject);
        }
    }

    private void MoveSolution(float deltaX)
    {
        float next = Mathf.Clamp(solutionStrip.anchoredPosition.x + deltaX, -MaxOffset, MaxOffset);
        SetStripX(solutionStrip, next);
        if (Mathf.Abs(solutionStrip.anchoredPosition.x - answerStrip.anchoredPosition.x) > 0.5f)
            return;

        running = false;
        OnSuccess();
    }

    private void UpdateTimeGauge()
    {
        if (timeGauge != null)
            timeGauge.value = Mathf.Clamp01(remainingTime / TimeLimit);
    }

    private static void SetStripX(RectTransform strip, float x)
    {
        Vector2 position = strip.anchoredPosition;
        position.x = x;
        strip.anchoredPosition = position;
    }

    private static float ReadWheelNotches()
    {
        float delta = Input.mouseScrollDelta.y;
        if (Mathf.Abs(delta) >= 10f)
            return delta / 120f;
        if (Mathf.Abs(delta) > 0f && Mathf.Abs(delta) < 0.5f)
            return delta / 0.1f;
        return delta;
    }
}

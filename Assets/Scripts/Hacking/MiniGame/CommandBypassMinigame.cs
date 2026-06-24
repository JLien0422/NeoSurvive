using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 커맨드 바이패스 미니게임
/// 화면에 나타나는 화살표 방향키 배열을 보고 빠르게 입력합니다.
/// 
/// 변경점:
/// - 기존 Text/TMP 문자(↑ ↓ ← →) 표시 방식 제거
/// - ArrowContainer 안의 Arrow_0~Arrow_4 Image에 화살표 Sprite를 적용
/// </summary>
public class CommandBypassMinigame : HackingMinigameBase
{
    [Header("UI 설정")]
    [SerializeField] private Transform commandParent; // ArrowContainer

    [Header("화살표 스프라이트")]
    [SerializeField] private Sprite upArrowSprite;
    [SerializeField] private Sprite downArrowSprite;
    [SerializeField] private Sprite leftArrowSprite;
    [SerializeField] private Sprite rightArrowSprite;

    [Header("화살표 색상")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color correctColor = Color.green;

    private Transform preMadeArrowContainer;
    private readonly List<Image> preMadeArrowImages = new List<Image>();

    private Slider progressSlider;
    private Image progressImage;
    private Slider timeGauge;

    private readonly List<KeyCode> currentSequence = new List<KeyCode>();
    private int currentInputIndex = 0;
    private int totalCorrectKeysPressed = 0;

    [Header("게임 설정")]
    [SerializeField] private int totalKeysNeeded = 20;
    [SerializeField] private float timeLimit = 15f;

    private const int arrowsPerSequence = 5;

    private float remainingTime = 0f;
    private bool isActive = false;

    private readonly KeyCode[] arrowKeys =
    {
        KeyCode.UpArrow,
        KeyCode.DownArrow,
        KeyCode.LeftArrow,
        KeyCode.RightArrow
    };

    /// <summary>
    /// 손으로 만든 UI 사용
    /// HackingSystem에서 호출하는 함수
    /// </summary>
    public void SetPreMadeUI(
        Transform arrowContainer,
        Slider progress = null,
        Image progressFill = null,
        Slider timeSlider = null)
    {
        preMadeArrowContainer = arrowContainer;
        commandParent = arrowContainer;

        progressSlider = progress;
        progressImage = progressFill;
        timeGauge = timeSlider;

        CachePreMadeArrowImages();
    }

    /// <summary>
    /// ArrowContainer 안의 Arrow_0~Arrow_4 Image 캐싱
    /// </summary>
    private void CachePreMadeArrowImages()
    {
        preMadeArrowImages.Clear();

        if (preMadeArrowContainer == null)
            return;

        for (int i = 0; i < preMadeArrowContainer.childCount; i++)
        {
            Transform child = preMadeArrowContainer.GetChild(i);
            Image image = child.GetComponent<Image>();

            if (image == null)
            {
                image = child.GetComponentInChildren<Image>(true);
            }

            preMadeArrowImages.Add(image);
        }
    }

    public override void Initialize(System.Action onSuccessCallback, System.Action onFailureCallback)
    {
        base.Initialize(onSuccessCallback, onFailureCallback);

        if (preMadeArrowContainer == null && commandParent != null)
        {
            preMadeArrowContainer = commandParent;
            CachePreMadeArrowImages();
        }

        ResetMinigame();
    }

    private void ResetMinigame()
    {
        totalCorrectKeysPressed = 0;
        currentInputIndex = 0;
        remainingTime = timeLimit;
        isActive = true;

        ClearArrows();
        GenerateNewSequence();
        UpdateProgressGauge();
        UpdateTimeGauge();
    }

    private void GenerateNewSequence()
    {
        currentSequence.Clear();
        currentInputIndex = 0;

        for (int i = 0; i < arrowsPerSequence; i++)
        {
            int randomIndex = Random.Range(0, arrowKeys.Length);
            currentSequence.Add(arrowKeys[randomIndex]);
        }

        DisplaySequence();
    }

    private void DisplaySequence()
    {
        if (preMadeArrowImages.Count == 0)
        {
            CachePreMadeArrowImages();
        }

        for (int i = 0; i < preMadeArrowImages.Count; i++)
        {
            Image image = preMadeArrowImages[i];

            if (image == null)
                continue;

            bool isUsedSlot = i < currentSequence.Count;
            image.gameObject.SetActive(isUsedSlot);

            if (!isUsedSlot)
            {
                image.sprite = null;
                continue;
            }

            image.sprite = GetArrowSprite(currentSequence[i]);
            image.color = i < currentInputIndex ? correctColor : normalColor;
            image.enabled = image.sprite != null;
            image.preserveAspect = true;
        }
    }

    private Sprite GetArrowSprite(KeyCode key)
    {
        switch (key)
        {
            case KeyCode.UpArrow:
                return upArrowSprite;

            case KeyCode.DownArrow:
                return downArrowSprite;

            case KeyCode.LeftArrow:
                return leftArrowSprite;

            case KeyCode.RightArrow:
                return rightArrowSprite;

            default:
                return null;
        }
    }

    private void ClearArrows()
    {
        for (int i = 0; i < preMadeArrowImages.Count; i++)
        {
            Image image = preMadeArrowImages[i];

            if (image == null)
                continue;

            image.sprite = null;
            image.color = normalColor;
            image.gameObject.SetActive(false);
        }
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

        if (currentInputIndex >= currentSequence.Count)
            return;

        KeyCode expectedKey = currentSequence[currentInputIndex];

        if (Input.GetKeyDown(expectedKey))
        {
            currentInputIndex++;
            totalCorrectKeysPressed++;

            DisplaySequence();
            UpdateProgressGauge();

            if (totalCorrectKeysPressed >= totalKeysNeeded)
            {
                OnSuccess();
                return;
            }

            if (currentInputIndex >= currentSequence.Count)
            {
                GenerateNewSequence();
            }
        }
        else if (Input.anyKeyDown && !Input.GetKeyDown(KeyCode.Escape))
        {
            bool wrongArrowKeyPressed = false;

            for (int i = 0; i < arrowKeys.Length; i++)
            {
                KeyCode key = arrowKeys[i];

                if (Input.GetKeyDown(key) && key != expectedKey)
                {
                    wrongArrowKeyPressed = true;
                    break;
                }
            }

            if (wrongArrowKeyPressed)
            {
                OnFailure();
            }
        }
    }

    private void UpdateTimeGauge()
    {
        if (timeGauge != null)
        {
            timeGauge.value = remainingTime / timeLimit;
        }
    }

    private void UpdateProgressGauge()
    {
        float progress = (float)totalCorrectKeysPressed / totalKeysNeeded;

        if (progressSlider != null)
        {
            progressSlider.value = progress;
        }

        if (progressImage != null)
        {
            progressImage.fillAmount = progress;
        }
    }

    public override void OnSuccess()
    {
        isActive = false;

        if (progressSlider != null)
        {
            progressSlider.value = 1f;
        }

        if (progressImage != null)
        {
            progressImage.fillAmount = 1f;
        }

        if (timeGauge != null)
        {
            timeGauge.value = 1f;
        }

        base.OnSuccess();
    }

    public override void OnFailure()
    {
        isActive = false;

        if (timeGauge != null)
        {
            timeGauge.value = 0f;
        }

        base.OnFailure();
    }
}
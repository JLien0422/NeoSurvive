using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 보스 HP바 — 빨강(1) + 주황(N) + 노랑(1) 세그먼트 조립형.
/// HP가 줄면 오른쪽(노랑 → 주황) 칸부터 사라집니다.
/// 세그먼트 UI는 Hierarchy에 직접 배치하고 Inspector 또는 X좌표 순서로 연결합니다.
/// </summary>
public class BossHealthBar : MonoBehaviour
{
    private const int OrangeSegmentCount = 8;
    private const int TotalSegmentCount = OrangeSegmentCount + 2;

    [Header("UI 참조")]
    [SerializeField] private TextMeshProUGUI bossNameText;
    [SerializeField] private TextMeshProUGUI hpValueText;

    [Header("세그먼트 HP바 (Hierarchy 직접 배치)")]
    [SerializeField] private RectTransform segmentBarRoot;
    [SerializeField] private Image capLeftImage;
    [SerializeField] private Image capRightImage;
    [SerializeField] private Image[] orangeSegmentImages;

    [Header("레거시 Slider (자동 비활성)")]
    [SerializeField] private Slider hpSlider;

    private readonly List<Image> orderedSegments = new List<Image>();

    private Boss currentBoss;
    private bool segmentReferencesResolved;

    private void Awake()
    {
        ResolveSegmentReferences();
        SetLegacySliderVisible(false);
    }

    private void OnEnable()
    {
        Boss.OnBossDefeated += HideBossHealthBar;
    }

    private void OnDisable()
    {
        Boss.OnBossDefeated -= HideBossHealthBar;
    }

    private void OnDestroy()
    {
        Boss.OnBossDefeated -= HideBossHealthBar;
    }

    public void SetBoss(Boss boss)
    {
        if (boss == null) return;

        if (currentBoss != null)
            currentBoss.CurrentHP.onValueChanged -= UpdateHP;

        if (!segmentReferencesResolved)
            ResolveSegmentReferences();

        currentBoss = boss;
        currentBoss.CurrentHP.onValueChanged += UpdateHP;

        float maxHP = currentBoss.MaxHP.GetValue();
        float curHP = currentBoss.CurrentHP.CurrentValue;

        if (bossNameText != null)
            bossNameText.text = boss.GetBossDisplayName();

        UpdateSegmentDisplay(curHP, maxHP);
        UpdateHPValueText(curHP, maxHP);

        gameObject.SetActive(true);
    }

    private void UpdateHP(float current, float _)
    {
        float maxHp = currentBoss != null ? currentBoss.MaxHP.GetValue() : _;
        UpdateSegmentDisplay(current, maxHp);
        UpdateHPValueText(current, maxHp);
    }

    private void UpdateHPValueText(float current, float max)
    {
        if (hpValueText != null)
            hpValueText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
    }

    /// <summary>
    /// 왼쪽→오른쪽 순서 리스트 기준, activeCount만큼만 켭니다.
    /// 오른쪽(노랑)부터 꺼지려면 리스트 마지막이 노랑이어야 합니다.
    /// 예: max 1000 → 900=9칸(노랑OFF), 800=8칸(노랑+주1 OFF)
    /// </summary>
    private void UpdateSegmentDisplay(float currentHp, float maxHp)
    {
        if (!segmentReferencesResolved || maxHp <= 0f)
            return;

        float hpPerSegment = maxHp / TotalSegmentCount;
        int activeCount = hpPerSegment <= 0f
            ? 0
            : Mathf.Clamp(Mathf.CeilToInt(currentHp / hpPerSegment), 0, TotalSegmentCount);

        for (int i = 0; i < orderedSegments.Count; i++)
        {
            if (orderedSegments[i] != null)
                orderedSegments[i].gameObject.SetActive(i < activeCount);
        }
    }

    private void HideBossHealthBar()
    {
        if (currentBoss != null)
        {
            currentBoss.CurrentHP.onValueChanged -= UpdateHP;
            currentBoss = null;
        }

        gameObject.SetActive(false);
    }

    private void ResolveSegmentReferences()
    {
        segmentReferencesResolved = false;
        orderedSegments.Clear();

        if (segmentBarRoot == null)
        {
            Transform root = transform.Find("SegmentBarRoot") ?? transform.Find("BarRoot");
            if (root != null)
                segmentBarRoot = root.GetComponent<RectTransform>();
        }

        if (segmentBarRoot == null)
        {
            Debug.LogError("[BossHealthBar] segmentBarRoot가 없습니다. Hierarchy에 SegmentBarRoot(또는 BarRoot)를 배치하세요.");
            return;
        }

        if (TryResolveFromExplicitReferences())
        {
            segmentReferencesResolved = orderedSegments.Count > 0;
            WarnSegmentCountIfNeeded();
            return;
        }

        if (TryResolveFromNamedReferences())
        {
            segmentReferencesResolved = orderedSegments.Count > 0;
            WarnSegmentCountIfNeeded();
            return;
        }

        if (TryResolveFromVisualOrder())
        {
            segmentReferencesResolved = orderedSegments.Count > 0;
            WarnSegmentCountIfNeeded();
            return;
        }

        Debug.LogError("[BossHealthBar] 세그먼트 Image 참조 실패. SegmentBarRoot 아래 Image 10개를 배치하거나 Inspector에 연결하세요.");
    }

    private bool TryResolveFromExplicitReferences()
    {
        if (capLeftImage == null || capRightImage == null)
            return false;

        if (orangeSegmentImages == null || orangeSegmentImages.Length == 0)
            return false;

        var segments = new List<Image> { capLeftImage };
        for (int i = 0; i < orangeSegmentImages.Length; i++)
        {
            if (orangeSegmentImages[i] != null)
                segments.Add(orangeSegmentImages[i]);
        }

        segments.Add(capRightImage);
        AssignOrderedSegments(segments);
        return orderedSegments.Count > 0;
    }

    private bool TryResolveFromNamedReferences()
    {
        capLeftImage ??= FindSegmentImage(segmentBarRoot, "CapLeft")
            ?? FindSegmentImage(segmentBarRoot, "Red");

        capRightImage ??= FindSegmentImage(segmentBarRoot, "CapRight")
            ?? FindSegmentImage(segmentBarRoot, "Yellow");

        if (capLeftImage == null || capRightImage == null)
            return false;

        var oranges = new List<Image>();
        if (orangeSegmentImages != null && orangeSegmentImages.Length > 0)
        {
            for (int i = 0; i < orangeSegmentImages.Length; i++)
            {
                if (orangeSegmentImages[i] != null)
                    oranges.Add(orangeSegmentImages[i]);
            }
        }

        if (oranges.Count == 0)
            CollectOrangeSegmentsFromHierarchy(oranges);

        if (oranges.Count == 0)
            return false;

        var segments = new List<Image> { capLeftImage };
        segments.AddRange(oranges);
        segments.Add(capRightImage);
        AssignOrderedSegments(segments);
        return orderedSegments.Count > 0;
    }

    private bool TryResolveFromVisualOrder()
    {
        var images = new List<Image>();
        CollectSegmentImages(segmentBarRoot, images);

        if (images.Count < TotalSegmentCount)
        {
            Transform altRoot = transform.Find("BarRoot");
            if (altRoot != null && altRoot != segmentBarRoot)
            {
                images.Clear();
                CollectSegmentImages(altRoot, images);
                if (images.Count >= TotalSegmentCount)
                    segmentBarRoot = altRoot.GetComponent<RectTransform>();
            }
        }

        if (images.Count < TotalSegmentCount)
            return false;

        AssignOrderedSegments(images);
        capLeftImage = orderedSegments[0];
        capRightImage = orderedSegments[orderedSegments.Count - 1];
        return orderedSegments.Count > 0;
    }

    private void AssignOrderedSegments(List<Image> segments)
    {
        orderedSegments.Clear();

        var unique = new List<Image>();
        for (int i = 0; i < segments.Count; i++)
        {
            Image image = segments[i];
            if (image != null && !unique.Contains(image))
                unique.Add(image);
        }

        unique.Sort(CompareSegmentVisualOrder);
        orderedSegments.AddRange(unique);
    }

    private static int CompareSegmentVisualOrder(Image a, Image b)
    {
        float xCompare = GetSegmentVisualX(a).CompareTo(GetSegmentVisualX(b));
        if (!Mathf.Approximately(xCompare, 0f))
            return xCompare > 0f ? 1 : -1;

        return a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex());
    }

    private static float GetSegmentVisualX(Image image)
    {
        RectTransform rect = image.rectTransform;
        return rect.TransformPoint(rect.rect.center).x;
    }

    private static void CollectSegmentImages(Transform root, List<Image> images)
    {
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            Image image = child.GetComponent<Image>();
            if (image != null)
            {
                images.Add(image);
                continue;
            }

            for (int j = 0; j < child.childCount; j++)
            {
                Image nested = child.GetChild(j).GetComponent<Image>();
                if (nested != null)
                    images.Add(nested);
            }
        }
    }

    private void CollectOrangeSegmentsFromHierarchy(List<Image> oranges)
    {
        Transform orangeContainer = segmentBarRoot.Find("OrangeContainer");
        if (orangeContainer != null)
        {
            for (int i = 0; i < orangeContainer.childCount; i++)
            {
                Image image = orangeContainer.GetChild(i).GetComponent<Image>();
                if (image != null)
                    oranges.Add(image);
            }

            oranges.Sort(CompareSegmentVisualOrder);
            return;
        }

        for (int i = 0; i < segmentBarRoot.childCount; i++)
        {
            Transform child = segmentBarRoot.GetChild(i);
            if (!child.name.StartsWith("Orange", System.StringComparison.OrdinalIgnoreCase))
                continue;

            Image image = child.GetComponent<Image>();
            if (image != null)
                oranges.Add(image);
        }

        oranges.Sort(CompareSegmentVisualOrder);
    }

    private void WarnSegmentCountIfNeeded()
    {
        if (orderedSegments.Count != TotalSegmentCount)
        {
            Debug.LogWarning($"[BossHealthBar] 세그먼트 {orderedSegments.Count}개 연결됨 (권장 {TotalSegmentCount}개).");
        }
    }

    private static Image FindSegmentImage(Transform root, string objectName)
    {
        Transform found = root.Find(objectName);
        return found != null ? found.GetComponent<Image>() : null;
    }

    private void SetLegacySliderVisible(bool visible)
    {
        if (hpSlider != null)
            hpSlider.enabled = visible;

        Transform background = transform.Find("Background");
        if (background != null)
            background.gameObject.SetActive(visible);

        Transform fillArea = transform.Find("Fill Area");
        if (fillArea != null)
            fillArea.gameObject.SetActive(visible);
    }
}

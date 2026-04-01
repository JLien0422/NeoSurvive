using System.Collections;
using DG.Tweening;
using UnityEngine;

public class UICharacterSelection : MonoBehaviour
{
    private enum CharacterSlot
    {
        None,
        Hacker,
        Cyborg
    }

    [Header("Buttons")]
    [SerializeField] private RectTransform hackerButtonRoot;
    [SerializeField] private RectTransform cyborgButtonRoot;

    [Header("Info Panels")]
    [SerializeField] private RectTransform hackerInfoRoot;
    [SerializeField] private RectTransform cyborgInfoRoot;

    [Header("Animation")]
    [SerializeField] private float fadeDuration = 0.22f;
    [SerializeField] private Ease showEase = Ease.OutCubic;
    [SerializeField] private Ease hideEase = Ease.OutQuad;

    private CharacterSlot hoveredSlot = CharacterSlot.None;
    private CharacterSlot selectedSlot = CharacterSlot.None;
    private Coroutine exitCheckCoroutine;

    private void Awake()
    {
        EnsureCanvasGroup(hackerButtonRoot);
        EnsureCanvasGroup(cyborgButtonRoot);
        EnsureCanvasGroup(hackerInfoRoot);
        EnsureCanvasGroup(cyborgInfoRoot);

        // 초기 상태: 아무것도 선택하지 않았으므로 버튼 2개 표시, 정보 패널은 숨김
        ApplyStateInstant();
    }

    public void OnHackerMouseEnter()
    {
        CancelExitCheck();
        LobbySoundManager.Instance?.PlayCharacterHover();
        hoveredSlot = CharacterSlot.Hacker;
        UpdateVisualState();
    }

    public void OnCyborgMouseEnter()
    {
        CancelExitCheck();
        LobbySoundManager.Instance?.PlayCharacterHover();
        hoveredSlot = CharacterSlot.Cyborg;
        UpdateVisualState();
    }

    public void OnHackerMouseExit()
    {
        BeginExitCheck(CharacterSlot.Hacker);
    }

    public void OnCyborgMouseExit()
    {
        BeginExitCheck(CharacterSlot.Cyborg);
    }

    public void OnHackerSelected()
    {
        if (selectedSlot == CharacterSlot.Hacker)
        {
            LobbySoundManager.Instance?.PlayCharacterDeselect();
            ClearSelection();
            return;
        }

        LobbySoundManager.Instance?.PlayCharacterSelect();
        selectedSlot = CharacterSlot.Hacker;
        hoveredSlot = CharacterSlot.Hacker;
        UpdateVisualState();
    }

    public void OnCyborgSelected()
    {
        if (selectedSlot == CharacterSlot.Cyborg)
        {
            LobbySoundManager.Instance?.PlayCharacterDeselect();
            ClearSelection();
            return;
        }

        LobbySoundManager.Instance?.PlayCharacterSelect();
        selectedSlot = CharacterSlot.Cyborg;
        hoveredSlot = CharacterSlot.Cyborg;
        UpdateVisualState();
    }

    public void ClearSelection()
    {
        CancelExitCheck();
        LobbySoundManager.Instance?.PlayCharacterDeselect();
        selectedSlot = CharacterSlot.None;
        hoveredSlot = CharacterSlot.None;
        UpdateVisualState();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ClearSelection();
        }
    }

    private void BeginExitCheck(CharacterSlot exitedSlot)
    {
        CancelExitCheck();
        exitCheckCoroutine = StartCoroutine(ExitCheckNextFrame(exitedSlot));
    }

    private IEnumerator ExitCheckNextFrame(CharacterSlot exitedSlot)
    {
        yield return null;

        // 빠르게 다른 버튼으로 이동한 경우, 늦게 온 Exit가 최신 Enter 상태를 덮지 않도록 방지
        if (hoveredSlot == exitedSlot)
        {
            hoveredSlot = CharacterSlot.None;
            UpdateVisualState();
        }

        exitCheckCoroutine = null;
    }

    private void CancelExitCheck()
    {
        if (exitCheckCoroutine == null) return;
        StopCoroutine(exitCheckCoroutine);
        exitCheckCoroutine = null;
    }

    private void UpdateVisualState()
    {
        CharacterSlot displaySlot = hoveredSlot != CharacterSlot.None ? hoveredSlot : selectedSlot;

        if (displaySlot == CharacterSlot.None)
        {
            AnimateVisible(hackerButtonRoot, true);
            AnimateVisible(cyborgButtonRoot, true);
            AnimateVisible(hackerInfoRoot, false);
            AnimateVisible(cyborgInfoRoot, false);
            return;
        }

        if (displaySlot == CharacterSlot.Hacker)
        {
            AnimateVisible(hackerButtonRoot, true);
            AnimateVisible(cyborgButtonRoot, false);
            AnimateVisible(hackerInfoRoot, true);
            AnimateVisible(cyborgInfoRoot, false);
            return;
        }

        AnimateVisible(hackerButtonRoot, false);
        AnimateVisible(cyborgButtonRoot, true);
        AnimateVisible(hackerInfoRoot, false);
        AnimateVisible(cyborgInfoRoot, true);
    }

    private void ApplyStateInstant()
    {
        SetVisibleInstant(hackerButtonRoot, true);
        SetVisibleInstant(cyborgButtonRoot, true);
        SetVisibleInstant(hackerInfoRoot, false);
        SetVisibleInstant(cyborgInfoRoot, false);
    }

    private void SetVisibleInstant(RectTransform target, bool visible)
    {
        if (target == null) return;

        var group = EnsureCanvasGroup(target);
        target.DOKill();
        group.DOKill();

        target.gameObject.SetActive(visible);
        group.alpha = visible ? 1f : 0f;
    }

    private void AnimateVisible(RectTransform target, bool visible)
    {
        if (target == null) return;

        CanvasGroup group = EnsureCanvasGroup(target);
        target.DOKill();
        group.DOKill();

        if (visible)
        {
            target.gameObject.SetActive(true);
            target.DOScale(1f, fadeDuration).SetEase(showEase).SetUpdate(true);
            group.DOFade(1f, fadeDuration).SetEase(showEase).SetUpdate(true);
        }
        else
        {
            group.DOFade(0f, fadeDuration)
                .SetEase(hideEase)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    // 중간에 다시 Show가 들어온 경우(알파가 다시 올라간 경우) 비활성화를 막는다.
                    if (target != null && group != null && group.alpha <= 0.001f)
                        target.gameObject.SetActive(false);
                });
        }
    }

    private CanvasGroup EnsureCanvasGroup(RectTransform target)
    {
        if (target == null) return null;

        if (!target.TryGetComponent<CanvasGroup>(out var group))
            group = target.gameObject.AddComponent<CanvasGroup>();

        return group;
    }
}

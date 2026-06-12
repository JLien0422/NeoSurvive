using System;
using NeoSurvive.Weapon;
using UnityEngine;

public class WeaponChoiceUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject rootPanel; // 패널 표시/숨김만 담당

    [Header("Cards")]
    [SerializeField] private WeaponChoiceCard[] cards = new WeaponChoiceCard[3]; // 카드 3장 관리만 담당

    private Action<WeaponBase> onPicked;
    private Action onPickAllCompleted;
    private int remainingPickAllCount;

    public bool IsOpen => rootPanel != null && rootPanel.activeSelf;

    private void Awake()
    {
        ValidateReferences();
    }

    /// <summary>
    /// 무기 선택 패널을 열고 카드 3장을 채우는 기능만 담당
    /// </summary>
    public void Show(
        WeaponBase[] choices,
        Func<WeaponBase, string> nameProvider,
        Func<WeaponBase, string> descProvider,
        Func<WeaponBase, string> levelProvider,
        Action<WeaponBase> pickedCallback)
    {
        ValidateReferences();

        if (choices == null)
        {
            Debug.LogError("[WeaponChoiceUI] choices is null.");
            return;
        }

        if (choices.Length == 0)
        {
            Debug.LogError("[WeaponChoiceUI] choices is empty.");
            return;
        }

        if (nameProvider == null)
        {
            Debug.LogError("[WeaponChoiceUI] nameProvider is null.");
            return;
        }

        if (descProvider == null)
        {
            Debug.LogError("[WeaponChoiceUI] descProvider is null.");
            return;
        }

        if (levelProvider == null)
        {
            Debug.LogError("[WeaponChoiceUI] levelProvider is null.");
            return;
        }

        if (pickedCallback == null)
        {
            Debug.LogError("[WeaponChoiceUI] pickedCallback is null.");
            return;
        }

        onPicked = pickedCallback;
        onPickAllCompleted = null;
        remainingPickAllCount = 0;

        for (int i = 0; i < cards.Length; i++)
        {
            if (cards[i] == null)
            {
                Debug.LogError($"[WeaponChoiceUI] cards[{i}] is null.");
                return;
            }

            // ✅ [수정]
            // choices가 1개 또는 2개만 들어온 경우,
            // 남는 카드는 숨긴다.
            if (i >= choices.Length)
            {
                cards[i].gameObject.SetActive(false);
                continue;
            }

            // ✅ [수정]
            // 실제 선택 가능한 무기가 있는 카드만 켠다.
            cards[i].gameObject.SetActive(true);

            WeaponBase weapon = choices[i];

            cards[i].Setup(
                weapon,
                nameProvider(weapon),
                descProvider(weapon),
                levelProvider(weapon),
                HandlePicked
            );
        }

        rootPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    /// <summary>
    /// 상자 보상처럼 표시된 무기를 전부 획득해야 하는 패널을 엽니다.
    /// </summary>
    public void ShowPickAll(
        WeaponBase[] choices,
        Func<WeaponBase, string> nameProvider,
        Func<WeaponBase, string> descProvider,
        Func<WeaponBase, string> levelProvider,
        Action<WeaponBase> pickedCallback,
        Action completedCallback)
    {
        ValidateReferences();

        if (choices == null)
        {
            Debug.LogError("[WeaponChoiceUI] choices is null.");
            return;
        }

        if (choices.Length == 0)
        {
            Debug.LogError("[WeaponChoiceUI] choices is empty.");
            return;
        }

        if (nameProvider == null)
        {
            Debug.LogError("[WeaponChoiceUI] nameProvider is null.");
            return;
        }

        if (descProvider == null)
        {
            Debug.LogError("[WeaponChoiceUI] descProvider is null.");
            return;
        }

        if (levelProvider == null)
        {
            Debug.LogError("[WeaponChoiceUI] levelProvider is null.");
            return;
        }

        if (pickedCallback == null)
        {
            Debug.LogError("[WeaponChoiceUI] pickedCallback is null.");
            return;
        }

        if (completedCallback == null)
        {
            Debug.LogError("[WeaponChoiceUI] completedCallback is null.");
            return;
        }

        onPicked = pickedCallback;
        onPickAllCompleted = completedCallback;
        remainingPickAllCount = choices.Length;

        for (int i = 0; i < cards.Length; i++)
        {
            if (cards[i] == null)
            {
                Debug.LogError($"[WeaponChoiceUI] cards[{i}] is null.");
                return;
            }

            if (i >= choices.Length)
            {
                cards[i].gameObject.SetActive(false);
                continue;
            }

            cards[i].gameObject.SetActive(true);

            int cardIndex = i;
            WeaponBase weapon = choices[i];

            cards[i].Setup(
                weapon,
                nameProvider(weapon),
                descProvider(weapon),
                levelProvider(weapon),
                selected => HandlePickAllPicked(cardIndex, selected)
            );
        }

        rootPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    /// <summary>
    /// 무기 선택 패널을 닫는 기능만 담당
    /// </summary>
    public void Hide()
    {
        ValidateReferences();

        rootPanel.SetActive(false);
        Time.timeScale = 1f;
        onPicked = null;
        onPickAllCompleted = null;
        remainingPickAllCount = 0;
    }

    /// <summary>
    /// 카드가 선택한 무기를 바깥으로 전달하는 기능만 담당
    /// </summary>
    private void HandlePicked(WeaponBase weapon)
    {
        if (weapon == null)
        {
            Debug.LogError("[WeaponChoiceUI] picked weapon is null.");
            return;
        }

        // ✅ [수정]
        // Hide()를 먼저 호출하면 Hide() 안에서 onPicked가 null이 되어
        // WeaponManager.AddWeapon(picked)가 실행되지 않는다.
        // 그래서 콜백을 먼저 임시 변수에 저장한다.
        Action<WeaponBase> pickedCallback = onPicked;

        Hide();

        // ✅ [수정]
        // Hide() 이후에도 저장해둔 콜백으로 무기 추가를 실행한다.
        pickedCallback?.Invoke(weapon);
    }

    private void HandlePickAllPicked(int cardIndex, WeaponBase weapon)
    {
        if (weapon == null)
        {
            Debug.LogError("[WeaponChoiceUI] picked weapon is null.");
            return;
        }

        if (cardIndex < 0 || cardIndex >= cards.Length || cards[cardIndex] == null)
        {
            Debug.LogError($"[WeaponChoiceUI] invalid cardIndex={cardIndex}");
            return;
        }

        cards[cardIndex].gameObject.SetActive(false);
        onPicked?.Invoke(weapon);

        remainingPickAllCount = Mathf.Max(0, remainingPickAllCount - 1);
        if (remainingPickAllCount > 0)
            return;

        Action completedCallback = onPickAllCompleted;
        Hide();
        completedCallback?.Invoke();
    }

    private void ValidateReferences()
    {
        if (rootPanel == null)
            Debug.LogError($"[WeaponChoiceUI] rootPanel is not assigned. object={name}");

        if (cards == null)
        {
            Debug.LogError($"[WeaponChoiceUI] cards array is null. object={name}");
            return;
        }

        if (cards.Length != 3)
            Debug.LogError($"[WeaponChoiceUI] cards length must be 3. object={name}, current={cards.Length}");
    }
}
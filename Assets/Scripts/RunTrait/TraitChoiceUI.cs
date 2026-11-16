using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TraitChoiceUI : MonoBehaviour
{
    [SerializeField] private GameObject rootPanel;
    [SerializeField] private TraitChoiceCard[] cards = new TraitChoiceCard[3];
    [SerializeField] private TMP_Text titleText;

    private bool isOpen;
    private float previousTimeScale;
    private Action onClosed;
    public bool IsOpen => isOpen;

    private void Start()
    {
        // 하이어라키에서 활성 상태로 배치해도 게임 시작 시에는 닫습니다.
        if (!isOpen && rootPanel != null)
            rootPanel.SetActive(false);
    }

    public bool Show(IReadOnlyList<RunTraitDefinition> choices,
        Func<RunTraitDefinition, string> levelProvider, Func<int, bool> trySelect,
        Action closedCallback)
    {
        if (isOpen || rootPanel == null || cards == null || cards.Length != 3
            || choices == null || choices.Count == 0 || choices.Count > cards.Length
            || levelProvider == null || trySelect == null)
            return false;
        foreach (TraitChoiceCard card in cards)
            if (card == null || !card.IsConfigured)
                return false;
        foreach (RunTraitDefinition trait in choices)
            if (trait == null)
                return false;

        previousTimeScale = Time.timeScale;
        onClosed = closedCallback;
        isOpen = true;
        if (titleText != null)
            titleText.text = "런 특성 선택";
        for (int i = 0; i < cards.Length; i++)
        {
            cards[i].gameObject.SetActive(i < choices.Count);
            if (i >= choices.Count)
                continue;
            int index = i;
            cards[i].Setup(choices[i], levelProvider(choices[i]), () =>
            {
                if (isOpen && trySelect(index))
                    Hide();
            });
        }
        rootPanel.SetActive(true);
        Time.timeScale = 0f;
        return true;
    }

    public void Hide()
    {
        if (!isOpen)
            return;
        isOpen = false;
        Action callback = onClosed;
        onClosed = null;
        Time.timeScale = previousTimeScale;
        if (rootPanel != null)
            rootPanel.SetActive(false);
        callback?.Invoke();
    }

    private void OnDisable()
    {
        Hide();
    }
}

using System.Collections.Generic;
using System;
using DG.Tweening;
using UnityEngine;

namespace UIEffectDemo
{
    [DisallowMultipleComponent]
    public sealed class CardSelectionController : MonoBehaviour
    {
        [Header("Cards")]
        [SerializeField] private List<CardView> cards = new List<CardView>();
        [SerializeField] private int selectedIndex;

        [Header("DOTween Motion")]
        [SerializeField, Min(0.05f)] private float moveDuration = 0.35f;
        [SerializeField] private Ease moveEase = Ease.OutCubic;
        [SerializeField, Range(0.5f, 1f)] private float sideScale = 0.96f;
        [SerializeField, Range(0f, 1f)] private float sideAlpha = 0.9f;
        [SerializeField] private float selectedLift = 8f;

        private bool selectionInitialized;

        public int SelectedIndex => selectedIndex;
        public event Action<int> SelectionChanged;
        public event Action<int> SelectionConfirmed;

        private void Start()
        {
            Refresh(true);
            selectionInitialized = true;
        }

        public void SelectCard(int index)
        {
            if (index < 0 || index >= cards.Count) return;
            if (selectionInitialized && index == selectedIndex) return;
            selectedIndex = index;
            Refresh(false);
            selectionInitialized = true;
            SelectionChanged?.Invoke(selectedIndex);
        }

        public void ConfirmSelection()
        {
            if (selectedIndex < 0 || selectedIndex >= cards.Count || !cards[selectedIndex]) return;
            cards[selectedIndex].PlayConfirmFeedback();
            SelectionConfirmed?.Invoke(selectedIndex);
        }

        [ContextMenu("Refresh Card Layout")]
        private void RefreshFromInspector() => Refresh(true);

        private void Refresh(bool immediate)
        {
            if (cards.Count == 0) return;
            selectedIndex = Mathf.Clamp(selectedIndex, 0, cards.Count - 1);

            for (int index = 0; index < cards.Count; index++)
            {
                CardView card = cards[index];
                if (!card) continue;
                bool selected = index == selectedIndex;
                card.ApplyState(selectedLift,
                    selected ? 1f : sideScale, selected ? 1f : sideAlpha,
                    selected, moveDuration, moveEase, immediate);
            }
        }
    }
}

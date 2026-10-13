using UnityEngine;
using UnityEngine.EventSystems;

namespace UIEffectDemo
{
    [DisallowMultipleComponent]
    public sealed class CardConfirmButton : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private CardSelectionController selectionController;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
                selectionController.ConfirmSelection();
        }
    }
}

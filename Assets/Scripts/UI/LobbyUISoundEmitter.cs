using UnityEngine;
using UnityEngine.EventSystems;

public class LobbyUISoundEmitter : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler, ISubmitHandler
{
    [Header("Events")]
    [SerializeField] private bool playHover = true;
    [SerializeField] private bool playClick = true;

    [Header("Custom Mapping")]
    [SerializeField] private bool useCustomHoverEvent = false;
    [SerializeField] private LobbySoundManager.LobbySfxEvent hoverEventType = LobbySoundManager.LobbySfxEvent.UIHover;
    [SerializeField] private bool useCustomClickEvent = false;
    [SerializeField] private LobbySoundManager.LobbySfxEvent clickEventType = LobbySoundManager.LobbySfxEvent.UIClick;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!playHover || LobbySoundManager.Instance == null) return;
        LobbySoundManager.Instance.Play(useCustomHoverEvent ? hoverEventType : LobbySoundManager.LobbySfxEvent.UIHover);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!playClick || LobbySoundManager.Instance == null) return;
        LobbySoundManager.Instance.Play(useCustomClickEvent ? clickEventType : LobbySoundManager.LobbySfxEvent.UIClick);
    }

    public void OnSubmit(BaseEventData eventData)
    {
        if (!playClick || LobbySoundManager.Instance == null) return;
        LobbySoundManager.Instance.Play(useCustomClickEvent ? clickEventType : LobbySoundManager.LobbySfxEvent.UIClick);
    }
}

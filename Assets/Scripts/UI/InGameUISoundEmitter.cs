using UnityEngine;
using UnityEngine.EventSystems;

public class InGameUISoundEmitter : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler, ISubmitHandler
{
    [Header("Events")]
    [SerializeField] private bool playHover = true;
    [SerializeField] private bool playClick = true;

    [Header("Custom Mapping")]
    [SerializeField] private bool useCustomHoverEvent = false;
    [SerializeField] private GameSoundEventDefinition hoverEvent;
    [SerializeField] private bool useCustomClickEvent = false;
    [SerializeField] private GameSoundEventDefinition clickEvent;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!playHover || SoundManager.Instance == null) return;

        if (useCustomHoverEvent && hoverEvent != null)
        {
            SoundManager.Instance.Play(hoverEvent);
            return;
        }

        SoundManager.Instance.Play(SoundManager.KeyUiHover);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!playClick || SoundManager.Instance == null) return;

        if (useCustomClickEvent && clickEvent != null)
        {
            SoundManager.Instance.Play(clickEvent);
            return;
        }

        SoundManager.Instance.Play(SoundManager.KeyUiClick);
    }

    public void OnSubmit(BaseEventData eventData)
    {
        if (!playClick || SoundManager.Instance == null) return;

        if (useCustomClickEvent && clickEvent != null)
        {
            SoundManager.Instance.Play(clickEvent);
            return;
        }

        SoundManager.Instance.Play(SoundManager.KeyUiClick);
    }
}

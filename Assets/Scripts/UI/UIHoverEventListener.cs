using UnityEngine;
using UnityEngine.Events; // 1. UnityEvent를 쓰기 위해 필수!
using UnityEngine.EventSystems;

public class UIHoverEventListener : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    // 2. 이 부분이 인스펙터에 'OnClick' 같은 칸을 만들어줍니다.
    [SerializeField]
    public UnityEvent onHoverEnter;

    [SerializeField]
    public UnityEvent onHoverExit;

    // 마우스를 올릴 때 호출
    public void OnPointerEnter(PointerEventData eventData)
    {
        // 3. 인스펙터에 등록된 함수들을 실행(Invoke)합니다.
        onHoverEnter?.Invoke();
    }

    // 마우스가 나갈 때 호출
    public void OnPointerExit(PointerEventData eventData)
    {
        onHoverExit?.Invoke();
    }
}
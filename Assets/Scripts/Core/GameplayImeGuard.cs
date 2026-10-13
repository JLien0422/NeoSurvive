using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 한글 입력기가 켜져 있으면 Input.GetKey가 문자 키(W, A, S, D, E, R 등)를 받지 못한다.
/// 텍스트 입력 중이 아니면 IME를 꺼서, 한/영과 무관하게 그 자리의 영문 KeyCode가 들어오게 한다.
/// </summary>
public sealed class GameplayImeGuard : MonoBehaviour
{
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
  private static void Install()
  {
    GameObject guardObject = new GameObject(nameof(GameplayImeGuard));
    DontDestroyOnLoad(guardObject);
    guardObject.hideFlags = HideFlags.HideAndDontSave;
    guardObject.AddComponent<GameplayImeGuard>();
  }

  private void OnEnable()
  {
    Apply();
  }

  private void LateUpdate()
  {
    Apply();
  }

  private static void Apply()
  {
    if (IsTextInputFocused())
      return;

    if (Input.imeCompositionMode != IMECompositionMode.Off)
      Input.imeCompositionMode = IMECompositionMode.Off;
  }

  private static bool IsTextInputFocused()
  {
    EventSystem eventSystem = EventSystem.current;
    if (eventSystem == null)
      return false;

    GameObject selected = eventSystem.currentSelectedGameObject;
    if (selected == null)
      return false;

    return selected.GetComponent<TMP_InputField>() != null
      || selected.GetComponent<InputField>() != null;
  }
}

using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace NeoSurvive.UI
{
  /// <summary>
  /// 데미지 숫자를 화면에 띄워주는 스크립트
  /// </summary>
  public class DamageText : MonoBehaviour
  {
    public float moveSpeed = 100f; // UI 공간이므로 속도를 높임
    public float alphaSpeed = 2f;
    public float destroyTime = 1.0f;
    [SerializeField] private Color outlineColor = Color.black;
    [SerializeField] private float outlineDistance = 2f;

    private TextMeshProUGUI textMesh;
    private Outline outline;
    private Color color;
    private RectTransform rectTransform;
    private Vector3 worldPosition;
    private float verticalOffset;

    void Awake()
    {
      textMesh = GetComponent<TextMeshProUGUI>();
      rectTransform = GetComponent<RectTransform>();

      if (textMesh == null)
      {
        textMesh = gameObject.AddComponent<TextMeshProUGUI>();
        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.color = Color.yellow;
        var font = Resources.Load<TMP_FontAsset>("Fonts/Maplestory Light SDF");
        if (font != null) textMesh.font = font;
      }

      textMesh.raycastTarget = false;

      outline = GetComponent<Outline>();
      if (outline == null)
        outline = gameObject.AddComponent<Outline>();

      outline.effectColor = outlineColor;
      outline.effectDistance = Vector2.one * outlineDistance;
      outline.useGraphicAlpha = true;

      color = textMesh.color;
    }

    public void Setup(float damage, Vector3 spawnWorldPos)
    {
      textMesh.text = Mathf.RoundToInt(damage).ToString();
      worldPosition = spawnWorldPos;
      verticalOffset = 0f;
      Destroy(gameObject, destroyTime);
    }

    void Update()
    {
      // 시간에 따라 위로 올라가는 오프셋 증가 (UI 픽셀 단위)
      verticalOffset += moveSpeed * Time.deltaTime;

      // 월드 좌표를 현재 카메라 기준으로 스크린 좌표로 변환
      Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPosition);

      // 스크린 좌표에 오프셋을 더해 최종 위치 설정
      rectTransform.position = screenPos + new Vector3(0, verticalOffset, 0);

      // 알파값 조절
      color.a = Mathf.Lerp(color.a, 0, alphaSpeed * Time.deltaTime);
      textMesh.color = color;
    }
  }
}

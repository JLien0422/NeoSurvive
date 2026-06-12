using UnityEngine;

/// <summary>
/// Inspector에서 지정한 발밑 그림자 프리팹을 엔티티 자식으로 생성합니다.
/// </summary>
[DisallowMultipleComponent]
public class EntityShadow : MonoBehaviour
{
  [SerializeField] private GameObject shadowPrefab;
  [SerializeField] private Vector3 localOffset = Vector3.zero;
  [SerializeField] private int sortingOrderOffset = -1;
  [SerializeField] private bool autoScaleToEntity = true;
  [SerializeField] private Vector2 sizeMultiplier = new Vector2(0.9f, 0.25f);
  [SerializeField] private Vector2 minScale = new Vector2(0.2f, 0.08f);
  [SerializeField] private Vector2 maxScale = new Vector2(5f, 2f);

  private GameObject shadowInstance;
  private SpriteRenderer entityRenderer;
  private SpriteRenderer[] shadowRenderers;
  private Vector3 shadowBaseLocalScale;
  private Vector2 shadowBaseWorldSize;

  private void Awake()
  {
    EnsureShadow();
  }

  private void OnEnable()
  {
    if (shadowInstance != null)
      shadowInstance.SetActive(true);
  }

  private void OnDisable()
  {
    if (shadowInstance != null)
      shadowInstance.SetActive(false);
  }

  public void EnsureShadow()
  {
    if (shadowInstance != null)
      return;

    if (shadowPrefab == null)
      return;

    shadowInstance = Instantiate(shadowPrefab, transform);
    shadowInstance.name = shadowPrefab.name;
    shadowInstance.transform.localPosition = localOffset;
    shadowInstance.transform.localRotation = Quaternion.identity;
    shadowInstance.transform.SetAsFirstSibling();
    shadowBaseLocalScale = shadowInstance.transform.localScale;

    RefreshRenderers();
    ApplySortingBelowEntity();

    if (autoScaleToEntity)
      ApplyScaleToEntityBounds();
  }

  private void RefreshRenderers()
  {
    if (shadowInstance == null)
      return;

    entityRenderer = GetComponent<SpriteRenderer>();
    shadowRenderers = shadowInstance.GetComponentsInChildren<SpriteRenderer>(true);
    shadowBaseWorldSize = CalculateShadowWorldSize();
  }

  private void ApplySortingBelowEntity()
  {
    if (shadowInstance == null)
      return;

    if (entityRenderer == null)
      RefreshRenderers();

    if (entityRenderer == null)
      return;

    if (shadowRenderers == null || shadowRenderers.Length == 0)
      return;

    foreach (SpriteRenderer renderer in shadowRenderers)
    {
      if (renderer == null)
        continue;

      renderer.sortingLayerID = entityRenderer.sortingLayerID;
      renderer.sortingOrder = entityRenderer.sortingOrder + sortingOrderOffset;
    }
  }

  private void ApplyScaleToEntityBounds()
  {
    if (shadowInstance == null)
      return;

    if (entityRenderer == null)
      RefreshRenderers();

    if (!TryGetEntityBounds(out Bounds entityBounds))
      return;

    if (shadowBaseWorldSize.x <= 0f || shadowBaseWorldSize.y <= 0f)
      shadowBaseWorldSize = CalculateShadowWorldSize();

    if (shadowBaseWorldSize.x <= 0f || shadowBaseWorldSize.y <= 0f)
      return;

    float targetWorldWidth = Mathf.Max(0.001f, entityBounds.size.x * sizeMultiplier.x);
    float targetWorldHeight = Mathf.Max(0.001f, entityBounds.size.y * sizeMultiplier.y);

    float scaleX = Mathf.Clamp(targetWorldWidth / shadowBaseWorldSize.x, minScale.x, maxScale.x);
    float scaleY = Mathf.Clamp(targetWorldHeight / shadowBaseWorldSize.y, minScale.y, maxScale.y);

    shadowInstance.transform.localScale = new Vector3(
      shadowBaseLocalScale.x * scaleX,
      shadowBaseLocalScale.y * scaleY,
      shadowBaseLocalScale.z
    );
  }

  private bool TryGetEntityBounds(out Bounds bounds)
  {
    bounds = default;

    if (entityRenderer == null)
      return false;

    bounds = entityRenderer.bounds;
    return true;
  }

  private Vector2 CalculateShadowWorldSize()
  {
    if (shadowRenderers == null || shadowRenderers.Length == 0)
      return Vector2.zero;

    bool found = false;
    Bounds bounds = default;

    foreach (SpriteRenderer renderer in shadowRenderers)
    {
      if (renderer == null)
        continue;

      if (!found)
      {
        bounds = renderer.bounds;
        found = true;
      }
      else
      {
        bounds.Encapsulate(renderer.bounds);
      }
    }

    if (!found)
      return Vector2.zero;

    return new Vector2(bounds.size.x, bounds.size.y);
  }
}

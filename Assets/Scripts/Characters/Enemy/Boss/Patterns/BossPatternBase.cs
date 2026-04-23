using System.Collections;
using UnityEngine;

/// <summary>
/// 보스 패턴 베이스. 텔레그래프는 <b>이 컴포넌트에 프리팹을 넣는 것이 1순위</b>이고,
/// 비어 있으면 보스 쪽 <see cref="BossAttackWarningDisplay"/>를 폴백으로 씁니다(한 번만 할당).
/// </summary>
public abstract class BossPatternBase : MonoBehaviour
{
  [Header("텔레그래프 (우선)")]
  [Tooltip("비우면 보스의 BossAttackWarningDisplay 프리팹 사용")]
  [SerializeField] private GameObject telegraphCirclePrefab;

  [Tooltip("비우면 보스의 BossAttackWarningDisplay 프리팹 사용")]
  [SerializeField] private GameObject telegraphRectPrefab;

  [SerializeField] private int telegraphSortingOrder = 80;

  protected Boss BossRef { get; private set; }
  protected Transform PlayerTarget { get; private set; }
  protected Rigidbody2D BossRb { get; private set; }
  private BossAttackWarningDisplay _warningFallback;

  public virtual void Initialize(Boss boss, Transform player, Rigidbody2D bossRb)
  {
    BossRef = boss;
    PlayerTarget = player;
    BossRb = bossRb;
    if (boss != null)
      _warningFallback = boss.GetComponentInChildren<BossAttackWarningDisplay>(true);
  }

  public virtual void RefreshPlayerTarget(Transform player)
  {
    if (player != null)
      PlayerTarget = player;
  }

  public abstract IEnumerator RunPatternCoroutine();

  protected bool BossAlive => BossRef != null && !BossRef.IsDead;

  protected void IgnoreCollisionWithBoss(GameObject spawnedObject)
  {
    if (spawnedObject == null || BossRef == null)
      return;

    Collider2D[] bossColliders = BossRef.GetComponentsInChildren<Collider2D>(true);
    Collider2D[] spawnedColliders = spawnedObject.GetComponentsInChildren<Collider2D>(true);
    if (bossColliders == null || spawnedColliders == null)
      return;

    foreach (var bossCollider in bossColliders)
    {
      if (bossCollider == null) continue;
      foreach (var spawnedCollider in spawnedColliders)
      {
        if (spawnedCollider == null) continue;
        Physics2D.IgnoreCollision(bossCollider, spawnedCollider, true);
      }
    }
  }

  /// <summary>원형 텔레그래프. 지름 1유닛 스프라이트 가정 시 스케일 = 2×반경.</summary>
  protected IEnumerator PlayTelegraphCircle(Vector3 worldCenter, float radius, float duration)
  {
    if (duration <= 0f) yield break;

    GameObject prefab = telegraphCirclePrefab;
    if (prefab == null && _warningFallback != null)
      prefab = _warningFallback.TelegraphCirclePrefab;

    GameObject instance = null;
    if (prefab != null)
    {
      instance = Instantiate(prefab, worldCenter, Quaternion.identity);
      float diameter = radius * 2f;
      instance.transform.localScale = new Vector3(diameter, diameter, 1f);
      ApplyTelegraphSorting(instance);
    }

    yield return new WaitForSeconds(duration);

    if (instance != null)
      Destroy(instance);
  }

  /// <summary>사각 텔레그래프. size = 월드 단위 가로·세로.</summary>
  protected IEnumerator PlayTelegraphRect(Vector3 worldCenter, Vector2 size, float angleZDeg, float duration)
  {
    if (duration <= 0f) yield break;

    GameObject prefab = telegraphRectPrefab;
    if (prefab == null && _warningFallback != null)
      prefab = _warningFallback.TelegraphRectPrefab;

    GameObject instance = null;
    if (prefab != null)
    {
      instance = Instantiate(
        prefab,
        worldCenter,
        Quaternion.Euler(0f, 0f, angleZDeg));
      instance.transform.localScale = new Vector3(size.x, size.y, 1f);
      ApplyTelegraphSorting(instance);
    }

    yield return new WaitForSeconds(duration);

    if (instance != null)
      Destroy(instance);
  }

  private void ApplyTelegraphSorting(GameObject root)
  {
    foreach (var sr in root.GetComponentsInChildren<SpriteRenderer>(true))
      sr.sortingOrder = telegraphSortingOrder;
  }
}

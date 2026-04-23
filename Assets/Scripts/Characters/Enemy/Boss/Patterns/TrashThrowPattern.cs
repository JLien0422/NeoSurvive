using System.Collections;
using UnityEngine;

/// <summary>
/// 직스 궁 형태: 플레이어 현재 위치에 낙하지점 경고를 띄운 뒤 즉시 낙하 피해를 주고,
/// 해당 지점 반경 내 랜덤 위치로 TrashObstacle을 분열 생성합니다.
/// </summary>
public class TrashThrowPattern : BossPatternBase
{
  [Header("낙하 패턴")]
  [SerializeField] private int throwCount = 3;
  [SerializeField] private float throwInterval = 0.7f;
  [SerializeField] private float telegraphDuration = 0.9f;
  [SerializeField] private float impactRadius = 1.35f;
  [SerializeField] private float impactDamage = 24f;

  [Header("분열 TrashObstacle")]
  [SerializeField] private GameObject splitObstaclePrefab;
  [SerializeField] private int splitCount = 4;
  [SerializeField] private float splitMinRadius = 0.8f;
  [SerializeField] private float splitMaxRadius = 2.2f;
  [SerializeField] private float minSpawnSpacing = 0.85f;
  [SerializeField] private int maxSpawnAttemptsPerTrash = 8;

  public override IEnumerator RunPatternCoroutine()
  {
    if (!BossAlive) yield break;
    if (splitObstaclePrefab == null)
    {
      Debug.LogWarning("[TrashThrowPattern] splitObstaclePrefab 미할당");
      yield break;
    }

    if (PlayerTarget == null) yield break;

    var wait = new WaitForSeconds(throwInterval);
    for (int i = 0; i < throwCount && BossAlive; i++)
    {
      if (PlayerTarget == null) yield break;

      // 플레이어 "현재 위치 스냅샷"에 경고
      Vector3 impactCenter = PlayerTarget.position;
      impactCenter.z = 0f;
      yield return PlayTelegraphCircle(impactCenter, impactRadius, telegraphDuration);

      // 경고 지점 플레이어 피해
      ApplyImpactDamage(impactCenter);

      // 충돌 지점 반경 내 랜덤 분열
      SpawnSplitTrash(impactCenter);

      yield return wait;
    }
  }

  private void ApplyImpactDamage(Vector3 center)
  {
    var hits = Physics2D.OverlapCircleAll(center, impactRadius);
    foreach (var hit in hits)
    {
      if (hit == null || !hit.CompareTag("Player")) continue;
      if (hit.TryGetComponent<Player>(out var p))
        p.TakeDamage(impactDamage);
    }
  }

  private void SpawnSplitTrash(Vector3 center)
  {
    int count = Mathf.Max(0, splitCount);
    float minR = Mathf.Max(0f, splitMinRadius);
    float maxR = Mathf.Max(minR, splitMaxRadius);

    for (int i = 0; i < count; i++)
    {
      if (!TryFindNonOverlappingSplitPosition(center, minR, maxR, out Vector3 spawnPos))
        continue;

      GameObject trash = Instantiate(splitObstaclePrefab, spawnPos, Quaternion.identity);
      IgnoreCollisionWithBoss(trash);
      if (trash.TryGetComponent<Rigidbody2D>(out var rb))
        rb.velocity = Vector2.zero;
    }
  }

  private bool TryFindNonOverlappingSplitPosition(Vector3 center, float minR, float maxR, out Vector3 spawnPos)
  {
    int attempts = Mathf.Max(1, maxSpawnAttemptsPerTrash);
    float spacing = Mathf.Max(0.1f, minSpawnSpacing);

    for (int attempt = 0; attempt < attempts; attempt++)
    {
      Vector2 dir = Random.insideUnitCircle.normalized;
      if (dir.sqrMagnitude < 0.0001f)
        dir = Vector2.right;

      float r = Random.Range(minR, maxR + (attempt * 0.1f));
      spawnPos = center + (Vector3)(dir * r);
      spawnPos.z = 0f;

      if (!IsTrashOverlappingAt(spawnPos, spacing))
        return true;
    }

    spawnPos = Vector3.zero;
    return false;
  }

  private bool IsTrashOverlappingAt(Vector3 position, float checkRadius)
  {
    var overlaps = Physics2D.OverlapCircleAll(position, checkRadius);
    foreach (var overlap in overlaps)
    {
      if (overlap == null)
        continue;
      if (overlap.TryGetComponent<TrashObstacle>(out _))
        return true;
    }
    return false;
  }
}

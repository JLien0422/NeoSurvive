using System.Collections;
using UnityEngine;

/// <summary>
/// 플레이어를 향한 2연 휘두르기 + 마지막 돌진 휘두르기.
/// 각 공격은 텔레그래프 후 판정하며, 마지막 공격은 쓰레기 더미를 제거합니다.
/// </summary>
public class SwingComboPattern : BossPatternBase
{
  [Header("공통")]
  [SerializeField] private float telegraphDuration = 1.5f;
  [SerializeField] private float comboDamage = 12f;

  [Header("1~2타 휘두르기")]
  [SerializeField] private int normalSwingCount = 2;
  [SerializeField] private Vector2 normalHitBoxSize = new Vector2(2.2f, 1.3f);
  [SerializeField] private float normalHitDistance = 1.25f;
  [SerializeField] private float gapBetweenNormalSwings = 0.15f;

  [Header("마지막 돌진 휘두르기")]
  [SerializeField] private float finalDashSpeed = 14f;
  [SerializeField] private float finalDashDuration = 0.4f;
  [SerializeField] private Vector2 finalHitBoxSize = new Vector2(3.2f, 1.6f);
  [SerializeField] private float finalHitDistance = 1.6f;
  [SerializeField] private float finalTrashClearWidth = 1.8f;

  public override IEnumerator RunPatternCoroutine()
  {
    if (!BossAlive || PlayerTarget == null) yield break;

    // 1~2타 휘두르기
    int swingCount = Mathf.Max(1, normalSwingCount);
    for (int s = 0; s < swingCount && BossAlive; s++)
    {
      if (!BossAlive || PlayerTarget == null) yield break;

      Vector2 toPlayer = (Vector2)(PlayerTarget.position - transform.position);
      Vector2 dir = toPlayer.sqrMagnitude > 0.0001f ? toPlayer.normalized : Vector2.right;
      Vector2 center = (Vector2)transform.position + dir * normalHitDistance;
      float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

      yield return PlayTelegraphRect(center, normalHitBoxSize, angle, telegraphDuration);

      if (!BossAlive || PlayerTarget == null) yield break;

      // 경고 직후 플레이어가 움직였으면 판정만 최신화
      toPlayer = (Vector2)(PlayerTarget.position - transform.position);
      dir = toPlayer.sqrMagnitude > 0.0001f ? toPlayer.normalized : Vector2.right;
      center = (Vector2)transform.position + dir * normalHitDistance;
      angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

      PlayBossAnimTrigger(s == 0 ? "Map1Boss Pattern3 First" : "Map1Boss Pattern3 Second");

      DamagePlayerInBox(center, normalHitBoxSize, angle, comboDamage);

      if (gapBetweenNormalSwings > 0f)
        yield return new WaitForSeconds(gapBetweenNormalSwings);
    }

    if (!BossAlive || PlayerTarget == null) yield break;

    // 마지막 돌진 휘두르기 (텔레그래프 1.5초)
    Vector2 finalDir = ((Vector2)PlayerTarget.position - (Vector2)transform.position).normalized;
    if (finalDir.sqrMagnitude < 0.0001f) finalDir = Vector2.right;
    Vector2 dashStartPos = transform.position;

    Vector2 finalCenter = (Vector2)transform.position + finalDir * finalHitDistance;
    float finalAngle = Mathf.Atan2(finalDir.y, finalDir.x) * Mathf.Rad2Deg;
    yield return PlayTelegraphRect(finalCenter, finalHitBoxSize, finalAngle, telegraphDuration);

    PlayBossAnimTrigger("Map1Boss Pattern3 Third");

    // 돌진
    float elapsed = 0f;
    while (elapsed < finalDashDuration && BossAlive)
    {
      elapsed += Time.deltaTime;
      if (BossRb != null)
        BossRb.velocity = finalDir * finalDashSpeed;
      yield return null;
    }
    if (BossRb != null)
      BossRb.velocity = Vector2.zero;

    // 돌진 종료 지점 휘두르기 판정
    finalCenter = (Vector2)transform.position + finalDir * finalHitDistance;
    finalAngle = Mathf.Atan2(finalDir.y, finalDir.x) * Mathf.Rad2Deg;
    DamagePlayerInBox(finalCenter, finalHitBoxSize, finalAngle, comboDamage);

    // 마지막 공격은 돌진 경로 기준으로 쓰레기 더미 제거
    ClearTrashAlongDash(dashStartPos, (Vector2)transform.position, finalDir);
  }

  private void DamagePlayerInBox(Vector2 center, Vector2 size, float angle, float damage)
  {
    var hits = Physics2D.OverlapBoxAll(center, size, angle);
    foreach (var hit in hits)
    {
      if (hit == null || !hit.CompareTag("Player")) continue;
      if (hit.TryGetComponent<Player>(out var p))
      {
        GameAnalyticsTracker.TrackBossPatternHit(nameof(SwingComboPattern), "swing_box", damage, p);
        p.TakeDamage(damage);
        return;
      }
    }
  }

  private void ClearTrashAlongDash(Vector2 startPos, Vector2 endPos, Vector2 dashDir)
  {
    float dashDistance = Vector2.Distance(startPos, endPos);
    float sweepLength = Mathf.Max(0.5f, dashDistance + finalHitDistance);
    Vector2 center = startPos + dashDir.normalized * (sweepLength * 0.5f);
    float angle = Mathf.Atan2(dashDir.y, dashDir.x) * Mathf.Rad2Deg;

    Vector2 sweepSize = new Vector2(sweepLength, Mathf.Max(0.2f, finalTrashClearWidth));
    var hits = Physics2D.OverlapBoxAll(center, sweepSize, angle);
    foreach (var hit in hits)
    {
      if (hit == null) continue;
      if (hit.TryGetComponent<TrashObstacle>(out var trash))
        Destroy(trash.gameObject);
    }
  }

#if UNITY_EDITOR
  private void OnDrawGizmosSelected()
  {
    Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.45f);
    Gizmos.DrawWireCube(transform.position, new Vector3(finalHitBoxSize.x, finalTrashClearWidth, 0.1f));
  }
#endif
}

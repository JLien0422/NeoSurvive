using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어 위치를 기준으로 차징 후 돌진하고,
/// 멈춘 뒤 충격파 + 주변 원형 Trash 생성 후 플레이어 방향으로 던집니다.
/// </summary>
public class ChargeShockwavePattern : BossPatternBase
{
  [Header("차징/돌진")]
  [SerializeField] private float chargeDuration = 2f;
  [SerializeField] private float dashSpeed = 12f;
  [SerializeField] private float dashMaxDuration = 1.2f;
  [SerializeField] private float playerHitRadius = 0.9f;
  [SerializeField] private float dashCollisionDamage = 12f;
  [SerializeField] private LayerMask wallLayers;
  [SerializeField] private float wallCheckDistance = 0.6f;

  [Header("점프/충격파")]
  [SerializeField] private float jumpDelay = 0.35f;
  [SerializeField] private float shockwaveRadius = 3f;
  [SerializeField] private float shockwaveDamage = 15f;

  [Header("원형 Trash 생성/투척")]
  [SerializeField] private GameObject ringTrashPrefab;
  [SerializeField] private int ringTrashCount = 8;
  [SerializeField] private float ringRadius = 2.5f;
  [SerializeField] private int maxThrowsFromRing = 8;
  [SerializeField] private float throwInterval = 0.25f;
  [SerializeField] private float thrownTrashSpeed = 8f;

  public override IEnumerator RunPatternCoroutine()
  {
    if (!BossAlive || PlayerTarget == null) yield break;

    if (BossRb != null)
      BossRb.velocity = Vector2.zero;

    // 차징 동안 충격파 위험 반경 예고
    yield return PlayTelegraphCircle(transform.position, shockwaveRadius, chargeDuration);
    if (!BossAlive || PlayerTarget == null) yield break;

    Vector2 dir = ((Vector2)(PlayerTarget.position - transform.position)).normalized;
    if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;

    // 플레이어 충돌 또는 벽 충돌 시 돌진 종료 (플레이어 접촉 시 1회 피해)
    float elapsed = 0f;
    bool dashDamageDealt = false;
    while (elapsed < dashMaxDuration && BossAlive)
    {
      elapsed += Time.deltaTime;

      if (BossRb != null)
        BossRb.velocity = dir * dashSpeed;

      if (PlayerTarget != null)
      {
        float sqrDist = ((Vector2)PlayerTarget.position - (Vector2)transform.position).sqrMagnitude;
        if (sqrDist <= playerHitRadius * playerHitRadius)
        {
          if (!dashDamageDealt && dashCollisionDamage > 0f &&
              PlayerTarget.TryGetComponent<Player>(out var hitPlayer))
          {
            hitPlayer.TakeDamage(dashCollisionDamage);
            dashDamageDealt = true;
          }
          break;
        }
      }

      if (wallLayers.value != 0)
      {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, dir, wallCheckDistance, wallLayers);
        if (hit.collider != null)
          break;
      }

      yield return null;
    }

    if (BossRb != null)
      BossRb.velocity = Vector2.zero;

    // 크게 점프하는 타이밍 연출(탑다운에서는 딜레이 처리)
    if (jumpDelay > 0f)
      yield return new WaitForSeconds(jumpDelay);

    ApplyShockwaveDamage();

    // 주변 원형 Trash 생성 후 플레이어 방향 투척
    List<GameObject> ringTrash = SpawnRingTrash();
    yield return ThrowRingTrash(ringTrash);
  }

  private void ApplyShockwaveDamage()
  {
    var hits = Physics2D.OverlapCircleAll(transform.position, shockwaveRadius);
    foreach (var c in hits)
    {
      if (c == null || !c.CompareTag("Player")) continue;
      if (c.TryGetComponent<Player>(out var p))
        p.TakeDamage(shockwaveDamage);
    }
  }

  private List<GameObject> SpawnRingTrash()
  {
    var list = new List<GameObject>();
    if (ringTrashPrefab == null || ringTrashCount <= 0)
      return list;

    int count = Mathf.Max(1, ringTrashCount);
    for (int i = 0; i < count; i++)
    {
      float a = (Mathf.PI * 2f) * (i / (float)count);
      Vector3 pos = transform.position + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * ringRadius;
      GameObject trash = Instantiate(ringTrashPrefab, pos, Quaternion.identity);
      list.Add(trash);
    }

    return list;
  }

  private IEnumerator ThrowRingTrash(List<GameObject> ringTrash)
  {
    if (ringTrash == null || ringTrash.Count == 0)
      yield break;

    int thrown = 0;
    var wait = new WaitForSeconds(throwInterval);

    while (thrown < maxThrowsFromRing && BossAlive)
    {
      ringTrash.RemoveAll(t => t == null);
      if (ringTrash.Count == 0)
        yield break; // 플레이어가 먼저 부수면 패턴 단축

      if (PlayerTarget == null)
        yield break;

      int index = FindClosestTrashIndex(ringTrash);
      if (index < 0)
        yield break;

      GameObject trash = ringTrash[index];
      ringTrash.RemoveAt(index); // 보스가 던지면 소모
      thrown++;

      Vector2 dir = ((Vector2)PlayerTarget.position - (Vector2)trash.transform.position).normalized;
      if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;

      if (trash.TryGetComponent<Rigidbody2D>(out var rb))
      {
        rb.velocity = dir * thrownTrashSpeed;
      }
      else
      {
        trash.transform.position += (Vector3)(dir * 0.2f);
      }

      yield return wait;
    }
  }

  private int FindClosestTrashIndex(List<GameObject> ringTrash)
  {
    int bestIdx = -1;
    float bestDist = float.MaxValue;

    for (int i = 0; i < ringTrash.Count; i++)
    {
      GameObject t = ringTrash[i];
      if (t == null) continue;

      float d = ((Vector2)t.transform.position - (Vector2)transform.position).sqrMagnitude;
      if (d < bestDist)
      {
        bestDist = d;
        bestIdx = i;
      }
    }

    return bestIdx;
  }

#if UNITY_EDITOR
  private void OnDrawGizmosSelected()
  {
    Gizmos.color = new Color(1f, 0.5f, 0f, 0.35f);
    Gizmos.DrawWireSphere(transform.position, shockwaveRadius);
  }
#endif
}

using UnityEngine;
using System.Collections;
using NeoSurvive.Core;

namespace NeoSurvive.Weapon
{
  /// <summary>
  /// 5번 무기: 중력 해머
  /// - 기본: 지면 강타 후 충격파(부채꼴)로 피해 + 넉백
  /// - Lv.Up: 넉백 거리(힘), 강타 범위 증가
  /// - Lv.5 마스터: 타격 지점 중앙에 소형 블랙홀(2초 흡입) 생성
  /// </summary>
  public class GravityHammer : MonoBehaviour
  {
    [Header("Stats")]
    public float damage = 18f;
    public float range = 4.5f;      // 부채꼴 반경
    public float angle = 70f;       // 부채꼴 전체 각도
    public float fireRate = 1.6f;   // 강타 주기

    [Header("Knockback")]
    public float knockbackForce = 6f;
    public float knockbackRadiusBonus = 0f; // 필요하면 확장

    [Header("Filter")]
    public LayerMask hitMask;
    public string enemyTag = "Enemy";

    [Header("Level Scaling")]
    public float damagePerLevel = 0.15f;
    public float rangePerLevel = 0.12f;        // 강타 범위 증가
    public float knockbackPerLevel = 0.15f;    // 넉백 증가(비율)

    [Header("Master (Lv5) - Blackhole")]
    public bool enableMaster = true;
    public GameObject blackholePrefab;   // 선택: 있으면 프리팹 생성
    public float blackholeDuration = 2f;
    public float blackholePullRadius = 2.5f;
    public float blackholePullForce = 10f;

    [Header("Debug")]
    public bool debugDraw = true;

    private float timer;

    // base stats
    private float baseDamage;
    private float baseRange;
    private float baseKnockback;
    private float baseFireRate;

    private int currentLevel = 1;

    private void Start()
    {
      baseDamage = damage;
      baseRange = range;
      baseKnockback = knockbackForce;
      baseFireRate = fireRate;

      ApplyLevel(1);
    }

    private void Update()
    {
      timer += Time.deltaTime;
      if (timer >= fireRate)
      {
        Slam();
        timer = 0f;
      }
    }

    public void OnLevelUp(int level)
    {
      ApplyLevel(level);
      Debug.Log($"[GravityHammer] Lv.{currentLevel} dmg={damage} range={range} kb={knockbackForce}");
    }

    private void ApplyLevel(int level)
    {
      currentLevel = Mathf.Clamp(level, 1, 5);

      damage = baseDamage * (1f + (currentLevel - 1) * damagePerLevel);
      range  = baseRange  * (1f + (currentLevel - 1) * rangePerLevel);
      knockbackForce = baseKnockback * (1f + (currentLevel - 1) * knockbackPerLevel);
      fireRate = baseFireRate; // 고정(원하면 레벨에 따라 감소 가능)
    }

    private void Slam()
    {
      // 가까운 적 방향(없으면 오른쪽)
      Transform target = FindClosestEnemy(range * 1.5f);
      Vector3 forward = transform.right;
      if (target != null) forward = (target.position - transform.position).normalized;

      // 타격 지점(앞쪽)
      Vector3 impactPoint = transform.position + forward * Mathf.Min(range, 2.2f);

      if (debugDraw)
      {
        Debug.DrawRay(transform.position, forward * range, Color.yellow, 0.2f);
        Debug.DrawRay(transform.position, Quaternion.Euler(0, 0, angle * 0.5f) * forward * range, Color.magenta, 0.2f);
        Debug.DrawRay(transform.position, Quaternion.Euler(0, 0, -angle * 0.5f) * forward * range, Color.magenta, 0.2f);
        Debug.DrawLine(transform.position, impactPoint, Color.white, 0.2f);
      }

      // 범위 내 후보
      Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, range + knockbackRadiusBonus, hitMask);

      foreach (var col in hits)
      {
        if (col == null) continue;

        // 태그(자식 콜라이더 구조 고려)
        if (!string.IsNullOrEmpty(enemyTag) && !col.CompareTag(enemyTag))
        {
          if (col.transform.parent == null || !col.transform.parent.CompareTag(enemyTag))
            continue;
        }

        Vector3 dirToEnemy = (col.transform.position - transform.position).normalized;
        if (Vector3.Angle(forward, dirToEnemy) > angle * 0.5f) continue;

        Enemy enemy = col.GetComponentInParent<Enemy>();
        if (enemy == null) continue;

        enemy.TakeDamage(damage);

        // 넉백(가능한 경우만)
        ApplyKnockback(col.transform, forward, impactPoint);
      }

      // Lv5 마스터: 블랙홀 생성(선택)
      if (enableMaster && currentLevel >= 5)
      {
        SpawnBlackhole(impactPoint);
      }
    }

    private void ApplyKnockback(Transform enemyTransform, Vector3 forward, Vector3 impactPoint)
    {
      // Rigidbody2D가 있으면 힘으로 넉백
      Rigidbody2D rb = enemyTransform.GetComponentInParent<Rigidbody2D>();
      if (rb != null)
      {
        Vector2 dir = (enemyTransform.position - impactPoint).normalized;
        if (dir.sqrMagnitude < 0.001f) dir = forward;
        rb.AddForce(dir * knockbackForce, ForceMode2D.Impulse);
      }
      // Rigidbody2D가 없으면 그냥 패스(팀 Enemy 구조에 따라 결정)
    }

    private void SpawnBlackhole(Vector3 pos)
    {
      // 방법 A) 프리팹 있으면 생성 (시각효과/콜라이더/스크립트 자유)
      if (blackholePrefab != null)
      {
        GameObject obj = Instantiate(blackholePrefab, pos, Quaternion.identity);
        Destroy(obj, blackholeDuration);
      }

      // 방법 B) 프리팹 없어도 동작하게: 이 오브젝트에서 코루틴으로 흡입 처리
      StartCoroutine(BlackholePullRoutine(pos, blackholeDuration));
    }

    private IEnumerator BlackholePullRoutine(Vector3 center, float duration)
    {
      float t = 0f;
      while (t < duration)
      {
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, blackholePullRadius, hitMask);
        foreach (var col in hits)
        {
          if (col == null) continue;

          if (!string.IsNullOrEmpty(enemyTag) && !col.CompareTag(enemyTag))
          {
            if (col.transform.parent == null || !col.transform.parent.CompareTag(enemyTag))
              continue;
          }

          Rigidbody2D rb = col.GetComponentInParent<Rigidbody2D>();
          if (rb != null)
          {
            Vector2 dir = (center - rb.transform.position);
            rb.AddForce(dir.normalized * blackholePullForce * Time.deltaTime, ForceMode2D.Force);
          }
          else
          {
            // Rigidbody2D가 없는 경우: 강제로 당기기(원하면 사용)
            col.transform.position = Vector3.MoveTowards(col.transform.position, center, (blackholePullForce * 0.05f) * Time.deltaTime);
          }
        }

        t += Time.deltaTime;
        yield return null;
      }
    }

    private Transform FindClosestEnemy(float searchRange)
    {
      GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
      GameObject closest = null;
      float minDist = searchRange > 0 ? searchRange : 10f;

      foreach (GameObject e in enemies)
      {
        float d = Vector3.Distance(transform.position, e.transform.position);
        if (d < minDist)
        {
          minDist = d;
          closest = e;
        }
      }
      return closest ? closest.transform : null;
    }

    private void OnDrawGizmosSelected()
    {
      Gizmos.color = Color.yellow;
      Gizmos.DrawWireSphere(transform.position, range);
    }
  }
}

using UnityEngine;
using System.Collections;
using NeoSurvive.Core;

namespace NeoSurvive.Weapon
{
  /// <summary>
  /// 5번 무기: GravityHammer
  /// - 기본: 전방(부채꼴) 강타 + 넉백
  /// - Lv.Up: 데미지/범위/넉백 증가
  /// - Lv5 마스터: 블랙홀 생성(흡입)
  /// - LaserSword.cs 스타일(팀원 방식)로 통일
  /// </summary>
  public class GravityHammer : MonoBehaviour
  {
    [Header("Stats")]
    public float damage = 18f;
    public float range = 4.5f;
    public float angle = 70f;
    public float fireRate = 1.6f;

    [Header("Knockback")]
    public float knockbackForce = 6f;

    [Header("Target / Hit")]
    public LayerMask hitMask;          // Enemy 레이어 권장
    public string enemyTag = "Enemy";

    [Header("Level Scaling")]
    public float damagePerLevel = 0.15f;
    public float rangePerLevel = 0.12f;
    public float knockbackPerLevel = 0.15f;

    [Header("Master (Lv5) - Blackhole")]
    public bool enableMaster = true;
    public GameObject blackholePrefab;   // 시각효과용(선택)
    public float blackholeDuration = 2f;
    public float blackholePullRadius = 2.5f;
    public float blackholePullForce = 10f;

    [Header("Debug")]
    public bool debugLog = false;
    public bool debugDraw = true;

    private float timer;

    // base stats
    private float baseDamage;
    private float baseRange;
    private float baseKnockback;
    private float baseFireRate;

    private int currentLevel = 1;
    private Coroutine blackholeRoutine;

    private void Start()
    {
      baseDamage = damage;
      baseRange = range;
      baseKnockback = knockbackForce;
      baseFireRate = fireRate;

      // LaserSword처럼 플레이어와 겹침 방지(앞으로 살짝)
      transform.localPosition = Vector3.right * 0.5f;

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

    // WeaponManager.SendMessage("OnLevelUp", level) 호환
    public void OnLevelUp(int level)
    {
      // LaserSword와 동일한 안전장치
      if (baseDamage <= 0f && damage > 0f) baseDamage = damage;
      if (baseRange <= 0f && range > 0f) baseRange = range;
      if (baseKnockback <= 0f && knockbackForce > 0f) baseKnockback = knockbackForce;
      if (baseFireRate <= 0f && fireRate > 0f) baseFireRate = fireRate;

      ApplyLevel(level);

      if (debugLog)
        Debug.Log($"[GravityHammer] Lv.{currentLevel} Dmg={damage}, Range={range}, KB={knockbackForce}, Rate={fireRate}");
    }

    private void ApplyLevel(int level)
    {
      currentLevel = Mathf.Clamp(level, 1, 5);

      damage = baseDamage * (1f + (currentLevel - 1) * damagePerLevel);
      range  = baseRange  * (1f + (currentLevel - 1) * rangePerLevel);
      knockbackForce = baseKnockback * (1f + (currentLevel - 1) * knockbackPerLevel);
      fireRate = baseFireRate; // 원하면 레벨업 시 감소도 가능
    }

    private void Slam()
    {
      if (debugLog) Debug.Log("[GravityHammer] Slam!");

      // Player 방향 기준 (LaserSword와 동일)
      Transform target = FindClosestEnemy();
      Vector3 forward = transform.parent != null ? transform.parent.right : transform.right;

      if (target != null)
        forward = (target.position - transform.position).normalized;

      // 디버그: 공격 방향
      if (debugDraw)
      {
        Debug.DrawRay(transform.position, forward * range, Color.red, 0.2f);
        Debug.DrawRay(transform.position, Quaternion.Euler(0, 0, angle * 0.5f) * forward * range, Color.magenta, 0.2f);
        Debug.DrawRay(transform.position, Quaternion.Euler(0, 0, -angle * 0.5f) * forward * range, Color.magenta, 0.2f);
      }

      // ✅ LaserSword와 동일하게 OverlapCircleAll 사용
      Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, range, hitMask);

      if (debugLog) Debug.Log($"[GravityHammer] hits={hits.Length}");

      foreach (var col in hits)
      {
        if (col == null) continue;

        // Tag는 부모까지 체크 (LaserSword 방식)
        if (!string.IsNullOrEmpty(enemyTag))
        {
          bool okTag =
            col.CompareTag(enemyTag) ||
            (col.transform.parent != null && col.transform.parent.CompareTag(enemyTag));

          if (!okTag) continue;
        }

        // 부채꼴 판정
        Vector3 dirToEnemy = (col.transform.position - transform.position).normalized;
        if (Vector3.Angle(forward, dirToEnemy) > angle * 0.5f) continue;

        // Character 베이스로 Enemy/Boss 모두 처리
        Character character = col.GetComponentInParent<Character>();
        if (character == null) continue;

        var src = GetComponentInParent<WeaponSource>();
        character.TakeDamage(damage, src != null ? src.weaponData : null);

        // 넉백
        ApplyKnockback(col.transform, forward);
      }

      // Lv5 마스터: 블랙홀
      if (enableMaster && currentLevel >= 5)
      {
        Vector3 center = transform.position + forward * Mathf.Min(range, 2.2f);
        SpawnBlackhole(center);
      }
    }

    private void ApplyKnockback(Transform hitTransform, Vector3 forward)
    {
      // 적 루트의 Rigidbody2D 찾기
      Rigidbody2D rb = hitTransform.GetComponentInParent<Rigidbody2D>();

      // 밀리는 방향: 플레이어->적 방향 우선
      Vector2 dir = (hitTransform.position - transform.position);
      if (dir.sqrMagnitude < 0.001f) dir = forward;
      dir.Normalize();

      if (rb != null && rb.bodyType == RigidbodyType2D.Dynamic)
      {
        rb.AddForce(dir * knockbackForce, ForceMode2D.Impulse);
      }
      else
      {
        // Dynamic이 아니면 강제 이동 넉백(체감용)
        hitTransform.position = Vector3.MoveTowards(
          hitTransform.position,
          hitTransform.position + (Vector3)(dir * 1.2f),
          (knockbackForce * 0.08f) * Time.deltaTime
        );
      }
    }

    private void SpawnBlackhole(Vector3 pos)
    {
      if (blackholePrefab != null)
      {
        GameObject obj = Instantiate(blackholePrefab, pos, Quaternion.identity);
        Destroy(obj, blackholeDuration);
      }

      if (blackholeRoutine != null) StopCoroutine(blackholeRoutine);
      blackholeRoutine = StartCoroutine(BlackholePullRoutine(pos, blackholeDuration));
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

          if (!string.IsNullOrEmpty(enemyTag))
          {
            bool okTag =
              col.CompareTag(enemyTag) ||
              (col.transform.parent != null && col.transform.parent.CompareTag(enemyTag));

            if (!okTag) continue;
          }

          Rigidbody2D rb = col.GetComponentInParent<Rigidbody2D>();
          if (rb != null && rb.bodyType == RigidbodyType2D.Dynamic)
          {
            Vector2 dir = (center - rb.transform.position);
            rb.AddForce(dir.normalized * blackholePullForce * Time.deltaTime, ForceMode2D.Force);
          }
          else
          {
            // Dynamic이 아니면 강제 당김(선택)
            col.transform.position = Vector3.MoveTowards(
              col.transform.position,
              center,
              (blackholePullForce * 0.05f) * Time.deltaTime
            );
          }
        }

        t += Time.deltaTime;
        yield return null;
      }

      blackholeRoutine = null;
    }

    private Transform FindClosestEnemy()
    {
      GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
      Transform closest = null;
      float closestDist = range > 0 ? range * 1.5f : 10f;

      foreach (var e in enemies)
      {
        if (e == null) continue;
        float d = Vector3.Distance(transform.position, e.transform.position);
        if (d < closestDist)
        {
          closestDist = d;
          closest = e.transform;
        }
      }

      return closest;
    }

    private void OnDrawGizmosSelected()
    {
      Gizmos.color = Color.yellow;
      Gizmos.DrawWireSphere(transform.position, range);
    }
  }
}

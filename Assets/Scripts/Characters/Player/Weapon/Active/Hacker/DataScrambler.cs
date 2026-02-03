using UnityEngine;
using System.Collections.Generic;
using NeoSurvive.Core;

namespace NeoSurvive.Weapon
{
  /// <summary>
  /// 3번 무기: 데이터 스크램블러
  /// 부채꼴 범위에 교란 신호 방사. 혼란에 걸린 적은 일정 시간 후 폭발하여 광역 피해.
  /// </summary>
  public class DataScrambler : MonoBehaviour
  {
    [Header("Stats")]
    public float damage = 10f;
    public float range = 5f;       // 부채꼴 반지름
    public float angle = 60f;      // 부채꼴 각도 (전체 각도)
    public float fireRate = 2f;
    public float duration = 3f;    // 혼란 지속 시간

    private float fireTimer;

    // 레벨업 기준 스탯
    private float baseDamage;
    private float baseRange;
    private float baseDuration;

    private void Start()
    {
      baseDamage = damage;
      baseRange = range;
      baseDuration = duration;
    }

    private void Update()
    {

      // [Coop] 서버에 공격 보고 (로컬 플레이어일 때만)
      var runtimeInfo = GetComponent<WeaponRuntimeInfo>();
      if (runtimeInfo != null)
      fireTimer += Time.deltaTime;
      if (fireTimer >= fireRate)
      {
        Attack();
        fireTimer = 0f;
      }
    }

    private void Attack()
    {
      // 가장 가까운 적을 향해 발사 (없으면 오른쪽)
      Transform target = FindClosestEnemy();
      Vector3 forward = transform.right;
      if (target != null)
      {
        forward = (target.position - transform.position).normalized;
      }

      // [Coop] 서버에 공격 보고 (로컬 플레이어일 때만)
      var runtimeInfo = GetComponent<WeaponRuntimeInfo>();
      if (runtimeInfo != null)
      {
        runtimeInfo.ReportCone(transform.position, forward, angle, range, duration);
      }

      // 시각적 효과 (디버그용)
      Debug.DrawRay(transform.position, Quaternion.Euler(0, 0, angle / 2) * forward * range, Color.magenta, 0.5f);
      Debug.DrawRay(transform.position, Quaternion.Euler(0, 0, -angle / 2) * forward * range, Color.magenta, 0.5f);

      // 범위 내 적 감지
      Collider2D[] enemies = Physics2D.OverlapCircleAll(transform.position, range);
      foreach (var col in enemies)
      {
        if (col.CompareTag("Enemy"))
        {
          Vector3 dirToEnemy = (col.transform.position - transform.position).normalized;
          // 내적을 이용한 각도 계산보다 Vector3.Angle이 직관적
          if (Vector3.Angle(forward, dirToEnemy) < angle / 2)
          {
            // 부채꼴 범위 안
            if (col.TryGetComponent<Enemy>(out var enemy))
            {
              enemy.TakeDamage(damage); // 즉시 피해
              ApplyConfusion(col.gameObject);
            }
          }
        }
      }
    }

    private void ApplyConfusion(GameObject enemyObj)
    {
      // 혼란 효과 컴포넌트 부착
      var confusion = enemyObj.GetComponent<ConfusionEffect>();
      if (confusion == null)
      {
        confusion = enemyObj.AddComponent<ConfusionEffect>();
      }
      // 이미 있으면 시간/데미지 갱신
      confusion.Initialize(damage, duration);
    }

    public void OnLevelUp(int level)
    {
      if (baseDamage == 0 && damage > 0) baseDamage = damage;

      // 레벨업: 범위, 지속 시간 증가 (기획서 반영) + 데미지도 10%씩 증가
      range = baseRange * (1f + (level - 1) * 0.15f);
      duration = baseDuration * (1f + (level - 1) * 0.2f);
      damage = baseDamage * (1f + (level - 1) * 0.1f);

      Debug.Log($"[DataScrambler] Lv.{level} : Dmg {damage}, Range {range}, Duration {duration}");
    }

    private Transform FindClosestEnemy()
    {
      GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
      GameObject closest = null;
      float closestDistance = range > 0 ? (range * 1.5f) : 10f; // 사거리보다 조금 더 멀리까지 탐색

      foreach (GameObject enemy in enemies)
      {
        float distance = Vector3.Distance(transform.position, enemy.transform.position);
        if (distance < closestDistance)
        {
          closestDistance = distance;
          closest = enemy;
        }
      }
      return closest != null ? closest.transform : null;
    }

    private void OnDrawGizmos()
    {
      Gizmos.color = Color.magenta;
      Gizmos.DrawWireSphere(transform.position, range);
    }
  }

  /// <summary>
  /// 적에게 부착되어 일정 시간 후 폭발하는 혼란 효과
  /// </summary>
  public class ConfusionEffect : MonoBehaviour
  {
    private float damage;
    private float timer;
    private bool initialized = false;

    public void Initialize(float dmg, float duration)
    {
      this.damage = dmg;
      this.timer = duration;
      this.initialized = true;

      // 시각 효과? (색상 변경 등)
      var sr = GetComponent<SpriteRenderer>();
      if (sr) sr.color = Color.magenta;
    }

    void Update()
    {
      if (!initialized) return;

      timer -= Time.deltaTime;
      if (timer <= 0)
      {
        Explode();
      }
    }

    void Explode()
    {
      // 폭발 범위 2.5f (고정)
      Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 2.5f);
      foreach (var h in hits)
      {
        if (h.gameObject == gameObject) continue; // 나 자신 제외
        if (h.CompareTag("Enemy") && h.TryGetComponent<Enemy>(out var e))
        {
          e.TakeDamage(damage); // 광역 피해
        }
      }

      // 효과 종료 (컴포넌트 제거 or 색상 복구)
      var sr = GetComponent<SpriteRenderer>();
      if (sr) sr.color = Color.white; // 원래대로 (Enemy 기본색이 흰색이라 가정)

      Destroy(this); // 효과 끝
    }

    // 적이 죽을 때도 폭발하게 하려면 OnDestroy 활용?
    // 하지만 Unity에서 Destroy(gameObject) 될 땐 OnDestroy가 불리지만, 
    // 다른 로직과 꼬일 수 있으므로(이미 죽은 적을 또 죽임), 여기선 타이머 폭발만 구현.
  }
}

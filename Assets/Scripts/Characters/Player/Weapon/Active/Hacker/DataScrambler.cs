using UnityEngine;
using System.Collections.Generic;
using NeoSurvive.Core;
using NeoSurvive.Buff; // (추가) FrenzyDebuff / BuffUtil

namespace NeoSurvive.Weapon
{
  /// <summary>
  /// 3번 무기: 데이터 스크램블러
  /// 부채꼴 범위에 교란 신호 방사. 혼란에 걸린 적은 일정 시간 후 폭발하여 광역 피해.
  /// Lv5: 혼란 대상에게 Frenzy(광란) 디버프도 함께 적용
  /// </summary>
  public class DataScrambler : MonoBehaviour
  {
    [Header("Stats")]
    public float damage = 10f;
    public float range = 5f;       // 부채꼴 반지름
    public float angle = 60f;      // 부채꼴 각도 (전체 각도)
    public float fireRate = 2f;
    public float duration = 3f;    // 혼란 지속 시간

    // (추가) Lv5 Frenzy 설정
    [Header("Lv5 Frenzy (추가)")]
    public bool enableFrenzyAtLv5 = true; // (추가)
    public float frenzyDuration = 2.5f;   // (추가) 광란 지속 시간(원하는 값으로)

    private float fireTimer;

    // 레벨업 기준 스탯
    private float baseDamage = 10f;
    private float baseRange = 5f;
    private float baseDuration = 3f;

    private int currentLevel = 1; // (추가) 현재 레벨 캐시

    private void Start()
    {
      Debug.Log("[DataScrambler] Initialized");
      baseDamage = damage;
      baseRange = range;
      baseDuration = duration;
    }

    private void Update()
    {
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

      // 시각적 효과 (디버그용)
      Debug.DrawRay(transform.position, Quaternion.Euler(0, 0, angle / 2) * forward * range, Color.magenta, 0.5f);
      Debug.DrawRay(transform.position, Quaternion.Euler(0, 0, -angle / 2) * forward * range, Color.magenta, 0.5f);

      // 범위 내 적 감지
      Collider2D[] enemies = Physics2D.OverlapCircleAll(transform.position, range);
      foreach (var col in enemies)
      {
        if (col == null) continue; // (추가) 안전

        // (변경) 자식 콜라이더 구조 대응:
        // col이 Enemy 태그가 아닐 수 있으므로, 부모 태그도 허용
        bool isEnemy =
          col.CompareTag("Enemy") ||
          (col.transform.parent != null && col.transform.parent.CompareTag("Enemy"));

        if (!isEnemy) continue;

        // 부채꼴 범위 체크
        Vector3 dirToEnemy = (col.transform.position - transform.position).normalized;
        if (Vector3.Angle(forward, dirToEnemy) < angle / 2)
        {
          // 부채꼴 범위 안
          if (col.TryGetComponent<Character>(out var character))
          {
            var src = GetComponentInParent<WeaponSource>();
            character.TakeDamage(damage, src != null ? src.weaponData : null); // 즉시 피해
            if (character is Enemy) ApplyConfusion(col.gameObject);
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
      var src = GetComponentInParent<WeaponSource>();
      confusion.Initialize(damage, duration, src != null ? src.weaponData : null);

      // (추가) Lv5일 때 FrenzyDebuff 같이 적용
      if (enableFrenzyAtLv5 && currentLevel >= 5)
      {
        BuffUtil.Apply(enemyObj, new FrenzyDebuff(frenzyDuration)); // (추가)
      }
    }

    public void OnLevelUp(int level)
    {
      currentLevel = Mathf.Clamp(level, 1, 5); // (추가)

      if (baseDamage == 0 && damage > 0) baseDamage = damage;

      // 레벨업: 범위, 지속 시간 증가 + 데미지 10%씩 증가
      range = baseRange * (1f + (currentLevel - 1) * 0.15f);
      duration = baseDuration * (1f + (currentLevel - 1) * 0.2f);
      damage = baseDamage * (1f + (currentLevel - 1) * 0.1f);

      Debug.Log($"[DataScrambler] Lv.{currentLevel} : Dmg {damage}, Range {range}, Duration {duration}");
    }

    private Transform FindClosestEnemy()
    {
      GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
      GameObject closest = null;
      float closestDistance = range > 0 ? (range * 1.5f) : 10f;

      foreach (GameObject enemy in enemies)
      {
        if (enemy == null) continue;
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
    private WeaponBase sourceWeapon;

    public void Initialize(float dmg, float duration, WeaponBase source = null)
    {
      this.damage = dmg;
      this.timer = duration;
      this.sourceWeapon = source;
      this.initialized = true;

      // (변경) SpriteRenderer가 자식에 있을 수도 있으니 InChildren 사용
      var sr = GetComponentInChildren<SpriteRenderer>();
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
      Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 2.5f);
      foreach (var h in hits)
      {
        if (h.gameObject == gameObject) continue; // 나 자신 제외
        if (h.CompareTag("Enemy") && h.TryGetComponent<Enemy>(out var e))
        {
          e.TakeDamage(damage, sourceWeapon); // 광역 피해
        }
      }

      var sr = GetComponentInChildren<SpriteRenderer>();
      if (sr) sr.color = Color.white;

      Destroy(this);
    }
  }
}

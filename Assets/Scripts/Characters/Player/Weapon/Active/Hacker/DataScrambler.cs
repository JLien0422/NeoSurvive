using UnityEngine;
using System.Collections.Generic;
using NeoSurvive.Core;
using NeoSurvive.Buff;

namespace NeoSurvive.Weapon
{
  /// <summary>
  /// 3번 무기: 데이터 스크램블러
  /// 부채꼴 범위에 교란 신호 방사.
  /// 혼란에 걸린 적은 일정 시간 후 폭발하여 광역 피해.
  /// Lv5: 혼란 대상에게 Frenzy(광란) 디버프도 함께 적용
  /// </summary>
  public class DataScrambler : MonoBehaviour
  {
    [Header("Stats")]
    public float damage = 10f;
    public float range = 5f;
    public float angle = 60f;
    public float fireRate = 2f;
    public float duration = 3f;

    [Header("Lv5 Frenzy")]
    public bool enableFrenzyAtLv5 = true;
    public float frenzyDuration = 2.5f;

    [Header("Explosion")]
    public float explosionRadius = 2.5f; // ★ 추가: CSV explosionradius 적용용

    private float fireTimer;

    private int currentLevel = 1;

    // ★ 추가: CSV weaponid
    private readonly string weaponId = "datascrambler";

    private void Start()
    {
      Debug.Log("[DataScrambler] Initialized");

      // ★ 수정: 시작 시 Lv1 CSV 적용
      ApplyStatsFromCSV(1);
    }

    private void Update()
    {
      // ★ 추가: 기존 코드에 이게 빠져 있었음
      fireTimer += Time.deltaTime;

      if (fireTimer >= fireRate)
      {
        Attack();
        fireTimer = 0f;
      }
    }

    private void Attack()
    {
      Transform target = FindClosestEnemy();
      Vector3 forward = transform.right;

      if (target != null)
      {
        forward = (target.position - transform.position).normalized;
      }

      Debug.DrawRay(transform.position, Quaternion.Euler(0, 0, angle / 2) * forward * range, Color.magenta, 0.5f);
      Debug.DrawRay(transform.position, Quaternion.Euler(0, 0, -angle / 2) * forward * range, Color.magenta, 0.5f);

      Collider2D[] enemies = Physics2D.OverlapCircleAll(transform.position, range);

      foreach (var col in enemies)
      {
        if (col == null) continue;

        bool isEnemy =
          col.CompareTag("Enemy") ||
          (col.transform.parent != null && col.transform.parent.CompareTag("Enemy"));

        if (!isEnemy) continue;

        Vector3 dirToEnemy = (col.transform.position - transform.position).normalized;

        if (Vector3.Angle(forward, dirToEnemy) < angle / 2)
        {
          if (col.TryGetComponent<Character>(out var character))
          {
            var src = GetComponentInParent<WeaponSource>();
            character.TakeDamage(damage, src != null ? src.weaponData : null);

            if (character is Enemy)
              ApplyConfusion(col.gameObject);
          }
        }
      }
    }

    private void ApplyConfusion(GameObject enemyObj)
    {
      var confusion = enemyObj.GetComponent<ConfusionEffect>();
      if (confusion == null)
      {
        confusion = enemyObj.AddComponent<ConfusionEffect>();
      }

      var src = GetComponentInParent<WeaponSource>();

      // ★ 수정: explosionRadius도 같이 전달
      confusion.Initialize(damage, duration, explosionRadius, src != null ? src.weaponData : null);

      if (enableFrenzyAtLv5 && currentLevel >= 5)
      {
        BuffUtil.Apply(enemyObj, new FrenzyDebuff(frenzyDuration));
      }
    }

    public void OnLevelUp(int level)
    {
      currentLevel = Mathf.Clamp(level, 1, 5);

      // ★ 수정: 레벨업 시 CSV 재적용
      ApplyStatsFromCSV(currentLevel);

      Debug.Log($"[DataScrambler] Lv.{currentLevel} : Dmg {damage}, Range {range}, Duration {duration}, ExplosionRadius {explosionRadius}");
    }

    // ★ 추가: CSV 적용 함수
    private void ApplyStatsFromCSV(int level)
    {
      if (WeaponStatLoader.DB == null)
      {
        Debug.LogWarning("[DataScrambler] WeaponStatLoader.DB 없음");
        return;
      }

      if (!WeaponStatLoader.DB.rows.TryGetValue(weaponId, out var levelDict))
      {
        Debug.LogWarning($"[DataScrambler] weaponId 없음: {weaponId}");
        return;
      }

      if (!levelDict.TryGetValue(level, out var row))
      {
        Debug.LogWarning($"[DataScrambler] level 데이터 없음: {level}");
        return;
      }

      WeaponStatDB.Row levelOneRow = row;
      if (levelDict.TryGetValue(1, out var baseRow))
      {
        levelOneRow = baseRow;
      }

      float damagePerLevel = row.damageperlevel > 0f ? row.damageperlevel : levelOneRow.damageperlevel;
      float rangePerLevel = row.rangeperlevel > 0f ? row.rangeperlevel : levelOneRow.rangeperlevel;
      float durationPerLevel = row.durationperlevel > 0f ? row.durationperlevel : levelOneRow.durationperlevel;

      damage = levelOneRow.damage * (1f + (level - 1) * damagePerLevel);
      range = levelOneRow.range * (1f + (level - 1) * rangePerLevel);
      duration = levelOneRow.duration * (1f + (level - 1) * durationPerLevel);

      angle = row.angle;
      fireRate = row.firerate;
      frenzyDuration = row.frenzyduration;
      explosionRadius = row.explosionradius;

      Debug.Log($"[DataScrambler] CSV 적용 | Lv={level}, Damage={damage}, Range={range}, Duration={duration}, FireRate={fireRate}, ExplosionRadius={explosionRadius}");
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

  public class ConfusionEffect : MonoBehaviour
  {
    private float damage;
    private float timer;
    private float explosionRadius = 2.5f; // ★ 추가
    private bool initialized = false;
    private WeaponBase sourceWeapon;

    // ★ 수정: explosionRadius 인자 추가
    public void Initialize(float dmg, float duration, float explosionRadius, WeaponBase source = null)
    {
      this.damage = dmg;
      this.timer = duration;
      this.explosionRadius = explosionRadius;
      this.sourceWeapon = source;
      this.initialized = true;

      var sr = GetComponentInChildren<SpriteRenderer>();
      if (sr) sr.color = Color.magenta;
    }

    private void Update()
    {
      if (!initialized) return;

      timer -= Time.deltaTime;
      if (timer <= 0)
      {
        Explode();
      }
    }

    private void Explode()
    {
      // ★ 수정: 하드코딩 2.5f 대신 CSV explosionRadius 사용
      Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius);

      foreach (var h in hits)
      {
        if (h.gameObject == gameObject) continue;

        if (h.CompareTag("Enemy") && h.TryGetComponent<Enemy>(out var e))
        {
          e.TakeDamage(damage, sourceWeapon);
          continue;
        }
        // =========================
        // 2. 추가: IDamageable 맵오브젝트 폭발 데미지 처리
        // - 자판기 같은 맵오브젝트만 맞게 함
        // - Enemy는 위에서 이미 처리했으므로 여기서는 제외
        // =========================
        if (h.CompareTag("Enemy"))
          continue;

        IDamageable damageable = h.GetComponent<IDamageable>();

        if (damageable == null)
          damageable = h.GetComponentInParent<IDamageable>();

        if (damageable == null)
          continue;

        damageable.TakeDamage(damage);
      }

      var sr = GetComponentInChildren<SpriteRenderer>();
      if (sr) sr.color = Color.white;

      Destroy(this);
    }
  }
}
using UnityEngine;
using System.Collections.Generic;
using NeoSurvive.Core;
using NeoSurvive.Buff;

namespace NeoSurvive.Weapon
{
  public class DataScrambler : MonoBehaviour
  {
    [Header("Stats")]
    public float damage = 10f;
    public float range = 5f;
    public float angle = 60f;
    public float fireRate = 2f;
    public float duration = 3f;

    [Header("Frenzy / Lv5 Death Explosion")]
    public bool enableFrenzyAtLv5 = true;
    public float frenzyDuration = 2.5f;

    [Header("Explosion")]
    public float explosionRadius = 2.5f;

    [Header("Attack VFX")]
    public GameObject attackVfxPrefab;
    public float attackVfxDuration = 0.5f;
    public float attackVfxOffset = 1.0f;
    public float attackVfxScale = 1.0f;

    [Header("Explosion VFX")]
    public GameObject explosionVfxPrefab;
    public float explosionVfxLifetime = 0.7f;
    public float explosionVfxScale = 1.0f;

    private float fireTimer;
    private int currentLevel = 1;

    private readonly string weaponId = "datascrambler";

    private void Start()
    {
      Debug.Log("[DataScrambler] Initialized");
      ApplyStatsFromCSV(1);
    }

    private void Update()
    {
      fireTimer += Time.deltaTime;

      if (fireTimer >= fireRate)
      {
        Attack();
        fireTimer = 0f;
      }
    }

    private void Attack()
    {
      if (InGameSoundManager.Instance != null)
      {
        InGameSoundManager.Instance.PlayDataScramblerAttack();
      }

      Transform target = FindClosestEnemy();
      Vector3 forward = transform.right;

      if (target != null)
      {
        forward = (target.position - transform.position).normalized;
      }

      // ★ 공격 VFX 생성
      SpawnAttackVFX(forward);

      Debug.DrawRay(
        transform.position,
        Quaternion.Euler(0, 0, angle / 2) * forward * range,
        Color.magenta,
        0.5f);

      Debug.DrawRay(
        transform.position,
        Quaternion.Euler(0, 0, -angle / 2) * forward * range,
        Color.magenta,
        0.5f);

      Collider2D[] enemies = Physics2D.OverlapCircleAll(transform.position, range);

      foreach (var col in enemies)
      {
        if (col == null) continue;

        bool isEnemy =
          col.CompareTag("Enemy") ||
          (col.transform.parent != null &&
           col.transform.parent.CompareTag("Enemy"));

        if (!isEnemy) continue;

        Vector3 dirToEnemy =
          (col.transform.position - transform.position).normalized;

        if (Vector3.Angle(forward, dirToEnemy) < angle / 2)
        {
          if (col.TryGetComponent<Character>(out var character))
          {
            var src = GetComponentInParent<WeaponSource>();

            character.TakeDamage(
              damage,
              src != null ? src.weaponData : null);

            if (character is Enemy)
              ApplyConfusion(col.gameObject);
          }
        }
      }
    }

    // ★ 수정: 전방향 회전 대응 공격 VFX
    private void SpawnAttackVFX(Vector3 forward)
    {
      if (attackVfxPrefab == null) return;

      // 방향 예외 방지
      if (forward.sqrMagnitude <= 0.001f)
        forward = transform.right;

      forward.Normalize();

      Vector3 spawnPos =
        transform.position + forward * attackVfxOffset;

      // ★ 방향 회전 계산
      float angleZ =
        Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg;

      // ※ 만약 프리팹 기본 방향이 위쪽이면 -90f 추가
      // angleZ -= 90f;

      GameObject vfx = Instantiate(
        attackVfxPrefab,
        spawnPos,
        Quaternion.Euler(0f, 0f, angleZ)
      );

      // ★ 기존 좌우반전 제거
      vfx.transform.localScale =
        Vector3.one * attackVfxScale;

      Destroy(vfx, attackVfxDuration);
    }

    private void ApplyConfusion(GameObject enemyObj)
    {
      var confusion = enemyObj.GetComponent<ConfusionEffect>();

      if (confusion == null)
      {
        confusion = enemyObj.AddComponent<ConfusionEffect>();
      }

      var src = GetComponentInParent<WeaponSource>();

      confusion.Initialize(
        damage,
        duration,
        explosionRadius,
        enableFrenzyAtLv5 && currentLevel >= 5,
        src != null ? src.weaponData : null,
        explosionVfxPrefab,
        explosionVfxLifetime,
        explosionVfxScale
      );

      BuffUtil.Apply(
        enemyObj,
        new FrenzyDebuff(duration));
    }

    public void OnLevelUp(int level)
    {
      currentLevel = Mathf.Clamp(level, 1, 5);

      ApplyStatsFromCSV(currentLevel);

      Debug.Log(
        $"[DataScrambler] Lv.{currentLevel} : " +
        $"Dmg {damage}, Range {range}, " +
        $"Duration {duration}, ExplosionRadius {explosionRadius}");
    }

    private void ApplyStatsFromCSV(int level)
    {
      if (WeaponStatLoader.DB == null)
      {
        Debug.LogWarning("[DataScrambler] WeaponStatLoader.DB 없음");
        return;
      }

      if (!WeaponStatLoader.DB.rows.TryGetValue(
            weaponId,
            out var levelDict))
      {
        Debug.LogWarning(
          $"[DataScrambler] weaponId 없음: {weaponId}");
        return;
      }

      if (!levelDict.TryGetValue(level, out var row))
      {
        Debug.LogWarning(
          $"[DataScrambler] level 데이터 없음: {level}");
        return;
      }

      damage = row.damage;
      range = row.range;
      duration = row.duration;
      angle = row.angle;
      fireRate = row.firerate;
      frenzyDuration = row.frenzyduration;
      explosionRadius = row.explosionradius;

      Debug.Log(
        $"[DataScrambler] CSV 적용 | " +
        $"Lv={level}, Damage={damage}, " +
        $"Range={range}, Duration={duration}, " +
        $"FireRate={fireRate}, " +
        $"ExplosionRadius={explosionRadius}");
    }

    private Transform FindClosestEnemy()
    {
      GameObject[] enemies =
        GameObject.FindGameObjectsWithTag("Enemy");

      GameObject closest = null;

      float closestDistance =
        range > 0 ? (range * 1.5f) : 10f;

      foreach (GameObject enemy in enemies)
      {
        if (enemy == null) continue;

        float distance =
          Vector3.Distance(
            transform.position,
            enemy.transform.position);

        if (distance < closestDistance)
        {
          closestDistance = distance;
          closest = enemy;
        }
      }

      return closest != null
        ? closest.transform
        : null;
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
    private float explosionRadius = 2.5f;
    private bool explodeOnDeath;
    private bool initialized = false;
    private bool expired = false;
    private bool hasExploded = false;
    private WeaponBase sourceWeapon;
    private Enemy targetEnemy;

    // ★ 폭발 VFX
    private GameObject explosionVfxPrefab;
    private float explosionVfxLifetime;
    private float explosionVfxScale;

    public void Initialize(
      float dmg,
      float duration,
      float explosionRadius,
      bool explodeOnDeath,
      WeaponBase source = null,
      GameObject explosionVfxPrefab = null,
      float explosionVfxLifetime = 0.7f,
      float explosionVfxScale = 1.0f)
    {
      this.damage = dmg;
      this.timer = duration;
      this.explosionRadius = explosionRadius;
      this.explodeOnDeath = explodeOnDeath;
      this.sourceWeapon = source;
      this.targetEnemy = GetComponent<Enemy>();

      this.explosionVfxPrefab = explosionVfxPrefab;
      this.explosionVfxLifetime = explosionVfxLifetime;
      this.explosionVfxScale = explosionVfxScale;

      this.initialized = true;
      this.expired = false;
      this.hasExploded = false;

      var sr = GetComponentInChildren<SpriteRenderer>();

      if (sr)
        sr.color = Color.magenta;
    }

    private void Update()
    {
      if (!initialized) return;

      if (explodeOnDeath && targetEnemy != null && targetEnemy.IsDead)
      {
        Explode();
        return;
      }

      timer -= Time.deltaTime;

      if (timer <= 0)
      {
        expired = true;
        Destroy(this);
      }
    }

    private void OnDestroy()
    {
      if (!initialized || expired || hasExploded)
        return;

      if (explodeOnDeath && targetEnemy != null && targetEnemy.IsDead)
        Explode();
    }

    private void Explode()
    {
      if (hasExploded)
        return;

      hasExploded = true;

      // ★ 혼란 폭발 VFX 생성
      SpawnExplosionVFX();

      Collider2D[] hits =
        Physics2D.OverlapCircleAll(
          transform.position,
          explosionRadius);

      foreach (var h in hits)
      {
        if (h.gameObject == gameObject)
          continue;

        if (h.CompareTag("Enemy") &&
            h.TryGetComponent<Enemy>(out var e))
        {
          e.TakeDamage(damage, sourceWeapon);
          continue;
        }

        if (h.CompareTag("Enemy"))
          continue;

        IDamageable damageable =
          h.GetComponent<IDamageable>();

        if (damageable == null)
        {
          damageable =
            h.GetComponentInParent<IDamageable>();
        }

        if (damageable == null)
          continue;

        damageable.TakeDamage(damage);
      }

      var sr = GetComponentInChildren<SpriteRenderer>();

      if (sr)
        sr.color = Color.white;

      Destroy(this);
    }

    // ★ 혼란 폭발 VFX 생성
    private void SpawnExplosionVFX()
    {
      if (explosionVfxPrefab == null)
        return;

      GameObject vfx = Instantiate(
        explosionVfxPrefab,
        transform.position,
        Quaternion.identity
      );

      vfx.transform.localScale =
        Vector3.one * explosionVfxScale;

      Destroy(vfx, explosionVfxLifetime);
    }
  }
}
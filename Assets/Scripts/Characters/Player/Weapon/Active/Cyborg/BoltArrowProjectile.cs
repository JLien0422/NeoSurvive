using UnityEngine;
using NeoSurvive.Core;

namespace NeoSurvive.Weapon
{
  /// <summary>
  /// 볼트 런처 유도 화살
  /// - 가장 가까운 적을 향해 유도
  /// - 적중 시 피해
  /// - (Lv5) 과부하 스택 부여, 5스택 시 폭발
  /// </summary>
  public class BoltArrowProjectile : MonoBehaviour
  {
    private Vector3 dir;
    private float damage;
    private float speed;
    private float maxDistance;
    private float turnSpeed;

    private LayerMask enemyMask;
    private string enemyTag;

    private bool master;
    private int stacksToExplode;
    private float explosionRadius;
    private float explosionDamageFactor;

    private Vector3 startPos;
    private WeaponBase sourceWeapon;

    [Header("Collision")]
    public bool useTrigger = true;

    public void Initialize(
      Vector3 dir,
      float damage,
      float speed,
      float maxDistance,
      float turnSpeed,
      LayerMask enemyMask,
      string enemyTag,
      bool master,
      int stacksToExplode,
      float explosionRadius,
      float explosionDamageFactor)
    {
      this.dir = dir.normalized;
      this.damage = damage;
      this.speed = speed;
      this.maxDistance = maxDistance;
      this.turnSpeed = turnSpeed;

      this.enemyMask = enemyMask;
      this.enemyTag = enemyTag;

      this.master = master;
      this.stacksToExplode = stacksToExplode;
      this.explosionRadius = explosionRadius;
      this.explosionDamageFactor = explosionDamageFactor;

      startPos = transform.position;
    }

    public void SetSourceWeapon(WeaponBase weapon)
    {
      sourceWeapon = weapon;
    }

    private void Update()
    {
      // 유도: 가장 가까운 적을 향해 회전
      Transform target = FindClosestEnemy(maxDistance);
      if (target != null)
      {
        Vector3 desired = (target.position - transform.position).normalized;
        float maxStepRad = (turnSpeed * Mathf.Deg2Rad) * Time.deltaTime;
        dir = Vector3.RotateTowards(dir, desired, maxStepRad, 0f).normalized;
      }

      transform.position += dir * (speed * Time.deltaTime);

      if (Vector3.Distance(startPos, transform.position) >= maxDistance)
      {
        Destroy(gameObject);
      }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
      if (!useTrigger) return;
      HandleHit(other);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
      if (useTrigger) return;
      HandleHit(collision.collider);
    }

    private void HandleHit(Collider2D other)
    {
      if (other == null) return;

      // 적 레이어만
      if (((1 << other.gameObject.layer) & enemyMask.value) == 0) return;

      // 태그(자식 콜라이더 구조 고려)
      if (!string.IsNullOrEmpty(enemyTag) && !other.CompareTag(enemyTag))
      {
        if (other.transform.parent == null || !other.transform.parent.CompareTag(enemyTag))
          return;
      }

      Enemy enemy = other.GetComponentInParent<Enemy>();
      if (enemy == null) return;

      // 기본 피해
      enemy.TakeDamage(damage, sourceWeapon);

      // 마스터: 과부하 스택
      if (master)
      {
        ApplyOverload(enemy.gameObject);
      }

      Destroy(gameObject);
    }

    private void ApplyOverload(GameObject enemyObj)
    {
      var stack = enemyObj.GetComponent<OverloadStack>();
      if (stack == null) stack = enemyObj.AddComponent<OverloadStack>();

      stack.AddStack(
        1,
        stacksToExplode,
        explosionRadius,
        damage * explosionDamageFactor,
        enemyMask,
        enemyTag,
        sourceWeapon
      );
    }

    private Transform FindClosestEnemy(float searchRange)
    {
      GameObject[] enemies = GameObject.FindGameObjectsWithTag(enemyTag);
      GameObject closest = null;

      float minDist = (searchRange > 0f) ? searchRange : 10f;
      foreach (var e in enemies)
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
  }
}

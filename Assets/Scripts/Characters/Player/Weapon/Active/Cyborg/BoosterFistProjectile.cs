using UnityEngine;
using NeoSurvive.Core;

namespace NeoSurvive.Weapon
{
  /// <summary>
  /// 부스터 너클 투사체
  /// - 직진 이동
  /// - 적 충돌 시 피해 후 파괴
  /// - 벽 충돌 시: (Lv5 마스터) 튕김 + 가장 가까운 적 유도
  /// </summary>
  public class BoosterFistProjectile : MonoBehaviour
  {
    [Header("Runtime")]
    public float damage;
    public float speed;
    public float maxDistance;

    [Header("Filter")]
    public LayerMask enemyMask;
    public string enemyTag = "Enemy";

    [Header("Collision")]
    public LayerMask wallMask;     // 벽 레이어(프로젝트에 맞게 설정)
    public bool useTrigger = true; // 콜라이더가 Trigger라면 true

    [Header("Master Homing")]
    public bool masterHoming = false;
    public float homingTurnSpeed = 360f; // deg/sec
    public float homingSearchRange = 10f;

    private Vector3 dir;
    private Vector3 startPos;
    private bool bounced = false;
    private WeaponBase sourceWeapon;

    public void Initialize(Vector3 dir, float damage, float speed, float maxDistance,
      LayerMask enemyMask, string enemyTag, bool masterHoming)
    {
      this.dir = dir.normalized;
      this.damage = damage;
      this.speed = speed;
      this.maxDistance = maxDistance;
      this.enemyMask = enemyMask;
      this.enemyTag = enemyTag;
      this.masterHoming = masterHoming;

      startPos = transform.position;
    }

    public void SetSourceWeapon(WeaponBase weapon)
    {
      sourceWeapon = weapon;
    }

    private void Update()
    {
      // 마스터 유도: 1회 튕긴 뒤부터 가장 가까운 적 방향으로 서서히 회전
      if (masterHoming && bounced)
      {
        Transform target = FindClosestEnemy(homingSearchRange);
        if (target != null)
        {
          Vector3 desired = (target.position - transform.position).normalized;
          float maxStep = homingTurnSpeed * Time.deltaTime;
          dir = Vector3.RotateTowards(dir, desired, maxStep * Mathf.Deg2Rad, 0f).normalized;
        }
      }

      transform.position += dir * (speed * Time.deltaTime);

      // 거리 제한
      if (Vector3.Distance(startPos, transform.position) >= maxDistance)
      {
        Destroy(gameObject);
      }
    }

    // Trigger 기반(권장)
    private void OnTriggerEnter2D(Collider2D other)
    {
      if (!useTrigger) return;
      HandleHit(other);
    }

    // Collision 기반(필요하면 사용)
    private void OnCollisionEnter2D(Collision2D collision)
    {
      if (useTrigger) return;
      HandleHit(collision.collider);
    }

    private void HandleHit(Collider2D other)
    {
      if (other == null) return;

      // 1) 벽 충돌
      if (((1 << other.gameObject.layer) & wallMask.value) != 0)
      {
        // 일반: 그냥 파괴 / 마스터: 튕김
        if (!masterHoming)
        {
          Destroy(gameObject);
          return;
        }

        // 튕김: 현재 진행방향 반전(간단 반사)
        dir = -dir;
        bounced = true;
        return;
      }

      // 2) 적 충돌
      if (((1 << other.gameObject.layer) & enemyMask.value) == 0) return;

      if (!string.IsNullOrEmpty(enemyTag) && !other.CompareTag(enemyTag))
      {
        if (other.transform.parent == null || !other.transform.parent.CompareTag(enemyTag))
          return;
      }

      Enemy enemy = other.GetComponentInParent<Enemy>();
      if (enemy != null)
      {
        enemy.TakeDamage(damage, sourceWeapon);
        Destroy(gameObject);
      }
    }

    private Transform FindClosestEnemy(float searchRange)
    {
      GameObject[] enemies = GameObject.FindGameObjectsWithTag(enemyTag);
      GameObject closest = null;
      float minDist = searchRange > 0 ? searchRange : 10f;

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

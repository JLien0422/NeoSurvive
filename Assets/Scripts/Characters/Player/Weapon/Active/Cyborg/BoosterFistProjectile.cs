using System.Collections.Generic;
using UnityEngine;
using NeoSurvive.Core;

namespace NeoSurvive.Weapon
{
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
    public LayerMask wallMask;

    [Header("Master Homing")]
    public bool masterHoming = false;
    public float homingTurnSpeed = 360f;
    public float homingSearchRange = 10f;

    [Header("Hit Detection")]
    public float hitRadius = 0.1f; // ★ 수정: 플레이어 주변 오검출 방지용으로 축소

    [Header("Rotation")]
    public float spriteAngleOffset = 0f;

    private const string weaponId = "boosterknuckle";
    private int currentLevel = 1;

    private GameObject owner;
    private Transform ownerRoot;

    private Vector3 dir;
    private Vector3 startPos;

    private bool bounced = false;
    private bool destroyed = false;

    private Transform lastHitEnemy;
    private WeaponBase sourceWeapon;

    private void Awake()
    {
      // ★ 물리 충돌은 사용하지 않음
      // 데미지 판정은 Physics2D.OverlapCircleAll로 직접 처리
      Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true);
      foreach (Collider2D col in colliders)
      {
        if (col == null) continue;
        col.enabled = false;
      }

      Rigidbody2D[] rigidbodies = GetComponentsInChildren<Rigidbody2D>(true);
      foreach (Rigidbody2D rb in rigidbodies)
      {
        if (rb == null) continue;
        rb.simulated = false;
      }
    }

    public void Initialize(
      GameObject owner,
      Vector3 dir,
      float damage,
      float speed,
      float maxDistance,
      LayerMask enemyMask,
      string enemyTag,
      bool masterHoming)
    {
      this.owner = owner;
      this.ownerRoot = owner != null ? owner.transform.root : null;

      this.dir = dir.normalized;
      this.damage = damage;
      this.speed = speed;
      this.maxDistance = maxDistance;
      this.enemyMask = enemyMask;
      this.enemyTag = enemyTag;
      this.masterHoming = masterHoming;

      startPos = transform.position;

      RotateToDirection();
    }

    public void SetSourceWeapon(WeaponBase weapon)
    {
      sourceWeapon = weapon;
    }

    public void ApplyStatsFromCSV(int level)
    {
      currentLevel = Mathf.Clamp(level, 1, 5);

      if (WeaponStatLoader.DB == null)
        return;

      if (!WeaponStatLoader.DB.rows.TryGetValue(weaponId, out var weaponLevels))
        return;

      if (!weaponLevels.TryGetValue(currentLevel, out var row))
        return;

      if (row.homingturnspeed > 0f)
        homingTurnSpeed = row.homingturnspeed;

      if (row.homingsearchrange > 0f)
        homingSearchRange = row.homingsearchrange;
    }

    private void Update()
    {
      if (destroyed)
        return;

      if (masterHoming && bounced)
      {
        Transform target = FindRandomEnemy(homingSearchRange, lastHitEnemy);

        if (target != null)
        {
          Vector3 desired = (target.position - transform.position).normalized;
          float step = homingTurnSpeed * Mathf.Deg2Rad * Time.deltaTime;
          dir = Vector3.RotateTowards(dir, desired, step, 0f).normalized;
        }
      }

      RotateToDirection();

      transform.position += dir * speed * Time.deltaTime;

      CheckHitByOverlap();

      if (Vector3.Distance(startPos, transform.position) >= maxDistance)
      {
        DestroyProjectile();
      }
    }

    private void CheckHitByOverlap()
    {
      // ★ 핵심 수정:
      // 전체 Collider를 검사하지 않고 enemyMask에 포함된 Collider만 검사
      // 그래서 Player, Weapon, VFX, 기타 오브젝트를 건드리지 않음
      Collider2D[] hits = Physics2D.OverlapCircleAll(
        transform.position,
        hitRadius,
        enemyMask
      );

      foreach (Collider2D hit in hits)
      {
        if (hit == null)
          continue;

        if (owner != null)
        {
          if (hit.gameObject == owner)
            continue;

          if (hit.transform.root == owner.transform.root)
            continue;
        }

        if (ownerRoot != null && hit.transform.root == ownerRoot)
          continue;

        if (!IsEnemy(hit))
          continue;

        Character character = hit.GetComponent<Character>();

        if (character == null)
          character = hit.GetComponentInParent<Character>();

        if (character == null)
          continue;

        HandleEnemyHit(character);
        return;
      }

      // ★ 벽은 별도 Mask가 있을 때만 검사
      if (wallMask.value != 0)
      {
        Collider2D[] wallHits = Physics2D.OverlapCircleAll(
          transform.position,
          hitRadius,
          wallMask
        );

        foreach (Collider2D wall in wallHits)
        {
          if (wall == null)
            continue;

          HandleWallBounce();
          return;
        }
      }
    }

    private void HandleEnemyHit(Character character)
    {
      if (character == null)
        return;

      character.TakeDamage(Mathf.Max(1f, damage), sourceWeapon);

      if (!masterHoming)
      {
        DestroyProjectile();
        return;
      }

      if (!bounced)
      {
        bounced = true;
        lastHitEnemy = character.transform;

        dir = -dir;

        Transform target = FindRandomEnemy(homingSearchRange, lastHitEnemy);

        if (target != null)
        {
          dir = (target.position - transform.position).normalized;
        }

        RotateToDirection();
        return;
      }

      DestroyProjectile();
    }

    private void HandleWallBounce()
    {
      if (!masterHoming)
      {
        DestroyProjectile();
        return;
      }

      if (!bounced)
      {
        bounced = true;
        dir = -dir;

        Transform target = FindRandomEnemy(homingSearchRange, null);

        if (target != null)
        {
          dir = (target.position - transform.position).normalized;
        }

        RotateToDirection();
        return;
      }

      DestroyProjectile();
    }

    private void RotateToDirection()
    {
      if (dir.sqrMagnitude <= 0.0001f)
        return;

      float angleZ = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + spriteAngleOffset;
      transform.rotation = Quaternion.Euler(0f, 0f, angleZ);
    }

    private bool IsEnemy(Collider2D other)
    {
      if (other == null)
        return false;

      if (!string.IsNullOrEmpty(enemyTag))
      {
        if (other.CompareTag(enemyTag))
          return true;

        if (other.transform.parent != null && other.transform.parent.CompareTag(enemyTag))
          return true;
      }

      Character character = other.GetComponent<Character>();

      if (character == null)
        character = other.GetComponentInParent<Character>();

      if (character == null)
        return false;

      if (!string.IsNullOrEmpty(enemyTag) && !character.CompareTag(enemyTag))
        return false;

      return true;
    }

    private Transform FindRandomEnemy(float searchRange, Transform excludeTarget)
    {
      Collider2D[] hits = Physics2D.OverlapCircleAll(
        transform.position,
        searchRange,
        enemyMask
      );

      List<Transform> candidates = new List<Transform>();

      foreach (Collider2D hit in hits)
      {
        if (hit == null)
          continue;

        if (!IsEnemy(hit))
          continue;

        Character character = hit.GetComponent<Character>();

        if (character == null)
          character = hit.GetComponentInParent<Character>();

        if (character == null)
          continue;

        Transform target = character.transform;

        if (excludeTarget != null && target == excludeTarget)
          continue;

        if (!candidates.Contains(target))
          candidates.Add(target);
      }

      if (candidates.Count == 0)
        return null;

      return candidates[Random.Range(0, candidates.Count)];
    }

    private void DestroyProjectile()
    {
      if (destroyed)
        return;

      destroyed = true;
      Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
      Gizmos.color = Color.yellow;
      Gizmos.DrawWireSphere(transform.position, hitRadius);
    }
  }
}
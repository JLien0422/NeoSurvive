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
    public float hitRadius = 0.1f;

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

    private Transform lastHitTarget;
    private WeaponBase sourceWeapon;

    private readonly HashSet<int> hitIds = new HashSet<int>();

    private void Awake()
    {
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
        Transform target = FindRandomTarget(homingSearchRange, lastHitTarget);

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
      Collider2D[] hits = Physics2D.OverlapCircleAll(
        transform.position,
        hitRadius
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

        if (TryHitTarget(hit))
          return;
      }

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

    private bool TryHitTarget(Collider2D hit)
    {
      if (hit == null)
        return false;

      bool isEnemy = IsEnemy(hit);

      IDamageable damageable = hit.GetComponent<IDamageable>();
      if (damageable == null)
        damageable = hit.GetComponentInParent<IDamageable>();

      bool isInEnemyMask =
        enemyMask.value == 0 ||
        ((1 << hit.gameObject.layer) & enemyMask.value) != 0;

      if (!isInEnemyMask && damageable == null)
        return false;

      if (!isEnemy && damageable == null)
        return false;

      Character character = hit.GetComponent<Character>();
      if (character == null)
        character = hit.GetComponentInParent<Character>();

      if (character != null && isEnemy)
      {
        int id = character.gameObject.GetInstanceID();

        if (hitIds.Contains(id))
          return false;

        hitIds.Add(id);

        HandleCharacterHit(character);
        return true;
      }

      if (damageable != null)
      {
        MonoBehaviour mb = damageable as MonoBehaviour;

        if (mb == null)
          return false;

        int id = mb.gameObject.GetInstanceID();

        if (hitIds.Contains(id))
          return false;

        hitIds.Add(id);

        HandleDamageableHit(damageable, mb.transform);
        return true;
      }

      return false;
    }

    private void HandleCharacterHit(Character character)
    {
      if (character == null)
        return;

      character.TakeDamage(Mathf.Max(1f, damage), sourceWeapon);

      HandleAfterHit(character.transform);
    }

    private void HandleDamageableHit(IDamageable damageable, Transform targetTransform)
    {
      if (damageable == null)
        return;

      damageable.TakeDamage(Mathf.Max(1f, damage));

      HandleAfterHit(targetTransform);
    }

    private void HandleAfterHit(Transform hitTarget)
    {
      if (!masterHoming)
      {
        DestroyProjectile();
        return;
      }

      if (!bounced)
      {
        bounced = true;
        lastHitTarget = hitTarget;

        dir = -dir;

        Transform target = FindRandomTarget(homingSearchRange, lastHitTarget);

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

        Transform target = FindRandomTarget(homingSearchRange, null);

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

    private Transform FindRandomTarget(float searchRange, Transform excludeTarget)
    {
      Collider2D[] hits = Physics2D.OverlapCircleAll(
        transform.position,
        searchRange
      );

      List<Transform> candidates = new List<Transform>();

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

        bool isEnemy = IsEnemy(hit);

        IDamageable damageable = hit.GetComponent<IDamageable>();
        if (damageable == null)
          damageable = hit.GetComponentInParent<IDamageable>();

        bool isInEnemyMask =
          enemyMask.value == 0 ||
          ((1 << hit.gameObject.layer) & enemyMask.value) != 0;

        if (!isInEnemyMask && damageable == null)
          continue;

        if (!isEnemy && damageable == null)
          continue;

        Transform target = hit.transform;

        Character character = hit.GetComponent<Character>();
        if (character == null)
          character = hit.GetComponentInParent<Character>();

        if (character != null)
          target = character.transform;
        else if (damageable is Component damageableComponent)
          target = damageableComponent.transform;

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
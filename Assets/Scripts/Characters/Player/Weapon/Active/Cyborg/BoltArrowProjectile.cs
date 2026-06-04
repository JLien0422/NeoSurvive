using System.Collections.Generic;
using UnityEngine;
using NeoSurvive.Core;

namespace NeoSurvive.Weapon
{
  public class BoltArrowProjectile : MonoBehaviour
  {
    [Header("Runtime")]
    private Vector3 dir;
    private float damage;
    private float speed;
    private float maxDistance;
    private float turnSpeed;

    [Header("Filter")]
    private LayerMask enemyMask;
    private string enemyTag;

    [Header("Master")]
    private bool master;
    private int stacksToExplode;
    private float explosionRadius;
    private float explosionDamageFactor;

    [Header("Hit Detection")]
    public float hitRadius = 0.1f;

    [Header("Rotation")]
    public float spriteAngleOffset = 0f;

    private Vector3 startPos;
    private bool destroyed = false;

    private GameObject owner;
    private Transform ownerRoot;
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
      float turnSpeed,
      LayerMask enemyMask,
      string enemyTag,
      bool master,
      int stacksToExplode,
      float explosionRadius,
      float explosionDamageFactor)
    {
      this.owner = owner;
      this.ownerRoot = owner != null ? owner.transform.root : null;

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

      RotateToDirection();
    }

    public void SetSourceWeapon(WeaponBase weapon)
    {
      sourceWeapon = weapon;
    }

    private void Update()
    {
      if (destroyed)
        return;

      Transform target = FindClosestTarget(maxDistance);

      if (target != null)
      {
        Vector3 desired = (target.position - transform.position).normalized;
        float maxStepRad = turnSpeed * Mathf.Deg2Rad * Time.deltaTime;
        dir = Vector3.RotateTowards(dir, desired, maxStepRad, 0f).normalized;
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

        HandleDamageableHit(damageable);
        return true;
      }

      return false;
    }

    private void HandleCharacterHit(Character character)
    {
      if (character == null)
        return;

      character.TakeDamage(Mathf.Max(1f, damage), sourceWeapon);

      if (master)
      {
        Enemy enemy = character.GetComponent<Enemy>();

        if (enemy == null)
          enemy = character.GetComponentInParent<Enemy>();

        if (enemy != null)
          ApplyOverload(enemy.gameObject);
      }

      DestroyProjectile();
    }

    private void HandleDamageableHit(IDamageable damageable)
    {
      if (damageable == null)
        return;

      damageable.TakeDamage(Mathf.Max(1f, damage));

      // 자판기 같은 IDamageable에는 OverloadStack을 붙이지 않음
      DestroyProjectile();
    }

    private void ApplyOverload(GameObject enemyObj)
    {
      if (enemyObj == null)
        return;

      OverloadStack stack = enemyObj.GetComponent<OverloadStack>();

      if (stack == null)
        stack = enemyObj.AddComponent<OverloadStack>();

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

    private Transform FindClosestTarget(float searchRange)
    {
      Collider2D[] hits = Physics2D.OverlapCircleAll(
        transform.position,
        searchRange
      );

      Transform closest = null;
      float minDist = searchRange > 0f ? searchRange : 10f;

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

        float dist = Vector3.Distance(transform.position, target.position);

        if (dist < minDist)
        {
          minDist = dist;
          closest = target;
        }
      }

      return closest;
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

    private void RotateToDirection()
    {
      if (dir.sqrMagnitude <= 0.0001f)
        return;

      float angleZ = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + spriteAngleOffset;
      transform.rotation = Quaternion.Euler(0f, 0f, angleZ);
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
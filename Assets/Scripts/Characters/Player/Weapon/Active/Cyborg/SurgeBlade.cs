using System.Collections.Generic;
using UnityEngine;
using NeoSurvive.Core;

namespace NeoSurvive.Weapon
{
  public class SurgeBlade : MonoBehaviour
  {
    [Header("Stats")]
    public float damage = 20f;
    public float range = 3.0f;
    public float hitWidth = 0.8f;
    public float fireRate = 1.2f;

    [Header("Target Filter")]
    public LayerMask hitMask;
    public string enemyTag = "Enemy";

    [Header("Level Scaling")]
    public float rangePerLevel = 0.05f;
    public int baseMaxTargets = 1;
    public int maxTargetsAtLv5 = 3;

    [Header("VFX")]
    public GameObject surgePrefab;
    public GameObject surgePrefabEnhanced;
    public float surgeDuration = 0.35f;
    public float surgeSpawnOffset = 0.6f;
    public float surgeScale = 1.0f;

    [Header("Master (Lv5)")]
    public bool enableMaster = true;
    public float masterMultiplier = 2.0f;
    public bool debugDraw = true;

    private float fireTimer;

    private float baseRange;
    private float baseFireRate;

    private const string weaponId = "surgeblade";
    private int currentLevel = 1;
    private int currentMaxTargets = 1;

    private int attackCount = 0;

    private void Start()
    {
      baseRange = range;
      baseFireRate = fireRate;

      ApplyLevel(1);
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

    public void OnLevelUp(int level)
    {
      if (baseRange <= 0f && range > 0f) baseRange = range;
      if (baseFireRate <= 0f && fireRate > 0f) baseFireRate = fireRate;

      ApplyLevel(level);

      Debug.Log($"[SurgeBlade] Lv.{currentLevel} -> Dmg:{damage}, Range:{range}, Width:{hitWidth}, MaxTargets:{currentMaxTargets}");
    }

    private void ApplyLevel(int level)
    {
      currentLevel = Mathf.Clamp(level, 1, 5);

      ApplyStatsFromCSV(currentLevel);

      range = baseRange * (1f + (currentLevel - 1) * rangePerLevel);

      if (currentLevel <= 1)
        currentMaxTargets = baseMaxTargets;
      else if (currentLevel <= 3)
        currentMaxTargets = Mathf.Min(2, maxTargetsAtLv5);
      else
        currentMaxTargets = maxTargetsAtLv5;
    }

    private void ApplyStatsFromCSV(int level)
    {
      if (WeaponStatLoader.DB == null)
        return;

      if (!WeaponStatLoader.DB.rows.TryGetValue(weaponId, out var levelDict))
        return;

      if (!levelDict.TryGetValue(level, out var row))
        return;

      if (levelDict.TryGetValue(1, out var baseRow))
      {
        if (baseRow.range > 0f) baseRange = baseRow.range;
        if (baseRow.firerate > 0f) baseFireRate = baseRow.firerate;
        if (baseRow.rangeperlevel > 0f) rangePerLevel = baseRow.rangeperlevel;
        if (baseRow.basemaxtargets > 0) baseMaxTargets = baseRow.basemaxtargets;
        if (baseRow.maxtargetsatlv5 > 0) maxTargetsAtLv5 = baseRow.maxtargetsatlv5;
        if (baseRow.mastermultiplier > 0f) masterMultiplier = baseRow.mastermultiplier;
        if (baseRow.width > 0f) hitWidth = baseRow.width;
      }

      if (row.damage > 0f) damage = row.damage;
      if (row.firerate > 0f) fireRate = row.firerate;
      if (row.width > 0f) hitWidth = row.width;
    }

    private void Attack()
    {
      attackCount++;

      if (InGameSoundManager.Instance != null)
        InGameSoundManager.Instance.PlaySurgeBladeFire();

      float useRange = range;
      float useDamage = damage;

      bool empowered = false;

      if (enableMaster && currentLevel >= 5 && attackCount % 2 == 0)
      {
        empowered = true;
        useRange *= masterMultiplier;
        useDamage *= masterMultiplier;
      }

      Transform target = FindClosestTarget(useRange * 1.5f);

      Vector3 origin = transform.position;
      Vector3 forward = transform.parent != null ? transform.parent.right : transform.right;

      if (target != null)
        forward = (target.position - origin).normalized;

      BuildThrustBox(origin, forward, useRange, out Vector2 boxCenter, out Vector2 boxSize, out float boxAngleZ);

      if (debugDraw)
        DrawDebugBox(boxCenter, boxSize, boxAngleZ, empowered ? Color.yellow : Color.cyan, 0.2f);

      Collider2D[] hits = Physics2D.OverlapBoxAll(boxCenter, boxSize, boxAngleZ);

      System.Array.Sort(hits, (a, b) =>
      {
        if (a == null && b == null) return 0;
        if (a == null) return 1;
        if (b == null) return -1;

        float da = ((Vector2)a.transform.position - (Vector2)origin).sqrMagnitude;
        float db = ((Vector2)b.transform.position - (Vector2)origin).sqrMagnitude;
        return da.CompareTo(db);
      });

      int damaged = 0;
      HashSet<int> processedIds = new HashSet<int>();

      foreach (var col in hits)
      {
        if (col == null) continue;

        bool isEnemy = IsEnemyCollider(col);

        IDamageable damageable = col.GetComponent<IDamageable>();
        if (damageable == null)
          damageable = col.GetComponentInParent<IDamageable>();

        bool isInHitMask =
          hitMask.value == 0 ||
          ((1 << col.gameObject.layer) & hitMask.value) != 0;

        // Enemy는 hitMask 기준 유지
        // 자판기 같은 IDamageable은 hitMask 밖이어도 허용
        if (!isInHitMask && damageable == null)
          continue;

        // Enemy도 아니고 IDamageable도 아니면 무시
        if (!isEnemy && damageable == null)
          continue;

        if (isEnemy)
        {
          Character character = col.GetComponentInParent<Character>();

          if (character != null)
          {
            int id = character.gameObject.GetInstanceID();

            if (processedIds.Contains(id))
              continue;

            processedIds.Add(id);

            var src = GetComponentInParent<WeaponSource>();
            character.TakeDamage(useDamage, src != null ? src.weaponData : null);

            damaged++;

            if (damaged >= currentMaxTargets)
              break;

            continue;
          }
        }

        if (damageable != null)
        {
          MonoBehaviour mb = damageable as MonoBehaviour;

          if (mb != null)
          {
            int id = mb.gameObject.GetInstanceID();

            if (processedIds.Contains(id))
              continue;

            processedIds.Add(id);
          }

          damageable.TakeDamage(useDamage);

          damaged++;

          if (damaged >= currentMaxTargets)
            break;
        }
      }

      SpawnSurgeEffect(forward, empowered);
    }

    private void BuildThrustBox(
      Vector3 origin,
      Vector3 forward,
      float useRange,
      out Vector2 center,
      out Vector2 size,
      out float angleZ)
    {
      Vector2 dir = forward;
      center = (Vector2)origin + dir * (useRange * 0.5f);
      size = new Vector2(useRange, hitWidth);
      angleZ = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
    }

    private static void DrawDebugBox(Vector2 center, Vector2 size, float angleZ, Color color, float duration)
    {
      float rad = angleZ * Mathf.Deg2Rad;
      Vector2 right = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
      Vector2 up = new Vector2(-right.y, right.x);
      Vector2 hx = right * (size.x * 0.5f);
      Vector2 hy = up * (size.y * 0.5f);

      Vector2 c0 = center - hx - hy;
      Vector2 c1 = center + hx - hy;
      Vector2 c2 = center + hx + hy;
      Vector2 c3 = center - hx + hy;

      Debug.DrawLine(c0, c1, color, duration);
      Debug.DrawLine(c1, c2, color, duration);
      Debug.DrawLine(c2, c3, color, duration);
      Debug.DrawLine(c3, c0, color, duration);
    }

    private bool IsEnemyCollider(Collider2D col)
    {
      if (col == null) return false;
      if (string.IsNullOrEmpty(enemyTag)) return false;

      return
        col.CompareTag(enemyTag) ||
        (col.transform.parent != null && col.transform.parent.CompareTag(enemyTag));
    }

    private void SpawnSurgeEffect(Vector3 forward, bool empowered)
    {
      GameObject prefab =
        empowered && surgePrefabEnhanced != null
        ? surgePrefabEnhanced
        : surgePrefab;

      if (prefab == null) return;

      Vector3 spawnPos = transform.position + forward * surgeSpawnOffset;
      float angleDeg = Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg;

      GameObject go = Instantiate(
        prefab,
        spawnPos,
        Quaternion.Euler(0f, 0f, angleDeg)
      );

      go.transform.localScale = Vector3.one * surgeScale;

      Destroy(go, surgeDuration);
    }

    private Transform FindClosestTarget(float searchRange)
    {
      Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, searchRange);

      Transform closest = null;
      float minDist = searchRange > 0f ? searchRange : 10f;

      foreach (Collider2D hit in hits)
      {
        if (hit == null) continue;

        bool isEnemy = IsEnemyCollider(hit);

        bool isInHitMask =
          hitMask.value == 0 ||
          ((1 << hit.gameObject.layer) & hitMask.value) != 0;

        IDamageable damageable = hit.GetComponent<IDamageable>();
        if (damageable == null)
          damageable = hit.GetComponentInParent<IDamageable>();

        if ((!isEnemy || !isInHitMask) && damageable == null)
          continue;

        Transform targetTransform = hit.transform;

        Enemy enemy = hit.GetComponentInParent<Enemy>();

        if (enemy != null)
          targetTransform = enemy.transform;
        else if (damageable is Component damageableComponent)
          targetTransform = damageableComponent.transform;

        float distance = Vector3.Distance(transform.position, targetTransform.position);

        if (distance < minDist)
        {
          minDist = distance;
          closest = targetTransform;
        }
      }

      return closest;
    }

    private void OnDrawGizmosSelected()
    {
      Vector3 origin = transform.position;
      Vector3 forward = transform.parent != null ? transform.parent.right : transform.right;
      BuildThrustBox(origin, forward, range, out Vector2 center, out Vector2 size, out float angleZ);

      Gizmos.color = Color.yellow;
      Matrix4x4 old = Gizmos.matrix;
      Gizmos.matrix = Matrix4x4.TRS(center, Quaternion.Euler(0f, 0f, angleZ), Vector3.one);
      Gizmos.DrawWireCube(Vector3.zero, new Vector3(size.x, size.y, 0.1f));
      Gizmos.matrix = old;
    }
  }
}

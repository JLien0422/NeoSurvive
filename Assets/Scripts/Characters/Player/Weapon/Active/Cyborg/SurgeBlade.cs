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
    public float angle = 50f;
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
    private float baseAngle;
    private float baseFireRate;

    private const string weaponId = "surgeblade";
    private int currentLevel = 1;
    private int currentMaxTargets = 1;

    private int attackCount = 0;

    private void Start()
    {
      baseRange = range;
      baseAngle = angle;
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
      if (baseAngle <= 0f && angle > 0f) baseAngle = angle;
      if (baseFireRate <= 0f && fireRate > 0f) baseFireRate = fireRate;

      ApplyLevel(level);

      Debug.Log($"[SurgeBlade] Lv.{currentLevel} -> Dmg:{damage}, Range:{range}, MaxTargets:{currentMaxTargets}");
    }

    private void ApplyLevel(int level)
    {
      currentLevel = Mathf.Clamp(level, 1, 5);

      ApplyStatsFromCSV(currentLevel);

      range = baseRange * (1f + (currentLevel - 1) * rangePerLevel);
      angle = baseAngle;

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
        if (baseRow.angle > 0f) baseAngle = baseRow.angle;
        if (baseRow.firerate > 0f) baseFireRate = baseRow.firerate;
        if (baseRow.rangeperlevel > 0f) rangePerLevel = baseRow.rangeperlevel;
        if (baseRow.basemaxtargets > 0) baseMaxTargets = baseRow.basemaxtargets;
        if (baseRow.maxtargetsatlv5 > 0) maxTargetsAtLv5 = baseRow.maxtargetsatlv5;
        if (baseRow.mastermultiplier > 0f) masterMultiplier = baseRow.mastermultiplier;
      }

      if (row.damage > 0f) damage = row.damage;
      if (row.firerate > 0f) fireRate = row.firerate;
    }

    private void Attack()
    {
      attackCount++;

      if (InGameSoundManager.Instance != null)
        InGameSoundManager.Instance.PlaySurgeBladeFire();

      float useRange = range;
      float useAngle = angle;
      float useDamage = damage;

      bool empowered = false;

      if (enableMaster && currentLevel >= 5 && attackCount % 2 == 0)
      {
        empowered = true;
        useRange *= masterMultiplier;
        useAngle *= masterMultiplier;
        useDamage *= masterMultiplier;
      }

      Transform target = FindClosestTarget(useRange * 1.5f);

      Vector3 forward = transform.parent != null ? transform.parent.right : transform.right;

      if (target != null)
        forward = (target.position - transform.position).normalized;

      if (debugDraw)
      {
        Debug.DrawRay(transform.position, forward * useRange, empowered ? Color.yellow : Color.cyan, 0.2f);
        Debug.DrawRay(transform.position, Quaternion.Euler(0, 0, useAngle * 0.5f) * forward * useRange, Color.magenta, 0.2f);
        Debug.DrawRay(transform.position, Quaternion.Euler(0, 0, -useAngle * 0.5f) * forward * useRange, Color.magenta, 0.2f);
      }

      Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, useRange);

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

        Vector3 dirToTarget = (col.transform.position - transform.position).normalized;

        if (Vector3.Angle(forward, dirToTarget) > useAngle * 0.5f)
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
      Gizmos.color = Color.yellow;
      Gizmos.DrawWireSphere(transform.position, range);
    }
  }
}
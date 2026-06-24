using System.Collections.Generic;
using UnityEngine;
using NeoSurvive.Core;
using NeoSurvive.UI;

namespace NeoSurvive.Weapon
{
  public class LaserSword : MonoBehaviour
  {
    [Header("Stats")]
    public float damage = 12f;
    public float range = 2.2f;
    public float angle = 70f;
    public float fireRate = 0.8f;

    [Header("Target / Hit")]
    public LayerMask hitMask;
    public string enemyTag = "Enemy";

    [Header("Sweep VFX (Sprite)")]
    public GameObject sweepPrefab;
    public GameObject sweepPrefabEnhanced;
    public float sweepDuration = 0.25f;
    public float sweepSpawnOffset = 0.6f;
    public float sweepScale = 1.0f;

    [Header("Master (Lv5) - Rift")]
    public bool enableMaster = true;
    public GameObject riftPrefab;
    public float riftDuration = 1.5f;
    public float riftRadius = 1.0f;
    public float riftTick = 0.25f;
    public float riftDamageFactor = 0.35f;

    [Header("Level Scaling")]
    public float rangePerLevel = 0.05f;
    public float fireRateMulPerLevel = 0.94f;

    private float fireTimer;

    private float baseRange;
    private float baseFireRate;

    private const string weaponId = "lasersword";
    private int currentLevel = 1;

    private void Start()
    {
      baseRange = range;
      baseFireRate = fireRate;

      transform.localPosition = Vector3.right * 0.5f;

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

      Debug.Log($"[LaserSword] Lv.{currentLevel} Dmg={damage}, Range={range}, Rate={fireRate}");
    }

    private void ApplyLevel(int level)
    {
      currentLevel = Mathf.Clamp(level, 1, 5);

      ApplyStatsFromCSV(currentLevel);

      float expansionMul = Player.Instance != null ? Player.Instance.GetCompileNodeExpansionMultiplier() : 1f;
      float inertiaMul = Player.Instance != null ? Player.Instance.GetInertiaChargeMultiplier() : 1f;

      range = baseRange * (1f + (currentLevel - 1) * rangePerLevel) * expansionMul;
      fireRate = baseFireRate * Mathf.Pow(fireRateMulPerLevel, (currentLevel - 1));
      fireRate /= Mathf.Max(0.0001f, inertiaMul);
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
        if (baseRow.fireratemulperlevel > 0f) fireRateMulPerLevel = baseRow.fireratemulperlevel;
      }

      if (row.damage > 0f) damage = row.damage;
      if (row.angle > 0f) angle = row.angle;
      if (row.riftduration > 0f) riftDuration = row.riftduration;
      if (row.riftradius > 0f) riftRadius = row.riftradius;
      if (row.rifttick > 0f) riftTick = row.rifttick;
      if (row.riftdamagefactor > 0f) riftDamageFactor = row.riftdamagefactor;
    }

    private void Attack()
    {
      Debug.Log("[LaserSword] Attack!");

      if (InGameSoundManager.Instance != null)
        InGameSoundManager.Instance.PlayLaserSwordFire();

      Transform target = FindClosestTarget();

      Vector3 forward = transform.parent != null ? transform.parent.right : transform.right;

      if (target != null)
        forward = (target.position - transform.position).normalized;

      Debug.DrawRay(transform.position, forward * range, Color.red, 0.2f);

      SpawnSweepEffect(forward);

      // ★ 변경:
      // 기존: OverlapCircleAll(transform.position, range, hitMask)
      // 변경: 전체 Collider 검사 후 Enemy / IDamageable 여부를 직접 판단
      Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, range);

      Debug.Log($"[LaserSword] hits={hits.Length}");

      HashSet<int> processedIds = new HashSet<int>();

      foreach (var col in hits)
      {
        if (col == null) continue;

        bool isEnemy = IsEnemyCollider(col);

        IDamageable damageable = col.GetComponent<IDamageable>();
        if (damageable == null)
          damageable = col.GetComponentInParent<IDamageable>();

        bool isInHitMask = hitMask.value == 0 ||
          ((1 << col.gameObject.layer) & hitMask.value) != 0;

        // Enemy는 hitMask 기준 유지
        // 자판기 같은 IDamageable은 hitMask 밖이어도 허용
        if (!isInHitMask && damageable == null)
          continue;

        // Enemy도 아니고 IDamageable도 아니면 무시
        if (!isEnemy && damageable == null)
          continue;

        Vector3 dirToTarget = (col.transform.position - transform.position).normalized;

        if (Vector3.Angle(forward, dirToTarget) > angle * 0.5f)
          continue;

        // =========================
        // 1. 기존 Enemy 처리
        // =========================
        if (isEnemy)
        {
          Character character = col.GetComponentInParent<Character>();

          if (character != null)
          {
            int id = character.gameObject.GetInstanceID();
            if (processedIds.Contains(id)) continue;
            processedIds.Add(id);

            var src = GetComponentInParent<WeaponSource>();
            character.TakeDamage(damage, src != null ? src.weaponData : null);

            continue;
          }
        }

        // =========================
        // 2. 추가: IDamageable MapObject 처리
        // =========================
        if (damageable != null)
        {
          MonoBehaviour mb = damageable as MonoBehaviour;

          if (mb != null)
          {
            int id = mb.gameObject.GetInstanceID();
            if (processedIds.Contains(id)) continue;
            processedIds.Add(id);
          }

          damageable.TakeDamage(damage);
        }
      }

      if (enableMaster && currentLevel >= 5)
      {
        SpawnRift(forward);
      }
    }

    private bool IsEnemyCollider(Collider2D col)
    {
      if (col == null) return false;
      if (string.IsNullOrEmpty(enemyTag)) return false;

      return
        col.CompareTag(enemyTag) ||
        (col.transform.parent != null && col.transform.parent.CompareTag(enemyTag));
    }

    private void SpawnSweepEffect(Vector3 forward)
    {
      GameObject prefab =
        currentLevel >= 5 && sweepPrefabEnhanced != null
        ? sweepPrefabEnhanced
        : sweepPrefab;

      if (prefab == null) return;

      Vector3 spawnPos = transform.position + forward * sweepSpawnOffset;
      float angleDeg = Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg;

      GameObject go = Instantiate(
        prefab,
        spawnPos,
        Quaternion.Euler(0f, 0f, angleDeg)
      );

      go.transform.localScale = Vector3.one * sweepScale;

      Destroy(go, sweepDuration);
    }

    private void SpawnRift(Vector3 forward)
    {
      if (riftPrefab == null) return;

      Vector3 spawnPos = transform.position + forward * Mathf.Min(range, 2.0f);
      GameObject obj = Instantiate(riftPrefab, spawnPos, Quaternion.identity);

      RiftArea rift = obj.GetComponent<RiftArea>();

      if (rift != null)
      {
        var src = GetComponentInParent<WeaponSource>();

        rift.Initialize(
          damage,
          riftDuration,
          riftRadius,
          riftTick,
          riftDamageFactor,
          hitMask,
          enemyTag,
          src != null ? src.weaponData : null
        );
      }
    }

    private Transform FindClosestTarget()
    {
      Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, range * 1.5f);

      Transform closest = null;
      float closestDistance = range > 0f ? range * 1.5f : 10f;

      foreach (Collider2D hit in hits)
      {
        if (hit == null) continue;

        bool isEnemy = IsEnemyCollider(hit);

        bool isInHitMask = hitMask.value == 0 ||
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

        if (distance < closestDistance)
        {
          closestDistance = distance;
          closest = targetTransform;
        }
      }

      return closest;
    }

    private void OnDrawGizmosSelected()
    {
      Gizmos.color = Color.cyan;
      Gizmos.DrawWireSphere(transform.position, range);
    }
  }
}
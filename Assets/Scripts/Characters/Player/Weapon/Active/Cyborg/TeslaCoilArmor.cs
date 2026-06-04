using System.Collections.Generic;
using UnityEngine;
using NeoSurvive.Core;

namespace NeoSurvive.Weapon
{
  public class TeslaCoilArmor : MonoBehaviour
  {
    [Header("Aura")]
    public float auraDamage = 6f;
    public float auraRadius = 2.2f;
    public float auraTick = 0.4f;

    [Header("Filter")]
    public LayerMask enemyMask;
    public string enemyTag = "Enemy";

    [Header("Scaling")]
    public float radiusPerLevel = 0.12f;

    [Header("Master (Lv5) - Electric Puddle")]
    public bool enableMaster = true;

    public GameObject puddleAreaPrefab;
    public GameObject puddleVfxPrefab;

    public float puddleInterval = 0.35f;
    public float puddleDuration = 1.6f;
    public float puddleRadius = 1.1f;
    public float puddleTick = 0.35f;
    public float puddleDamageFactor = 0.5f;

    [SerializeField]
    private Vector3 puddleSpawnOffset = new Vector3(0f, -0.6f, 0f);

    [Header("Master (Lv5) - Lightning")]
    public GameObject lightningPrefab;
    public GameObject lightningVfxPrefab;

    public float lightningInterval = 1.2f;
    public float lightningRange = 10f;
    public float lightningDamageFactor = 1.2f;

    [Header("Debug")]
    public bool debugLog = true;
    public bool debugDraw = true;

    private const string weaponId = "teslacoilarmor";

    private float auraTimer;
    private float puddleTimer;
    private float lightningTimer;

    private Vector3 lastPuddlePos;

    private float baseAuraDamage;
    private float baseAuraRadius;

    private int currentLevel = 1;

    private void Start()
    {
      ApplyStatsFromCSV(1);

      baseAuraDamage = auraDamage;
      baseAuraRadius = auraRadius;

      ApplyLevel(1);

      lastPuddlePos = transform.position;
    }

    private void Update()
    {
      auraTimer += Time.deltaTime;

      if (auraTimer >= auraTick)
      {
        AuraTickDamage();
        auraTimer = 0f;
      }

      if (!(enableMaster && currentLevel >= 5))
        return;

      puddleTimer += Time.deltaTime;

      if (puddleTimer >= puddleInterval)
      {
        DropPuddleIfMoved();
        puddleTimer = 0f;
      }

      lightningTimer += Time.deltaTime;

      if (lightningTimer >= lightningInterval)
      {
        StrikeRandomTarget();
        lightningTimer = 0f;
      }
    }

    public void OnLevelUp(int level)
    {
      ApplyStatsFromCSV(level);
      ApplyLevel(level);

      if (debugLog)
        Debug.Log($"[TeslaCoilArmor] Lv Up → {currentLevel}");
    }

    private void ApplyLevel(int level)
    {
      currentLevel = Mathf.Clamp(level, 1, 5);

      auraRadius =
        baseAuraRadius *
        (1f + (currentLevel - 1) * radiusPerLevel);
    }

    private void AuraTickDamage()
    {
      Collider2D[] hits = Physics2D.OverlapCircleAll(
        transform.position,
        auraRadius
      );

      HashSet<int> processedIds = new HashSet<int>();

      foreach (var col in hits)
      {
        if (col == null)
          continue;

        TryDamageTarget(col, auraDamage, processedIds);
      }
    }

    private void DropPuddleIfMoved()
    {
      float moved = Vector3.Distance(transform.position, lastPuddlePos);

      if (moved < 0.2f)
        return;

      lastPuddlePos = transform.position;

      Vector3 spawnPos = transform.position + puddleSpawnOffset;

      if (puddleAreaPrefab != null)
      {
        GameObject areaObj = Instantiate(
          puddleAreaPrefab,
          spawnPos,
          Quaternion.identity
        );

        ElectricPuddle puddle = areaObj.GetComponent<ElectricPuddle>();

        if (puddle != null)
        {
          puddle.Initialize(
            auraDamage,
            puddleDuration,
            puddleRadius,
            puddleTick,
            puddleDamageFactor,
            enemyMask,
            enemyTag
          );

          var src = GetComponentInParent<WeaponSource>();

          if (src != null)
            puddle.SetSourceWeapon(src.weaponData);
        }
      }

      if (puddleVfxPrefab != null)
      {
        GameObject vfxObj = Instantiate(
          puddleVfxPrefab,
          spawnPos,
          Quaternion.identity
        );

        Destroy(vfxObj, puddleDuration);
      }
    }

    private void StrikeRandomTarget()
    {
      List<Transform> targets = GetTargetsInRange(lightningRange);

      if (targets.Count == 0)
      {
        if (debugLog)
          Debug.Log("[TeslaCoilArmor] 낙뢰 대상 없음");

        return;
      }

      Transform target = targets[Random.Range(0, targets.Count)];

      float dmg = auraDamage * lightningDamageFactor;

      Collider2D col = target.GetComponent<Collider2D>();

      if (col == null)
        col = target.GetComponentInChildren<Collider2D>();

      if (col != null)
      {
        HashSet<int> processedIds = new HashSet<int>();
        TryDamageTarget(col, dmg, processedIds);
      }

      Vector3 fxPos = target.position + new Vector3(0f, 1f, 0f);

      if (lightningPrefab != null)
        Instantiate(lightningPrefab, fxPos, Quaternion.identity);

      if (lightningVfxPrefab != null)
      {
        GameObject vfxObj = Instantiate(
          lightningVfxPrefab,
          fxPos,
          Quaternion.identity
        );

        Destroy(vfxObj, 1.0f);
      }
    }

    private bool TryDamageTarget(Collider2D col, float damageValue, HashSet<int> processedIds)
    {
      if (col == null)
        return false;

      bool isEnemy = IsEnemyCollider(col);

      Character character = col.GetComponent<Character>();
      if (character == null)
        character = col.GetComponentInParent<Character>();

      IDamageable damageable = col.GetComponent<IDamageable>();
      if (damageable == null)
        damageable = col.GetComponentInParent<IDamageable>();

      bool isInEnemyMask =
        enemyMask.value == 0 ||
        ((1 << col.gameObject.layer) & enemyMask.value) != 0;

      if (!isInEnemyMask && damageable == null)
        return false;

      if (character == null && !isEnemy && damageable == null)
        return false;

      if (character != null)
      {
        int id = character.gameObject.GetInstanceID();

        if (processedIds.Contains(id))
          return false;

        processedIds.Add(id);

        var src = GetComponentInParent<WeaponSource>();

        character.TakeDamage(
          damageValue,
          src != null ? src.weaponData : null
        );

        return true;
      }

      if (damageable != null)
      {
        MonoBehaviour mb = damageable as MonoBehaviour;

        if (mb != null)
        {
          int id = mb.gameObject.GetInstanceID();

          if (processedIds.Contains(id))
            return false;

          processedIds.Add(id);
        }

        damageable.TakeDamage(damageValue);
        return true;
      }

      return false;
    }

    private bool IsEnemyCollider(Collider2D col)
    {
      if (col == null) return false;
      if (string.IsNullOrEmpty(enemyTag)) return false;

      return
        col.CompareTag(enemyTag) ||
        (col.transform.parent != null &&
         col.transform.parent.CompareTag(enemyTag));
    }

    private List<Transform> GetTargetsInRange(float range)
    {
      Collider2D[] hits = Physics2D.OverlapCircleAll(
        transform.position,
        range
      );

      List<Transform> list = new List<Transform>();
      HashSet<int> addedIds = new HashSet<int>();

      foreach (var col in hits)
      {
        if (col == null)
          continue;

        bool isEnemy = IsEnemyCollider(col);

        Character character = col.GetComponent<Character>();
        if (character == null)
          character = col.GetComponentInParent<Character>();

        IDamageable damageable = col.GetComponent<IDamageable>();
        if (damageable == null)
          damageable = col.GetComponentInParent<IDamageable>();

        bool isInEnemyMask =
          enemyMask.value == 0 ||
          ((1 << col.gameObject.layer) & enemyMask.value) != 0;

        if (!isInEnemyMask && damageable == null)
          continue;

        if (character == null && !isEnemy && damageable == null)
          continue;

        Transform targetTransform = col.transform;

        if (character != null)
          targetTransform = character.transform;
        else if (damageable is Component damageableComponent)
          targetTransform = damageableComponent.transform;

        int id = targetTransform.gameObject.GetInstanceID();

        if (addedIds.Contains(id))
          continue;

        addedIds.Add(id);
        list.Add(targetTransform);
      }

      return list;
    }

    private void ApplyStatsFromCSV(int level)
    {
      if (WeaponStatLoader.DB == null)
        return;

      if (!WeaponStatLoader.DB.rows.TryGetValue(weaponId, out var levels))
        return;

      int lv = Mathf.Clamp(level, 1, 5);

      if (!levels.TryGetValue(lv, out var row))
        return;

      if (row.auradamage > 0) auraDamage = row.auradamage;
      if (row.auraradius > 0) auraRadius = row.auraradius;
      if (row.auratick > 0) auraTick = row.auratick;
      if (row.radiusperlevel > 0) radiusPerLevel = row.radiusperlevel;

      if (row.puddleinterval > 0) puddleInterval = row.puddleinterval;
      if (row.puddleduration > 0) puddleDuration = row.puddleduration;
      if (row.puddleradius > 0) puddleRadius = row.puddleradius;
      if (row.puddletick > 0) puddleTick = row.puddletick;
      if (row.puddledamagefactor > 0) puddleDamageFactor = row.puddledamagefactor;

      if (row.lightninginterval > 0) lightningInterval = row.lightninginterval;
      if (row.lightningrange > 0) lightningRange = row.lightningrange;
      if (row.lightningdamagefactor > 0) lightningDamageFactor = row.lightningdamagefactor;
    }

    private void OnDrawGizmosSelected()
    {
      Gizmos.color = Color.yellow;
      Gizmos.DrawWireSphere(transform.position, auraRadius);

      Gizmos.color = Color.cyan;
      Gizmos.DrawWireSphere(transform.position, lightningRange);
    }
  }
}
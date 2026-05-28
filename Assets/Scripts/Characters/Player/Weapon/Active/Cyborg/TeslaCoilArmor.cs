using System.Collections.Generic;
using UnityEngine;
using NeoSurvive.Core;

namespace NeoSurvive.Weapon
{
  /// <summary>
  /// 7번 무기: 테슬라 코일 아머
  /// - 기본: 몸 주변 지속 전류(오라)
  /// - Lv.5: 이동 경로 전기 장판 + 낙뢰
  /// </summary>
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

    // ★ 수정: 데미지 판정용 프리팹
    // - ElectricPuddle.cs가 붙어있는 프리팹
    public GameObject puddleAreaPrefab;

    // ★ 추가: 애니메이션 VFX 프리팹
    // - SpriteRenderer + Animator만 있는 프리팹
    public GameObject puddleVfxPrefab;

    public float puddleInterval = 0.35f;
    public float puddleDuration = 1.6f;
    public float puddleRadius = 1.1f;
    public float puddleTick = 0.35f;
    public float puddleDamageFactor = 0.5f;

    [SerializeField] private Vector3 puddleSpawnOffset = new Vector3(0f, -0.6f, 0f);

    [Header("Master (Lv5) - Lightning")]
    public GameObject lightningPrefab;
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

      if (debugLog)
        Debug.Log($"[TeslaCoilArmor] Start | Lv={currentLevel}");
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
        StrikeRandomEnemy();
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

      auraRadius = baseAuraRadius * (1f + (currentLevel - 1) * radiusPerLevel);
    }

    private void AuraTickDamage()
    {
      Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, auraRadius, enemyMask);

      foreach (var col in hits)
      {
        Enemy enemy = GetValidEnemy(col);
        if (enemy == null) continue;

        var src = GetComponentInParent<WeaponSource>();
        enemy.TakeDamage(auraDamage, src != null ? src.weaponData : null);
      }

      if (debugDraw)
        Debug.DrawRay(transform.position, Vector3.right * 0.01f, Color.yellow, 0.1f);
    }

    private void DropPuddleIfMoved()
    {
      float moved = Vector3.Distance(transform.position, lastPuddlePos);
      if (moved < 0.2f) return;

      lastPuddlePos = transform.position;

      Vector3 spawnPos = transform.position + puddleSpawnOffset;

      // =========================
      // ★ 수정: 데미지 판정용 Area 프리팹 생성
      // =========================
      if (puddleAreaPrefab != null)
      {
        GameObject areaObj = Instantiate(puddleAreaPrefab, spawnPos, Quaternion.identity);

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
        else
        {
          Debug.LogWarning("[TeslaCoilArmor] puddleAreaPrefab에 ElectricPuddle.cs가 없습니다.");
        }
      }
      else if (debugLog)
      {
        Debug.LogWarning("[TeslaCoilArmor] puddleAreaPrefab 없음");
      }

      // =========================
      // ★ 추가: 애니메이션 VFX 프리팹 생성
      // =========================
      if (puddleVfxPrefab != null)
      {
        GameObject vfxObj = Instantiate(puddleVfxPrefab, spawnPos, Quaternion.identity);
        Destroy(vfxObj, puddleDuration);
      }
      else if (debugLog)
      {
        Debug.LogWarning("[TeslaCoilArmor] puddleVfxPrefab 없음");
      }

      if (debugLog)
        Debug.Log($"[TeslaCoilArmor] Puddle Area/VFX 생성 | pos={spawnPos}");
    }

    private void StrikeRandomEnemy()
    {
      List<Enemy> enemies = GetEnemiesInRange(lightningRange);

      if (enemies.Count == 0)
      {
        if (debugLog)
          Debug.Log("[TeslaCoilArmor] 낙뢰 대상 없음");

        return;
      }

      Enemy target = enemies[Random.Range(0, enemies.Count)];

      float dmg = auraDamage * lightningDamageFactor;
      var src = GetComponentInParent<WeaponSource>();

      target.TakeDamage(dmg, src != null ? src.weaponData : null);

      if (debugLog)
        Debug.Log($"[TeslaCoilArmor] 낙뢰 적중 → {target.name}");

      if (lightningPrefab != null)
      {
        Vector3 fxPos = target.transform.position + new Vector3(0f, 1f, 0f);
        Instantiate(lightningPrefab, fxPos, Quaternion.identity);
      }
    }

    private List<Enemy> GetEnemiesInRange(float range)
    {
      Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, range, enemyMask);
      List<Enemy> list = new List<Enemy>();

      foreach (var col in hits)
      {
        Enemy e = GetValidEnemy(col);

        if (e != null && !list.Contains(e))
          list.Add(e);
      }

      return list;
    }

    private Enemy GetValidEnemy(Collider2D col)
    {
      if (col == null) return null;

      if (!string.IsNullOrEmpty(enemyTag))
      {
        bool okTag =
          col.CompareTag(enemyTag) ||
          (col.transform.parent != null && col.transform.parent.CompareTag(enemyTag));

        if (!okTag) return null;
      }

      return col.GetComponentInParent<Enemy>();
    }

    private void ApplyStatsFromCSV(int level)
    {
      if (WeaponStatLoader.DB == null) return;

      if (!WeaponStatLoader.DB.rows.TryGetValue(weaponId, out var levels)) return;

      int lv = Mathf.Clamp(level, 1, 5);

      if (!levels.TryGetValue(lv, out var row)) return;

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
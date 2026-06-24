using UnityEngine;
using NeoSurvive.Core;

namespace NeoSurvive.Weapon
{
  public class BlastBreath : MonoBehaviour
  {
    [Header("Breath Prefab")]
    public GameObject breathAreaPrefab;
    public Transform firePoint;

    [Header("VFX")]
    public GameObject normalVfxPrefab;      // ★ 추가: 기본 브레스 VFX
    public GameObject masterVfxPrefab;      // ★ 추가: 마스터 브레스 VFX
    public float vfxDurationOffset = 0.05f; // ★ 추가: VFX 삭제 보정 시간
    public float vfxScale = 1.0f;           // ★ 추가: VFX 크기

    [Header("Spawn Tuning")]
    public float spawnForwardOffset = 0.3f;
    public float areaCenterOffset = 0.8f;

    [Header("Stats")]
    public float damagePerTick = 4f;
    public float range = 4.5f;
    public float width = 2.2f;
    public float tickInterval = 0.08f;
    public float areaDuration = 0.35f;
    public float fireRate = 0.12f;

    [Header("Level Scaling")]
    public float rangePerLevel = 0.12f;
    public float widthPerLevel = 0.10f;

    [Header("Master (Lv5)")]
    public bool enableMaster = true;
    public bool masterColdMode = true;

    [Header("Targeting")]
    public float aimRange = 8f;
    public LayerMask enemyMask;
    public string enemyTag = "Enemy";

    private const string weaponId = "blastbreath";

    private float timer;
    private int currentLevel = 1;

    private void Start()
    {
      ApplyLevel(1);
    }

    private void Update()
    {
      timer += Time.deltaTime;

      if (timer >= fireRate)
      {
        EmitBreath();
        timer = 0f;
      }
    }

    public void OnLevelUp(int level)
    {
      ApplyLevel(level);
    }

    private void ApplyLevel(int level)
    {
      currentLevel = Mathf.Clamp(level, 1, 5);
      ApplyStatsFromCSV(currentLevel);
    }

    private void ApplyStatsFromCSV(int level)
    {
      if (WeaponStatLoader.DB == null)
      {
        Debug.LogWarning("[BlastBreath] WeaponStatLoader.DB 없음");
        return;
      }

      if (!WeaponStatLoader.DB.rows.TryGetValue(weaponId, out var levelDict))
      {
        Debug.LogWarning($"[BlastBreath] weaponId 없음: {weaponId}");
        return;
      }

      if (!levelDict.TryGetValue(level, out var row))
      {
        Debug.LogWarning($"[BlastBreath] level 데이터 없음: {level}");
        return;
      }

      levelDict.TryGetValue(1, out var baseRow);

      float baseRange = row.range;
      float baseWidth = row.width;

      if (baseRow != null)
      {
        baseRange = baseRow.range;
        baseWidth = baseRow.width;
      }

      rangePerLevel = row.rangeperlevel;
      widthPerLevel = row.widthperlevel;

      if (rangePerLevel <= 0f && baseRow != null)
        rangePerLevel = baseRow.rangeperlevel;

      if (widthPerLevel <= 0f && baseRow != null)
        widthPerLevel = baseRow.widthperlevel;

      damagePerTick = row.damagepertick;
      range = baseRange * (1f + (level - 1) * rangePerLevel);
      width = baseWidth * (1f + (level - 1) * widthPerLevel);

      tickInterval = row.tickinterval;
      areaDuration = row.areaduration;
      fireRate = row.firerate;
      aimRange = row.aimrange;
    }

    private void EmitBreath()
    {
      if (breathAreaPrefab == null) return;

      if (InGameSoundManager.Instance != null)
        InGameSoundManager.Instance.PlayBlastBreathFire();

      Vector3 aimOrigin = firePoint ? firePoint.position : transform.position;

      Transform target = FindClosestTarget(aimOrigin);

      Vector3 forward = target != null
        ? (target.position - aimOrigin).normalized
        : transform.right;

      Vector3 origin = firePoint
        ? firePoint.position
        : transform.position + forward * spawnForwardOffset;

      Vector3 center = origin + forward * areaCenterOffset;

      float angleZ = Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg;
      Quaternion rot = Quaternion.Euler(0f, 0f, angleZ);

      bool cold = enableMaster && currentLevel >= 5 && masterColdMode;
      var src = GetComponentInParent<WeaponSource>();

      GameObject obj = Instantiate(breathAreaPrefab, center, rot);

      var area = obj.GetComponentInChildren<BreathArea>();

      if (area != null)
      {
        area.Initialize(
          damagePerTick,
          tickInterval,
          areaDuration,
          range,
          width,
          cold,
          enemyMask,
          enemyTag,
          src != null ? src.weaponData : null
        );
      }

      Destroy(obj, areaDuration + 0.05f);

      SpawnBreathVFX(center, rot, cold);
    }

    // ★ 추가: 브레스 VFX 생성
    private void SpawnBreathVFX(Vector3 center, Quaternion rot, bool cold)
    {
      GameObject prefab =
        cold && masterVfxPrefab != null
        ? masterVfxPrefab
        : normalVfxPrefab;

      if (prefab == null) return;

      GameObject vfx = Instantiate(prefab, center, rot);

      vfx.transform.localScale = Vector3.one * vfxScale;

      Destroy(vfx, areaDuration + vfxDurationOffset);
    }

    private Transform FindClosestTarget(Vector3 origin)
    {
      Collider2D[] hits = Physics2D.OverlapCircleAll(origin, aimRange);

      Transform closest = null;
      float minDist = float.MaxValue;

      foreach (var h in hits)
      {
        if (h == null) continue;

        bool isInEnemyMask = ((1 << h.gameObject.layer) & enemyMask.value) != 0;

        bool isEnemy =
          !string.IsNullOrEmpty(enemyTag) &&
          (
            h.CompareTag(enemyTag) ||
            (h.transform.parent != null && h.transform.parent.CompareTag(enemyTag))
          );

        IDamageable damageable = h.GetComponent<IDamageable>();

        if (damageable == null)
          damageable = h.GetComponentInParent<IDamageable>();

        if ((!isInEnemyMask || !isEnemy) && damageable == null)
          continue;

        Transform targetTransform = h.transform;

        Enemy enemy = h.GetComponentInParent<Enemy>();

        if (enemy != null)
          targetTransform = enemy.transform;
        else if (damageable is Component damageableComponent)
          targetTransform = damageableComponent.transform;

        float d = Vector2.Distance(origin, targetTransform.position);

        if (d < minDist)
        {
          minDist = d;
          closest = targetTransform;
        }
      }

      return closest;
    }
  }
}
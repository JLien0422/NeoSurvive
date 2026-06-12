using System.Collections.Generic;
using UnityEngine;

namespace NeoSurvive.Weapon
{
  public class EnergyShield : MonoBehaviour
  {
    [Header("Orb Prefab")]
    public GameObject shieldOrbPrefab;

    [Header("Follow Target")]
    [SerializeField] private Transform target;

    [Header("Stats")]
    public float damage = 8f;
    public float radius = 2.0f;
    public float rotationSpeed = 180f;

    [Header("Level Scaling")]
    public int baseOrbCount = 1;
    public int maxOrbCount = 5;

    [Header("Collision Filter")]
    public LayerMask enemyMask;
    public string enemyTag = "Enemy";

    [Header("Projectile Block")]
    public bool blockProjectiles = true;
    public string projectileTag = "EnemyProjectile";

    [Header("Master (Lv5)")]
    public bool enableMaster = true;
    public float masterDamageFactor = 1.5f;

    private readonly List<ShieldOrb> orbs = new();

    private int currentLevel = 1;
    private int currentOrbCount = 1;
    private float currentAngle = 0f;

    private const string weaponId = "energyshield";

    private void Awake()
    {
      if (target == null)
      {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

        if (playerObj != null)
          target = playerObj.transform;
      }
    }

    private void Start()
    {
      ApplyLevel(1);
      RebuildOrbs();
    }

    private void LateUpdate()
    {
      if (target == null) return;

      currentAngle += rotationSpeed * Time.deltaTime;

      if (currentAngle >= 360f)
        currentAngle -= 360f;

      UpdateOrbPositions();
    }

    public void OnLevelUp(int level)
    {
      ApplyLevel(level);
      RebuildOrbs();

      Debug.Log($"[EnergyShield] Lv.{currentLevel} | Orbs={currentOrbCount}");
    }

    private void ApplyLevel(int level)
    {
      currentLevel = Mathf.Clamp(level, 1, 5);

      ApplyStatsFromCSV(currentLevel);

      currentOrbCount = Mathf.Clamp(
        baseOrbCount + (currentLevel - 1),
        baseOrbCount,
        maxOrbCount
      );
    }

    private void ApplyStatsFromCSV(int level)
    {
      if (WeaponStatLoader.DB == null)
        return;

      if (!WeaponStatLoader.DB.rows.TryGetValue(weaponId, out var levelDict))
        return;

      if (!levelDict.TryGetValue(level, out var row))
        return;

      if (row.damage > 0f) damage = row.damage;
      if (row.radius > 0f) radius = row.radius;
      if (row.rotationspeed > 0f) rotationSpeed = row.rotationspeed;
      if (row.baseorbcount > 0) baseOrbCount = row.baseorbcount;
      if (row.maxorbcount > 0) maxOrbCount = row.maxorbcount;
      if (row.masterdamagefactor > 0f) masterDamageFactor = row.masterdamagefactor;
    }

    private void RebuildOrbs()
    {
      if (shieldOrbPrefab == null)
      {
        Debug.LogError("[EnergyShield] shieldOrbPrefab is NULL!");
        return;
      }

      for (int i = orbs.Count - 1; i >= 0; i--)
      {
        if (orbs[i] != null)
          Destroy(orbs[i].gameObject);
      }

      orbs.Clear();

      var src = GetComponentInParent<WeaponSource>();
      WeaponBase sourceWeapon = src != null ? src.weaponData : null;

      for (int i = 0; i < currentOrbCount; i++)
      {
        GameObject obj = Instantiate(shieldOrbPrefab);
        obj.name = $"ShieldOrb_{i}";

        ShieldOrb orb = obj.GetComponent<ShieldOrb>();

        if (orb == null)
        {
          Debug.LogError("[EnergyShield] ShieldOrb 컴포넌트 없음");
          Destroy(obj);
          continue;
        }

        float finalDamage = damage;

        if (enableMaster && currentLevel >= 5)
          finalDamage *= masterDamageFactor;

        orb.Configure(
          finalDamage,
          enemyMask,
          enemyTag,
          blockProjectiles,
          projectileTag,
          sourceWeapon
        );

        orbs.Add(orb);
      }

      UpdateOrbPositions();
    }

    private void UpdateOrbPositions()
    {
      if (target == null || orbs.Count == 0)
        return;

      float step = 360f / orbs.Count;
      Vector3 center = target.position;

      for (int i = 0; i < orbs.Count; i++)
      {
        if (orbs[i] == null)
          continue;

        float ang = currentAngle + step * i;
        float rad = ang * Mathf.Deg2Rad;

        Vector3 offset = new Vector3(
          Mathf.Cos(rad) * radius,
          Mathf.Sin(rad) * radius,
          0f
        );

        orbs[i].SetWorldPosition(center + offset);
        orbs[i].transform.rotation = Quaternion.identity;
      }
    }

    private void OnDestroy()
    {
      for (int i = 0; i < orbs.Count; i++)
      {
        if (orbs[i] != null)
          Destroy(orbs[i].gameObject);
      }

      orbs.Clear();
    }
  }
}
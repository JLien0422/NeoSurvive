using UnityEngine;
using NeoSurvive.Core;

namespace NeoSurvive.Weapon
{
  public class BoosterKnuckle : MonoBehaviour
  {
    [Header("Projectile")]
    public GameObject fistProjectilePrefab;
    public Transform firePoint;

    [Header("Muzzle VFX")]
    public GameObject muzzleVfxPrefab;
    public float muzzleVfxScale = 1f;
    public float muzzleVfxDuration = 0.25f;

    [Header("Fire Offset")]
    [SerializeField] private float fireOffset = 0.8f;

    [Header("Stats")]
    public float damage = 14f;
    public float range = 12f;
    public float fireRate = 1.2f;
    public float speed = 18f;

    [Header("Level Scaling")]
    public float speedPerLevel = 0.12f;
    public int baseCount = 1;
    public int maxCount = 4;

    [Header("Filter")]
    public LayerMask enemyMask;
    public string enemyTag = "Enemy";

    [Header("Master (Lv5)")]
    public bool enableMaster = true;

    private const string weaponId = "boosterknuckle";

    private float timer;

    private float baseSpeed;
    private float baseRange;
    private float baseFireRate;

    private int currentLevel = 1;
    private int currentCount = 1;

    private void Start()
    {
      ApplyStatsFromCSV(1);

      baseSpeed = speed;
      baseRange = range;
      baseFireRate = fireRate;

      ApplyLevel(1);
    }

    private void Update()
    {
      timer += Time.deltaTime;

      if (timer >= fireRate)
      {
        Fire();
        timer = 0f;
      }
    }

    public void OnLevelUp(int level)
    {
      ApplyStatsFromCSV(level);
      ApplyLevel(level);

      Debug.Log($"[BoosterKnuckle] Lv.{currentLevel} damage={damage}, speed={speed}, count={currentCount}");
    }

    private void ApplyLevel(int level)
    {
      currentLevel = Mathf.Clamp(level, 1, 5);

      speed = baseSpeed * (1f + (currentLevel - 1) * speedPerLevel);
      range = baseRange;
      fireRate = baseFireRate;

      if (damage <= 0f)
        damage = 14f;

      if (speed <= 0f)
        speed = 18f;

      if (currentLevel <= 1)
        currentCount = baseCount;
      else if (currentLevel <= 3)
        currentCount = Mathf.Min(2, maxCount);
      else
        currentCount = Mathf.Min(3, maxCount);
    }

    private void Fire()
    {
      if (fistProjectilePrefab == null) return;

      if (InGameSoundManager.Instance != null)
        InGameSoundManager.Instance.PlayBoosterKnuckleFire();

      Vector3 baseOrigin = firePoint != null ? firePoint.position : transform.position;

      Transform target = FindClosestTarget(baseOrigin);

      Vector3 forward = target != null
        ? (target.position - baseOrigin).normalized
        : transform.right;

      Vector3 origin = baseOrigin + forward * fireOffset;

      float angleZ = Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg;
      Quaternion muzzleRot = Quaternion.Euler(0f, 0f, angleZ);

      SpawnMuzzleVFX(origin, muzzleRot);

      float spread = currentCount <= 1 ? 0f : 10f;

      for (int i = 0; i < currentCount; i++)
      {
        float t = currentCount == 1 ? 0.5f : (float)i / (currentCount - 1);
        float ang = Mathf.Lerp(-spread, spread, t);

        Vector3 dir = Quaternion.Euler(0f, 0f, ang) * forward;

        float projectileAngleZ = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        Quaternion projectileRot = Quaternion.Euler(0f, 0f, projectileAngleZ);

        GameObject obj = Instantiate(fistProjectilePrefab, origin, projectileRot);

        BoosterFistProjectile proj = obj.GetComponent<BoosterFistProjectile>();
        if (proj == null) continue;

        bool master = enableMaster && currentLevel >= 5;

        proj.Initialize(
          gameObject,
          dir,
          damage,
          speed,
          range,
          enemyMask,
          enemyTag,
          master
        );

        proj.ApplyStatsFromCSV(currentLevel);

        WeaponSource src = GetComponent<WeaponSource>();
        if (src != null)
          proj.SetSourceWeapon(src.weaponData);
      }
    }

    private void SpawnMuzzleVFX(Vector3 position, Quaternion rotation)
    {
      if (muzzleVfxPrefab == null) return;

      GameObject vfx = Instantiate(muzzleVfxPrefab, position, rotation);

      DisablePhysicsOnVFX(vfx);

      vfx.transform.localScale = Vector3.one * muzzleVfxScale;

      Destroy(vfx, muzzleVfxDuration);
    }

    private void DisablePhysicsOnVFX(GameObject obj)
    {
      if (obj == null) return;

      Collider2D[] colliders = obj.GetComponentsInChildren<Collider2D>(true);
      foreach (Collider2D col in colliders)
      {
        if (col == null) continue;
        col.enabled = false;
      }

      Rigidbody2D[] rigidbodies = obj.GetComponentsInChildren<Rigidbody2D>(true);
      foreach (Rigidbody2D rb in rigidbodies)
      {
        if (rb == null) continue;
        rb.simulated = false;
      }
    }

    private Transform FindClosestTarget(Vector3 origin)
    {
      Collider2D[] hits = Physics2D.OverlapCircleAll(origin, range);

      Transform closest = null;
      float minDist = float.MaxValue;

      foreach (Collider2D hit in hits)
      {
        if (hit == null) continue;

        bool isEnemy =
          hit.CompareTag(enemyTag) ||
          (hit.transform.parent != null && hit.transform.parent.CompareTag(enemyTag));

        IDamageable damageable = hit.GetComponent<IDamageable>();
        if (damageable == null)
          damageable = hit.GetComponentInParent<IDamageable>();

        if (!isEnemy && damageable == null)
          continue;

        Transform targetTransform = hit.transform;

        Character character = hit.GetComponentInParent<Character>();

        if (character != null)
          targetTransform = character.transform;
        else if (damageable is Component damageableComponent)
          targetTransform = damageableComponent.transform;

        float dist = Vector3.Distance(origin, targetTransform.position);

        if (dist < minDist)
        {
          minDist = dist;
          closest = targetTransform;
        }
      }

      return closest;
    }

    private void ApplyStatsFromCSV(int level)
    {
      if (WeaponStatLoader.DB == null)
      {
        Debug.LogWarning("[BoosterKnuckle] WeaponStatLoader.DB가 null입니다.");
        return;
      }

      if (!WeaponStatLoader.DB.rows.TryGetValue(weaponId, out var weaponLevels))
      {
        Debug.LogWarning($"[BoosterKnuckle] CSV에 '{weaponId}'가 없습니다.");
        return;
      }

      int clampedLevel = Mathf.Clamp(level, 1, 5);

      if (!weaponLevels.TryGetValue(clampedLevel, out var row))
      {
        Debug.LogWarning($"[BoosterKnuckle] '{weaponId}'의 level {clampedLevel} 데이터가 없습니다.");
        return;
      }

      if (row.damage > 0f) damage = row.damage;
      if (row.range > 0f) range = row.range;
      if (row.firerate > 0f) fireRate = row.firerate;
      if (row.speed > 0f) speed = row.speed;

      if (row.speedperlevel > 0f) speedPerLevel = row.speedperlevel;

      if (row.basecount > 0) baseCount = row.basecount;
      if (row.maxcount > 0) maxCount = row.maxcount;

      Debug.Log($"[BoosterKnuckle] CSV 적용 완료 | Lv={clampedLevel}, damage={damage}, range={range}, fireRate={fireRate}, speed={speed}, baseCount={baseCount}, maxCount={maxCount}");
    }
  }
}
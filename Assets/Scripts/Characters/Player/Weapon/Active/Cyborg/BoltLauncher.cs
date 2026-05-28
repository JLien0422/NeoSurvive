using System.Collections;
using UnityEngine;

namespace NeoSurvive.Weapon
{
  public class BoltLauncher : MonoBehaviour
  {
    [Header("Projectile")]
    public GameObject boltProjectilePrefab;
    public Transform firePoint;

    [Header("Muzzle VFX")]
    public GameObject muzzleVfxPrefab;
    public float muzzleVfxScale = 1f;
    public float muzzleVfxDuration = 0.25f;

    [Header("Fire Offset")]
    [SerializeField] private float fireOffset = 0.8f;

    [Header("Spawn Spread")]
    [SerializeField] private float spawnSideOffset = 0.25f;

    [Header("Damage / Flight")]
    public float damage = 10f;
    public float range = 12f;
    public float speed = 16f;
    public float homingTurnSpeed = 540f;

    [Header("Burst Spec")]
    public float baseBurstInterval = 5f;
    public int baseBurstCount = 4;
    public float intervalDecreasePerLevel = 1f;
    public int countIncreasePerLevel = 1;
    public float minBurstInterval = 1f;
    public int maxBurstCount = 8;

    [Header("Spread")]
    public float spreadAngle = 22f;

    [Header("Filter")]
    public LayerMask enemyMask;
    public string enemyTag = "Enemy";

    [Header("Master (Lv5) - Overload")]
    public bool enableMaster = true;
    public int overloadStacksToExplode = 5;
    public float overloadExplosionRadius = 2.8f;
    public float overloadExplosionDamageFactor = 1.4f;

    private int currentLevel = 1;
    private float burstInterval;
    private int burstCount;

    private Coroutine loop;

    private void Start()
    {
      ApplyLevel(1);
      loop = StartCoroutine(BurstLoop());
    }

    private void OnDisable()
    {
      if (loop != null)
      {
        StopCoroutine(loop);
        loop = null;
      }
    }

    public void OnLevelUp(int level)
    {
      ApplyLevel(level);
      Debug.Log($"[BoltLauncher] Lv.{currentLevel} interval={burstInterval}s, count={burstCount}");
    }

    private void ApplyLevel(int level)
    {
      currentLevel = Mathf.Clamp(level, 1, 5);

      burstInterval = baseBurstInterval - (currentLevel - 1) * intervalDecreasePerLevel;
      burstCount = baseBurstCount + (currentLevel - 1) * countIncreasePerLevel;

      burstInterval = Mathf.Max(minBurstInterval, burstInterval);
      burstCount = Mathf.Min(maxBurstCount, burstCount);
    }

    private IEnumerator BurstLoop()
    {
      while (true)
      {
        yield return new WaitForSeconds(burstInterval);
        FireBurst();
      }
    }

    private void FireBurst()
    {
      if (boltProjectilePrefab == null) return;

      if (InGameSoundManager.Instance != null)
        InGameSoundManager.Instance.PlayBoltLauncherFire();

      Vector3 baseOrigin = firePoint != null ? firePoint.position : transform.position;

      // ★ 수정: 전 방향 대응. 가장 가까운 적 방향으로 발사 기준 잡기
      Transform target = FindClosestEnemy(baseOrigin);

      Vector3 forward = target != null
        ? (target.position - baseOrigin).normalized
        : transform.right.normalized;

      Vector3 origin = baseOrigin + forward * fireOffset;

      float muzzleAngleZ = Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg;
      Quaternion muzzleRot = Quaternion.Euler(0f, 0f, muzzleAngleZ);

      // ★ Trigger/VFX도 적 방향으로 회전해서 생성
      SpawnMuzzleVFX(origin, muzzleRot);

      float half = spreadAngle * 0.5f;
      bool master = enableMaster && currentLevel >= 5;

      Vector3 perpendicular = new Vector3(-forward.y, forward.x, 0f).normalized;

      for (int i = 0; i < burstCount; i++)
      {
        float t = burstCount == 1 ? 0.5f : (float)i / (burstCount - 1);
        float ang = Mathf.Lerp(-half, half, t);

        Vector3 dir = Quaternion.Euler(0f, 0f, ang) * forward;

        float projectileAngleZ = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        Quaternion projectileRot = Quaternion.Euler(0f, 0f, projectileAngleZ);

        float spawnT = burstCount == 1 ? 0f : Mathf.Lerp(-1f, 1f, i / (float)(burstCount - 1));
        Vector3 spawnOrigin = origin + perpendicular * (spawnT * spawnSideOffset);

        GameObject obj = Instantiate(boltProjectilePrefab, spawnOrigin, projectileRot);

        BoltArrowProjectile proj = obj.GetComponent<BoltArrowProjectile>();
        if (proj == null) continue;

        proj.Initialize(
          gameObject,
          dir,
          damage,
          speed,
          range,
          homingTurnSpeed,
          enemyMask,
          enemyTag,
          master,
          overloadStacksToExplode,
          overloadExplosionRadius,
          overloadExplosionDamageFactor
        );

        WeaponSource src = GetComponentInParent<WeaponSource>();
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

    // ★ 추가: 가장 가까운 적 찾기
    private Transform FindClosestEnemy(Vector3 origin)
    {
      Collider2D[] hits = Physics2D.OverlapCircleAll(origin, range, enemyMask);

      Transform closest = null;
      float minDist = float.MaxValue;

      foreach (Collider2D hit in hits)
      {
        if (hit == null) continue;

        if (!string.IsNullOrEmpty(enemyTag) && !hit.CompareTag(enemyTag))
          continue;

        Character character = hit.GetComponent<Character>();

        if (character == null)
          character = hit.GetComponentInParent<Character>();

        if (character == null)
          continue;

        float dist = Vector3.Distance(origin, character.transform.position);

        if (dist < minDist)
        {
          minDist = dist;
          closest = character.transform;
        }
      }

      return closest;
    }
  }
}
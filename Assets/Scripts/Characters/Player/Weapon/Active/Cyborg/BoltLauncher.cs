using System.Collections;
using UnityEngine;

namespace NeoSurvive.Weapon
{
  /// <summary>
  /// 8번 무기: 볼트 런처 (버스트형)
  /// - Lv1: 9초마다 4발
  /// - 레벨업마다: 대기시간 -1초, 발사 수 +1
  /// - Lv5: 5초마다 8발
  /// </summary>
  public class BoltLauncher : MonoBehaviour
  {
    [Header("Projectile")]
    public GameObject boltProjectilePrefab;   // BoltArrowProjectile 프리팹
    public Transform firePoint;

    [Header("Damage / Flight")]
    public float damage = 10f;
    public float range = 12f;
    public float speed = 16f;
    public float homingTurnSpeed = 540f;

    [Header("Burst Spec (Fixed Design)")]
    public float baseBurstInterval = 5f;   // Lv1
    public int baseBurstCount = 4;         // Lv1
    public float intervalDecreasePerLevel = 1f; // 레벨당 -1초
    public int countIncreasePerLevel = 1;       // 레벨당 +1발
    public float minBurstInterval = 1f;    // Lv5 하한
    public int maxBurstCount = 8;          // Lv5 상한

    [Header("Spread")]
    public float spreadAngle = 22f;        // 여러 발 퍼짐 각도(전체 범위 느낌)

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

      // 무기 생성 시 자동 루프 시작
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

    // WeaponManager가 SendMessage로 호출
    public void OnLevelUp(int level)
    {
      ApplyLevel(level);
      Debug.Log($"[BoltLauncher] Lv.{currentLevel} interval={burstInterval}s, count={burstCount}");
    }

    private void ApplyLevel(int level)
    {
      currentLevel = Mathf.Clamp(level, 1, 5);

      // ✅ 요구사항 그대로 계산
      burstInterval = baseBurstInterval - (currentLevel - 1) * intervalDecreasePerLevel;
      burstCount = baseBurstCount + (currentLevel - 1) * countIncreasePerLevel;

      // 안전장치(최종 레벨 고정)
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

      Vector3 origin = firePoint ? firePoint.position : transform.position;

      // 동시에 여러 발
      float half = spreadAngle * 0.5f;
      bool master = enableMaster && currentLevel >= 5;

      for (int i = 0; i < burstCount; i++)
      {
        float t = (burstCount == 1) ? 0.5f : (float)i / (burstCount - 1);
        float ang = Mathf.Lerp(-half, half, t);

        Vector3 dir = Quaternion.Euler(0, 0, ang) * transform.right;

        GameObject obj = Instantiate(boltProjectilePrefab, origin, Quaternion.identity);
        var proj = obj.GetComponent<BoltArrowProjectile>();
        if (proj != null)
        {
          proj.Initialize(
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
          var src = GetComponentInParent<WeaponSource>();
          if (src != null) proj.SetSourceWeapon(src.weaponData);
        }
      }
    }
  }
}

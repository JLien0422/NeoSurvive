using UnityEngine;
using NeoSurvive.Core;

namespace NeoSurvive.Weapon
{
  /// <summary>
  /// 6번 무기: 부스터 너클
  /// - 로켓 추진 주먹을 전방 발사
  /// - Lv.Up: 주먹 속도/데미지/주먹 개수 증가
  /// - Lv.5 마스터: 벽에 부딪히면 튕겨 나와 가장 가까운 적을 재추격(유도)
  /// </summary>
  public class BoosterKnuckle : MonoBehaviour
  {
    [Header("Projectile")]
    public GameObject fistProjectilePrefab;   // BoosterFistProjectile가 붙은 프리팹(필수)
    public Transform firePoint;               // 없으면 transform.position에서 발사

    [Header("Stats")]
    public float damage = 14f;
    public float range = 12f;      // 투사체 최대 이동거리
    public float fireRate = 1.2f;
    public float speed = 18f;

    [Header("Level Scaling")]
    public float damagePerLevel = 0.18f;
    public float speedPerLevel = 0.12f;
    public int baseCount = 1;
    public int maxCount = 4;

    [Header("Filter")]
    public LayerMask enemyMask;
    public string enemyTag = "Enemy";

    [Header("Master (Lv5)")]
    public bool enableMaster = true;

    private float timer;

    private float baseDamage;
    private float baseSpeed;
    private float baseRange;
    private float baseFireRate;

    private int currentLevel = 1;
    private int currentCount = 1;

    private void Start()
    {
      baseDamage = damage;
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
      ApplyLevel(level);
      Debug.Log($"[BoosterKnuckle] Lv.{currentLevel} dmg={damage} speed={speed} count={currentCount}");
    }

    private void ApplyLevel(int level)
    {
      currentLevel = Mathf.Clamp(level, 1, 5);

      damage = baseDamage * (1f + (currentLevel - 1) * damagePerLevel);
      speed  = baseSpeed  * (1f + (currentLevel - 1) * speedPerLevel);
      range  = baseRange; // 고정(원하면 증가)
      fireRate = baseFireRate; // 고정(원하면 감소)

      // 주먹 개수 증가: Lv1=1, Lv3=2, Lv5=3~4 (원하면 조절)
      if (currentLevel <= 1) currentCount = baseCount;
      else if (currentLevel <= 3) currentCount = Mathf.Min(2, maxCount);
      else currentCount = Mathf.Min(3, maxCount);
    }

    private void Fire()
    {
      if (fistProjectilePrefab == null) return;

      Vector3 origin = firePoint ? firePoint.position : transform.position;

      // 기본 발사 방향: 플레이어 오른쪽(게임에서 캐릭터 방향 시스템 있으면 거기로 교체)
      Vector3 forward = transform.right;

      // 여러 발이면 살짝 퍼지게
      float spread = (currentCount <= 1) ? 0f : 10f; // 각도 퍼짐
      for (int i = 0; i < currentCount; i++)
      {
        float t = (currentCount == 1) ? 0.5f : (float)i / (currentCount - 1);
        float ang = Mathf.Lerp(-spread, spread, t);

        Vector3 dir = Quaternion.Euler(0, 0, ang) * forward;

        GameObject obj = Instantiate(fistProjectilePrefab, origin, Quaternion.identity);
        var proj = obj.GetComponent<BoosterFistProjectile>();
        if (proj != null)
        {
          bool master = enableMaster && currentLevel >= 5;
          proj.Initialize(dir, damage, speed, range, enemyMask, enemyTag, master);
          var src = GetComponent<WeaponSource>();
          if (src != null) proj.SetSourceWeapon(src.weaponData);
        }
      }
    }
  }
}

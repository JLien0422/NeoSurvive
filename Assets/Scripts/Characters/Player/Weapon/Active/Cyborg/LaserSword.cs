using UnityEngine;
using NeoSurvive.Core;
using NeoSurvive.UI;

namespace NeoSurvive.Weapon
{
  /// <summary>
  /// Cyborg 무기: LaserSword
  /// - 부채꼴 근접 베기
  /// - Player 방향 기준 공격
  /// - Lv5 마스터: RiftArea 생성(잔상딜)
  /// </summary>
  public class LaserSword : MonoBehaviour
  {
    [Header("Stats")]
    public float damage = 12f;
    public float range = 2.2f;        // 부채꼴 반경
    public float angle = 70f;         // 부채꼴 전체 각도
    public float fireRate = 0.8f;     // 공격 간격(초)

    [Header("Target / Hit")]
    public LayerMask hitMask;         // Enemy 레이어
    public string enemyTag = "Enemy";

    [Header("Sweep VFX (Sprite)")]
    public GameObject sweepPrefab;                    // 기본 스프라이트 휩쓸기 이펙트 프리팹 (오른쪽이 기준)
    public GameObject sweepPrefabEnhanced;            // Lv5 시 교체되는 강화 이펙트 프리팹
    public float sweepDuration = 0.25f;               // 프리팹 유지 시간
    public float sweepSpawnOffset = 0.6f;             // 생성 위치 오프셋(앞쪽)
    public float sweepScale = 1.0f;                   // 프리팹 기본 스케일

    [Header("Master (Lv5) - Rift")]
    public bool enableMaster = true;
    public GameObject riftPrefab;
    public float riftDuration = 1.5f;
    public float riftRadius = 1.0f;
    public float riftTick = 0.25f;
    public float riftDamageFactor = 0.35f;

    [Header("Level Scaling")]
    public float damagePerLevel = 0.15f;
    public float rangePerLevel = 0.05f;
    public float fireRateMulPerLevel = 0.94f;

    private float fireTimer;

    // base stats
    private float baseDamage;
    private float baseRange;
    private float baseFireRate;

    private int currentLevel = 1;

    private void Start()
    {
      baseDamage = damage;
      baseRange = range;
      baseFireRate = fireRate;

      // 위치를 살짝 앞으로 (Player와 겹침 방지)
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

    // WeaponManager.SendMessage("OnLevelUp", level) 호환
    public void OnLevelUp(int level)
    {
      if (baseDamage <= 0f && damage > 0f) baseDamage = damage;
      if (baseRange <= 0f && range > 0f) baseRange = range;
      if (baseFireRate <= 0f && fireRate > 0f) baseFireRate = fireRate;

      ApplyLevel(level);

      Debug.Log($"[LaserSword] Lv.{currentLevel} Dmg={damage}, Range={range}, Rate={fireRate}");
    }

    private void ApplyLevel(int level)
    {
      currentLevel = Mathf.Clamp(level, 1, 5);

      damage = baseDamage * (1f + (currentLevel - 1) * damagePerLevel);
      range = baseRange * (1f + (currentLevel - 1) * rangePerLevel);
      fireRate = baseFireRate * Mathf.Pow(fireRateMulPerLevel, (currentLevel - 1));
    }

    private void Attack()
    {
      Debug.Log("[LaserSword] Attack!");
      if (InGameSoundManager.Instance != null) InGameSoundManager.Instance.PlayLaserSwordFire();

      // Player 방향 기준
      Transform target = FindClosestEnemy();
      Vector3 forward = transform.parent != null ? transform.parent.right : transform.right;

      if (target != null)
      {
        forward = (target.position - transform.position).normalized;
      }

      // 디버그: 실제 공격 방향
      Debug.DrawRay(transform.position, forward * range, Color.red, 0.2f);

      // Sprite 기반 훑기 이펙트 생성
      SpawnSweepEffect(forward);

      Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, range, hitMask);
      Debug.Log($"[LaserSword] hits={hits.Length}");

      foreach (var col in hits)
      {
        if (col == null) continue;

        // Tag는 부모까지 체크
        if (!string.IsNullOrEmpty(enemyTag))
        {
          bool okTag =
            col.CompareTag(enemyTag) ||
            (col.transform.parent != null && col.transform.parent.CompareTag(enemyTag));

          if (!okTag) continue;
        }

        // 부채꼴 판정
        Vector3 dirToEnemy = (col.transform.position - transform.position).normalized;
        if (Vector3.Angle(forward, dirToEnemy) > angle * 0.5f) continue;

        // Character 베이스로 Enemy/Boss 모두 처리
        Character character = col.GetComponentInParent<Character>();
        if (character != null)
        {
          var src = GetComponentInParent<WeaponSource>();
          character.TakeDamage(damage, src != null ? src.weaponData : null);
        }
      }

      // Lv5 마스터 효과
      if (enableMaster && currentLevel >= 5)
      {
        SpawnRift(forward);
      }
    }

    private void SpawnSweepEffect(Vector3 forward)
    {
      GameObject prefab = (currentLevel >= 5 && sweepPrefabEnhanced != null) ? sweepPrefabEnhanced : sweepPrefab;
      if (prefab == null) return;

      Vector3 spawnPos = transform.position + forward * sweepSpawnOffset;
      float angleDeg = Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg;
      GameObject go = Instantiate(prefab, spawnPos, Quaternion.Euler(0f, 0f, angleDeg));
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
        rift.Initialize(
          damage,
          riftDuration,
          riftRadius,
          riftTick,
          riftDamageFactor,
          hitMask,
          enemyTag
        );
        var src = GetComponentInParent<WeaponSource>();
        if (src != null) rift.SetSourceWeapon(src.weaponData);
      }
    }

    private Transform FindClosestEnemy()
    {
      GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
      GameObject closest = null;
      float closestDistance = range > 0 ? range * 1.5f : 10f;

      foreach (GameObject enemy in enemies)
      {
        if (enemy == null) continue;

        float distance = Vector3.Distance(transform.position, enemy.transform.position);
        if (distance < closestDistance)
        {
          closestDistance = distance;
          closest = enemy;
        }
      }

      return closest != null ? closest.transform : null;
    }

    private void OnDrawGizmosSelected()
    {
      Gizmos.color = Color.cyan;
      Gizmos.DrawWireSphere(transform.position, range);
    }
  }
}

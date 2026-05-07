using UnityEngine;
using NeoSurvive.Core;

namespace NeoSurvive.Weapon
{
  /// <summary>
  /// 2번 무기: 서지 블레이드
  /// - 기본: 강한 한방, 기본 1명 타격
  /// - Lv.Up: 타격 가능 인원 + 데미지 증가
  /// - Lv.5 마스터: 2회 공격마다 공격력/사거리/범위(각도) 2배(강화 공격)
  /// </summary>
  public class SurgeBlade : MonoBehaviour
  {
    [Header("Stats")]
    public float damage = 20f;
    public float range = 3.0f;     // 타격 반경(근접 판정)
    public float angle = 50f;      // 부채꼴 각도
    public float fireRate = 1.2f;  // 공격 간격

    [Header("Target Filter")]
    public LayerMask hitMask;
    public string enemyTag = "Enemy";

    [Header("Level Scaling")]
    public float damagePerLevel = 0.25f; // 레벨당 데미지 +25%
    public float rangePerLevel = 0.05f;  // 레벨당 사거리 +5%
    public int baseMaxTargets = 1;       // 기본 타격 인원
    public int maxTargetsAtLv5 = 3;      // Lv5에서 최대 타격 인원(원하면 조절)

    [Header("VFX")]
    public GameObject surgePrefab;               // 기본 시전/휩쓸기 프리팹
    public GameObject surgePrefabEnhanced;       // Lv5 강화 시 사용할 프리팹
    public float surgeDuration = 0.35f;
    public float surgeSpawnOffset = 0.6f;
    public float surgeScale = 1.0f;

    [Header("Master (Lv5)")]
    public bool enableMaster = true;
    public float masterMultiplier = 2.0f;   // 강화 공격 시 damage/range/angle 배수
    public bool debugDraw = true;

    private float fireTimer;

    // base stats
    private float baseDamage;
    private float baseRange;
    private float baseAngle;
    private float baseFireRate;

    private int currentLevel = 1;
    private int currentMaxTargets = 1;

    private int attackCount = 0; // 마스터용: 몇 번 공격했는지

    private void Start()
    {
      baseDamage = damage;
      baseRange = range;
      baseAngle = angle;
      baseFireRate = fireRate;

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

    // WeaponManager가 SendMessage로 호출
    public void OnLevelUp(int level)
    {
      // 안전장치(혹시 base가 0으로 깨진 경우)
      if (baseDamage <= 0f && damage > 0f) baseDamage = damage;
      if (baseRange <= 0f && range > 0f) baseRange = range;
      if (baseAngle <= 0f && angle > 0f) baseAngle = angle;
      if (baseFireRate <= 0f && fireRate > 0f) baseFireRate = fireRate;

      ApplyLevel(level);
      Debug.Log($"[SurgeBlade] Lv.{currentLevel} -> Dmg:{damage}, Range:{range}, MaxTargets:{currentMaxTargets}");
    }

    private void ApplyLevel(int level)
    {
      currentLevel = Mathf.Clamp(level, 1, 5);

      damage = baseDamage * (1f + (currentLevel - 1) * damagePerLevel);
      range = baseRange * (1f + (currentLevel - 1) * rangePerLevel);
      angle = baseAngle; // 기본은 고정(강화 공격에서만 배수 적용)

      // 타격 인원 증가 (Lv1=1, Lv3=2, Lv5=3 느낌)
      if (currentLevel <= 1) currentMaxTargets = baseMaxTargets;
      else if (currentLevel <= 3) currentMaxTargets = Mathf.Min(2, maxTargetsAtLv5);
      else currentMaxTargets = maxTargetsAtLv5;
    }

    private void Attack()
    {
      attackCount++;
      if (InGameSoundManager.Instance != null) InGameSoundManager.Instance.PlaySurgeBladeFire();

      // 기본 공격 파라미터
      float useRange = range;
      float useAngle = angle;
      float useDamage = damage; // ✅ 추가: 이번 공격에 사용할 데미지

      // Lv5 마스터: 2회 공격마다 강화(2,4,6...)
      bool empowered = false;
      if (enableMaster && currentLevel >= 5 && (attackCount % 2 == 0))
      {
        empowered = true;
        useRange *= masterMultiplier;
        useAngle *= masterMultiplier;
        useDamage *= masterMultiplier; // ✅ 핵심: 공격력도 2배
      }

      // 가장 가까운 적 방향(없으면 오른쪽)
      Transform target = FindClosestEnemy(useRange * 1.5f);

      // Player 자식이면 Player 방향 기준이 더 안전
      Vector3 forward = transform.parent != null ? transform.parent.right : transform.right;
      if (target != null)
        forward = (target.position - transform.position).normalized;

      if (debugDraw)
      {
        Debug.DrawRay(transform.position, forward * useRange, empowered ? Color.yellow : Color.cyan, 0.2f);
        Debug.DrawRay(transform.position, Quaternion.Euler(0, 0, useAngle * 0.5f) * forward * useRange, Color.magenta, 0.2f);
        Debug.DrawRay(transform.position, Quaternion.Euler(0, 0, -useAngle * 0.5f) * forward * useRange, Color.magenta, 0.2f);
      }

      // 근접 판정: OverlapCircle + 부채꼴 필터
      Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, useRange, hitMask);

      int damaged = 0;
      foreach (var col in hits)
      {
        if (col == null) continue;

        // 태그는 자식 콜라이더일 수 있어서 parent도 허용
        if (!string.IsNullOrEmpty(enemyTag) && !col.CompareTag(enemyTag))
        {
          if (col.transform.parent == null || !col.transform.parent.CompareTag(enemyTag))
            continue;
        }

        Vector3 dirToEnemy = (col.transform.position - transform.position).normalized;
        if (Vector3.Angle(forward, dirToEnemy) > useAngle * 0.5f) continue;

        // Character 베이스로 Enemy/Boss 모두 처리
        Character character = col.GetComponentInParent<Character>();
        if (character == null) continue;

        var src = GetComponentInParent<WeaponSource>();
        character.TakeDamage(useDamage, src != null ? src.weaponData : null); // ✅ 변경: 이번 공격 데미지 반영
        damaged++;

        if (damaged >= currentMaxTargets) break;
      }

      // ✅ 디버그(원하면 주석 처리)
      // Debug.Log($"[SurgeBlade] Attack#{attackCount} empowered={empowered} damaged={damaged} useDmg={useDamage} useRange={useRange}");

      // VFX 생성
      SpawnSurgeEffect(forward, empowered);
    }

    private void SpawnSurgeEffect(Vector3 forward, bool empowered)
    {
      GameObject prefab = (empowered && surgePrefabEnhanced != null) ? surgePrefabEnhanced : surgePrefab;
      if (prefab == null) return;

      Vector3 spawnPos = transform.position + forward * surgeSpawnOffset;
      float angleDeg = Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg;
      GameObject go = Instantiate(prefab, spawnPos, Quaternion.Euler(0f, 0f, angleDeg));
      go.transform.localScale = Vector3.one * surgeScale;

      Destroy(go, surgeDuration);
    }

    private Transform FindClosestEnemy(float searchRange)
    {
      GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
      GameObject closest = null;
      float minDist = searchRange > 0 ? searchRange : 10f;

      foreach (GameObject e in enemies)
      {
        if (e == null) continue;
        float d = Vector3.Distance(transform.position, e.transform.position);
        if (d < minDist)
        {
          minDist = d;
          closest = e;
        }
      }
      return closest ? closest.transform : null;
    }

    private void OnDrawGizmosSelected()
    {
      Gizmos.color = Color.yellow;
      Gizmos.DrawWireSphere(transform.position, range);
    }
  }
}

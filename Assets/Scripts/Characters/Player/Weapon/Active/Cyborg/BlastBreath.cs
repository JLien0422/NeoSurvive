using UnityEngine;

namespace NeoSurvive.Weapon
{
  /// <summary>
  /// 10번 무기: 블래스트 브레스
  /// - 플레이어 앞에서 발사
  /// - 가장 가까운 적을 향해 자동 조준
  /// - Lv5 마스터: 냉기 브레스로 전환
  /// </summary>
  public class BlastBreath : MonoBehaviour
  {
    [Header("Breath Prefab")]
    public GameObject breathAreaPrefab;   // BreathArea가 붙은 프리팹
    public Transform firePoint;           // 선택: 총구 위치

    [Header("Stats")]
    public float damagePerTick = 4f;
    public float range = 4.5f;
    public float width = 2.2f;
    public float tickInterval = 0.25f;
    public float areaDuration = 0.8f;
    public float fireRate = 1.1f;

    [Header("Level Scaling")]
    public float damagePerLevel = 0.18f;
    public float rangePerLevel = 0.12f;
    public float widthPerLevel = 0.10f;

    [Header("Master (Lv5)")]
    public bool enableMaster = true;
    public bool masterColdMode = true;

    [Header("Targeting")]
    public float aimRange = 8f;
    public LayerMask enemyMask;

    private float timer;

    // base stat cache
    private float baseDamage;
    private float baseRange;
    private float baseWidth;

    private int currentLevel = 1;

    private void Start()
    {
      baseDamage = damagePerTick;
      baseRange = range;
      baseWidth = width;

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

      damagePerTick = baseDamage * (1f + (currentLevel - 1) * damagePerLevel);
      range = baseRange * (1f + (currentLevel - 1) * rangePerLevel);
      width = baseWidth * (1f + (currentLevel - 1) * widthPerLevel);
    }

    /// <summary>
    /// 브레스 발사
    /// </summary>
    private void EmitBreath()
    {
      if (breathAreaPrefab == null) return;

      // 1️⃣ 시작 위치
      Vector3 origin = firePoint ? firePoint.position : transform.position;

      // 2️⃣ 가장 가까운 적 찾기
      Transform target = FindClosestEnemy(origin);

      // 3️⃣ 방향 결정
      Vector3 forward = target != null
        ? (target.position - origin).normalized
        : transform.right;

      // 4️⃣ 브레스 영역 중심
      Vector3 center = origin + forward * (range * 0.5f);

      // 5️⃣ 회전
      float angleZ = Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg;
      Quaternion rot = Quaternion.Euler(0, 0, angleZ);

      // 6️⃣ 생성
      GameObject obj = Instantiate(breathAreaPrefab, center, rot);

      var area = obj.GetComponent<BreathArea>();
      if (area != null)
      {
        bool cold = enableMaster && currentLevel >= 5 && masterColdMode;
        area.Initialize(damagePerTick, tickInterval, areaDuration, range, width, cold);
      }

      Destroy(obj, areaDuration + 0.05f);
    }

    /// <summary>
    /// 가장 가까운 적 탐색
    /// </summary>
    private Transform FindClosestEnemy(Vector3 origin)
    {
      Collider2D[] hits =
        Physics2D.OverlapCircleAll(origin, aimRange, enemyMask);

      Transform closest = null;
      float minDist = float.MaxValue;

      foreach (var h in hits)
      {
        if (!h.CompareTag("Enemy")) continue;

        float d = Vector2.Distance(origin, h.transform.position);
        if (d < minDist)
        {
          minDist = d;
          closest = h.transform;
        }
      }

      return closest;
    }
  }
}

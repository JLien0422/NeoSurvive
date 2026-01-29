using UnityEngine;

namespace NeoSurvive.Weapon
{
  /// <summary>
  /// 10번 무기: 블래스트 브레스
  /// - 전방에 고열 화염 방사(지속 영역 생성)
  /// - Lv.Up: 방사 거리(=range), 지속딜 증가(=damage)
  /// - Lv.5 마스터(임시): 브레스가 "냉기"로 강화(슬로우 훅/빙결 훅)
  /// </summary>
  public class BlastBreath : MonoBehaviour
  {
    [Header("Breath Prefab")]
    public GameObject breathAreaPrefab;     // BreathArea가 붙은 프리팹(필수)
    public Transform firePoint;             // 없으면 transform.position

    [Header("Stats")]
    public float damagePerTick = 4f;
    public float range = 4.5f;              // 전방 길이(거리)
    public float width = 2.2f;              // 폭(부채꼴 느낌을 직사각+둥근 가장자리로 근사)
    public float tickInterval = 0.25f;      // 지속딜 틱 간격
    public float areaDuration = 0.8f;       // 한 번 방사 영역 유지 시간
    public float fireRate = 1.1f;           // 방사 주기

    [Header("Level Scaling")]
    public float damagePerLevel = 0.18f;
    public float rangePerLevel = 0.12f;
    public float widthPerLevel = 0.10f;

    [Header("Master (Lv5)")]
    public bool enableMaster = true;
    public bool masterColdMode = true;      // Lv5에서 냉기 모드로 전환(시각/효과 훅)

    [Header("Debug")]
    public bool debugLog = false;

    private float timer;

    // base
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
      if (debugLog)
        Debug.Log($"[BlastBreath] Lv.{currentLevel} dmgTick={damagePerTick} range={range} width={width}");
    }

    private void ApplyLevel(int level)
    {
      currentLevel = Mathf.Clamp(level, 1, 5);

      damagePerTick = baseDamage * (1f + (currentLevel - 1) * damagePerLevel);
      range = baseRange * (1f + (currentLevel - 1) * rangePerLevel);
      width = baseWidth * (1f + (currentLevel - 1) * widthPerLevel);
    }

    private void EmitBreath()
    {
      if (breathAreaPrefab == null) return;

      Vector3 origin = firePoint ? firePoint.position : transform.position;

      // 기본 방향: 플레이어 오른쪽(방향 시스템 있으면 교체)
      Vector3 forward = transform.right;

      // 영역 위치: 전방 range/2 지점
      Vector3 center = origin + forward * (range * 0.5f);

      // 회전: forward를 바라보도록
      float angleZ = Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg;
      Quaternion rot = Quaternion.Euler(0, 0, angleZ);

      GameObject obj = Instantiate(breathAreaPrefab, center, rot);

      var area = obj.GetComponent<BreathArea>();
      if (area != null)
      {
        bool cold = enableMaster && currentLevel >= 5 && masterColdMode;
        area.Initialize(damagePerTick, tickInterval, areaDuration, range, width, cold);
      }

      Destroy(obj, areaDuration + 0.05f);
    }
  }
}

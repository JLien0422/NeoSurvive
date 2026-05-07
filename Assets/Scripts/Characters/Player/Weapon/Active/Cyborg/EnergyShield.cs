using System.Collections.Generic;
using UnityEngine;

namespace NeoSurvive.Weapon
{
  /// <summary>
  /// 3번 무기: 에너지 방패
  /// - 주변을 회전하며 적/투사체 방어 + 접촉딜
  /// - Lv.Up: 방패 갯수, 데미지 증가
  /// - Lv.5 마스터: 방패들이 연결된 완전한 원형(시각 효과 + 접촉딜 강화)
  /// </summary>
  public class EnergyShield : MonoBehaviour
  {
    [Header("Orb Prefab")]
    public GameObject shieldOrbPrefab;   // ShieldOrb가 붙은 프리팹(필수)

    [Header("Stats")]
    public float damage = 8f;
    public float radius = 1.6f;          // 플레이어 중심에서 방패 거리
    public float rotationSpeed = 180f;   // deg/sec
    public float fireRate = 0f;          // 사용 안함(연속형 무기라 Attack 없음)

    [Header("Level Scaling")]
    public float damagePerLevel = 0.2f;  // 레벨당 +20%
    public float radiusPerLevel = 0.05f; // 레벨당 +5%
    public int baseOrbCount = 1;
    public int maxOrbCount = 5;

    [Header("Collision Filter")]
    public LayerMask enemyMask;          // Enemy 레이어
    public string enemyTag = "Enemy";

    [Header("Projectile Block")]
    public bool blockProjectiles = true;
    public string projectileTag = "Projectile"; // 팀 프로젝트에 맞게 변경 가능

    [Header("Master (Lv5)")]
    public bool enableMaster = true;
    public float masterDamageFactor = 1.5f; // Lv5 접촉딜 강화 배수
    public bool drawRingGizmo = true;

    private readonly List<ShieldOrb> orbs = new();

    // base stats
    private float baseDamage;
    private float baseRadius;

    private int currentLevel = 1;
    private int currentOrbCount = 1;
    private float currentAngle = 0f;

    private void Start()
    {
      baseDamage = damage;
      baseRadius = radius;

      ApplyLevel(1);
      RebuildOrbs();
    }

    private void Update()
    {
      // 부모의 방향이나 스케일 반전에 영향받지 않게 절대 각도 사용
      currentAngle += rotationSpeed * Time.deltaTime;
      UpdateOrbPositions();
    }

    // WeaponManager가 SendMessage로 호출
    public void OnLevelUp(int level)
    {
      ApplyLevel(level);
      RebuildOrbs();

      Debug.Log($"[EnergyShield] Lv.{currentLevel} -> Orbs:{currentOrbCount}, Dmg:{damage}, Radius:{radius}");
    }

    private void ApplyLevel(int level)
    {
      currentLevel = Mathf.Clamp(level, 1, 5);

      damage = baseDamage * (1f + (currentLevel - 1) * damagePerLevel);
      radius = baseRadius * (1f + (currentLevel - 1) * radiusPerLevel);

      // 방패 개수 증가 규칙(원하면 변경 가능)
      // Lv1=1, Lv2=2, Lv3=3, Lv4=4, Lv5=5(최대)
      currentOrbCount = Mathf.Clamp(baseOrbCount + (currentLevel - 1), baseOrbCount, maxOrbCount);
    }

    private void RebuildOrbs()
    {
      if (shieldOrbPrefab == null)
      {
        Debug.LogError("[EnergyShield] shieldOrbPrefab is NULL! ShieldOrb 프리팹을 넣어줘야 함");
        return;
      }
      if (InGameSoundManager.Instance != null) InGameSoundManager.Instance.PlayEnergyShieldFire();

      // 기존 오브 제거
      for (int i = orbs.Count - 1; i >= 0; i--)
      {
        if (orbs[i] != null) Destroy(orbs[i].gameObject);
      }
      orbs.Clear();

      // 오브 생성
      for (int i = 0; i < currentOrbCount; i++)
      {
        GameObject obj = Instantiate(shieldOrbPrefab, transform);
        obj.name = $"ShieldOrb_{i}";
        var orb = obj.GetComponent<ShieldOrb>();
        if (orb == null)
        {
          Debug.LogError("[EnergyShield] shieldOrbPrefab에 ShieldOrb 컴포넌트가 없음");
          Destroy(obj);
          continue;
        }

        // 오브 설정
        float dmg = damage;
        bool master = enableMaster && currentLevel >= 5;
        if (master) dmg *= masterDamageFactor;

        orb.Configure(dmg, enemyMask, enemyTag, blockProjectiles, projectileTag);

        orbs.Add(orb);
      }

      UpdateOrbPositions();
    }

    private void UpdateOrbPositions()
    {
      if (orbs.Count == 0) return;

      float step = 360f / orbs.Count;
      for (int i = 0; i < orbs.Count; i++)
      {
        if (orbs[i] == null) continue;

        float ang = (currentAngle + step * i) * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f) * radius;

        // 로컬 포지션 대신 글로벌 포지션으로 강제 할당하여 캐릭터 좌우반전 스케일에 영향받지 않게 함
        orbs[i].transform.position = transform.position + offset;
        orbs[i].transform.rotation = Quaternion.identity; // 오브 자체는 정면 유지
      }
    }

    private void OnDrawGizmosSelected()
    {
      if (!drawRingGizmo) return;

      Gizmos.color = (enableMaster && currentLevel >= 5) ? Color.magenta : Color.cyan;
      Gizmos.DrawWireSphere(transform.position, radius);
    }
  }
}

using UnityEngine;
using NeoSurvive.Core;

namespace NeoSurvive.Weapon
{
  public class AutoTurret : MonoBehaviour
  {
    [Header("터렛 생성기 설정")]
    public GameObject dronePrefab;
    public GameObject projectilePrefab;
    public float installCooldown = 5f;

    [Header("터렛 설정")]
    public float lifeTime = 10f;
    public float damage = 5f;
    public float range = 5f;
    public float fireRate = 1f;

    public float elpased = 0f;

    // ★ 추가
    private readonly string weaponId = "autoturret";

    // ★ 추가: 현재 레벨 저장
    private int currentLevel = 1;

    // ★ 추가: Lv5 이상이면 마스터 포탑으로 설치
    private bool IsMasterLevel => currentLevel >= 5;

    private void Start()
    {
      // ★ 수정: 현재 레벨 저장 후 CSV 적용
      currentLevel = 1;
      ApplyStatsFromCSV(currentLevel);
    }

    public void OnLevelUp(int level)
    {
      // ★ 추가: 현재 레벨 저장
      currentLevel = level;

      // ★ 수정: CSV 적용
      ApplyStatsFromCSV(currentLevel);

      Debug.Log($"[AutoTurret] CSV 적용 | Lv={currentLevel}, Damage={damage}, Range={range}, LifeTime={lifeTime}, Master={IsMasterLevel}");
    }

    // ★ 핵심
    private void ApplyStatsFromCSV(int level)
    {
      if (WeaponStatLoader.DB == null)
      {
        Debug.LogWarning("[AutoTurret] DB 없음");
        return;
      }

      if (!WeaponStatLoader.DB.rows.TryGetValue(weaponId, out var levelDict))
      {
        Debug.LogWarning($"[AutoTurret] weaponId 없음: {weaponId}");
        return;
      }

      if (!levelDict.TryGetValue(level, out var row))
      {
        Debug.LogWarning($"[AutoTurret] level 데이터 없음: {level}");
        return;
      }

      // Lv1 기준 가져오기
      var baseRow = row;
      if (levelDict.TryGetValue(1, out var levelOneRow))
      {
        baseRow = levelOneRow;
      }

      // 증가율 가져오기
      float dmgPer = row.damageperlevel > 0 ? row.damageperlevel : baseRow.damageperlevel;
      float rangePer = row.rangeperlevel > 0 ? row.rangeperlevel : baseRow.rangeperlevel;
      float lifePer = row.lifetimeperlevel > 0 ? row.lifetimeperlevel : baseRow.lifetimeperlevel;

      // 최종 계산
      damage = baseRow.damage * (1f + (level - 1) * dmgPer);
      range = baseRow.range * (1f + (level - 1) * rangePer);
      lifeTime = baseRow.lifetime * (1f + (level - 1) * lifePer);

      // 그대로 쓰는 값들
      installCooldown = row.installcooldown;
      fireRate = row.firerate;

      Debug.Log($"[AutoTurret] CSV 적용 | Lv={level}, Damage={damage}, Range={range}, Life={lifeTime}, Cool={installCooldown}");
    }

    private void Update()
    {
      elpased += Time.deltaTime;
      if (elpased < installCooldown) return;

      Vector2 randomOffset = Random.insideUnitCircle * 0.5f;

      GameObject drone = Instantiate(dronePrefab, transform.position + (Vector3)randomOffset, Quaternion.identity);

      DeployedTurret deployedTurret = drone.GetComponent<DeployedTurret>();

      if (deployedTurret == null)
      {
        Debug.LogWarning("[AutoTurret] DeployedTurret 컴포넌트 없음");
        return;
      }

      var src = GetComponentInParent<WeaponSource>();

      deployedTurret.Initialize(
        damage,
        range,
        fireRate,
        lifeTime,
        projectilePrefab,
        src != null ? src.weaponData : null,
        IsMasterLevel
      );

      elpased = 0f;
    }
  }
}
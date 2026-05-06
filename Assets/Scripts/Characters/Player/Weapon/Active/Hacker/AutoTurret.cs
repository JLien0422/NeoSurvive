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

    private void Start()
    {
      // ★ 수정: CSV 적용
      ApplyStatsFromCSV(1);
    }

    public void OnLevelUp(int level)
    {
      // ★ 수정: CSV 적용
      ApplyStatsFromCSV(level);

      Debug.Log($"[AutoTurret] CSV 적용 | Lv={level}, Damage={damage}, Range={range}, LifeTime={lifeTime}");
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

      var src = GetComponentInParent<WeaponSource>();

      deployedTurret.Initialize(
        damage,
        range,
        fireRate,
        lifeTime,
        projectilePrefab,
        src != null ? src.weaponData : null
      );

      elpased = 0f;
    }
  }
}
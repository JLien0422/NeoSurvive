using UnityEngine;
using NeoSurvive.Core;

namespace NeoSurvive.Weapon
{
  /// <summary>
  /// 설치형 자동 포탑 생성기 (Spawn Weapon)
  /// </summary>
  public class AutoTurret : MonoBehaviour
  {
    [Header("터렛 생성기 설정")]
    /// <summary>
    /// 설치 터렛 프리팹
    /// </summary>
    public GameObject dronePrefab;

    /// <summary>
    /// 발사체 프리팹
    /// </summary>
    public GameObject projectilePrefab;

    /// <summary>
    /// 드론 터렛 설치 쿨타임
    /// </summary>
    public float installCooldown = 5f;

    [Header("터렛 설정")]
    /// <summary>
    /// 터렛 지속 시간
    /// </summary>
    public float lifeTime = 10f;

    /// <summary>
    /// 터렛 총알 당 데미지
    /// </summary>
    public float damage = 5f;

    /// <summary>
    /// 터렛 공격 범위
    /// </summary>
    public float range = 5f;

    /// <summary>
    /// 터렛 공격 속도
    /// </summary>
    public float fireRate = 1f;

    public float elpased = 0f;

    private float baseDamage;
    private float baseRange;
    private float baseLifeTime;

    private void Start()
    {
      baseDamage = damage;
      baseRange = range;
      baseLifeTime = lifeTime;
    }

    public void OnLevelUp(int level)
    {
      if (baseDamage == 0 && damage > 0)
      {
        baseDamage = damage;
        baseRange = range;
        baseLifeTime = lifeTime;
      }

      // 레벨업 스탯 증가
      damage = baseDamage * (1f + (level - 1) * 0.2f);
      range = baseRange * (1f + (level - 1) * 0.1f);
      lifeTime = baseLifeTime * (1f + (level - 1) * 0.1f);

      Debug.Log($"[AutoTurret] Lv.{level} : Damage {damage}, Range {range}, Duration {lifeTime}");

      if (level >= 5)
      {
        // TODO: 호위 기능 (DeployedTurret 이동 로직 필요)
      }
    }

    private void Update()
    {
      elpased += Time.deltaTime;
      if (elpased < installCooldown) return;
      Debug.Log("터렛 생성!");

      Vector2 randomOffset = Random.insideUnitCircle * 0.5f;

      // [Coop] 서버에 소환 보고 (로컬 플레이어일 때만)
      var runtimeInfo = GetComponent<WeaponRuntimeInfo>();
      if (runtimeInfo != null)
      {
        runtimeInfo.ReportSummon(transform.position + (Vector3)randomOffset, lifeTime, 0f, false, 0f);
      }

      GameObject drone = Instantiate(dronePrefab, transform.position + (Vector3)randomOffset, Quaternion.identity);
      DeployedTurret deployedTurret = drone.GetComponent<DeployedTurret>();
      var src = GetComponentInParent<WeaponSource>();
      deployedTurret.Initialize(damage, range, fireRate, lifeTime, projectilePrefab, src != null ? src.weaponData : null);

      elpased = 0f;
    }
  }
}

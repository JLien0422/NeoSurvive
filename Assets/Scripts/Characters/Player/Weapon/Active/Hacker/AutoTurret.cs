using System.Collections;
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

    [Header("Deploy VFX")]
    public GameObject deployVfxPrefab;      // ★ 추가: 전개 애니메이션 프리팹
    public float deployDuration = 0.7f;     // ★ 추가: 전개 시간
    public float deployVfxScale = 1.0f;     // ★ 추가: 전개 VFX 크기

    [Header("터렛 설정")]
    public float lifeTime = 10f;
    public float damage = 5f;
    public float range = 5f;
    public float fireRate = 1f;

    public float elpased = 0f;

    private readonly string weaponId = "autoturret";

    private int currentLevel = 1;

    private bool IsMasterLevel => currentLevel >= 5;

    private void Start()
    {
      currentLevel = 1;
      ApplyStatsFromCSV(currentLevel);
    }

    public void OnLevelUp(int level)
    {
      currentLevel = level;
      ApplyStatsFromCSV(currentLevel);

      Debug.Log($"[AutoTurret] CSV 적용 | Lv={currentLevel}, Damage={damage}, Range={range}, LifeTime={lifeTime}, Master={IsMasterLevel}");
    }

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

      damage = row.damage;
      range = row.range;
      lifeTime = row.lifetime;
      installCooldown = row.installcooldown;
      fireRate = row.firerate;

      if (Player.Instance != null)
      {
        installCooldown *= Player.Instance.GetComputeOptimizationCooldownMultiplier();
        lifeTime *= Player.Instance.GetFacilityAugmentationDurationMultiplier();
      }

      Debug.Log($"[AutoTurret] CSV 적용 | Lv={level}, Damage={damage}, Range={range}, Life={lifeTime}, Cool={installCooldown}");
    }

    private void Update()
    {
      elpased += Time.deltaTime;
      if (elpased < installCooldown) return;

      Vector2 randomOffset = Random.insideUnitCircle * 0.5f;
      Vector3 spawnPos = transform.position + (Vector3)randomOffset;

      // ★ 수정: 전개 VFX가 끝난 뒤 실제 포탑 생성
      StartCoroutine(DeployTurretRoutine(spawnPos));

      elpased = 0f;
    }

    // ★ 추가: 전개 연출 후 포탑 생성
    private IEnumerator DeployTurretRoutine(Vector3 spawnPos)
    {
      if (deployVfxPrefab != null)
      {
        GameObject vfx = Instantiate(
          deployVfxPrefab,
          spawnPos,
          Quaternion.identity
        );

        vfx.transform.localScale = Vector3.one * deployVfxScale;

        Destroy(vfx, deployDuration);
      }

      if (deployDuration > 0f)
        yield return new WaitForSeconds(deployDuration);

      if (dronePrefab == null)
        yield break;

      if (InGameSoundManager.Instance != null)
      {
        InGameSoundManager.Instance.PlayTacticalTurretSpawn();
      }

      GameObject drone = Instantiate(
        dronePrefab,
        spawnPos,
        Quaternion.identity
      );

      DeployedTurret deployedTurret = drone.GetComponent<DeployedTurret>();

      if (deployedTurret == null)
      {
        Debug.LogWarning("[AutoTurret] DeployedTurret 컴포넌트 없음");
        yield break;
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

      if (Player.Instance != null && Player.Instance.HasTrait("bandwidth_expansion"))
      {
        SpawnExtraTurret(spawnPos + Vector3.right * 0.6f, src != null ? src.weaponData : null);
      }
    }

    private void SpawnExtraTurret(Vector3 spawnPos, WeaponBase sourceWeapon)
    {
      if (dronePrefab == null)
        return;

      GameObject extraDrone = Instantiate(dronePrefab, spawnPos, Quaternion.identity);
      DeployedTurret extraTurret = extraDrone.GetComponent<DeployedTurret>();

      if (extraTurret == null)
        return;

      float cloneMul = Player.Instance != null ? Player.Instance.GetBandwidthExpansionCloneMultiplier() : 1f;

      extraTurret.Initialize(
        damage * cloneMul,
        range * cloneMul,
        fireRate,
        lifeTime * cloneMul,
        projectilePrefab,
        sourceWeapon,
        IsMasterLevel
      );
    }
  }
}
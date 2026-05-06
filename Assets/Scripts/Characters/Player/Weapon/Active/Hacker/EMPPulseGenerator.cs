using UnityEngine;
using System.Collections.Generic;
using NeoSurvive.Core;

namespace NeoSurvive.Weapon
{
  public class EMPPulseGenerator : MonoBehaviour
  {
    [Header("Stats")]
    public GameObject empFieldPrefab;

    public float damage = 5f;
    public float areaSize = 2.0f;
    public float duration = 4f;
    public float fireRate = 3f;
    public float spawnRadius = 4.0f;

    private float fireTimer;
    private int currentLevel = 1;

    // ★ 추가: CSV weaponid
    private readonly string weaponId = "emppulsegenerator";

    private void Start()
    {
      // ★ 수정: 시작 시 Lv1 CSV 적용
      ApplyStatsFromCSV(1);
    }

    private void Update()
    {
      fireTimer += Time.deltaTime;
      if (fireTimer >= fireRate)
      {
        SpawnEMPField();
        fireTimer = 0f;
      }
    }

    private void SpawnEMPField()
    {
      if (empFieldPrefab == null) return;

      Vector2 randomPos = Random.insideUnitCircle * spawnRadius;
      Vector3 spawnPosition = transform.position + (Vector3)randomPos;

      GameObject obj = Instantiate(empFieldPrefab, spawnPosition, Quaternion.identity);

      if (obj.TryGetComponent<EMPField>(out var field))
      {
        var src = GetComponentInParent<WeaponSource>();
        field.Initialize(damage, areaSize, duration, currentLevel >= 5, src != null ? src.weaponData : null);
      }
    }

    public void OnLevelUp(int level)
    {
      currentLevel = Mathf.Clamp(level, 1, 5);

      // ★ 수정: CSV 적용
      ApplyStatsFromCSV(currentLevel);

      Debug.Log($"[EMP Pulse] CSV 적용 | Lv={currentLevel}, Dmg={damage}, Area={areaSize}, Duration={duration}, ProjDestroy={currentLevel >= 5}");
    }

    // ★ 추가: CSV 적용 함수
    private void ApplyStatsFromCSV(int level)
    {
      if (WeaponStatLoader.DB == null)
      {
        Debug.LogWarning("[EMP Pulse] WeaponStatLoader.DB 없음");
        return;
      }

      if (!WeaponStatLoader.DB.rows.TryGetValue(weaponId, out var levelDict))
      {
        Debug.LogWarning($"[EMP Pulse] weaponId 없음: {weaponId}");
        return;
      }

      if (!levelDict.TryGetValue(level, out var row))
      {
        Debug.LogWarning($"[EMP Pulse] level 데이터 없음: {level}");
        return;
      }

      WeaponStatDB.Row baseRow = row;
      if (levelDict.TryGetValue(1, out var levelOneRow))
      {
        baseRow = levelOneRow;
      }

      float damagePer = row.damageperlevel > 0f ? row.damageperlevel : baseRow.damageperlevel;
      float areaPer = row.areasizeperlevel > 0f ? row.areasizeperlevel : baseRow.areasizeperlevel;

      damage = baseRow.damage * (1f + (level - 1) * damagePer);
      areaSize = baseRow.areasize * (1f + (level - 1) * areaPer);

      duration = row.duration;
      fireRate = row.firerate;
      spawnRadius = row.spawnradius;

      Debug.Log($"[EMP Pulse] CSV 적용 | Lv={level}, Damage={damage}, Area={areaSize}, Duration={duration}, FireRate={fireRate}, SpawnRadius={spawnRadius}");
    }
  }
}
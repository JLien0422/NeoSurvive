using UnityEngine;
using System.Collections.Generic;
using NeoSurvive.Core;

namespace NeoSurvive.Weapon
{
  /// <summary>
  /// 6번 무기: EMP 펄스 생성기
  /// 화면 내 무작위 위치에 지속 피해 장판 생성.
  /// Lv.5 달성 시 생성 시점에 적 투사체 파괴.
  /// </summary>
  public class EMPPulseGenerator : MonoBehaviour
  {
    [Header("Stats")]
    public GameObject empFieldPrefab; // 장판 프리팹 (SpriteRenderer + EMPField 스크립트 필요)

    public float damage = 5f;
    public float areaSize = 2.0f;     // 장판 반지름 (LocalScale로 조정 or OverlapCircle 범위)
    public float duration = 4f;       // 장판 지속 시간
    public float fireRate = 3f;       // 장판 생성 주기
    public float spawnRadius = 4.0f;  // 플레이어 주변 생성 반경

    private float fireTimer;
    private int currentLevel = 1;

    // Base Stats
    private float baseDamage = 2f;
    private float baseAreaSize = 2f;

    private void Start()
    {
      baseDamage = damage;
      baseAreaSize = areaSize;
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

      // 플레이어 주변 무작위 위치
      Vector2 randomPos = Random.insideUnitCircle * spawnRadius;
      Vector3 spawnPosition = transform.position + (Vector3)randomPos;

      // [Coop] 서버에 장판 생성 보고 (로컬 플레이어일 때만)
      var runtimeInfo = GetComponent<WeaponRuntimeInfo>();
      if (runtimeInfo != null)
      {
        runtimeInfo.ReportArea(spawnPosition, areaSize, duration, 0.5f, currentLevel >= 5);
      }

      GameObject obj = Instantiate(empFieldPrefab, spawnPosition, Quaternion.identity);

      // 장판 초기화
      if (obj.TryGetComponent<EMPField>(out var field))
      {
        // Lv.5 이상이면 투사체 파괴 옵션 true
        field.Initialize(damage, areaSize, duration, currentLevel >= 5);
      }
    }

    public void OnLevelUp(int level)
    {
      currentLevel = level;
      if (baseDamage == 0 && damage > 0) baseDamage = damage;

      // 레벨업: 범위(areaSize) 증가 (기획) + 데미지도 10% 증가
      areaSize = baseAreaSize * (1f + (level - 1) * 0.15f);
      damage = baseDamage * (1f + (level - 1) * 0.1f);

      Debug.Log($"[EMP Pulse] Lv.{level} : Dmg {damage}, Area {areaSize}, ProjDestroy: {level >= 5}");
    }
  }
}



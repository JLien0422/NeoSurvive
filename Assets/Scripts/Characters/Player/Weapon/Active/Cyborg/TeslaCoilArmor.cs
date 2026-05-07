using UnityEngine;
using NeoSurvive.Core;

namespace NeoSurvive.Weapon
{
  /// <summary>
  /// 7번 무기: 테슬라 코일 아머
  /// - 기본: 몸 주변 지속 전류(오라)로 지속 피해
  /// - Lv.5 마스터: 이동 경로에 전기 장판 + 무작위 적에게 낙뢰
  /// - Lv.Up: 전류 범위, 대미지 증가
  /// </summary>
  public class TeslaCoilArmor : MonoBehaviour
  {
    [Header("Aura Stats")]
    public float auraDamage = 6f;
    public float auraRadius = 2.2f;
    public float auraTick = 0.4f;

    [Header("Filter")]
    public LayerMask enemyMask;
    public string enemyTag = "Enemy";

    [Header("Level Scaling")]
    public float damagePerLevel = 0.18f;
    public float radiusPerLevel = 0.12f;

    [Header("Master (Lv5) - Electric Puddle")]
    public bool enableMaster = true;
    public GameObject puddlePrefab;      // ElectricPuddle 프리팹(선택)
    public float puddleInterval = 0.35f; // 이동 중 생성 간격
    public float puddleDuration = 1.6f;
    public float puddleRadius = 1.1f;
    public float puddleTick = 0.35f;
    public float puddleDamageFactor = 0.5f; // 오라 데미지 대비 배수

    [Header("Master (Lv5) - Lightning")]
    public GameObject lightningPrefab;   // LightningStrike 프리팹(선택)
    public float lightningInterval = 1.2f;
    public float lightningRange = 10f;
    public float lightningDamageFactor = 1.2f;

    [Header("Debug")]
    public bool debugDraw = true;

    private float auraTimer;
    private float puddleTimer;
    private float lightningTimer;

    private Vector3 lastPuddlePos;

    // base
    private float baseAuraDamage;
    private float baseAuraRadius;

    private int currentLevel = 1;

    private void Start()
    {
      baseAuraDamage = auraDamage;
      baseAuraRadius = auraRadius;

      ApplyLevel(1);
      lastPuddlePos = transform.position;
    }

    private void Update()
    {
      // 1) 오라 틱딜
      auraTimer += Time.deltaTime;
      if (auraTimer >= auraTick)
      {
        AuraTickDamage();
        auraTimer = 0f;
      }

      bool master = enableMaster && currentLevel >= 5;

      // 2) 마스터: 이동 경로 전기 장판
      if (master)
      {
        puddleTimer += Time.deltaTime;
        if (puddleTimer >= puddleInterval)
        {
          DropPuddleIfMoved();
          puddleTimer = 0f;
        }

        // 3) 마스터: 랜덤 번개
        lightningTimer += Time.deltaTime;
        if (lightningTimer >= lightningInterval)
        {
          StrikeRandomEnemy();
          lightningTimer = 0f;
        }
      }
    }

    public void OnLevelUp(int level)
    {
      ApplyLevel(level);
      Debug.Log($"[TeslaCoilArmor] Lv.{currentLevel} auraDmg={auraDamage} auraRadius={auraRadius}");
    }

    private void ApplyLevel(int level)
    {
      currentLevel = Mathf.Clamp(level, 1, 5);

      auraDamage = baseAuraDamage * (1f + (currentLevel - 1) * damagePerLevel);
      auraRadius = baseAuraRadius * (1f + (currentLevel - 1) * radiusPerLevel);
    }

    private void AuraTickDamage()
    {
      Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, auraRadius, enemyMask);
      bool didHit = false;
      foreach (var col in hits)
      {
        if (col == null) continue;

        // 태그(자식 콜라이더 구조 고려)
        if (!string.IsNullOrEmpty(enemyTag) && !col.CompareTag(enemyTag))
        {
          if (col.transform.parent == null || !col.transform.parent.CompareTag(enemyTag))
            continue;
        }

        Character character = col.GetComponentInParent<Character>();
        if (character != null)
        {
          var src = GetComponentInParent<WeaponSource>();
          character.TakeDamage(auraDamage, src != null ? src.weaponData : null);
          didHit = true;
        }
      }

      if (didHit && InGameSoundManager.Instance != null) InGameSoundManager.Instance.PlayTeslaCoilArmorFire();

      if (debugDraw)
        Debug.DrawRay(transform.position, Vector3.right * 0.01f, Color.yellow, 0.1f);
    }

    private void DropPuddleIfMoved()
    {
      if (puddlePrefab == null) return;

      float moved = Vector3.Distance(transform.position, lastPuddlePos);
      if (moved < 0.2f) return; // 거의 안 움직였으면 생성 안 함

      lastPuddlePos = transform.position;

      GameObject obj = Instantiate(puddlePrefab, transform.position, Quaternion.identity);
      var puddle = obj.GetComponent<ElectricPuddle>();
      if (puddle != null)
      {
        float dmg = auraDamage * puddleDamageFactor;
        puddle.Initialize(dmg, puddleDuration, puddleRadius, puddleTick, enemyMask, enemyTag);
      }
      Destroy(obj, puddleDuration + 0.1f);
    }

    private void StrikeRandomEnemy()
    {
      // 범위 내 적 찾기
      Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, lightningRange, enemyMask);
      if (hits == null || hits.Length == 0) return;

      // 랜덤 선택 (유효 Enemy만 필터)
      // 후보가 많지 않으면 단순 루프가 더 안전
      int attempts = Mathf.Min(10, hits.Length);
      for (int i = 0; i < attempts; i++)
      {
        var col = hits[Random.Range(0, hits.Length)];
        if (col == null) continue;

        if (!string.IsNullOrEmpty(enemyTag) && !col.CompareTag(enemyTag))
        {
          if (col.transform.parent == null || !col.transform.parent.CompareTag(enemyTag))
            continue;
        }

        Character character = col.GetComponentInParent<Character>();
        if (character == null) continue;

        if (InGameSoundManager.Instance != null) InGameSoundManager.Instance.PlayTeslaCoilArmorFire();

        float dmg = auraDamage * lightningDamageFactor;
        var src2 = GetComponentInParent<WeaponSource>();
        character.TakeDamage(dmg, src2 != null ? src2.weaponData : null);

        // 이펙트(선택)
        if (lightningPrefab != null)
        {
          Instantiate(lightningPrefab, character.transform.position, Quaternion.identity);
        }
        return;
      }
    }

    private void OnDrawGizmosSelected()
    {
      Gizmos.color = Color.yellow;
      Gizmos.DrawWireSphere(transform.position, auraRadius);
    }
  }
}

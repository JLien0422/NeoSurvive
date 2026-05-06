using System.Collections;
using System.Collections.Generic;
using NeoSurvive.Weapon;
using UnityEngine;

public class PlasmaRifle : MonoBehaviour
{
  [Header("Weapon Settings")]
  public float damage = 5f;
  public float range = 10f;
  public float fireRate = 1f;
  public float bulletSpeed = 20f;

  [Header("Lazer Settings")]
  public GameObject lazerPrefab;

  private float fireTimer = 0f;

  // ★ 추가
  private readonly string weaponId = "plasmarifle";

  private int currentPenetration = 0;

  private void Start()
  {
    // ★ 추가: 시작 시 Lv1 적용
    ApplyStatsFromCSV(1);
  }

  public void OnLevelUp(int level)
  {
    // ★ 수정: CSV 기반으로 변경
    ApplyStatsFromCSV(level);

    Debug.Log($"[PlasmaRifle] Lv.{level}, Dmg:{damage}, Pen:{currentPenetration}");
  }

  // ★ 핵심: CSV 적용 함수
  private void ApplyStatsFromCSV(int level)
  {
    if (WeaponStatLoader.DB == null)
    {
      Debug.LogWarning("[PlasmaRifle] DB 없음");
      return;
    }

    if (!WeaponStatLoader.DB.rows.TryGetValue(weaponId, out var levelDict))
    {
      Debug.LogWarning($"[PlasmaRifle] weaponId 없음: {weaponId}");
      return;
    }

    if (!levelDict.TryGetValue(level, out var row))
    {
      Debug.LogWarning($"[PlasmaRifle] level 데이터 없음: {level}");
      return;
    }

    // =========================
    // 🔥 데미지 계산 (LinkPistol 방식 동일)
    // =========================

    float baseDamage = row.damage;

    if (levelDict.TryGetValue(1, out var levelOneRow))
    {
      baseDamage = levelOneRow.damage;
    }

    float perLevel = row.damageperlevel;

    if (perLevel <= 0f && levelDict.TryGetValue(1, out var baseRow))
    {
      perLevel = baseRow.damageperlevel;
    }

    damage = baseDamage * (1f + (level - 1) * perLevel);

    // =========================
    // 🔥 나머지 값 CSV 그대로
    // =========================
    range = row.range;
    fireRate = row.firerate;
    bulletSpeed = row.bulletspeed;

    // 🔥 penetration은 CSV 그대로 사용
    currentPenetration = (int)row.penetration;

    Debug.Log($"[PlasmaRifle] CSV 적용 | Lv={level}, Dmg={damage}, Pen={currentPenetration}");
  }

  private void Update()
  {
    fireTimer += Time.deltaTime;
    if (fireTimer >= fireRate)
    {
      Attack();
      fireTimer = 0f;
    }
  }

  private void Attack()
  {
    GameObject target = FindClosestEnemy();
    if (target == null) return;

    Vector3 dir = (target.transform.position - transform.position).normalized;

    GameObject obj = Instantiate(lazerPrefab, transform.position, Quaternion.identity);
    obj.SetActive(true);

    if (obj.TryGetComponent<PlasmaLazer>(out var lazer))
    {
      var src = GetComponentInParent<NeoSurvive.Weapon.WeaponSource>();
      lazer.Initialize(dir, damage, currentPenetration, src != null ? src.weaponData : null);
    }
  }

  private GameObject FindClosestEnemy()
  {
    GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
    GameObject closest = null;
    float closestDistance = range > 0 ? range : 10f;

    foreach (GameObject enemy in enemies)
    {
      float distance = Vector3.Distance(transform.position, enemy.transform.position);
      if (distance < closestDistance)
      {
        closestDistance = distance;
        closest = enemy;
      }
    }
    return closest;
  }
}
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

  private float baseDamage;
  private int currentPenetration = 0;

  private void Start()
  {
    baseDamage = damage;
  }

  public void OnLevelUp(int level)
  {
    if (baseDamage == 0 && damage > 0) baseDamage = damage;

    damage = baseDamage * (1f + (level - 1) * 0.2f);
    currentPenetration = level - 1;

    Debug.Log($"[PlasmaRifle] Lv.{level}, Dmg:{damage}, Pen:{currentPenetration}");
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
    // Debug.Log 삭제
    GameObject target = FindClosestEnemy();
    if (target == null) return;

    Vector3 dir = (target.transform.position - transform.position).normalized;

    // [Coop] 서버에 공격 보고 (로컬 플레이어일 때만)
    var runtimeInfo = GetComponent<NeoSurvive.Weapon.WeaponRuntimeInfo>();
    if (runtimeInfo != null)
    {
      float duration = 0f;
      float speed = 0f;
      if (lazerPrefab != null && lazerPrefab.TryGetComponent<PlasmaLazer>(out var lazerInfo))
      {
        duration = lazerInfo.duration;
        speed = lazerInfo.bulletSpeed;
      }

      runtimeInfo.ReportBeam(transform.position, dir, duration, speed, currentPenetration);
    }

    GameObject obj = Instantiate(lazerPrefab, transform.position, Quaternion.identity);
    obj.SetActive(true);

    if (obj.TryGetComponent<PlasmaLazer>(out var lazer))
    {
      lazer.Initialize(dir, damage, currentPenetration);
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

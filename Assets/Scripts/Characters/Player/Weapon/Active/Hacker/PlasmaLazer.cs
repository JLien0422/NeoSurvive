using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NeoSurvive.Weapon;

public class PlasmaLazer : MonoBehaviour
{
  public float damage = 5f;
  public float range = 10f;
  public float bulletSpeed = 2000f;
  public float duration = 1f;

  public float elapsed = 0f;
  public float alpha = 1f;

  public TrailRenderer trailRenderer;

  private Vector3 direction;
  private void Start()
  {
    trailRenderer = GetComponent<TrailRenderer>();
  }

  // Update is called once per frame
  void Update()
  {
    elapsed += Time.deltaTime;
    if (elapsed >= duration)
    {
      Destroy(gameObject);
      return;
    }

    // 시간 경과에 따른 알파 비율 (1 -> 0)
    alpha = 1f - (elapsed / duration);

    float moveDist = bulletSpeed * Time.deltaTime;

    // RaycastAll로 경로상 모든 적 감지 후 관통 처리
    RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position, direction, moveDist);
    // 거리순 정렬
    System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

    foreach (var hit in hits)
    {
      if (hit.collider != null && hit.collider.CompareTag("Enemy") && hit.collider.TryGetComponent<Character>(out var character))
      {
        int id = character.GetInstanceID();
        if (hitEnemyIds.Contains(id)) continue;

        character.TakeDamage(damage, sourceWeapon);
        hitEnemyIds.Add(id);
        currentHitCount++;

        // maxPenetration 만큼 추가 관통 가능
        if (currentHitCount > maxPenetration)
        {
          Destroy(gameObject);
          return;
        }
      }
    }

    transform.position += direction * moveDist;
  }

  private int maxPenetration = 0;
  private int currentHitCount = 0;
  private HashSet<int> hitEnemyIds = new HashSet<int>();

  // DPM 기록용 소스 무기
  private WeaponBase sourceWeapon;

  public void Initialize(Vector3 direction, float damage, int penetration, WeaponBase weaponBase = null)
  {
    this.direction = direction;
    this.damage = damage;
    this.maxPenetration = penetration;
    this.sourceWeapon = weaponBase;

    if (trailRenderer != null)
    {
      trailRenderer.time = duration;
    }
  }
}

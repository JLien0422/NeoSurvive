using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NeoSurvive.Weapon;
using NeoSurvive.Core;

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

  private int maxPenetration = 0;
  private int currentHitCount = 0;
  private HashSet<int> hitEnemyIds = new HashSet<int>();

  // DPM 기록용 소스 무기
  private WeaponBase sourceWeapon;

  // ★ 추가: PlasmaRifle Lv5 마스터 효과 여부
  private bool isMaster = false;

  // ★ 추가: 이 레이저가 분열을 만들 수 있는지 여부
  // - 원본 레이저만 true
  // - 분열 레이저는 false로 해서 무한 분열 방지
  private bool canSplit = true;

  [Header("Master Split Settings")]
  [SerializeField] private int splitCount = 2;
  [SerializeField] private float splitSearchRange = 5f;

  private void Start()
  {
    trailRenderer = GetComponent<TrailRenderer>();
  }

  private void Update()
  {
    elapsed += Time.deltaTime;

    if (elapsed >= duration)
    {
      Destroy(gameObject);
      return;
    }

    alpha = 1f - (elapsed / duration);

    float moveDist = bulletSpeed * Time.deltaTime;

    RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position, direction, moveDist);

    System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

    foreach (var hit in hits)
    {
      if (hit.collider != null && hit.collider.CompareTag("Enemy") && hit.collider.TryGetComponent<Character>(out var character))
      {
        int id = character.GetInstanceID();

        if (hitEnemyIds.Contains(id))
          continue;

        character.TakeDamage(damage, sourceWeapon);
        hitEnemyIds.Add(id);
        currentHitCount++;

        // ★ 추가: Lv5 마스터 효과
        // - 적 관통/명중 시 주변 적 2명에게 분열 레이저 생성
        // - 원본 레이저만 분열 가능
        if (isMaster && canSplit)
        {
          SplitToNearbyEnemies(character.transform);
        }

        if (currentHitCount > maxPenetration)
        {
          Destroy(gameObject);
          return;
        }

        continue;
      }

      IDamageable damageable = hit.collider.GetComponent<IDamageable>();

      if (damageable == null)
        damageable = hit.collider.GetComponentInParent<IDamageable>();

      if (damageable == null)
        continue;

      if (hit.collider.CompareTag("Enemy"))
        continue;

      MonoBehaviour mb = damageable as MonoBehaviour;
      if (mb == null)
        continue;

      int objId = mb.GetInstanceID();

      if (hitEnemyIds.Contains(objId))
        continue;

      damageable.TakeDamage(damage);
      hitEnemyIds.Add(objId);
      currentHitCount++;

      if (currentHitCount > maxPenetration)
      {
        Destroy(gameObject);
        return;
      }
    }

    transform.position += direction * moveDist;
  }

  public void Initialize(
    Vector3 direction,
    float damage,
    int penetration,
    WeaponBase weaponBase = null,
    bool isMaster = false,
    bool canSplit = true)
  {
    this.direction = direction.normalized;
    this.damage = damage;
    this.maxPenetration = penetration;
    this.sourceWeapon = weaponBase;

    // ★ 추가
    this.isMaster = isMaster;
    this.canSplit = canSplit;

    if (trailRenderer != null)
    {
      trailRenderer.time = duration;
    }
  }

  // ★ 추가: 주변 적 2명에게 분열 레이저 생성
  private void SplitToNearbyEnemies(Transform hitTarget)
  {
    if (hitTarget == null)
      return;

    Collider2D[] enemies = Physics2D.OverlapCircleAll(
      hitTarget.position,
      splitSearchRange,
      LayerMask.GetMask("Enemy")
    );

    int createdCount = 0;

    foreach (Collider2D enemy in enemies)
    {
      if (enemy == null)
        continue;

      if (!enemy.CompareTag("Enemy"))
        continue;

      if (enemy.transform == hitTarget)
        continue;

      if (!enemy.TryGetComponent<Character>(out var character))
        continue;

      int id = character.GetInstanceID();

      // 이미 이 레이저가 맞춘 적이면 제외
      if (hitEnemyIds.Contains(id))
        continue;

      Vector3 splitDir = (enemy.transform.position - hitTarget.position).normalized;

      if (splitDir == Vector3.zero)
        continue;

      GameObject splitObj = Instantiate(gameObject, hitTarget.position, Quaternion.identity);

      if (splitObj.TryGetComponent<PlasmaLazer>(out var splitLaser))
      {
        // ★ 분열 레이저는 다시 분열하지 않음
        splitLaser.Initialize(
          splitDir,
          damage,
          0,
          sourceWeapon,
          false,
          false
        );
      }

      createdCount++;

      if (createdCount >= splitCount)
        break;
    }
  }
}
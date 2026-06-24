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
  private float traveledDistance = 0f;
  private bool reachedMaxRange = false;

  private int currentHitCount = 0;
  private int maxDamageTargets = 1;
  private readonly HashSet<int> damagedEnemyIds = new HashSet<int>();
  private readonly HashSet<int> damagedObjectIds = new HashSet<int>();

  // DPS 기록용 소스 무기
  private WeaponBase sourceWeapon;

  // ★ PlasmaRifle Lv5 마스터 효과 여부
  private bool isMaster = false;

  // ★ 이 레이저가 분열을 만들 수 있는지 여부
  // - 원본 레이저만 true
  // - 분열 레이저는 false로 해서 무한 분열 방지
  private bool canSplit = true;

  [Header("Master Split Settings")]
  [SerializeField] private int splitCount = 2;
  [SerializeField] private float splitSearchRange = 100f;

  private void Start()
  {
    if (trailRenderer == null)
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

    if (reachedMaxRange)
      return;

    float moveDist = bulletSpeed * Time.deltaTime;
    float remainingRange = range > 0f ? range - traveledDistance : float.PositiveInfinity;

    if (remainingRange <= 0f)
    {
      reachedMaxRange = true;
      return;
    }

    moveDist = Mathf.Min(moveDist, remainingRange);

    if (moveDist <= 0f)
      return;

    RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position, direction, moveDist);
    System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

    foreach (var hit in hits)
    {
      if (hit.collider == null)
        continue;

      Enemy enemy = hit.collider.GetComponentInParent<Enemy>();

      if (enemy != null)
      {
        if (!CanApplyDamageToEnemy(enemy))
          continue;

        enemy.TakeDamage(damage, sourceWeapon);
        currentHitCount++;
        damagedEnemyIds.Add(enemy.GetInstanceID());

        if (isMaster && canSplit)
        {
          SplitToNearbyEnemies(enemy, hit.point);
        }

        continue;
      }

      IDamageable damageable = hit.collider.GetComponent<IDamageable>();

      if (damageable == null)
        damageable = hit.collider.GetComponentInParent<IDamageable>();

      if (damageable == null)
        continue;

      MonoBehaviour mb = damageable as MonoBehaviour;

      if (mb == null)
        continue;

      if (!CanApplyDamageToObject(mb))
        continue;

      damageable.TakeDamage(damage);
      currentHitCount++;
      damagedObjectIds.Add(mb.GetInstanceID());
    }

    transform.position += direction * moveDist;
    traveledDistance += moveDist;

    if (range > 0f && traveledDistance >= range)
      reachedMaxRange = true;
  }

  public void Initialize(
    Vector3 direction,
    float damage,
    float range,
    float bulletSpeed,
    float duration,
    int penetration,
    WeaponBase weaponBase = null,
    bool isMaster = false,
    bool canSplit = true)
  {
    this.direction = direction.normalized;
    this.damage = damage;
    float expansionMul = Player.Instance != null ? Player.Instance.GetCompileNodeExpansionMultiplier() : 1f;
    this.range = Mathf.Max(0f, range * expansionMul);
    this.bulletSpeed = Mathf.Max(0f, bulletSpeed);
    this.duration = Mathf.Max(0.01f, duration);
    this.maxDamageTargets = Mathf.Max(1, penetration + 1);
    this.sourceWeapon = weaponBase;

    this.isMaster = isMaster;
    this.canSplit = canSplit;

    elapsed = 0f;
    alpha = 1f;
    traveledDistance = 0f;
    reachedMaxRange = false;
    currentHitCount = 0;
    damagedEnemyIds.Clear();
    damagedObjectIds.Clear();

    if (trailRenderer == null)
      trailRenderer = GetComponent<TrailRenderer>();

    if (trailRenderer != null)
    {
      trailRenderer.Clear();
      trailRenderer.time = duration;
    }
  }

  private bool CanApplyDamageToEnemy(Enemy enemy)
  {
    if (enemy == null)
      return false;

    if (currentHitCount >= maxDamageTargets)
      return false;

    return !damagedEnemyIds.Contains(enemy.GetInstanceID());
  }

  private bool CanApplyDamageToObject(MonoBehaviour target)
  {
    if (target == null)
      return false;

    if (currentHitCount >= maxDamageTargets)
      return false;

    return !damagedObjectIds.Contains(target.GetInstanceID());
  }

  // ★ 수정:
  // 원본 레이저가 이미 맞춘 적도 분열 대상으로 허용
  private void SplitToNearbyEnemies(Enemy hitEnemy, Vector2 splitStartPoint)
  {
    if (hitEnemy == null)
      return;

    Enemy[] enemies = FindObjectsOfType<Enemy>();

    List<Enemy> candidates = new List<Enemy>();
    HashSet<int> candidateIds = new HashSet<int>();

    foreach (Enemy enemy in enemies)
    {
      if (enemy == null)
        continue;

      if (enemy == hitEnemy)
        continue;

      if (enemy.IsDead)
        continue;

      int id = enemy.GetInstanceID();

      if (candidateIds.Contains(id))
        continue;

      float distance = Vector2.Distance(splitStartPoint, enemy.transform.position);

      if (distance > splitSearchRange)
        continue;

      candidates.Add(enemy);
      candidateIds.Add(id);
    }

    candidates.Sort((a, b) =>
      Vector2.Distance(splitStartPoint, a.transform.position)
      .CompareTo(Vector2.Distance(splitStartPoint, b.transform.position))
    );

    int createdCount = 0;

    foreach (Enemy targetEnemy in candidates)
    {
      if (targetEnemy == null)
        continue;

      Vector3 splitDir = (targetEnemy.transform.position - (Vector3)splitStartPoint).normalized;

      if (splitDir == Vector3.zero)
        continue;

      GameObject splitObj = Instantiate(gameObject, splitStartPoint, Quaternion.identity);
      splitObj.SetActive(true);

      PlasmaLazer splitLaser = splitObj.GetComponent<PlasmaLazer>();

      if (splitLaser != null)
      {
        // ★ 분열 레이저는 다시 분열하지 않음
        splitLaser.Initialize(
          splitDir,
          damage,
          range,
          bulletSpeed,
          duration,
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
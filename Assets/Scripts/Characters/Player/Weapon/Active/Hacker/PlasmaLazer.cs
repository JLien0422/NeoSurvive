using System.Collections.Generic;
using UnityEngine;
using NeoSurvive.Weapon;
using NeoSurvive.Core;

public class PlasmaLazer : MonoBehaviour
{
  [SerializeField, Min(0.001f)] private float beamHitWidth = 0.1f;
  private Vector3 baseVisualScale;
  private float baseTrailWidth;
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
  private bool ricochetPending, ricochetDone, finished;
  private Vector2 ricochetOrigin;
  private Vector3 firingOrigin;
  private RunTraitAttackSnapshot replay;

  [Header("Master Split Settings")]
  [SerializeField] private int splitCount = 2;
  [SerializeField] private float splitSearchRange = 100f;

  private void Start()
  {
    baseVisualScale = transform.localScale;
    if (trailRenderer == null)
      trailRenderer = GetComponent<TrailRenderer>();
    if (trailRenderer != null)
      baseTrailWidth = trailRenderer.widthMultiplier;
  }

  private void Update()
  {
    elapsed += Time.deltaTime;

    if (elapsed >= duration)
    {
      FinishAttack();
      Destroy(gameObject);
      return;
    }

    alpha = 1f - (elapsed / duration);

    if (reachedMaxRange)
      return;

    float widthMultiplier = RunTraitWeaponStats.Scale(sourceWeapon, 1f, RunTraitStat.BeamWidth);
    transform.localScale = new Vector3(baseVisualScale.x, baseVisualScale.y * widthMultiplier, baseVisualScale.z);
    if (trailRenderer != null)
      trailRenderer.widthMultiplier = baseTrailWidth * widthMultiplier;
    float effectiveRange = RunTraitWeaponStats.Scale(sourceWeapon, range, RunTraitStat.AttackRange);
    float moveDist = bulletSpeed * Time.deltaTime;
    float remainingRange = effectiveRange > 0f ? effectiveRange - traveledDistance : float.PositiveInfinity;

    if (remainingRange <= 0f)
    {
      reachedMaxRange = true;
      return;
    }

    moveDist = Mathf.Min(moveDist, remainingRange);

    if (moveDist <= 0f)
      return;

    // 기존 특성이 없을 때는 원래의 선 판정을 유지합니다.
    RaycastHit2D[] hits = widthMultiplier <= 1f
      ? Physics2D.RaycastAll(transform.position, direction, moveDist)
      : Physics2D.BoxCastAll(transform.position, new Vector2(0.001f, beamHitWidth * widthMultiplier),
          Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg, direction, moveDist);
    System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

    foreach (var hit in hits)
    {
      if (hit.collider == null)
        continue;

      Character enemy = RunTraitTarget.Find(hit.collider);

      if (enemy != null)
      {
        if (!CanApplyDamageToEnemy(enemy))
          continue;

        damagedEnemyIds.Add(enemy.GetInstanceID());
        using (new RunTraitDamageContext.Scope(secondary: !canSplit, deferRicochet: true))
          enemy.TakeDamage(damage, sourceWeapon);
        currentHitCount++;
        if (!ricochetPending) { ricochetOrigin = hit.point; ricochetPending = true; }

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

    if (effectiveRange > 0f && traveledDistance >= effectiveRange)
      reachedMaxRange = true;
    if (reachedMaxRange || currentHitCount >= maxDamageTargets)
      FlushRicochet();
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
    firingOrigin = transform.position;
    finished = false;
    ricochetPending = false;
    ricochetDone = false;
    replay = new RunTraitAttackSnapshot {
      shape = RunTraitAttackSnapshot.Shape.Beam, origin = firingOrigin, forward = this.direction,
      range = RunTraitWeaponStats.Scale(sourceWeapon, this.range, RunTraitStat.AttackRange),
      width = RunTraitWeaponStats.Scale(sourceWeapon, 1f, RunTraitStat.BeamWidth) <= 1f ? 0f
        : RunTraitWeaponStats.Scale(sourceWeapon, beamHitWidth, RunTraitStat.BeamWidth),
      damage = damage, maxTargets = maxDamageTargets, weapon = sourceWeapon
    };

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

  private void FinishAttack()
  {
    if (finished || !canSplit) return;
    finished = true;
    FlushRicochet();
    RunTraitSpecialRuntime.Repeat(sourceWeapon, RunTraitSpecialKind.Echo, 0f, replay.Replay);
  }

  private void FlushRicochet()
  {
    if (ricochetDone || !canSplit || !ricochetPending) return;
    ricochetDone = true;
    RunTraitSpecialRuntime.Ricochet(ricochetOrigin, null, sourceWeapon, damage, damagedEnemyIds);
  }

  private bool CanApplyDamageToEnemy(Character enemy)
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
  private void SplitToNearbyEnemies(Character hitEnemy, Vector2 splitStartPoint)
  {
    if (hitEnemy == null)
      return;

    Character[] enemies = FindObjectsOfType<Character>();

    List<Character> candidates = new List<Character>();
    HashSet<int> candidateIds = new HashSet<int>();

    foreach (Character enemy in enemies)
    {
      if (!RunTraitTarget.IsCombatant(enemy))
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

    foreach (Character targetEnemy in candidates)
    {
      if (targetEnemy == null)
        continue;

      Vector3 splitDir = (targetEnemy.transform.position - (Vector3)splitStartPoint).normalized;

      if (splitDir == Vector3.zero)
        continue;

      GameObject splitObj = Instantiate(gameObject, splitStartPoint, Quaternion.identity);
      // 보정된 원본을 복제하므로 기본 두께로 돌린 뒤 자식이 특성을 한 번만 적용합니다.
      splitObj.transform.localScale = baseVisualScale;
      TrailRenderer splitTrail = splitObj.GetComponent<TrailRenderer>();
      if (splitTrail != null)
        splitTrail.widthMultiplier = baseTrailWidth;
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

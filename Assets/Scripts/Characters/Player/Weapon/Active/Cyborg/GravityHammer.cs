using UnityEngine;
using System.Collections;
using NeoSurvive.Core;

namespace NeoSurvive.Weapon
{
  /// <summary>
  /// 5번 무기: GravityHammer
  /// - 기본: 전방(부채꼴) 강타 + 넉백
  /// - Lv.Up: 데미지/범위/넉백 증가
  /// - Lv5 마스터: 블랙홀 생성(흡입)
  /// - Hammer / Swing / Impact VFX 분리 구조
  /// </summary>
  public class GravityHammer : MonoBehaviour
  {
    [Header("Stats")]
    public float damage = 18f;
    public float range = 4.5f;
    public float angle = 70f;
    public float fireRate = 1.6f;

    [Header("Knockback")]
    public float knockbackForce = 6f;

    [Header("Target / Hit")]
    public LayerMask hitMask;
    public string enemyTag = "Enemy";

    [Header("Level Scaling")]
    public float rangePerLevel = 0.12f;
    public float knockbackPerLevel = 0.15f;

    [Header("Master (Lv5) - Blackhole")]
    public bool enableMaster = true;
    public GameObject blackholePrefab;
    public float blackholeDuration = 2f;
    public float blackholePullRadius = 2.5f;
    public float blackholePullForce = 10f;
    public float blackholeScale = 1.0f;

    // =========================
    // ★ 추가: Hammer 본체 VFX
    // =========================
    [Header("Hammer VFX")]
    public GameObject hammerEffectPrefab;
    public float hammerEffectDuration = 0.5f;
    public float hammerEffectOffset = 0.6f;
    public float hammerEffectScale = 1.0f;

    // =========================
    // ★ 추가: Swing VFX
    // =========================
    [Header("Swing VFX")]
    public GameObject swingEffectPrefab;
    public float swingEffectDuration = 0.5f;
    public float swingEffectOffset = 0.6f;
    public float swingEffectScale = 1.0f;

    // =========================
    // ★ 추가: Impact VFX
    // =========================
    [Header("Impact VFX")]
    public GameObject impactEffectPrefab;
    public float impactEffectDuration = 0.6f;
    public float impactEffectOffset = 1.0f;
    public float impactEffectScale = 1.0f;

    [Header("Debug")]
    public bool debugLog = false;
    public bool debugDraw = true;

    private float timer;

    private float baseRange;
    private float baseKnockback;
    private float baseFireRate;

    private const string weaponId = "gravityhammer";
    private int currentLevel = 1;
    private Coroutine blackholeRoutine;

    private void Start()
    {
      baseRange = range;
      baseKnockback = knockbackForce;
      baseFireRate = fireRate;

      // transform.localPosition = Vector3.right * 0.5f;

      ApplyLevel(1);
    }

    private void Update()
    {
      timer += Time.deltaTime;

      if (timer >= fireRate)
      {
        Slam();
        timer = 0f;
      }
    }

    public void OnLevelUp(int level)
    {
      if (baseRange <= 0f && range > 0f) baseRange = range;
      if (baseKnockback <= 0f && knockbackForce > 0f) baseKnockback = knockbackForce;
      if (baseFireRate <= 0f && fireRate > 0f) baseFireRate = fireRate;

      ApplyLevel(level);

      if (debugLog)
      {
        Debug.Log(
          $"[GravityHammer] Lv.{currentLevel} " +
          $"Dmg={damage}, Range={range}, KB={knockbackForce}, Rate={fireRate}"
        );
      }
    }

    private void ApplyLevel(int level)
    {
      currentLevel = Mathf.Clamp(level, 1, 5);

      ApplyStatsFromCSV(currentLevel);
      range = baseRange * (1f + (currentLevel - 1) * rangePerLevel);
      knockbackForce = baseKnockback * (1f + (currentLevel - 1) * knockbackPerLevel);
      fireRate = baseFireRate;
    }

    private void ApplyStatsFromCSV(int level)
    {
      if (WeaponStatLoader.DB == null)
        return;

      if (!WeaponStatLoader.DB.rows.TryGetValue(weaponId, out var levelDict))
        return;

      if (!levelDict.TryGetValue(level, out var row))
        return;

      if (levelDict.TryGetValue(1, out var baseRow))
      {
        if (baseRow.range > 0f) baseRange = baseRow.range;
        if (baseRow.knockbackforce > 0f) baseKnockback = baseRow.knockbackforce;
        if (baseRow.firerate > 0f) baseFireRate = baseRow.firerate;
        if (baseRow.rangeperlevel > 0f) rangePerLevel = baseRow.rangeperlevel;
        if (baseRow.knockbackperlevel > 0f) knockbackPerLevel = baseRow.knockbackperlevel;
      }

      if (row.damage > 0f) damage = row.damage;
      if (row.angle > 0f) angle = row.angle;
      if (row.blackholeduration > 0f) blackholeDuration = row.blackholeduration;
      if (row.blackholepullradius > 0f) blackholePullRadius = row.blackholepullradius;
      if (row.blackholepullforce > 0f) blackholePullForce = row.blackholepullforce;
    }

    private void Slam()
    {
      if (debugLog)
        Debug.Log("[GravityHammer] Slam!");

      if (InGameSoundManager.Instance != null)
        InGameSoundManager.Instance.PlayGravityHammerFire();

      Transform target = FindClosestEnemy();

      Vector3 forward =
        transform.parent != null
        ? transform.parent.right
        : transform.right;

      if (target != null)
        forward = (target.position - transform.position).normalized;

      if (debugDraw)
      {
        Debug.DrawRay(transform.position, forward * range, Color.red, 0.2f);

        Debug.DrawRay(
          transform.position,
          Quaternion.Euler(0, 0, angle * 0.5f) * forward * range,
          Color.magenta,
          0.2f
        );

        Debug.DrawRay(
          transform.position,
          Quaternion.Euler(0, 0, -angle * 0.5f) * forward * range,
          Color.magenta,
          0.2f
        );
      }

      Collider2D[] hits =
        Physics2D.OverlapCircleAll(
          transform.position,
          range,
          hitMask
        );

      foreach (var col in hits)
      {
        if (col == null) continue;

        if (!string.IsNullOrEmpty(enemyTag))
        {
          bool okTag =
            col.CompareTag(enemyTag) ||
            (col.transform.parent != null &&
             col.transform.parent.CompareTag(enemyTag));

          if (!okTag)
            continue;
        }

        Vector3 dirToEnemy =
          (col.transform.position - transform.position).normalized;

        if (Vector3.Angle(forward, dirToEnemy) > angle * 0.5f)
          continue;

        Character character =
          col.GetComponentInParent<Character>();

        if (character == null)
          continue;

        var src = GetComponentInParent<WeaponSource>();

        character.TakeDamage(
          damage,
          src != null ? src.weaponData : null
        );

        ApplyKnockback(col.transform, forward);
      }

      SpawnHammerEffect(forward);
      SpawnSwingEffect(forward);
      SpawnImpactEffect(forward);

      if (enableMaster && currentLevel >= 5)
      {
        Vector3 center =
          transform.position +
          forward * Mathf.Min(range, 2.2f);

        SpawnBlackhole(center);
      }
    }

    private void SpawnHammerEffect(Vector3 forward)
    {
      if (hammerEffectPrefab == null)
        return;

      Vector3 spawnPos =
        transform.position +
        forward * hammerEffectOffset;

      GameObject vfx = Instantiate(
        hammerEffectPrefab,
        spawnPos,
        Quaternion.identity
      );

      float dirX = forward.x >= 0f ? 1f : -1f;

      vfx.transform.localScale = new Vector3(
        hammerEffectScale * dirX,
        hammerEffectScale,
        hammerEffectScale
      );

      Destroy(vfx, hammerEffectDuration);
    }

    private void SpawnSwingEffect(Vector3 forward)
    {
      if (swingEffectPrefab == null)
        return;

      Vector3 spawnPos =
        transform.position +
        forward * swingEffectOffset;

      GameObject vfx = Instantiate(
        swingEffectPrefab,
        spawnPos,
        Quaternion.identity
      );

      float dirX = forward.x >= 0f ? 1f : -1f;

      vfx.transform.localScale = new Vector3(
        swingEffectScale * dirX,
        swingEffectScale,
        swingEffectScale
      );

      Destroy(vfx, swingEffectDuration);
    }

    private void SpawnImpactEffect(Vector3 forward)
    {
      if (impactEffectPrefab == null)
        return;

      Vector3 spawnPos =
        transform.position +
        forward * impactEffectOffset;

      GameObject vfx = Instantiate(
        impactEffectPrefab,
        spawnPos,
        Quaternion.identity
      );

      float dirX = forward.x >= 0f ? 1f : -1f;

      vfx.transform.localScale = new Vector3(
        impactEffectScale * dirX,
        impactEffectScale,
        impactEffectScale
      );

      Destroy(vfx, impactEffectDuration);
    }

    private void ApplyKnockback(Transform hitTransform, Vector3 forward)
    {
      Rigidbody2D rb =
        hitTransform.GetComponentInParent<Rigidbody2D>();

      Vector2 dir =
        (hitTransform.position - transform.position);

      if (dir.sqrMagnitude < 0.001f)
        dir = forward;

      dir.Normalize();

      if (rb != null && rb.bodyType == RigidbodyType2D.Dynamic)
      {
        rb.AddForce(
          dir * knockbackForce,
          ForceMode2D.Impulse
        );
      }
      else
      {
        hitTransform.position = Vector3.MoveTowards(
          hitTransform.position,
          hitTransform.position + (Vector3)(dir * 1.2f),
          (knockbackForce * 0.08f) * Time.deltaTime
        );
      }
    }

    private void SpawnBlackhole(Vector3 pos)
    {
      if (blackholePrefab != null)
      {
        GameObject obj =
          Instantiate(
            blackholePrefab,
            pos,
            Quaternion.identity
          );

        obj.transform.localScale =
          Vector3.one * blackholeScale;

        Destroy(obj, blackholeDuration);
      }

      if (blackholeRoutine != null)
        StopCoroutine(blackholeRoutine);

      blackholeRoutine =
        StartCoroutine(
          BlackholePullRoutine(pos, blackholeDuration)
        );
    }

    // =========================
    // ★ 수정: 안정적인 블랙홀 흡입
    // =========================
    private IEnumerator BlackholePullRoutine(Vector3 center, float duration)
    {
      float t = 0f;

      while (t < duration)
      {
        Collider2D[] hits =
          Physics2D.OverlapCircleAll(
            center,
            blackholePullRadius,
            hitMask
          );

        foreach (var col in hits)
        {
          if (col == null)
            continue;

          if (!string.IsNullOrEmpty(enemyTag))
          {
            bool okTag =
              col.CompareTag(enemyTag) ||
              (col.transform.parent != null &&
               col.transform.parent.CompareTag(enemyTag));

            if (!okTag)
              continue;
          }

          Rigidbody2D rb =
            col.GetComponentInParent<Rigidbody2D>();

          if (rb != null &&
              rb.bodyType == RigidbodyType2D.Dynamic)
          {
            Vector2 toCenter =
              (Vector2)center - rb.position;

            float dist = toCenter.magnitude;

            // 중심 근처 도착 시 정지
            if (dist < 0.08f)
            {
              rb.velocity = Vector2.zero;
              continue;
            }

            Vector2 pullDir = toCenter.normalized;

            // 가까울수록 약하게
            float distanceRatio =
              Mathf.Clamp01(dist / blackholePullRadius);

            float pullSpeed =
              blackholePullForce * distanceRatio;

            // 목표 속도
            Vector2 targetVelocity =
              pullDir * pullSpeed;

            // 기존 속도를 부드럽게 보정
            rb.velocity = Vector2.Lerp(
              rb.velocity,
              targetVelocity,
              Time.deltaTime * 6f
            );

            // 중심 방향으로 이동 보정
            rb.MovePosition(
              Vector2.MoveTowards(
                rb.position,
                center,
                pullSpeed * Time.deltaTime
              )
            );
          }
        }

        t += Time.deltaTime;
        yield return null;
      }

      blackholeRoutine = null;
    }

    private Transform FindClosestEnemy()
    {
      GameObject[] enemies =
        GameObject.FindGameObjectsWithTag("Enemy");

      Transform closest = null;
      float closestDist =
        range > 0 ? range * 1.5f : 10f;

      foreach (var e in enemies)
      {
        if (e == null)
          continue;

        float d =
          Vector3.Distance(
            transform.position,
            e.transform.position
          );

        if (d < closestDist)
        {
          closestDist = d;
          closest = e.transform;
        }
      }

      return closest;
    }

    private void OnDrawGizmosSelected()
    {
      Gizmos.color = Color.yellow;
      Gizmos.DrawWireSphere(transform.position, range);
    }
  }
}
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using NeoSurvive.Core;

namespace NeoSurvive.Weapon
{
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

    [Header("Hammer VFX")]
    public GameObject hammerEffectPrefab;
    public float hammerEffectDuration = 0.5f;
    public float hammerEffectOffset = 0.6f;
    public float hammerEffectScale = 1.0f;

    [Header("Swing VFX")]
    public GameObject swingEffectPrefab;
    public float swingEffectDuration = 0.5f;
    public float swingEffectOffset = 0.6f;
    public float swingEffectScale = 1.0f;

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
        InGameSoundManager.Instance.PlayGravityHammerSwing();

      Transform target = FindClosestTarget();

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

      Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, range);

      HashSet<int> processedIds = new HashSet<int>();

      foreach (var col in hits)
      {
        if (col == null) continue;

        bool isEnemy = IsEnemyCollider(col);

        IDamageable damageable = col.GetComponent<IDamageable>();
        if (damageable == null)
          damageable = col.GetComponentInParent<IDamageable>();

        bool isInHitMask =
          hitMask.value == 0 ||
          ((1 << col.gameObject.layer) & hitMask.value) != 0;

        if (!isInHitMask && damageable == null)
          continue;

        if (!isEnemy && damageable == null)
          continue;

        Vector3 dirToTarget =
          (col.transform.position - transform.position).normalized;

        if (Vector3.Angle(forward, dirToTarget) > angle * 0.5f)
          continue;

        if (isEnemy)
        {
          Character character = col.GetComponentInParent<Character>();

          if (character != null)
          {
            int id = character.gameObject.GetInstanceID();

            if (processedIds.Contains(id))
              continue;

            processedIds.Add(id);

            var src = GetComponentInParent<WeaponSource>();

            character.TakeDamage(
              damage,
              src != null ? src.weaponData : null
            );

            ApplyKnockback(col.transform, forward);
            continue;
          }
        }

        if (damageable != null)
        {
          MonoBehaviour mb = damageable as MonoBehaviour;

          if (mb != null)
          {
            int id = mb.gameObject.GetInstanceID();

            if (processedIds.Contains(id))
              continue;

            processedIds.Add(id);
          }

          damageable.TakeDamage(damage);
        }
      }

      SpawnHammerEffect(forward);
      SpawnSwingEffect(forward);
      SpawnImpactEffect(forward);

      if (InGameSoundManager.Instance != null)
        InGameSoundManager.Instance.PlayGravityHammerImpact();

      if (enableMaster && currentLevel >= 5)
      {
        Vector3 center =
          transform.position +
          forward * Mathf.Min(range, 2.2f);

        SpawnBlackhole(center);
      }
    }

    private bool IsEnemyCollider(Collider2D col)
    {
      if (col == null) return false;
      if (string.IsNullOrEmpty(enemyTag)) return false;

      return
        col.CompareTag(enemyTag) ||
        (col.transform.parent != null &&
         col.transform.parent.CompareTag(enemyTag));
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
        hitTransform.position - transform.position;

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
      if (InGameSoundManager.Instance != null)
        InGameSoundManager.Instance.PlayGravityHammerBlackhole();

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

            if (dist < 0.08f)
            {
              rb.velocity = Vector2.zero;
              continue;
            }

            Vector2 pullDir = toCenter.normalized;

            float distanceRatio =
              Mathf.Clamp01(dist / blackholePullRadius);

            float pullSpeed =
              blackholePullForce * distanceRatio;

            Vector2 targetVelocity =
              pullDir * pullSpeed;

            rb.velocity = Vector2.Lerp(
              rb.velocity,
              targetVelocity,
              Time.deltaTime * 6f
            );

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

    private Transform FindClosestTarget()
    {
      Collider2D[] hits =
        Physics2D.OverlapCircleAll(
          transform.position,
          range * 1.5f
        );

      Transform closest = null;
      float closestDist =
        range > 0 ? range * 1.5f : 10f;

      foreach (Collider2D hit in hits)
      {
        if (hit == null)
          continue;

        bool isEnemy = IsEnemyCollider(hit);

        bool isInHitMask =
          hitMask.value == 0 ||
          ((1 << hit.gameObject.layer) & hitMask.value) != 0;

        IDamageable damageable = hit.GetComponent<IDamageable>();

        if (damageable == null)
          damageable = hit.GetComponentInParent<IDamageable>();

        if ((!isEnemy || !isInHitMask) && damageable == null)
          continue;

        Transform targetTransform = hit.transform;

        Character character = hit.GetComponentInParent<Character>();

        if (character != null)
          targetTransform = character.transform;
        else if (damageable is Component damageableComponent)
          targetTransform = damageableComponent.transform;

        float d =
          Vector3.Distance(
            transform.position,
            targetTransform.position
          );

        if (d < closestDist)
        {
          closestDist = d;
          closest = targetTransform;
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
using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using NeoSurvive.Buff;
using NeoSurvive.Map.Map2.Core;
using NeoSurvive.Map.Map2.EnvironmentGimmicks;
using NeoSurvive.Map.Map1.MapObjects;

namespace NeoSurvive.Weapon
{
  /// <summary>
  /// EMP 수류탄 투사체
  /// - startPos -> targetPos 로 날아감
  /// - 중간에 위로 솟는 포물선 연출
  /// - 목표 지점 도착 후 폭발
  /// - 폭발 시 범위 데미지 + StunDebuff 적용
  /// </summary>
  public class EMPGrenadeProjectile : MonoBehaviour
  {
    [Header("Optional Visual")]
    [SerializeField] private GameObject explosionEffectPrefab;
    [SerializeField] private bool rotateWhileFlying = true;
    [SerializeField] private float rotateSpeed = 540f;

    private GameObject owner;
    private Vector2 startPos;
    private Vector2 targetPos;
    private float damage;
    private float explosionRadius;
    private float stunDuration;
    private float travelTime;
    private float arcHeight;
    private LayerMask enemyMask;
    private string enemyTag;

    private float timer = 0f;
    private bool initialized = false;
    private bool exploded = false;

    private bool canHitToxicValve = false;

    public void Initialize(
      GameObject owner,
      Vector2 startPos,
      Vector2 targetPos,
      float damage,
      float explosionRadius,
      float stunDuration,
      float travelTime,
      float arcHeight,
      LayerMask enemyMask,
      string enemyTag)
    {
      this.owner = owner;
      this.startPos = startPos;
      this.targetPos = targetPos;
      this.damage = damage;
      this.explosionRadius = explosionRadius;
      this.stunDuration = stunDuration;
      this.travelTime = Mathf.Max(0.05f, travelTime);
      this.arcHeight = arcHeight;
      this.enemyMask = enemyMask;
      this.enemyTag = enemyTag;

      Player player = owner != null ? owner.GetComponent<Player>() : null;
    /*
      if (player != null)
      {
        if (player.IsCyborg)
        {
          canHitToxicValve = true;
          Debug.Log("[EMPGrenadeProjectile] Owner = Cyborg -> ToxicValve 공격 가능");
        }
        else if (player.IsHacker)
        {
          canHitToxicValve = false;
          Debug.Log("[EMPGrenadeProjectile] Owner = Hacker -> ToxicValve 공격 불가");
        }
      }
    */
      transform.position = startPos;
      initialized = true;
    }

    private void Update()
    {
      if (!initialized || exploded)
        return;

      timer += Time.deltaTime;
      float t = Mathf.Clamp01(timer / travelTime);

      Vector2 lerpPos = Vector2.Lerp(startPos, targetPos, t);
      float arc = Mathf.Sin(t * Mathf.PI) * arcHeight;
      Vector2 finalPos = lerpPos + Vector2.up * arc;

      transform.position = finalPos;

      if (rotateWhileFlying)
      {
        transform.Rotate(0f, 0f, -rotateSpeed * Time.deltaTime);
      }

      if (t >= 1f)
      {
        Explode();
      }
    }

    private void Explode()
    {
      if (exploded)
        return;

      exploded = true;

      Vector2 explodePos = targetPos;

      if (explosionEffectPrefab != null)
      {
        Instantiate(explosionEffectPrefab, explodePos, Quaternion.identity);
      }

      Collider2D[] hits = Physics2D.OverlapCircleAll(explodePos, explosionRadius);
      HashSet<int> processedIds = new HashSet<int>();

      foreach (Collider2D hit in hits)
      {
        if (hit == null)
          continue;

        // ===== ToxicValve 처리 =====
        ToxicValve valve = hit.GetComponent<ToxicValve>();
        if (valve == null)
          valve = hit.GetComponentInParent<ToxicValve>();

        if (valve != null)
        {
          int id = valve.gameObject.GetInstanceID();
          if (processedIds.Contains(id)) continue;
          processedIds.Add(id);

          if (!canHitToxicValve)
          {
            Debug.Log($"[EMPGrenadeProjectile] ToxicValve 무시 | {valve.name}");
            continue;
          }

          Debug.Log($"[EMPGrenadeProjectile] ToxicValve 피격 | {valve.name} | damage={damage}");
          valve.TakeDamage(damage, owner);
          continue;
        }

        // ===== ToxicDrumBarrel 처리 =====
        ToxicDrumBarrel drum = hit.GetComponent<ToxicDrumBarrel>();
        if (drum == null)
          drum = hit.GetComponentInParent<ToxicDrumBarrel>();

        if (drum != null)
        {
          int id = drum.gameObject.GetInstanceID();
          if (processedIds.Contains(id)) continue;
          processedIds.Add(id);

          Debug.Log($"[EMPGrenadeProjectile] ToxicDrumBarrel 피격 | {drum.name} | damage={damage}");
          drum.TakeDamage(damage);
          continue;
        }

        // ===== DataServerRack 처리 =====
        DataServerRack rack = hit.GetComponent<DataServerRack>();
        if (rack == null)
          rack = hit.GetComponentInParent<DataServerRack>();

        if (rack != null)
        {
          int id = rack.gameObject.GetInstanceID();
          if (processedIds.Contains(id)) continue;
          processedIds.Add(id);

          Debug.Log($"[EMPGrenadeProjectile] DataServerRack 피격 | {rack.name} | damage={damage}");
          rack.TakeDamage(damage);
          continue;
        }

        // ===== CryoCylinder 처리 =====
        CryoCylinder cryo = hit.GetComponent<CryoCylinder>();
        if (cryo == null)
          cryo = hit.GetComponentInParent<CryoCylinder>();

        if (cryo != null)
        {
          int id = cryo.gameObject.GetInstanceID();
          if (processedIds.Contains(id)) continue;
          processedIds.Add(id);

          Debug.Log($"[EMPGrenadeProjectile] CryoCylinder 피격 | {cryo.name} | damage={damage}");
          cryo.TakeDamage(damage);
          continue;
        }

        // ===== MalfunctionVendingMachine 처리 ★ 추가 =====
        MalfunctionVendingMachine vendingMachine = hit.GetComponent<MalfunctionVendingMachine>();
        if (vendingMachine == null)
          vendingMachine = hit.GetComponentInParent<MalfunctionVendingMachine>();

        if (vendingMachine != null)
        {
          int id = vendingMachine.gameObject.GetInstanceID();
          if (processedIds.Contains(id)) continue;
          processedIds.Add(id);

          Debug.Log($"[EMPGrenadeProjectile] VendingMachine 피격 | {vendingMachine.name} | damage={damage}");
          vendingMachine.TakeDamage(damage);
          continue;
        }

        // ===== Enemy 처리 =====
        Enemy enemy = hit.GetComponent<Enemy>();
        if (enemy == null)
          enemy = hit.GetComponentInParent<Enemy>();

        if (enemy != null)
        {
          int id = enemy.gameObject.GetInstanceID();
          if (processedIds.Contains(id)) continue;
          processedIds.Add(id);

          if (!string.IsNullOrEmpty(enemyTag) && !enemy.CompareTag(enemyTag))
            continue;

          var src = GetComponent<WeaponSource>();
          enemy.TakeDamage(damage, src != null ? src.weaponData : null);
          ApplyStun(enemy.gameObject, stunDuration);
        }
      }

      Destroy(gameObject);
    }

    private void ApplyDamage(GameObject target, float amount)
    {
      if (target == null)
        return;

      Component[] components = target.GetComponents<MonoBehaviour>();

      foreach (Component comp in components)
      {
        if (comp == null)
          continue;

        Type type = comp.GetType();

        MethodInfo takeDamageFloat = type.GetMethod(
          "TakeDamage",
          BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
          null,
          new Type[] { typeof(float) },
          null
        );

        if (takeDamageFloat != null)
        {
          takeDamageFloat.Invoke(comp, new object[] { amount });
          return;
        }

        MethodInfo takeDamageInt = type.GetMethod(
          "TakeDamage",
          BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
          null,
          new Type[] { typeof(int) },
          null
        );

        if (takeDamageInt != null)
        {
          takeDamageInt.Invoke(comp, new object[] { Mathf.RoundToInt(amount) });
          return;
        }

        MethodInfo takeDamageWithSource = type.GetMethod(
          "TakeDamage",
          BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
          null,
          new Type[] { typeof(float), typeof(GameObject) },
          null
        );

        if (takeDamageWithSource != null)
        {
          takeDamageWithSource.Invoke(comp, new object[] { amount, owner });
          return;
        }
      }

      Debug.LogWarning($"[EMPGrenadeProjectile] {target.name} 에서 TakeDamage 메서드를 찾지 못했습니다.");
    }

    private void ApplyStun(GameObject target, float duration)
    {
      if (target == null)
        return;

      BuffUtil.Apply(target, new StunDebuff(duration));
    }

    private void OnDrawGizmosSelected()
    {
      Gizmos.color = Color.yellow;
      Gizmos.DrawWireSphere(targetPos == Vector2.zero ? transform.position : (Vector3)targetPos, explosionRadius);
    }
  }
}
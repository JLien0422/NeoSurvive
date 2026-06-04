using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using NeoSurvive.Core;

namespace NeoSurvive.Weapon
{
  public class ElectricPuddle : MonoBehaviour
  {
    private float weaponDamage;
    private float duration;
    private float radius;
    private float tick;
    private float damageFactor;
    private LayerMask hitMask;
    private string enemyTag;
    private WeaponBase sourceWeapon;

    private bool initialized = false;

    public void SetSourceWeapon(WeaponBase weapon)
    {
      sourceWeapon = weapon;
    }

    public void Initialize(
      float weaponDamage,
      float duration,
      float radius,
      float tick,
      float damageFactor,
      LayerMask hitMask,
      string enemyTag)
    {
      this.weaponDamage = weaponDamage;
      this.duration = duration;
      this.radius = radius;
      this.tick = tick;
      this.damageFactor = damageFactor;
      this.hitMask = hitMask;
      this.enemyTag = enemyTag;

      initialized = true;

      StartCoroutine(DamageRoutine());
      Destroy(gameObject, duration);
    }

    private IEnumerator DamageRoutine()
    {
      if (!initialized) yield break;

      float elapsed = 0f;

      while (elapsed < duration)
      {
        float tickDamage = weaponDamage * damageFactor;

        Collider2D[] hits = Physics2D.OverlapCircleAll(
          transform.position,
          radius
        );

        HashSet<int> processedIds = new HashSet<int>();

        foreach (var h in hits)
        {
          if (h == null) continue;

          TryDamageTarget(h, tickDamage, processedIds);
        }

        yield return new WaitForSeconds(tick);
        elapsed += tick;
      }
    }

    private bool TryDamageTarget(Collider2D h, float tickDamage, HashSet<int> processedIds)
    {
      if (h == null)
        return false;

      bool isEnemy = IsEnemyCollider(h);

      Character character = h.GetComponent<Character>();
      if (character == null)
        character = h.GetComponentInParent<Character>();

      IDamageable damageable = h.GetComponent<IDamageable>();
      if (damageable == null)
        damageable = h.GetComponentInParent<IDamageable>();

      bool isInHitMask =
        hitMask.value == 0 ||
        ((1 << h.gameObject.layer) & hitMask.value) != 0;

      if (!isInHitMask && damageable == null)
        return false;

      if (character == null && !isEnemy && damageable == null)
        return false;

      if (character != null)
      {
        int id = character.gameObject.GetInstanceID();

        if (processedIds.Contains(id))
          return false;

        processedIds.Add(id);

        character.TakeDamage(tickDamage, sourceWeapon);
        return true;
      }

      if (damageable != null)
      {
        MonoBehaviour mb = damageable as MonoBehaviour;

        if (mb != null)
        {
          int id = mb.gameObject.GetInstanceID();

          if (processedIds.Contains(id))
            return false;

          processedIds.Add(id);
        }

        damageable.TakeDamage(tickDamage);
        return true;
      }

      return false;
    }

    private bool IsEnemyCollider(Collider2D h)
    {
      if (h == null) return false;
      if (string.IsNullOrEmpty(enemyTag)) return false;

      return
        h.CompareTag(enemyTag) ||
        (h.transform.parent != null &&
         h.transform.parent.CompareTag(enemyTag));
    }

    private void OnDrawGizmosSelected()
    {
      Gizmos.color = Color.cyan;
      Gizmos.DrawWireSphere(transform.position, radius);
    }
  }
}
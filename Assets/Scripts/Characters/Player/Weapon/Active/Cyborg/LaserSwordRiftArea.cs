using UnityEngine;
using System.Collections;
using NeoSurvive.Core;

namespace NeoSurvive.Weapon
{
  /// <summary>
  /// LaserSword Lv5 마스터 효과: 공간 균열(잔상딜)
  /// - Initialize()로 파라미터를 받아 duration 동안 tick 피해
  /// </summary>
  public class RiftArea : MonoBehaviour
  {
    private float weaponDamage;
    private float duration;
    private float radius;
    private float tick;
    private float damageFactor;
    private LayerMask hitMask;
    private string enemyTag;

    private bool initialized = false;

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

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius, hitMask);
        foreach (var h in hits)
        {
          if (h == null) continue;
          if (!string.IsNullOrEmpty(enemyTag) && !h.CompareTag(enemyTag)) continue;

          if (h.TryGetComponent<Enemy>(out var e))
          {
            e.TakeDamage(tickDamage);
          }
        }

        yield return new WaitForSeconds(tick);
        elapsed += tick;
      }
    }

    private void OnDrawGizmosSelected()
    {
      Gizmos.color = Color.magenta;
      Gizmos.DrawWireSphere(transform.position, radius);
    }
  }
}

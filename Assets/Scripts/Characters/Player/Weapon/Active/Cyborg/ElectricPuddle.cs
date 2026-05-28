using UnityEngine;
using System.Collections;
using NeoSurvive.Core;

namespace NeoSurvive.Weapon
{
  /// <summary>
  /// TeslaCoilArmor Lv5 마스터 효과: 전기 장판 데미지 판정 영역
  /// - VFX와 분리된 Area 프리팹에 붙음
  /// - Initialize()로 파라미터를 받아 duration 동안 tick 피해
  /// </summary>
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

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius, hitMask);

        foreach (var h in hits)
        {
          if (h == null) continue;

          if (!string.IsNullOrEmpty(enemyTag))
          {
            bool okTag =
              h.CompareTag(enemyTag) ||
              (h.transform.parent != null && h.transform.parent.CompareTag(enemyTag));

            if (!okTag) continue;
          }

          Enemy enemy = h.GetComponentInParent<Enemy>();
          if (enemy != null)
          {
            enemy.TakeDamage(tickDamage, sourceWeapon);
          }
        }

        yield return new WaitForSeconds(tick);
        elapsed += tick;
      }
    }

    private void OnDrawGizmosSelected()
    {
      Gizmos.color = Color.cyan;
      Gizmos.DrawWireSphere(transform.position, radius);
    }
  }
}
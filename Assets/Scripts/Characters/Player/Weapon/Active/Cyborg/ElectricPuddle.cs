using UnityEngine;
using NeoSurvive.Core;

namespace NeoSurvive.Weapon
{
  /// <summary>
  /// 테슬라 아머 마스터: 이동 경로 전기 장판
  /// - duration 동안 tick마다 범위 내 적에게 피해
  /// </summary>
  public class ElectricPuddle : MonoBehaviour
  {
    private float damage;
    private float duration;
    private float radius;
    private float tick;
    private LayerMask enemyMask;
    private string enemyTag;

    private float timer;
    private float life;

    public void Initialize(float damage, float duration, float radius, float tick, LayerMask enemyMask, string enemyTag)
    {
      this.damage = damage;
      this.duration = duration;
      this.radius = radius;
      this.tick = tick;
      this.enemyMask = enemyMask;
      this.enemyTag = enemyTag;

      life = 0f;
      timer = 0f;
    }

    private void Update()
    {
      life += Time.deltaTime;
      if (life >= duration)
      {
        Destroy(gameObject);
        return;
      }

      timer += Time.deltaTime;
      if (timer >= tick)
      {
        TickDamage();
        timer = 0f;
      }
    }

    private void TickDamage()
    {
      Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius, enemyMask);
      foreach (var col in hits)
      {
        if (col == null) continue;

        if (!string.IsNullOrEmpty(enemyTag) && !col.CompareTag(enemyTag))
        {
          if (col.transform.parent == null || !col.transform.parent.CompareTag(enemyTag))
            continue;
        }

        Enemy enemy = col.GetComponentInParent<Enemy>();
        if (enemy != null)
        {
          enemy.TakeDamage(damage);
        }
      }
    }

    private void OnDrawGizmosSelected()
    {
      Gizmos.color = Color.cyan;
      Gizmos.DrawWireSphere(transform.position, radius);
    }
  }
}

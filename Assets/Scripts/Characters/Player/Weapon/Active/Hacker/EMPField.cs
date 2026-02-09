using UnityEngine;
using NeoSurvive.Core;

namespace NeoSurvive.Weapon
{
  /// <summary>
  /// EMP 펄스 장판 (투사체/소환물)
  /// </summary>
  public class EMPField : MonoBehaviour
  {
    public float damage;
    public float radius;
    public float duration;
    public bool destroyProjectile;

    private float tickTimer;
    private float tickRate = 0.5f; // 0.5초마다 데미지

    private float scaleRatio = 9.5f / 3f;

    public Animator animator;

    public void Initialize(float damage, float radius, float duration, bool destroyProjectile)
    {
      this.damage = damage;
      this.radius = radius;
      this.duration = duration;
      this.destroyProjectile = destroyProjectile;

      // 시각적 크기 조정 (기본 스프라이트 크기가 1x1이라 가정)
      // 반지름이 radius이므로 지름은 radius * 2
      transform.localScale = new Vector3(radius * scaleRatio, radius * scaleRatio, 1f);

      // 애니메이션 파라미터 설정 (tickSpeedMultiplier)
      animator.SetFloat("tickSpeedMultiplier", 1 / tickRate);

      // 초기 생성 시 투사체 파괴 (마스터 효과)
      if (this.destroyProjectile)
      {
        DestroyEnemyProjectiles();
      }

      // 즉시 1틱 데미지
      DealAreaDamage();

      // 지속 시간 후 소멸
      Destroy(gameObject, duration);

      // 시각 효과 설정 (없으면 추가 - Cyan 색상)
      var sr = GetComponent<SpriteRenderer>();
      if (sr == null)
      {
        sr = gameObject.AddComponent<SpriteRenderer>();
        sr.color = new Color(0, 1, 1, 0.4f); // Cyan, 투명도 40%
      }
      sr.sortingOrder = 0;
    }

    private void DealAreaDamage()
    {
      Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius);
      foreach (var hit in hits)
      {
        if (hit.CompareTag("Enemy"))
        {
          if (hit.TryGetComponent<Enemy>(out var enemy))
          {
            var src = GetComponent<WeaponSource>();
            enemy.TakeDamage(damage, src != null ? src.weaponData : null);
          }
        }
      }
    }

    private void DestroyEnemyProjectiles()
    {
      // 적 투사체 감지 (Tag: "EnemyProjectile" or "Bullet")
      Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius);
      foreach (var hit in hits)
      {
        if (hit.CompareTag("EnemyProjectile") || hit.CompareTag("Bullet"))
        {
          Destroy(hit.gameObject);
        }
      }
    }

    private void OnDrawGizmos()
    {
      Gizmos.color = Color.cyan;
      Gizmos.DrawWireSphere(transform.position, radius);
    }
  }
}

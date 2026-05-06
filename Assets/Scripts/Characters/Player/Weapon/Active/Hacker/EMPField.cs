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

    // DPM 기록용 소스 무기
    private WeaponBase sourceWeapon;

    public void Initialize(float damage, float radius, float duration, bool destroyProjectile, WeaponBase weaponBase = null)
    {
      this.damage = damage;
      this.radius = radius;
      this.duration = duration;
      this.destroyProjectile = destroyProjectile;
      this.sourceWeapon = weaponBase;

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
        if (hit == null)
          continue;

        if (hit.CompareTag("Enemy"))
        {
          if (hit.TryGetComponent<Character>(out var character))
            character.TakeDamage(damage, sourceWeapon);

          continue;
        }

        // =========================
        // 2. 추가: IDamageable 맵오브젝트 처리
        // - 자판기 같은 오브젝트가 여기서 데미지를 받음
        // - Enemy는 위에서 이미 처리했으므로 제외
        // =========================
        IDamageable damageable = hit.GetComponent<IDamageable>();

        if (damageable == null)
          damageable = hit.GetComponentInParent<IDamageable>();

        if (damageable == null)
          continue;

        damageable.TakeDamage(damage);
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

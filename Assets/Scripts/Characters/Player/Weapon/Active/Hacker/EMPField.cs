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

    private float tickRate = 0.5f;
    private float elapsed;
    private float nextDamageTime;

    private const float DamageStartOffset = 0.5f;
    private const float DamageInterval = 1f;
    public Animator animator;

    [Header("Enemy Projectile Block")]
    [SerializeField] private string enemyProjectileObjectName = "Shooter_Projectile";

    private WeaponBase sourceWeapon;

    public void Initialize(float damage, float radius, float duration, bool destroyProjectile, WeaponBase weaponBase = null)
    {
      this.damage = damage;
      float expansionMul = Player.Instance != null ? Player.Instance.GetCompileNodeExpansionMultiplier() : 1f;
      this.radius = radius * expansionMul;
      this.duration = duration;
      this.destroyProjectile = destroyProjectile;
      this.sourceWeapon = weaponBase;

      transform.localScale = new Vector3(radius, radius, 1f);

      if (animator != null)
        animator.SetFloat("tickSpeedMultiplier", 1 / tickRate);

      elapsed = 0f;
      nextDamageTime = DamageStartOffset;

      if (this.destroyProjectile)
      {
        DestroyEnemyProjectiles();
      }

      Destroy(gameObject, duration);

      var sr = GetComponent<SpriteRenderer>();
      if (sr == null)
      {
        sr = gameObject.AddComponent<SpriteRenderer>();
        sr.color = new Color(0, 1, 1, 0.4f);
      }

      sr.sortingOrder = 0;
    }

    private void Update()
    {
      elapsed += Time.deltaTime;

      if (destroyProjectile)
      {
        DestroyEnemyProjectiles();
      }

      if (elapsed >= nextDamageTime)
      {
        DealAreaDamage();
        nextDamageTime += DamageInterval;
      }
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
      Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius);

      foreach (var hit in hits)
      {
        if (hit == null)
          continue;

        GameObject target =
          hit.attachedRigidbody != null
            ? hit.attachedRigidbody.gameObject
            : hit.gameObject;

        if (target == gameObject)
          continue;

        string cleanName = target.name.Replace("(Clone)", "").Trim();

        // ★ ShieldOrb와 같은 방식
        // ★ Shooter_Projectile 이름을 가진 오브젝트만 적 투사체로 판정
        if (cleanName != enemyProjectileObjectName)
          continue;

        Debug.Log($"[EMPField] 적 투사체 제거: {target.name}");

        Destroy(target);
      }
    }

    private void OnDrawGizmos()
    {
      Gizmos.color = Color.cyan;
      Gizmos.DrawWireSphere(transform.position, radius);
    }
  }
}
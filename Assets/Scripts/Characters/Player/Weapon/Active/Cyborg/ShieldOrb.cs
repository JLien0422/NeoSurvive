using System.Collections.Generic;
using UnityEngine;
using NeoSurvive.Core;

namespace NeoSurvive.Weapon
{
  [RequireComponent(typeof(Collider2D))]
  [RequireComponent(typeof(Rigidbody2D))]
  public class ShieldOrb : MonoBehaviour
  {
    public static readonly List<ShieldOrb> ActiveShields = new();

    private float damage;
    private LayerMask enemyMask;
    private string enemyTag;
    private bool blockProjectiles;
    private string projectileTag;
    private WeaponBase sourceWeapon;

    private Rigidbody2D rb;

    [Header("Projectile Block Radius")]
    [SerializeField] private float projectileBlockRadius = 0.8f;

    [Header("Enemy Projectile Name")]
    [SerializeField] private string enemyProjectileObjectName = "Shooter_Projectile";

    private void OnEnable()
    {
      if (!ActiveShields.Contains(this))
        ActiveShields.Add(this);
    }

    private void OnDisable()
    {
      ActiveShields.Remove(this);
    }

    private void Awake()
    {
      Collider2D col = GetComponent<Collider2D>();
      col.isTrigger = true;

      rb = GetComponent<Rigidbody2D>();
      rb.bodyType = RigidbodyType2D.Kinematic;
      rb.gravityScale = 0f;
      rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    public void Configure(
      float damage,
      LayerMask enemyMask,
      string enemyTag,
      bool blockProjectiles,
      string projectileTag,
      WeaponBase sourceWeapon)
    {
      this.damage = damage;
      this.enemyMask = enemyMask;
      this.enemyTag = enemyTag;
      this.blockProjectiles = blockProjectiles;
      this.projectileTag = projectileTag;
      this.sourceWeapon = sourceWeapon;
    }

    public void SetWorldPosition(Vector2 pos)
    {
      if (rb != null)
        rb.MovePosition(pos);
      else
        transform.position = pos;
    }

    private void Update()
    {
      CheckProjectiles();
    }

    private void CheckProjectiles()
    {
      if (!blockProjectiles)
        return;

      Collider2D[] hits = Physics2D.OverlapCircleAll(
        transform.position,
        projectileBlockRadius
      );

      for (int i = 0; i < hits.Length; i++)
      {
        Collider2D hit = hits[i];

        if (hit == null)
          continue;

        if (hit.gameObject == gameObject)
          continue;

        GameObject target =
          hit.attachedRigidbody != null
            ? hit.attachedRigidbody.gameObject
            : hit.gameObject;

        if (target == gameObject)
          continue;

        string cleanName = target.name.Replace("(Clone)", "").Trim();

        if (cleanName != enemyProjectileObjectName)
          continue;

        Debug.Log($"[ShieldOrb] 적 투사체 차단: {target.name}");

        Destroy(target);
        return;
      }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
      TryDamageTarget(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
      TryDamageTarget(other);
    }

    private void TryDamageTarget(Collider2D other)
    {
      if (other == null)
        return;

      bool isEnemy = IsEnemyCollider(other);

      Character character = other.GetComponent<Character>();
      if (character == null)
        character = other.GetComponentInParent<Character>();

      IDamageable damageable = other.GetComponent<IDamageable>();
      if (damageable == null)
        damageable = other.GetComponentInParent<IDamageable>();

      bool isInEnemyMask =
        enemyMask.value == 0 ||
        ((1 << other.gameObject.layer) & enemyMask.value) != 0;

      // LaserSword 방식:
      // enemyMask에 있으면 Character/Enemy 계열 허용
      // enemyMask 밖이어도 IDamageable이면 허용
      if (!isInEnemyMask && damageable == null)
        return;

      // Character도 아니고, Enemy 태그도 아니고, IDamageable도 아니면 무시
      if (character == null && !isEnemy && damageable == null)
        return;

      // =========================
      // 1. Character 처리
      // Enemy뿐 아니라 TrashObstacle : Character 도 여기서 맞음
      // =========================
      if (character != null)
      {
        character.TakeDamage(damage, sourceWeapon);

        return;
      }

      // =========================
      // 2. IDamageable MapObject 처리
      // 자판기 같은 오브젝트
      // =========================
      if (damageable != null)
      {
        damageable.TakeDamage(damage);
      }
    }

    private bool IsEnemyCollider(Collider2D other)
    {
      if (other == null) return false;
      if (string.IsNullOrEmpty(enemyTag)) return false;

      return
        other.CompareTag(enemyTag) ||
        (other.transform.parent != null &&
         other.transform.parent.CompareTag(enemyTag));
    }

    private void OnDrawGizmosSelected()
    {
      Gizmos.color = Color.cyan;
      Gizmos.DrawWireSphere(transform.position, projectileBlockRadius);
    }
  }
}
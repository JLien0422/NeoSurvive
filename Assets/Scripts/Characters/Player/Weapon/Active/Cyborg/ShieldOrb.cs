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
      string projectileTag)
    {
      this.damage = damage;
      this.enemyMask = enemyMask;
      this.enemyTag = enemyTag;
      this.blockProjectiles = blockProjectiles;
      this.projectileTag = projectileTag;
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

        // ★ Shooter_Projectile만 적 투사체로 판정
        if (cleanName != enemyProjectileObjectName)
          continue;

        Debug.Log($"[ShieldOrb] 적 투사체 차단: {target.name}");

        Destroy(target);
        return;
      }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
      if (other == null)
        return;

      if (((1 << other.gameObject.layer) & enemyMask.value) == 0)
        return;

      if (!string.IsNullOrEmpty(enemyTag) &&
          !other.CompareTag(enemyTag))
      {
        if (other.transform.parent == null ||
            !other.transform.parent.CompareTag(enemyTag))
          return;
      }

      Enemy enemy = other.GetComponentInParent<Enemy>();

      if (enemy != null)
      {
        WeaponSource src = GetComponent<WeaponSource>();

        enemy.TakeDamage(
          damage,
          src != null ? src.weaponData : null
        );
      }
    }

    private void OnDrawGizmosSelected()
    {
      Gizmos.color = Color.cyan;
      Gizmos.DrawWireSphere(transform.position, projectileBlockRadius);
    }
  }
}
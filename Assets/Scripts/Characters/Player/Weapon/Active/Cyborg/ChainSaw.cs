using System.Collections.Generic;
using UnityEngine;
using NeoSurvive.Core;

namespace NeoSurvive.Weapon
{
  [RequireComponent(typeof(Rigidbody2D))]
  [RequireComponent(typeof(Collider2D))]
  public class ChainSaw : MonoBehaviour
  {
    [Header("Damage")]
    public float damage = 6f;
    public float hitCooldown = 0.20f;
    public LayerMask enemyMask;
    public string enemyTag = "Enemy";

    [Header("Movement")]
    public float speed = 8f;
    public float lifetime = 8f;
    public LayerMask wallMask;

    [Header("Visual / Size")]
    public float radiusScale = 1.0f;

    [Header("Level Scaling")]
    public float damagePerLevel = 0.15f;
    public float speedPerLevel = 0.10f;
    public float sizePerLevel = 0.12f;
    public float cooldownMulPerLevel = 0.92f;
    public float lifetimePerLevel = 0.15f;

    [Header("Master (Lv5)")]
    public bool enableMaster = true;
    public float masterExtraDamage = 6f;
    public float masterSizeFactor = 1.25f;

    private Rigidbody2D rb;

    private float baseDamage;
    private float baseSpeed;
    private float baseCooldown;
    private float baseLifetime;
    private float baseSize;

    private int currentLevel = 1;

    // 같은 적 연속 타격 방지
    private readonly Dictionary<int, float> lastHitTime = new();

    private void Awake()
    {
      rb = GetComponent<Rigidbody2D>();

      rb.gravityScale = 0f;
      rb.drag = 0f;
      rb.angularDrag = 0f;
      rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
      rb.interpolation = RigidbodyInterpolation2D.Interpolate;

      GetComponent<Collider2D>().isTrigger = false;
    }

    private void Start()
    {
      baseDamage = damage;
      baseSpeed = speed;
      baseCooldown = hitCooldown;
      baseLifetime = lifetime;
      baseSize = radiusScale;

      ApplyLevel(1);

      // 랜덤 초기 방향
      Vector2 dir = Random.insideUnitCircle.normalized;
      if (dir.sqrMagnitude < 0.01f) dir = Vector2.right;

      rb.velocity = dir * speed;

      Destroy(gameObject, lifetime);
    }

    private void FixedUpdate()
    {
      // 속도 유지(튕긴 후 감속 방지)
      Vector2 v = rb.velocity;
      if (v.sqrMagnitude > 0.01f)
      {
        rb.velocity = v.normalized * speed;
      }
    }

    /// <summary>
    /// WeaponManager → SendMessage("OnLevelUp")로 호출됨
    /// </summary>
    public void OnLevelUp(int level)
    {
      ApplyLevel(level);
      Debug.Log($"[ChainSaw] Lv.{currentLevel} dmg={damage} speed={speed} size={radiusScale}");
    }

    private void ApplyLevel(int level)
    {
      currentLevel = Mathf.Clamp(level, 1, 5);

      damage = baseDamage * (1f + (currentLevel - 1) * damagePerLevel);
      speed = baseSpeed * (1f + (currentLevel - 1) * speedPerLevel);
      hitCooldown = baseCooldown * Mathf.Pow(cooldownMulPerLevel, (currentLevel - 1));
      lifetime = baseLifetime * (1f + (currentLevel - 1) * lifetimePerLevel);
      radiusScale = baseSize * (1f + (currentLevel - 1) * sizePerLevel);

      if (enableMaster && currentLevel >= 5)
      {
        damage += masterExtraDamage;
        radiusScale *= masterSizeFactor;
      }

      transform.localScale = Vector3.one * radiusScale;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
      if (collision == null || collision.collider == null) return;

      // 벽 반사
      if (((1 << collision.collider.gameObject.layer) & wallMask.value) != 0)
      {
        Vector2 v = rb.velocity;
        Vector2 n = (collision.contacts.Length > 0) ? collision.contacts[0].normal : -v.normalized;
        rb.velocity = Vector2.Reflect(v, n).normalized * speed;
        return;
      }

      // 적 타격
      TryDamageEnemy(collision.collider);
    }

    private void TryDamageEnemy(Collider2D col)
    {
      if (((1 << col.gameObject.layer) & enemyMask.value) == 0) return;

      if (!string.IsNullOrEmpty(enemyTag) && !col.CompareTag(enemyTag))
      {
        if (col.transform.parent == null || !col.transform.parent.CompareTag(enemyTag))
          return;
      }

      Enemy enemy = col.GetComponentInParent<Enemy>();
      if (enemy == null) return;

      int id = enemy.gameObject.GetInstanceID();
      float now = Time.time;

      if (lastHitTime.TryGetValue(id, out float last) && now - last < hitCooldown)
        return;

      lastHitTime[id] = now;
      enemy.TakeDamage(damage);
    }
  }
}

using UnityEngine;
using NeoSurvive.Core;
using NeoSurvive.Buff;
using System.Collections.Generic;

namespace NeoSurvive.Weapon
{
  public class BreathArea : MonoBehaviour
  {
    [Header("Filter")]
    public LayerMask enemyMask;
    public string enemyTag = "Enemy";

    private float damagePerTick;
    private float tickInterval;
    private float duration;
    private float range;
    private float width;
    private bool coldMode;

    private float life;
    private float timer;

    [Header("Cold Settings")]
    public bool freezeOnHit = true;
    public float freezeDuration = 1.25f;
    public bool freezeAsStun = false;

    [Header("Cold Slow Optional")]
    public bool applySlowInCold = false;
    [Range(0.05f, 1f)] public float slowMul = 0.7f;
    public float slowDuration = 0.35f;

    public void Initialize(
      float damagePerTick,
      float tickInterval,
      float duration,
      float range,
      float width,
      bool coldMode,
      LayerMask enemyMask,
      string enemyTag)
    {
      this.damagePerTick = damagePerTick;
      this.tickInterval = tickInterval;
      this.duration = duration;
      this.range = range;
      this.width = width;
      this.coldMode = coldMode;
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
      if (timer >= tickInterval)
      {
        TickDamage();
        timer = 0f;
      }
    }

    private void TickDamage()
    {
      Vector2 boxSize = new Vector2(range, width);
      float angleZ = transform.eulerAngles.z;

      Collider2D[] hits = Physics2D.OverlapBoxAll(transform.position, boxSize, angleZ);

      HashSet<Enemy> damagedEnemies = new HashSet<Enemy>();
      HashSet<Component> damagedMapObjects = new HashSet<Component>();

      foreach (var col in hits)
      {
        if (col == null) continue;

        Enemy enemy = GetValidEnemy(col);

        if (enemy != null)
        {
          if (damagedEnemies.Contains(enemy))
            continue;

          damagedEnemies.Add(enemy);

          enemy.TakeDamage(damagePerTick);

          if (coldMode)
          {
            if (applySlowInCold)
              BuffUtil.Apply(enemy.gameObject, new SlowDebuff(slowMul, slowDuration));

            if (freezeOnHit)
            {
              if (freezeAsStun)
                BuffUtil.Apply(enemy.gameObject, new StunDebuff(freezeDuration));
              else
                BuffUtil.Apply(enemy.gameObject, new RootDebuff(freezeDuration));
            }
          }

          continue;
        }

        IDamageable damageable = col.GetComponent<IDamageable>();
        if (damageable == null)
          damageable = col.GetComponentInParent<IDamageable>();

        if (damageable == null)
          continue;

        Component damageableComponent = damageable as Component;
        if (damageableComponent != null && damagedMapObjects.Contains(damageableComponent))
          continue;

        if (damageableComponent != null)
          damagedMapObjects.Add(damageableComponent);

        damageable.TakeDamage(damagePerTick);
      }
    }

    private Enemy GetValidEnemy(Collider2D col)
    {
      if (col == null) return null;

      bool isInEnemyMask = ((1 << col.gameObject.layer) & enemyMask.value) != 0;
      if (!isInEnemyMask)
        return null;

      if (!string.IsNullOrEmpty(enemyTag))
      {
        bool self = col.CompareTag(enemyTag);
        bool parent = col.transform.parent != null && col.transform.parent.CompareTag(enemyTag);

        if (!self && !parent)
          return null;
      }

      return col.GetComponentInParent<Enemy>();
    }

    private void OnDrawGizmosSelected()
    {
      Gizmos.color = Color.white;

      Matrix4x4 oldMatrix = Gizmos.matrix;
      Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
      Gizmos.DrawWireCube(Vector3.zero, new Vector3(range, width, 1f));
      Gizmos.matrix = oldMatrix;
    }
  }
}
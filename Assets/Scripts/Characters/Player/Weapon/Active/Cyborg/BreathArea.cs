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

      Collider2D[] hits = Physics2D.OverlapBoxAll(
        transform.position,
        boxSize,
        angleZ
      );

      HashSet<int> damagedIds = new HashSet<int>();

      foreach (var col in hits)
      {
        if (col == null)
          continue;

        TryDamageTarget(col, damagedIds);
      }
    }

    private bool TryDamageTarget(Collider2D col, HashSet<int> damagedIds)
    {
      if (col == null)
        return false;

      bool isEnemy = IsEnemyCollider(col);

      IDamageable damageable = col.GetComponent<IDamageable>();

      if (damageable == null)
        damageable = col.GetComponentInParent<IDamageable>();

      bool isInEnemyMask =
        enemyMask.value == 0 ||
        ((1 << col.gameObject.layer) & enemyMask.value) != 0;

      // LaserSword 구조와 동일:
      // Enemy는 enemyMask 기준 유지
      // IDamageable은 enemyMask 밖이어도 허용
      if (!isInEnemyMask && damageable == null)
        return false;

      // Enemy도 아니고 IDamageable도 아니면 무시
      if (!isEnemy && damageable == null)
        return false;

      // =========================
      // 1. Enemy 태그 + Character 처리
      // 쓰레기처럼 TrashObstacle : Character 인 대상 포함
      // =========================
      if (isEnemy)
      {
        Character character = col.GetComponentInParent<Character>();

        if (character != null)
        {
          int id = character.gameObject.GetInstanceID();

          if (damagedIds.Contains(id))
            return false;

          damagedIds.Add(id);

          character.TakeDamage(damagePerTick, null);

          ApplyColdEffects(character.gameObject);

          return true;
        }
      }

      // =========================
      // 2. IDamageable MapObject 처리
      // 자판기 같은 오브젝트
      // =========================
      if (damageable != null)
      {
        Component damageableComponent = damageable as Component;

        if (damageableComponent != null)
        {
          int id = damageableComponent.gameObject.GetInstanceID();

          if (damagedIds.Contains(id))
            return false;

          damagedIds.Add(id);
        }

        damageable.TakeDamage(damagePerTick);

        return true;
      }

      return false;
    }

    private bool IsEnemyCollider(Collider2D col)
    {
      if (col == null)
        return false;

      if (string.IsNullOrEmpty(enemyTag))
        return false;

      return
        col.CompareTag(enemyTag) ||
        (col.transform.parent != null &&
         col.transform.parent.CompareTag(enemyTag));
    }

    private void ApplyColdEffects(GameObject target)
    {
      if (!coldMode)
        return;

      if (target == null)
        return;

      if (applySlowInCold)
        BuffUtil.Apply(target, new SlowDebuff(slowMul, slowDuration));

      if (freezeOnHit)
      {
        if (freezeAsStun)
          BuffUtil.Apply(target, new StunDebuff(freezeDuration));
        else
          BuffUtil.Apply(target, new RootDebuff(freezeDuration));
      }
    }

    private void OnDrawGizmosSelected()
    {
      Gizmos.color = Color.white;

      Matrix4x4 oldMatrix = Gizmos.matrix;
      Gizmos.matrix = Matrix4x4.TRS(
        transform.position,
        transform.rotation,
        Vector3.one
      );

      Gizmos.DrawWireCube(
        Vector3.zero,
        new Vector3(range, width, 1f)
      );

      Gizmos.matrix = oldMatrix;
    }
  }
}
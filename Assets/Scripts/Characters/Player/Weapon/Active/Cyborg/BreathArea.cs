using UnityEngine;
using NeoSurvive.Core;
using NeoSurvive.Buff;

namespace NeoSurvive.Weapon
{
  /// <summary>
  /// 블래스트 브레스 방사 영역
  /// - tick마다 범위 내 적에게 피해
  /// - Lv5 coldMode: 닿는 즉시 빙결(속박/기절)
  /// - SurgeBlade와 동일한 판정 방식 사용 (Enemy 프리팹 수정 불필요)
  /// </summary>
  public class BreathArea : MonoBehaviour
  {
    [Header("Filter")]
    public LayerMask enemyMask;          // 선택 사항 (비워도 동작)
    public string enemyTag = "Enemy";

    private float damagePerTick;
    private float tickInterval;
    private float duration;
    private float range;
    private float width;
    private bool coldMode;

    private float life;
    private float timer;

    // ===================== 냉기(빙결) 옵션 =====================

    [Header("Cold (Freeze) Settings")]
    public bool freezeOnHit = true;
    public float freezeDuration = 1.25f;
    public bool freezeAsStun = false; // true=기절, false=속박

    [Header("Cold (Slow) Optional")]
    public bool applySlowInCold = false;
    [Range(0.05f, 1f)] public float slowMul = 0.7f;
    public float slowDuration = 0.35f;

    // ==========================================================

    public void Initialize(
      float damagePerTick,
      float tickInterval,
      float duration,
      float range,
      float width,
      bool coldMode)
    {
      this.damagePerTick = damagePerTick;
      this.tickInterval = tickInterval;
      this.duration = duration;
      this.range = range;
      this.width = width;
      this.coldMode = coldMode;

      life = 0f;
      timer = 0f;

      // 시각용 색상
      var sr = GetComponent<SpriteRenderer>();
      if (sr != null)
        sr.color = coldMode ? Color.cyan : new Color(1f, 0.4f, 0.1f);
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

      // ✅ SurgeBlade 방식:
      // enemyMask가 있으면 사용, 없으면 전체 레이어 탐색
      Collider2D[] hits;
      if (enemyMask.value != 0)
        hits = Physics2D.OverlapBoxAll(transform.position, boxSize, angleZ, enemyMask);
      else
        hits = Physics2D.OverlapBoxAll(transform.position, boxSize, angleZ);

      foreach (var col in hits)
      {
        if (col == null) continue;

        // ✅ 태그 필터 (자식 콜라이더 허용)
        if (!string.IsNullOrEmpty(enemyTag) && !col.CompareTag(enemyTag))
        {
          if (col.transform.parent == null || !col.transform.parent.CompareTag(enemyTag))
            continue;
        }

        // ✅ Enemy는 부모에 있을 수 있음
        Enemy enemy = col.GetComponentInParent<Enemy>();
        if (enemy == null) continue;

        // 1) 데미지 적용
        enemy.TakeDamage(damagePerTick);

        // 2) Lv5 냉기 효과
        if (coldMode)
        {
          GameObject target = enemy.gameObject;

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
      }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
      Gizmos.color = Color.white;
      Gizmos.DrawWireCube(transform.position, new Vector3(range, width, 1f));
    }
#endif
  }
}

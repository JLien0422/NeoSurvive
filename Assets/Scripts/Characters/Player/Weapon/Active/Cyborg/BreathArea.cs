using UnityEngine;
using NeoSurvive.Core;
using NeoSurvive.Buff; // (추가)

namespace NeoSurvive.Weapon
{
  /// <summary>
  /// 블래스트 브레스 방사 영역
  /// - 일정 시간 동안 tick마다 범위 내 적에게 피해
  /// - (Lv5 coldMode) 냉기: 닿는 즉시 빙결(속박/기절) 적용
  /// </summary>
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

    // ===================== 냉기(빙결) 옵션 (추가) =====================

    [Header("Cold (Freeze) Settings (추가)")]
    [Tooltip("coldMode일 때 즉시 빙결 적용 여부")]
    public bool freezeOnHit = true; // (추가)

    [Tooltip("빙결 지속 시간(초)")]
    public float freezeDuration = 1.25f; // (추가)

    [Tooltip("빙결을 '속박'으로 할지, '기절'로 할지 선택")]
    public bool freezeAsStun = false; // (추가)
    // true  -> StunDebuff(freezeDuration)
    // false -> RootDebuff(freezeDuration)

    [Header("Cold (Slow) Optional (추가)")]
    public bool applySlowInCold = false; // (추가) 원하면 켜
    [Range(0.05f, 1f)] public float slowMul = 0.7f; // (추가)
    public float slowDuration = 0.35f; // (추가) 틱마다 갱신용(짧게)

    // ================================================================

    public void Initialize(float damagePerTick, float tickInterval, float duration, float range, float width, bool coldMode)
    {
      this.damagePerTick = damagePerTick;
      this.tickInterval = tickInterval;
      this.duration = duration;
      this.range = range;
      this.width = width;
      this.coldMode = coldMode;

      life = 0f;
      timer = 0f;

      // 보기용(선택): 냉기 모드면 색상 바꿈
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
      // 직사각 범위(회전 포함) = OverlapBoxAll
      Vector2 boxSize = new Vector2(range, width);
      float angleZ = transform.eulerAngles.z;

      Collider2D[] hits = Physics2D.OverlapBoxAll(transform.position, boxSize, angleZ, enemyMask);
      foreach (var col in hits)
      {
        if (col == null) continue;

        // 태그 필터(유지)
        if (!string.IsNullOrEmpty(enemyTag) && !col.CompareTag(enemyTag))
        {
          if (col.transform.parent == null || !col.transform.parent.CompareTag(enemyTag))
            continue;
        }

        Enemy enemy = col.GetComponentInParent<Enemy>();
        if (enemy == null) continue;

        // 1) 데미지
        var src = GetComponent<WeaponSource>();
        enemy.TakeDamage(damagePerTick, src != null ? src.weaponData : null);

        // 2) Lv5 냉기 모드: 닿는 즉시 빙결 (추가)
        if (coldMode)
        {
          GameObject target = enemy.gameObject;

          // (옵션) 냉기 슬로우도 같이 주고 싶으면
          if (applySlowInCold)
          {
            BuffUtil.Apply(target, new SlowDebuff(slowMul, slowDuration));
          }

          // 즉시 빙결
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

    private void OnDrawGizmosSelected()
    {
      Gizmos.color = Color.white;
      Gizmos.DrawWireCube(transform.position, new Vector3(range, width, 1f));
    }
  }
}

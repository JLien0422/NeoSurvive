using UnityEngine;

/// <summary>
/// 보스가 던지는 파괴 가능 장애물. Enemy 태그 + Character 피격으로 투사체/무기와 동일하게 연동됩니다.
/// 밀림 방지·고정은 Rigidbody2D <b>Constraints(Freeze Position X/Y 등)</b>를 프리팹·인스펙터에서 설정하세요.
/// 프리팹: Collider2D <b>Is Trigger 끄기</b>(길막), Rigidbody2D Dynamic·Gravity 0 권장.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class TrashObstacle : Character
{
  [Header("투척 충돌")]
  [SerializeField] private float impactDamage = 12f;
  [SerializeField] private float minImpactSpeed = 1.5f;

  private Rigidbody2D rb;

  protected override void Awake()
  {
    base.Awake();
    rb = GetComponent<Rigidbody2D>();
  }

  private void OnCollisionEnter2D(Collision2D collision)
  {
    if (collision == null || IsDead)
      return;
    if (!collision.collider.CompareTag("Player"))
      return;
    if (rb == null || rb.velocity.sqrMagnitude < minImpactSpeed * minImpactSpeed)
      return;

    if (collision.collider.TryGetComponent<Player>(out var player))
    {
      GameAnalyticsTracker.TrackBossPatternHit(nameof(TrashObstacle), "thrown_trash_collision", impactDamage, player);
      player.TakeDamage(impactDamage);
    }

    Die();
  }

  protected override void Die()
  {
    if (IsDead) return;
    base.Die();
    Destroy(gameObject);
  }
}

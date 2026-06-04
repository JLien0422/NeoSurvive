using UnityEngine;

/// <summary>
/// 보스가 던지는 파괴 가능 장애물.
/// 여러 스프라이트 중 랜덤으로 외형이 결정됩니다.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class TrashObstacle : Character
{
    [Header("투척 충돌")]
    [SerializeField] private float impactDamage = 12f;
    [SerializeField] private float minImpactSpeed = 1.5f;

    [Header("랜덤 스프라이트")]
    [SerializeField] private Sprite[] trashSprites;

    [SerializeField] private bool randomFlipX = true;
    [SerializeField] private bool randomRotation = true;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;

    protected override void Awake()
    {
        base.Awake();

        rb = GetComponent<Rigidbody2D>();

        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        ApplyRandomSprite();
    }

    private void ApplyRandomSprite()
    {
        if (spriteRenderer == null)
            return;

        if (trashSprites == null || trashSprites.Length == 0)
            return;

        // 랜덤 스프라이트 선택
        int index = Random.Range(0, trashSprites.Length);

        spriteRenderer.sprite = trashSprites[index];

        // 랜덤 좌우반전
        if (randomFlipX)
        {
            spriteRenderer.flipX = Random.value > 0.5f;
        }

        // 랜덤 회전
        if (randomRotation)
        {
            float rot = Random.Range(0f, 360f);

            transform.rotation =
                Quaternion.Euler(0f, 0f, rot);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision == null || IsDead)
            return;

        if (!collision.collider.CompareTag("Player"))
            return;

        if (rb == null)
            return;

        if (rb.velocity.sqrMagnitude <
            minImpactSpeed * minImpactSpeed)
            return;

        if (collision.collider.TryGetComponent<Player>(out var player))
        {
            GameAnalyticsTracker.TrackBossPatternHit(
                nameof(TrashObstacle),
                "thrown_trash_collision",
                impactDamage,
                player
            );

            player.TakeDamage(impactDamage);
        }

        Die();
    }

    protected override void Die()
    {
        if (IsDead)
            return;

        base.Die();

        Destroy(gameObject);
    }
}
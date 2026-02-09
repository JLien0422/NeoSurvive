using UnityEngine;

/// <summary>
/// 전기 울타리 한 조각(벽)
/// - Collider로 적을 막고
/// - 닿은 적에게 초당 데미지를 준다
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ElectricFenceSegment : MonoBehaviour
{
    [SerializeField] private float damagePerSecond = 20f;

    // 외부에서 설정 가능하게
    public void SetDps(float dps) => damagePerSecond = Mathf.Max(0f, dps);

    private void Reset()
    {
        // 기본값: 물리 차단을 위해 Trigger는 꺼둔다
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = false;
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        // 물리 차단(Trigger=false)일 때는 Collision으로 들어옴
        if (!collision.collider.CompareTag("Enemy")) return;

        Enemy enemy = collision.collider.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.TakeDamage(damagePerSecond * Time.deltaTime);
        }
    }
}

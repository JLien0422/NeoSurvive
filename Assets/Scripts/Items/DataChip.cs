using UnityEngine;

/// <summary>
/// 데이터칩 아이템
/// 플레이어가 획득하면 신경링크 게이지가 증가합니다.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class DataChip : MonoBehaviour
{
    [Header("신경링크 게이지 증가량")]
    [SerializeField]
    [Tooltip("플레이어가 획득 시 증가할 신경링크 게이지 양 (퍼센트)")]
    private float gaugeAmount = 10f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 플레이어가 획득했는지 확인
        if (other.CompareTag("Player"))
        {
            Player player = other.GetComponent<Player>();
            if (player != null)
            {
                player.AddNeuralLinkGauge(gaugeAmount);
                Destroy(gameObject);
            }
        }
    }

    // 컴포넌트가 추가되거나 리셋될 때 호출됩니다.
    // 콜라이더가 isTrigger로 설정되도록 보장합니다.
    private void Reset()
    {
        Collider2D[] colliders = GetComponents<Collider2D>();
        foreach (var col in colliders)
        {
            col.isTrigger = true;
        }
    }
}

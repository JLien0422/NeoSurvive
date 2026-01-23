using UnityEngine;

/// <summary>
/// 사이코 잠식도 아이템
/// 플레이어가 획득하면 사이코 잠식도가 증가합니다.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class PsychoCorruptionItem : MonoBehaviour
{
    [Header("사이코 잠식도 증가량")]
    [SerializeField]
    [Tooltip("플레이어가 획득 시 증가할 사이코 잠식도 양 (퍼센트)")]
    private float corruptionAmount = 10f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 플레이어가 획득했는지 확인
        if (other.CompareTag("Player"))
        {
            Player player = other.GetComponent<Player>();
            if (player != null)
            {
                player.AddPsychoCorruption(corruptionAmount);
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

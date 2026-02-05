using UnityEngine;
using System;
using NeoSurvive.Network;
using NeoSurvive.Network.Protocol;

public class ChestPickup : MonoBehaviour
{
    public static event Action OnChestOpened; // UIManager가 구독하는 이벤트

    private bool opened = false;

    private void OnEnable()
    {
        Debug.Log("[ChestPickup] OnEnable");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[ChestPickup] Enter: {other.name}, tag={other.tag}");

        if (opened) return;
        if (!other.CompareTag("Player")) return;

        Player player = other.GetComponent<Player>();
        if (player != null && player.IsLocal && UDPClient.Instance != null)
        {
            var networkItem = GetComponent<NetworkItem>();
            if (networkItem != null)
            {
                UDPClient.Instance.SendAction(ActionType.ObjectInteract, networkItem.ItemId);
            }
        }

        opened = true;
        Open();
    }

    private void Open()
    {
        Debug.Log("🎁 Chest Opened!");

        OnChestOpened?.Invoke();   // UIManager로 이벤트 전달
        Destroy(gameObject);       // 즉시 사라짐 (ExpOrb 느낌)
    }
}

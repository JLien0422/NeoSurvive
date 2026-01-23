using UnityEngine;
using System;

public class ChestPickup : MonoBehaviour
{
    public static event Action OnChestOpened; // 이벤트 선언

    public float holdSeconds = 1.5f;

    private float timer = 0f;
    private bool inRange = false;
    private bool opened = false;

    private void Update()
    {
        if (opened || !inRange) return;

        timer += Time.deltaTime;

        if (timer >= holdSeconds)
        {
            opened = true;
            Open();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        timer = 0f;
        inRange = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        timer = 0f;
        inRange = false;
    }

    private void Open()
    {
        Debug.Log("🎁 Chest Opened!");

        OnChestOpened?.Invoke(); // 이벤트 호출

        Destroy(gameObject);
    }
}


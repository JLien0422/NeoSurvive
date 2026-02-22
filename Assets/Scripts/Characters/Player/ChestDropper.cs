using System.Collections;
using UnityEngine;

public class ChestDropper : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject chestPrefab;
    public GameObject spawnIndicatorPrefab; // ★ 추가: 생성 위치 표시

    [Header("Spawn Timing")]
    public float dropDelay = 0.8f;           // ★ 변경: 0.8초

    [Header("Spawn Range")]
    public float spawnRadius = 2.5f;

    private void OnEnable()
    {
        Debug.Log("[ChestDropper] OnEnable - subscribed");
        Player.OnLevelUp += HandleLevelUp;
    }

    private void OnDisable()
    {
        Debug.Log("[ChestDropper] OnDisable - unsubscribed");
        Player.OnLevelUp -= HandleLevelUp;
    }

    private void OnDestroy()
    {
        Player.OnLevelUp -= HandleLevelUp;
    }

    private void HandleLevelUp(int newLevel)
    {
        Debug.Log($"[ChestDropper] HandleLevelUp called! newLevel={newLevel}");
        StartCoroutine(DropAfterDelay());
    }

    private IEnumerator DropAfterDelay()
    {
        if (chestPrefab == null)
        {
            Debug.LogError("[ChestDropper] chestPrefab is NULL (Inspector에 프리팹 넣어야 함)");
            yield break;
        }

        // 1️⃣ 스폰 위치를 미리 결정 (0.8초 동안 고정)
        Vector2 offset = Random.insideUnitCircle.normalized * Random.Range(1.0f, spawnRadius);
        Vector3 spawnPos = transform.position + (Vector3)offset;
        spawnPos.z = 0f;

        // 2️⃣ 생성 위치 표시 (Indicator)
        GameObject indicator = null;
        if (spawnIndicatorPrefab != null)
        {
            indicator = Instantiate(spawnIndicatorPrefab, spawnPos, Quaternion.identity);
        }

        // 3️⃣ 0.8초 대기
        yield return new WaitForSeconds(dropDelay);

        // 4️⃣ Indicator 제거
        if (indicator != null)
        {
            Destroy(indicator);
        }

        // 5️⃣ 상자 생성
        Instantiate(chestPrefab, spawnPos, Quaternion.identity);
        Debug.Log($"[ChestDropper] Chest spawned at {spawnPos}");
    }
}

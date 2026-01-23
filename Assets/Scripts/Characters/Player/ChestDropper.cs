using System.Collections;
using UnityEngine;

public class ChestDropper : MonoBehaviour
{
  public GameObject chestPrefab;
  public float dropDelay = 0.2f;
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

  private void HandleLevelUp(int newLevel)
  {
    Debug.Log($"[ChestDropper] HandleLevelUp called! newLevel={newLevel}");
    StartCoroutine(DropAfterDelay());
  }

  private IEnumerator DropAfterDelay()
  {
    yield return new WaitForSeconds(dropDelay);

    if (chestPrefab == null)
    {
      Debug.LogError("[ChestDropper] chestPrefab is NULL (Inspector에 프리팹 넣어야 함)");
      yield break;
    }

    Vector2 offset = Random.insideUnitCircle.normalized * Random.Range(1.0f, spawnRadius);
    Vector3 spawnPos = transform.position + (Vector3)offset;

    Instantiate(chestPrefab, spawnPos, Quaternion.identity);
    Debug.Log($"[ChestDropper] Chest spawned at {spawnPos}");
  }
}

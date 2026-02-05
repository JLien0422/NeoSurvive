using UnityEngine;

namespace NeoSurvive.Weapon
{
  /// <summary>
  /// ChainSaw 무기(지속 장착형): 일정 주기마다 ChainSaw(1회용)를 재소환
  /// WeaponManager.SendMessage("OnLevelUp", level) 호환
  /// </summary>
  public class ChainSawEmitter : MonoBehaviour
  {
    [Header("Spawn")]
    public GameObject chainSawPrefab;     // 실제 튕기는 ChainSaw 프리팹
    public float spawnInterval = 4f;      // 몇 초마다 새로 소환할지
    public int spawnCount = 1;            // 한 번에 몇 개
    public float spawnRadius = 0.6f;      // 플레이어 주변 소환 위치

    [Header("Pass Level To ChainSaw")]
    public bool sendLevelToSpawned = true;

    private float timer;
    private int currentLevel = 1;

    private void Update()
    {
      timer += Time.deltaTime;
      if (timer < spawnInterval) return;
      timer = 0f;

      Spawn();
    }

    public void OnLevelUp(int level)
    {
      currentLevel = Mathf.Clamp(level, 1, 5);

      // 예시 레벨업 규칙(원하면 네 기획대로 바꿔줄게)
      // - 레벨업마다 소환 주기 감소
      // - 레벨업마다 개수 증가(최대 3)
      spawnInterval = Mathf.Max(1.5f, 4f - (currentLevel - 1) * 0.5f);
      spawnCount = Mathf.Clamp(1 + (currentLevel - 1) / 2, 1, 3);

      Debug.Log($"[ChainSawEmitter] Lv.{currentLevel} interval={spawnInterval} count={spawnCount}");
    }

    private void Spawn()
    {
      if (chainSawPrefab == null) return;

      for (int i = 0; i < spawnCount; i++)
      {
        Vector2 offset = Random.insideUnitCircle * spawnRadius;
        Vector3 pos = transform.position + (Vector3)offset;

        GameObject obj = Instantiate(chainSawPrefab, pos, Quaternion.identity);

        if (sendLevelToSpawned)
          obj.SendMessage("OnLevelUp", currentLevel, SendMessageOptions.DontRequireReceiver);
      }
    }
  }
}

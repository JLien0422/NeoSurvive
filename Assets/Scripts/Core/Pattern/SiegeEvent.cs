using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SiegeEvent : MonoBehaviour
{
  [Header("Enemy Prefab Index (0:Basic,1:Shooter,2:Rusher,3:Bomber,4:Tanker)")]
  [SerializeField] private GameObject[] enemyPrefabs;

  [Header("포위 스폰 설정")]
  [SerializeField] private float innerRadius = 9.5f;
  [SerializeField] private float outerRadius = 14.5f;

  [Header("외부 링(슈터) 수")]
  [SerializeField] private int outerCount = 10;

  [Header("내부 링(좁혀오는 적) 수 - Phase별")]
  [SerializeField] private int innerCountP2 = 18; // ***** 추가
  [SerializeField] private int innerCountP3 = 24; // ***** 추가
  [SerializeField] private int innerCountP4 = 28; // ***** 추가
  [SerializeField] private int innerCountP5 = 32; // ***** 추가

  [SerializeField] private float siegeDuration = 10f;

  private Transform player;
  private bool running;
  private Action onEnd;

  private void Awake()
  {
    GameManager.onPlayerSpawned += RefreshPlayerReference;
  }

  private void Start()
  {
    RefreshPlayerReference();
  }

  private void OnDestroy()
  {
    GameManager.onPlayerSpawned -= RefreshPlayerReference;
  }

  public void BeginSiege(int phase, Action onEnded)
  {
    if (running) return;

    if (player == null)
    {
      RefreshPlayerReference();
    }

    if (player == null)
    {
      Debug.LogWarning("[SiegeEvent] Player를 찾지 못해 포위 이벤트를 시작하지 못했습니다.");
      return;
    }

    if (enemyPrefabs == null || enemyPrefabs.Length < 5)
    {
      Debug.LogError("[SiegeEvent] enemyPrefabs(0~4) 세팅이 필요합니다!");
      return;
    }

    if (EnemySpawnLoader.DB == null)
    {
      Debug.LogError("[SiegeEvent] EnemySpawnLoader.DB가 null입니다.");
      onEnded?.Invoke();
      return;
    }

    if (!EnemySpawnLoader.DB.TryGetRow(phase, out var row))
    {
      Debug.LogError($"[SiegeEvent] CSV에 phase {phase} 데이터가 없습니다.");
      onEnded?.Invoke();
      return;
    }

    running = true;
    onEnd = onEnded;

    SpawnInnerRing(row);
    SpawnOuterRing(row);

    StartCoroutine(EndRoutine(row.siegeDuration));
  }

  private void RefreshPlayerReference()
  {
    GameObject playerObj = GameObject.FindWithTag("Player");
    player = playerObj != null ? playerObj.transform : null;

    if (player == null)
    {
      Debug.LogWarning("[SiegeEvent] Player 태그 오브젝트를 아직 찾지 못했습니다.");
    }
  }

  private void SpawnInnerRing(EnemySpawnDB.Row row)
  {
    var weights = BuildWeightsFromRow(row);
    List<int> innerTypes = BuildExactTypeList(row.siegeInnerCount, weights);

    for (int i = 0; i < innerTypes.Count; i++)
    {
      float angleDeg = (360f / innerTypes.Count) * i;
      Vector3 pos = player.position + AngleToVector(angleDeg) * row.siegeInnerRadius;

      int typeIdx = innerTypes[i];
      Instantiate(enemyPrefabs[typeIdx], pos, Quaternion.identity);
    }

    Debug.Log($"[SiegeEvent] InnerRing Spawned phase={row.phase} count={innerTypes.Count}");
  }

  private void SpawnOuterRing(EnemySpawnDB.Row row)
  {
    for (int i = 0; i < row.siegeOuterCount; i++)
    {
      float angleDeg = (360f / row.siegeOuterCount) * i;
      Vector3 pos = player.position + AngleToVector(angleDeg) * row.siegeOuterRadius;

      GameObject obj = Instantiate(enemyPrefabs[1], pos, Quaternion.identity); // Shooter

      if (row.freezeOuterShooters)
      {
        var rb = obj.GetComponent<Rigidbody2D>();
        if (rb != null)
          rb.constraints = RigidbodyConstraints2D.FreezePosition;
      }
    }

    Debug.Log($"[SiegeEvent] OuterRing Shooter Spawned count={row.siegeOuterCount}");
  }

  private Dictionary<int, float> BuildWeightsFromRow(EnemySpawnDB.Row row)
  {
    Dictionary<int, float> weights = new();

    weights[0] = row.basicWeight;
    weights[1] = row.shooterWeight;
    weights[2] = row.rusherWeight;
    weights[3] = row.bomberWeight;
    weights[4] = row.tankerWeight;

    return weights;
  }

  private void SpawnInnerRingByPhaseWeights(int phase)
  {
    var weights = GetInnerWeights(phase);

    int innerCountThisPhase = phase switch // ***** 추가
    {
      2 => innerCountP2,
      3 => innerCountP3,
      4 => innerCountP4,
      5 => innerCountP5,
      _ => innerCountP2
    };

    List<int> innerTypes = BuildExactTypeList(innerCountThisPhase, weights);

    for (int i = 0; i < innerTypes.Count; i++)
    {
      float angleDeg = (360f / innerTypes.Count) * i;
      Vector3 pos = player.position + AngleToVector(angleDeg) * innerRadius;

      int typeIdx = innerTypes[i];
      Instantiate(enemyPrefabs[typeIdx], pos, Quaternion.identity);
    }

    Debug.Log($"[SiegeEvent] InnerRing Spawned phase={phase} (count={innerTypes.Count})");
  }

  private Dictionary<int, float> GetInnerWeights(int phase)
  {
    Dictionary<int, float> w = new();

    switch (phase)
    {
      case 2:
        w[0] = 60f;                 // Basic
        break;

      case 3:
        w[0] = 50f; w[2] = 20f;     // Basic, Rusher
        break;

      case 4:
        w[0] = 40f; w[2] = 15f; w[3] = 15f; // Basic, Rusher, Bomber
        break;

      case 5:
        w[0] = 35f; w[2] = 15f; w[3] = 15f; w[4] = 10f; // Basic, Rusher, Bomber, Tanker
        break;

      default:
        w[0] = 60f;
        break;
    }

    return w;
  }

  private List<int> BuildExactTypeList(int count, Dictionary<int, float> weights)
  {
    List<int> result = new List<int>(count);

    float total = 0f;
    foreach (var kv in weights) total += Mathf.Max(0f, kv.Value);
    if (total <= 0f)
    {
      for (int i = 0; i < count; i++) result.Add(0);
      return result;
    }

    Dictionary<int, int> baseCounts = new();
    Dictionary<int, float> remainders = new();

    int assigned = 0;
    foreach (var kv in weights)
    {
      float w = Mathf.Max(0f, kv.Value);
      float exact = (w / total) * count;
      int c = Mathf.FloorToInt(exact);

      baseCounts[kv.Key] = c;
      remainders[kv.Key] = exact - c;
      assigned += c;
    }

    int remain = count - assigned;
    while (remain > 0)
    {
      int bestKey = -1;
      float bestRem = -1f;

      foreach (var kv in remainders)
      {
        if (kv.Value > bestRem)
        {
          bestRem = kv.Value;
          bestKey = kv.Key;
        }
      }

      if (bestKey == -1) break;

      baseCounts[bestKey] += 1;
      remainders[bestKey] = 0f;
      remain--;
    }

    foreach (var kv in baseCounts)
      for (int i = 0; i < kv.Value; i++)
        result.Add(kv.Key);

    while (result.Count < count) result.Add(0);
    if (result.Count > count) result.RemoveRange(count, result.Count - count);

    Shuffle(result);
    return result;
  }

  private void Shuffle(List<int> list)
  {
    for (int i = 0; i < list.Count; i++)
    {
      int j = UnityEngine.Random.Range(i, list.Count);
      (list[i], list[j]) = (list[j], list[i]);
    }
  }

  private void SpawnOuterRingShooterOnly()
  {
    for (int i = 0; i < outerCount; i++)
    {
      float angleDeg = (360f / outerCount) * i;
      Vector3 pos = player.position + AngleToVector(angleDeg) * outerRadius;

      Instantiate(enemyPrefabs[1], pos, Quaternion.identity); // Shooter
    }

    Debug.Log($"[SiegeEvent] OuterRing Shooter Spawned (count={outerCount})");
  }

  private IEnumerator EndRoutine()
  {
    yield return new WaitForSeconds(siegeDuration);
    running = false;

    Debug.Log("[SiegeEvent] Siege End");
    onEnd?.Invoke();
    onEnd = null;
  }

  private IEnumerator EndRoutine(float duration)
  {
    yield return new WaitForSeconds(duration);
    running = false;

    Debug.Log("[SiegeEvent] Siege End");
    onEnd?.Invoke();
    onEnd = null;
  }

  private Vector3 AngleToVector(float angleDeg)
  {
    float rad = angleDeg * Mathf.Deg2Rad;
    return new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f);
  }
}


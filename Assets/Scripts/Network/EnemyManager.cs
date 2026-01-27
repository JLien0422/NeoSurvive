using UnityEngine;
using System.Collections.Generic;
using Protocol = NeoSurvive.Network.Protocol; // Alias to avoid ambiguity

namespace NeoSurvive.Network
{
  /// <summary>
  /// 서버에서 전송받은 적(Enemy) 데이터를 바탕으로 클라이언트에서 동기화된 적 오브젝트를 관리합니다.
  /// </summary>
  public class EnemyManager : MonoBehaviour
  {
    public static EnemyManager Instance { get; private set; }

    [Header("Prefab Settings")]
    [SerializeField] private List<EnemyPrefabMapping> enemyPrefabs; // TypeID별 프리팹 매핑

    [System.Serializable]
    public struct EnemyPrefabMapping
    {
      public uint typeId;
      public GameObject prefab;
    }

    // 서버 ID 기반 적 관리 (ID -> Enemy 객체)
    private Dictionary<uint, EnemyProxy> activeEnemies = new Dictionary<uint, EnemyProxy>();

    // 프리팹 테이블 (조회 최적화)
    private Dictionary<uint, GameObject> prefabTable = new Dictionary<uint, GameObject>();

    private void Awake()
    {
      if (Instance == null) Instance = this;
      else Destroy(gameObject);

      // 매핑 리스트를 딕셔너리로 변환
      foreach (var mapping in enemyPrefabs)
      {
        if (!prefabTable.ContainsKey(mapping.typeId))
          prefabTable.Add(mapping.typeId, mapping.prefab);
      }
    }

    /// <summary>
    /// 서버 월드 스냅샷의 EnemyStates 리스트를 받아 적들을 동기화합니다.
    /// </summary>
    public void SyncEnemies(IEnumerable<Protocol.EnemyState> states)
    {
      HashSet<uint> seenIds = new HashSet<uint>();

      foreach (var state in states)
      {
        seenIds.Add(state.EnemyId);

        if (!activeEnemies.ContainsKey(state.EnemyId))
        {
          // 신규 생성
          SpawnEnemyProxy(state);
        }
        else
        {
          // 위치 및 상태 업데이트
          activeEnemies[state.EnemyId].UpdateState(state);
        }
      }

      // 이번 스냅샷에 없는 아이디는 서버에서 삭제된 것이므로 제거
      List<uint> toRemove = new List<uint>();
      foreach (var id in activeEnemies.Keys)
      {
        if (!seenIds.Contains(id))
          toRemove.Add(id);
      }

      foreach (var id in toRemove)
      {
        RemoveEnemyProxy(id);
      }
    }

    private void SpawnEnemyProxy(Protocol.EnemyState state)
    {
      if (prefabTable.TryGetValue(state.TypeId, out GameObject prefab))
      {
        GameObject obj = Instantiate(prefab, new Vector3(state.PosX, state.PosY, 0), Quaternion.identity);
        obj.name = $"Enemy_Proxy_{state.EnemyId}";

        // AI 로직을 비활성화하고 위치 동기화용 컴포넌트 추가/사용
        EnemyProxy proxy = obj.GetComponent<EnemyProxy>();
        if (proxy == null) proxy = obj.AddComponent<EnemyProxy>();

        // [Coop] 기존 싱글용 AI 컨트롤러가 있다면 비활성화
        EnemyController localAI = obj.GetComponent<EnemyController>();
        if (localAI != null) localAI.enabled = false;

        proxy.Initialize(state.EnemyId);
        activeEnemies.Add(state.EnemyId, proxy);
      }
      else
      {
        Debug.LogWarning($"[EnemyManager] 알 수 없는 적 타입: {state.TypeId}");
      }
    }

    private void RemoveEnemyProxy(uint id)
    {
      if (activeEnemies.TryGetValue(id, out EnemyProxy proxy))
      {
        if (proxy != null && proxy.gameObject != null)
        {
          Destroy(proxy.gameObject);
        }
        activeEnemies.Remove(id);
      }
    }
  }
}

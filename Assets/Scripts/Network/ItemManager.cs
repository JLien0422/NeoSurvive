using UnityEngine;
using System.Collections.Generic;
using Protocol = NeoSurvive.Network.Protocol;

namespace NeoSurvive.Network
{
  /// <summary>
  /// 서버에서 전송받은 아이템 데이터를 바탕으로 클라이언트에서 동기화된 아이템을 관리합니다.
  /// </summary>
  public class ItemManager : MonoBehaviour
  {
    public static ItemManager Instance { get; private set; }

    [Header("Prefab Settings")]
    [SerializeField] private List<ItemPrefabMapping> itemPrefabs;

    [System.Serializable]
    public struct ItemPrefabMapping
    {
      public Protocol.ItemType itemType;
      public uint typeId;
      public GameObject prefab;
    }

    private Dictionary<uint, GameObject> activeItems = new Dictionary<uint, GameObject>();

    private Dictionary<(Protocol.ItemType, uint), GameObject> prefabTable = new Dictionary<(Protocol.ItemType, uint), GameObject>();
    private Dictionary<Protocol.ItemType, GameObject> prefabFallbackTable = new Dictionary<Protocol.ItemType, GameObject>();

    private void Awake()
    {
      if (Instance == null) Instance = this;
      else
      {
        Destroy(gameObject);
        return;
      }

      prefabTable.Clear();
      prefabFallbackTable.Clear();
      foreach (var mapping in itemPrefabs)
      {
        if (mapping.prefab == null) continue;

        var key = (mapping.itemType, mapping.typeId);
        if (!prefabTable.ContainsKey(key))
        {
          prefabTable.Add(key, mapping.prefab);
        }

        if (!prefabFallbackTable.ContainsKey(mapping.itemType))
        {
          prefabFallbackTable.Add(mapping.itemType, mapping.prefab);
        }
      }
    }

    public void SyncItems(IEnumerable<Protocol.ItemState> states)
    {
      if (states == null) return;

      HashSet<uint> seenIds = new HashSet<uint>();

      foreach (var state in states)
      {
        if (state == null) continue;

        seenIds.Add(state.ItemId);

        if (!activeItems.ContainsKey(state.ItemId))
        {
          SpawnItem(state);
        }
        else
        {
          UpdateItem(state);
        }
      }

      List<uint> toRemove = new List<uint>();
      foreach (var id in activeItems.Keys)
      {
        if (!seenIds.Contains(id))
        {
          toRemove.Add(id);
        }
      }

      foreach (var id in toRemove)
      {
        RemoveItem(id);
      }
    }

    public void UpdateItem(Protocol.ItemState state)
    {
      if (state == null) return;

      if (!activeItems.TryGetValue(state.ItemId, out GameObject obj) || obj == null)
      {
        SpawnItem(state);
        return;
      }

      var networkItem = obj.GetComponent<NetworkItem>();
      if (networkItem != null)
      {
        networkItem.Initialize(state);
      }

      var proxy = obj.GetComponent<ItemProxy>();
      if (proxy != null)
      {
        proxy.UpdateState(state);
      }
    }

    public void RemoveItem(uint itemId)
    {
      if (!activeItems.TryGetValue(itemId, out GameObject obj)) return;

      if (obj != null)
      {
        Destroy(obj);
      }
      activeItems.Remove(itemId);
    }

    private void SpawnItem(Protocol.ItemState state)
    {
      GameObject prefab = GetPrefab(state);
      if (prefab == null)
      {
        Debug.LogWarning($"[ItemManager] 알 수 없는 아이템 타입: {state.ItemType}, typeId: {state.TypeId}");
        return;
      }

      Vector3 pos = new Vector3(state.PosX, state.PosY, 0f);
      GameObject obj = Instantiate(prefab, pos, Quaternion.identity);
      obj.name = $"Item_Proxy_{state.ItemId}";

      var networkItem = obj.GetComponent<NetworkItem>();
      if (networkItem == null) networkItem = obj.AddComponent<NetworkItem>();
      networkItem.Initialize(state);

      var proxy = obj.GetComponent<ItemProxy>();
      if (proxy == null) proxy = obj.AddComponent<ItemProxy>();
      proxy.UpdateState(state);

      activeItems[state.ItemId] = obj;
    }

    private GameObject GetPrefab(Protocol.ItemState state)
    {
      var exactKey = (state.ItemType, state.TypeId);
      if (prefabTable.TryGetValue(exactKey, out GameObject exactPrefab))
      {
        return exactPrefab;
      }

      if (prefabFallbackTable.TryGetValue(state.ItemType, out GameObject fallback))
      {
        return fallback;
      }

      return null;
    }
  }
}

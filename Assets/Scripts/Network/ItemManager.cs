using UnityEngine;
using System.Collections.Generic;
using NeoSurvive.Exp;
using Protocol = NeoSurvive.Network.Protocol;

namespace NeoSurvive.Network
{
  /// <summary>
  /// 서버에서 전송받은 아이템 데이터를 바탕으로 클라이언트에서 동기화된 아이템을 관리합니다.
  /// </summary>
  public class ItemManager : MonoBehaviour
  {
    public static ItemManager Instance { get; private set; }

    private static Sprite fallbackSprite;

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
        Debug.LogWarning($"[ItemManager] 프리팹 없음. 폴백 생성: {state.ItemType}, typeId: {state.TypeId}");
        GameObject fallback = CreateFallbackItem(state);
        if (fallback == null)
        {
          Debug.LogWarning($"[ItemManager] 폴백 생성 실패: {state.ItemType}, typeId: {state.TypeId}");
          return;
        }

        var networkItemFallback = fallback.GetComponent<NetworkItem>();
        if (networkItemFallback == null) networkItemFallback = fallback.AddComponent<NetworkItem>();
        networkItemFallback.Initialize(state);

        var proxyFallback = fallback.GetComponent<ItemProxy>();
        if (proxyFallback == null) proxyFallback = fallback.AddComponent<ItemProxy>();
        proxyFallback.UpdateState(state);

        activeItems[state.ItemId] = fallback;
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

    private GameObject CreateFallbackItem(Protocol.ItemState state)
    {
      if (state == null) return null;

      GameObject obj = new GameObject($"Item_Fallback_{state.ItemId}");
      obj.transform.position = new Vector3(state.PosX, state.PosY, 0f);

      SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
      sr.sprite = GetFallbackSprite();
      sr.color = GetFallbackColor(state.ItemType);
      sr.sortingOrder = 5;

      switch (state.ItemType)
      {
        case Protocol.ItemType.ExpOrb:
          AddTriggerCollider(obj, 0.25f);
          var expOrb = obj.AddComponent<ExpOrb>();
          expOrb.SetAmount((int)Mathf.Max(1, state.Quantity));
          break;
        case Protocol.ItemType.NeuralLinkItem:
          AddTriggerCollider(obj, 0.25f);
          obj.AddComponent<DataChip>();
          break;
        case Protocol.ItemType.PsychoInfectionItem:
          AddTriggerCollider(obj, 0.25f);
          obj.AddComponent<PsychoCorruptionItem>();
          break;
        case Protocol.ItemType.Gold:
          AddTriggerCollider(obj, 0.25f);
          obj.AddComponent<GoldPickup>();
          break;
        case Protocol.ItemType.Chest:
          AddTriggerCollider(obj, 0.4f);
          obj.AddComponent<ChestPickup>();
          break;
        case Protocol.ItemType.Projectile:
          Rigidbody2D rb = obj.AddComponent<Rigidbody2D>();
          rb.gravityScale = 0f;
          rb.drag = 0f;
          rb.angularDrag = 0f;
          break;
      }

      float scale = GetFallbackScale(state.ItemType);
      obj.transform.localScale = new Vector3(scale, scale, 1f);

      return obj;
    }

    private void AddTriggerCollider(GameObject obj, float radius)
    {
      CircleCollider2D col = obj.AddComponent<CircleCollider2D>();
      col.isTrigger = true;
      col.radius = radius;
    }

    private Sprite GetFallbackSprite()
    {
      if (fallbackSprite != null) return fallbackSprite;

      Texture2D tex = new Texture2D(1, 1, TextureFormat.ARGB32, false);
      tex.SetPixel(0, 0, Color.white);
      tex.Apply();
      fallbackSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
      return fallbackSprite;
    }

    private Color GetFallbackColor(Protocol.ItemType itemType)
    {
      switch (itemType)
      {
        case Protocol.ItemType.ExpOrb: return new Color(0.3f, 0.9f, 1f, 1f);
        case Protocol.ItemType.Projectile: return new Color(1f, 0.5f, 0.1f, 1f);
        case Protocol.ItemType.NeuralLinkItem: return new Color(0.6f, 0.3f, 1f, 1f);
        case Protocol.ItemType.PsychoInfectionItem: return new Color(0.9f, 0.2f, 0.6f, 1f);
        case Protocol.ItemType.Gold: return new Color(1f, 0.85f, 0.2f, 1f);
        case Protocol.ItemType.Chest: return new Color(0.7f, 0.5f, 0.2f, 1f);
        default: return Color.white;
      }
    }

    private float GetFallbackScale(Protocol.ItemType itemType)
    {
      switch (itemType)
      {
        case Protocol.ItemType.Projectile: return 0.2f;
        case Protocol.ItemType.ExpOrb: return 0.35f;
        case Protocol.ItemType.Chest: return 0.6f;
        default: return 0.4f;
      }
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

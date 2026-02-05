using UnityEngine;
using NeoSurvive.Network.Protocol;

namespace NeoSurvive.Network
{
  /// <summary>
  /// 서버 동기화 아이템의 메타 정보를 보관합니다.
  /// </summary>
  public class NetworkItem : MonoBehaviour
  {
    public uint ItemId { get; private set; }
    public uint TypeId { get; private set; }
    public ItemType ItemType { get; private set; }
    public uint OwnerId { get; private set; }
    public uint Quantity { get; private set; }
    public bool IsLootable { get; private set; }

    public void Initialize(ItemState state)
    {
      if (state == null) return;

      ItemId = state.ItemId;
      TypeId = state.TypeId;
      ItemType = state.ItemType;
      OwnerId = state.OwnerId;
      Quantity = state.Quantity;
      IsLootable = state.IsLootable;
    }
  }
}

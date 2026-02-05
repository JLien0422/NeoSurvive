using System.Collections.Generic;
using UnityEngine;
using NeoSurvive.Buff;
using NeoSurvive.Network.Protocol;

namespace NeoSurvive.Network
{
  /// <summary>
  /// 서버에서 전달된 버프 상태/이벤트를 실제 버프 시스템에 반영합니다.
  /// </summary>
  public static class NetworkBuffSync
  {
    public static void ApplyBuff(GameObject target, uint buffId, float duration, uint stacks)
    {
      if (target == null) return;

      if (NetworkBuffRegistry.Instance == null) return;
      if (!NetworkBuffRegistry.Instance.TryCreateBuff(buffId, duration, stacks, out IBuff buff))
      {
        return;
      }

      BuffHandler handler = GetOrAddHandler(target);
      handler.AddBuffWithId(buffId, buff, stacks, duration, refreshIfExists: true);
    }

    public static void RemoveBuff(GameObject target, uint buffId)
    {
      if (target == null) return;

      BuffHandler handler = target.GetComponent<BuffHandler>();
      if (handler == null) return;

      handler.RemoveBuffById(buffId);
    }

    public static void SyncStatus(GameObject target, IEnumerable<NeoSurvive.Network.Protocol.BuffState> buffs)
    {
      if (target == null) return;
      if (NetworkBuffRegistry.Instance == null) return;

      BuffHandler handler = target.GetComponent<BuffHandler>();
      HashSet<uint> incoming = new HashSet<uint>();

      if (buffs != null)
      {
        foreach (var buffState in buffs)
        {
          if (buffState == null) continue;

          uint buffId = buffState.BuffId;
          incoming.Add(buffId);

          if (NetworkBuffRegistry.Instance.TryCreateBuff(buffId, buffState.RemainingTime, buffState.Stacks, out IBuff buff))
          {
            handler = handler ?? GetOrAddHandler(target);
            handler.SetOrRefreshNetworkBuff(buffId, buff, buffState.RemainingTime, buffState.Stacks);
          }
        }
      }

      if (handler != null)
      {
        HashSet<uint> current = handler.GetActiveNetworkBuffIds();
        foreach (uint id in current)
        {
          if (!incoming.Contains(id))
          {
            handler.RemoveBuffById(id);
          }
        }
      }
    }

    private static BuffHandler GetOrAddHandler(GameObject target)
    {
      BuffHandler handler = target.GetComponent<BuffHandler>();
      if (handler == null) handler = target.AddComponent<BuffHandler>();
      return handler;
    }
  }
}

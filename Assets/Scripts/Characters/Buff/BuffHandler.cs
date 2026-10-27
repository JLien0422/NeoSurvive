using UnityEngine;
using System.Collections.Generic;

namespace NeoSurvive.Buff
{
    /// <summary>
    /// 객체에 적용된 버프들을 관리하는 컴포넌트
    /// </summary>
    public class BuffHandler : MonoBehaviour
    {
        private class BuffEntry
        {
            public IBuff Buff;
            public float RemainingTime;
            public uint? BuffId;
            public uint Stacks;
            public System.Action OnExpired;
        }

        private readonly List<BuffEntry> activeBuffs = new List<BuffEntry>();

        public void AddBuff(IBuff buff)
        {
            AddBuffInternal(buff, null, 1, buff != null ? buff.Duration : 0f);
        }

        public void AddBuffWithId(uint buffId, IBuff buff, uint stacks = 1, float? durationOverride = null, bool refreshIfExists = true, System.Action onExpired = null)
        {
            if (buff == null) return;

            if (refreshIfExists)
            {
                RemoveBuffById(buffId);
            }

            AddBuffInternal(buff, buffId, stacks, durationOverride ?? buff.Duration, onExpired);
        }

        public void SetOrRefreshNetworkBuff(uint buffId, IBuff buff, float remainingTime, uint stacks = 1)
        {
            if (buff == null) return;

            RemoveBuffById(buffId);
            AddBuffInternal(buff, buffId, stacks, remainingTime > 0f ? remainingTime : buff.Duration);
        }

        public void RemoveBuffById(uint buffId)
        {
            for (int i = activeBuffs.Count - 1; i >= 0; i--)
            {
                if (activeBuffs[i].BuffId.HasValue && activeBuffs[i].BuffId.Value == buffId)
                {
                    activeBuffs[i].Buff.Remove(gameObject);
                    activeBuffs.RemoveAt(i);
                }
            }
            RestoreActiveControlFlags();
        }

        public HashSet<uint> GetActiveNetworkBuffIds()
        {
            HashSet<uint> ids = new HashSet<uint>();
            foreach (var entry in activeBuffs)
            {
                if (entry.BuffId.HasValue)
                {
                    ids.Add(entry.BuffId.Value);
                }
            }
            return ids;
        }

        public bool HasAnyActiveBuffs()
        {
            return activeBuffs.Count > 0;
        }

        private void AddBuffInternal(IBuff buff, uint? buffId, uint stacks, float duration, System.Action onExpired = null)
        {
            if (buff == null) return;

            activeBuffs.Add(new BuffEntry
            {
                Buff = buff,
                RemainingTime = duration,
                BuffId = buffId,
                Stacks = stacks,
                OnExpired = onExpired
            });

            buff.Apply(gameObject);
        }

        private void Update()
        {
            for (int i = activeBuffs.Count - 1; i >= 0; i--)
            {
                BuffEntry entry = activeBuffs[i];
                entry.RemainingTime -= Time.deltaTime;
                entry.Buff.Tick(gameObject, Time.deltaTime);

                if (entry.RemainingTime <= 0f)
                {
                    entry.Buff.Remove(gameObject);
                    activeBuffs.RemoveAt(i);
                    RestoreActiveControlFlags();
                    entry.OnExpired?.Invoke();
                }
            }
        }

        private void RestoreActiveControlFlags()
        {
            // CC 하나가 만료되어도 남아 있는 기절/속박/혼란은 유지합니다.
            var flags = GetComponent<StatusFlags>();
            if (flags == null) return;
            foreach (var entry in activeBuffs)
            {
                if (entry.Buff is StunDebuff) { flags.moveBlocked = true; flags.attackBlocked = true; }
                if (entry.Buff is RootDebuff) flags.moveBlocked = true;
                if (entry.Buff is FrenzyDebuff) flags.frenzy = true;
            }
        }
    }
}

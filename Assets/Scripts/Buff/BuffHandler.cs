using UnityEngine;
using System.Collections.Generic;

namespace NeoSurvive.Buff
{
    /// <summary>
    /// 객체에 적용된 버프들을 관리하는 컴포넌트
    /// </summary>
    public class BuffHandler : MonoBehaviour
    {
        private List<IBuff> activeBuffs = new List<IBuff>();
        private List<float> buffTimers = new List<float>();

        public void AddBuff(IBuff buff)
        {
            activeBuffs.Add(buff);
            buffTimers.Add(buff.Duration);
            buff.Apply(gameObject);
        }

        private void Update()
        {
            for (int i = activeBuffs.Count - 1; i >= 0; i--)
            {
                buffTimers[i] -= Time.deltaTime;
                activeBuffs[i].Tick(gameObject, Time.deltaTime);

                if (buffTimers[i] <= 0)
                {
                    activeBuffs[i].Remove(gameObject);
                    activeBuffs.RemoveAt(i);
                    buffTimers.RemoveAt(i);
                }
            }
        }
    }
}

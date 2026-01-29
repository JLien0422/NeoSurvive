using UnityEngine;

namespace NeoSurvive.Buff
{
    /// <summary>
    /// 어디서든 한 줄로 버프/디버프를 적용하기 위한 유틸.
    /// BuffHandler와 StatusFlags가 없으면 자동으로 붙인다.
    /// </summary>
    public static class BuffUtil
    {
        public static void Apply(GameObject target, IBuff buff)
        {
            if (target == null || buff == null) return;

            var handler = target.GetComponent<BuffHandler>();
            if (handler == null) handler = target.AddComponent<BuffHandler>();

            // StatusFlags는 버프가 Apply에서 필요할 때 붙이도록 해도 되지만,
            // 여기서 미리 붙여두면 안전함.
            if (target.GetComponent<StatusFlags>() == null)
                target.AddComponent<StatusFlags>();

            handler.AddBuff(buff);
        }
    }
}

using UnityEngine;

namespace NeoSurvive.Weapon
{
    /// <summary>
    /// 모든 패시브 아이템의 기본 클래스
    /// </summary>
    public abstract class PassiveItemBase : MonoBehaviour
    {
        public string itemName;
        public int level = 1;

        public abstract void ApplyPassive(GameObject player);
        public abstract void RemovePassive(GameObject player);
    }
}

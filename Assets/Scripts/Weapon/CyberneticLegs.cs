using UnityEngine;
using NeoSurvive.Player;

namespace NeoSurvive.Weapon
{
    /// <summary>
    /// 이동 속도를 증가시키는 패시브 아이템 (사이버네틱 다리)
    /// </summary>
    public class CyberneticLegs : PassiveItemBase
    {
        public float speedMultiplier = 1.2f;

        public override void ApplyPassive(GameObject player)
        {
            PlayerController controller = player.GetComponent<PlayerController>();
            if (controller != null)
            {
                controller.moveSpeed *= speedMultiplier;
                Debug.Log($"{itemName} 적용: 이동 속도 {speedMultiplier}배 증가");
            }
        }

        public override void RemovePassive(GameObject player)
        {
            PlayerController controller = player.GetComponent<PlayerController>();
            if (controller != null)
            {
                controller.moveSpeed /= speedMultiplier;
            }
        }
    }
}

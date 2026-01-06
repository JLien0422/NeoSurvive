using UnityEngine;
using System.Collections.Generic;

namespace NeoSurvive.Weapon
{
    /// <summary>
    /// 플레이어가 보유한 모든 무기와 패시브 아이템을 관리하는 매니저
    /// </summary>
    public class WeaponManager : MonoBehaviour
    {
        public List<WeaponBase> activeWeapons = new List<WeaponBase>();
        public List<PassiveItemBase> passiveItems = new List<PassiveItemBase>();

        /// <summary>
        /// 새로운 무기 추가 또는 레벨업
        /// </summary>
        public WeaponBase AddWeapon(GameObject weaponPrefab)
        {
            GameObject weaponObj = Instantiate(weaponPrefab, transform);
            WeaponBase weapon = weaponObj.GetComponent<WeaponBase>();
            if (weapon != null)
            {
                activeWeapons.Add(weapon);
            }
            return weapon;
        }

        /// <summary>
        /// 새로운 패시브 아이템 추가
        /// </summary>
        public void AddPassiveItem(GameObject passivePrefab)
        {
            GameObject passiveObj = Instantiate(passivePrefab, transform);
            PassiveItemBase passive = passiveObj.GetComponent<PassiveItemBase>();
            if (passive != null)
            {
                passiveItems.Add(passive);
                passive.ApplyPassive(gameObject);
            }
        }
    }
}

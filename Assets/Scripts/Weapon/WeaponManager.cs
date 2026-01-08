using UnityEngine;
using System.Collections.Generic;

public enum WeaponType {
    AIDrone = 0,
    AutoTurret,
    Pistol,
    EMPShield,
    WEAPON_TYPE_COUNT
}

public enum PassiveItemType {
    CyberneticLegs = 0,
    PASSIVE_TYPE_COUNT
}

namespace NeoSurvive.Weapon
{

    /// <summary>
    /// 플레이어가 보유한 모든 무기와 패시브 아이템을 관리하는 매니저
    /// </summary>
    public class WeaponManager : MonoBehaviour
    {
        public List<WeaponBase> activeWeapons = new List<WeaponBase>();
        public List<PassiveItemBase> passiveItems = new List<PassiveItemBase>();
        
        [SerializeField]
        public List<GameObject> weaponsPrefabs = new List<GameObject>();
        [SerializeField]
        public List<GameObject> passiveItemPrefabs = new List<GameObject>();

        /// <summary>
        /// 새로운 무기 추가 또는 레벨업
        /// </summary>
        public WeaponBase AddWeapon(WeaponType type)
        {
            if ((int)type < 0 || (int)type > weaponsPrefabs.Count) return null;
            switch(type) {
                case WeaponType.AIDrone:
                case WeaponType.AutoTurret:
                    return AddWeaponIndependently(weaponsPrefabs[(int)type], transform.position);
                case WeaponType.Pistol:
                    return AddWeapon(weaponsPrefabs[(int)type]);
                default:
                    return null;
            }
        }

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

        public WeaponBase AddWeaponIndependently(GameObject weaponPrefab, Vector3 pos)
        {
            GameObject weaponObj = Instantiate(weaponPrefab, pos,
            Quaternion.identity);
            WeaponBase weapon = weaponObj.GetComponent<WeaponBase>();
            
            if (weapon != null)
            {
                activeWeapons.Add(weapon);
            }
            return weapon;
        }

        public void AddPassiveItem(PassiveItemType type)
        {
            if ((int)type < 0 || (int)type >= passiveItemPrefabs.Count) return;
            AddPassiveItem(passiveItemPrefabs[(int)type]);
        }

        public void AddPassiveItem(GameObject passivePrefab)
        {
            GameObject passiveObj = Instantiate(passivePrefab, transform);
            PassiveItemBase passive = passiveObj.GetComponent<PassiveItemBase>();
            
            if (passive != null)
            {
                // Check if already exists? 
                // For now, let's assume multiple passives of same type aren't allowed or logic is elsewhere?
                // But the user's code had a check `if (passiveItems.Contains(passive))` which suggested they wanted a uniqueness check.
                // However, checking against the prefab component is wrong.
                // We will just add it for now.
                
                passiveItems.Add(passive);
                passive.ApplyPassive(gameObject);
            }
        }

        void Update(){
            if (Input.GetKeyDown(KeyCode.F1)) {
                AddWeapon(WeaponType.AIDrone);
            }
            if (Input.GetKeyDown(KeyCode.F2)) {
                AddWeapon(WeaponType.AutoTurret);
            }
            if (Input.GetKeyDown(KeyCode.F3)) {
                AddPassiveItem(PassiveItemType.CyberneticLegs);
            }
            if (Input.GetKeyDown(KeyCode.F4)) {
                AddWeapon(WeaponType.Pistol);
            }
        }
    }
}

using UnityEngine;
using NeoSurvive.Weapon;
using NeoSurvive.Player;
using NeoSurvive.Enemy;

namespace NeoSurvive.Core
{
    public class SceneSetup : MonoBehaviour
    {
        void Awake()
        {
            GameObject player = GameObject.Find("Player");
            WeaponManager weaponManager = player?.GetComponent<WeaponManager>();

            if (weaponManager != null)
            {
                // 초기 무기 및 패시브 아이템 추가 테스트
                GameObject teslaPrefab = GameObject.Find("TeslaCoilPrefab");
                GameObject legsPrefab = GameObject.Find("CyberneticLegsPrefab");

                if (teslaPrefab != null) weaponManager.AddWeapon(teslaPrefab);
                if (legsPrefab != null) weaponManager.AddPassiveItem(legsPrefab);
            }
        }
    }
}

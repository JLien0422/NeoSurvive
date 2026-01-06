using UnityEngine;
using NeoSurvive.Weapon;

namespace NeoSurvive.Core
{
    public class WeaponTestSetup : MonoBehaviour
    {
        public GameObject player;
        public GameObject projectilePrefab; // 추가

        [Header("Ranged")]
        public GameObject pistolPrefab;
        public GameObject plasmaRiflePrefab;
        public GameObject empBombPrefab;
        public GameObject plasmaPhotonPrefab;

        [Header("Melee")]
        public GameObject energyShieldPrefab;
        public GameObject surgeBladePrefab;
        public GameObject laserSwordPrefab;

        [Header("AI")]
        public GameObject aiDronePrefab;
        public GameObject autoTurretPrefab;

        void Start()
        {
            if (player == null) player = GameObject.Find("Player");
            if (player == null) return;

            WeaponManager wm = player.GetComponent<WeaponManager>();
            if (wm == null) return;

            // 프리팹 자동 찾기 (씬에 배치된 프리팹 오브젝트들 활용)
            if (projectilePrefab == null) projectilePrefab = GameObject.Find("ProjectileBasePrefab");
            if (pistolPrefab == null) pistolPrefab = GameObject.Find("PistolPrefab");
            if (plasmaRiflePrefab == null) plasmaRiflePrefab = GameObject.Find("PlasmaRiflePrefab");
            if (empBombPrefab == null) empBombPrefab = GameObject.Find("EMPBombPrefab");
            if (aiDronePrefab == null) aiDronePrefab = GameObject.Find("AIDronePrefab");
            if (autoTurretPrefab == null) autoTurretPrefab = GameObject.Find("AutoTurretPrefab");
            if (plasmaPhotonPrefab == null) plasmaPhotonPrefab = GameObject.Find("PlasmaPhotonPrefab");
            if (energyShieldPrefab == null) energyShieldPrefab = GameObject.Find("EnergyShieldPrefab");
            if (surgeBladePrefab == null) surgeBladePrefab = GameObject.Find("SurgeBladePrefab");
            if (laserSwordPrefab == null) laserSwordPrefab = GameObject.Find("LaserSwordPrefab");

            // 테스트를 위해 모든 무기 추가 및 프리팹 할당
            AddWeaponWithPrefab(wm, pistolPrefab, projectilePrefab);
            AddWeaponWithPrefab(wm, plasmaRiflePrefab, projectilePrefab);
            AddWeaponWithPrefab(wm, empBombPrefab, projectilePrefab);
            AddWeaponWithPrefab(wm, aiDronePrefab, projectilePrefab);
            AddWeaponWithPrefab(wm, autoTurretPrefab, projectilePrefab);

            if (plasmaPhotonPrefab) wm.AddWeapon(plasmaPhotonPrefab);
            if (energyShieldPrefab) wm.AddWeapon(energyShieldPrefab);
            if (surgeBladePrefab) wm.AddWeapon(surgeBladePrefab);
            if (laserSwordPrefab) wm.AddWeapon(laserSwordPrefab);
        }

        private void AddWeaponWithPrefab(WeaponManager wm, GameObject weaponPrefab, GameObject projPrefab)
        {
            if (weaponPrefab == null || projPrefab == null) return;

            WeaponBase instance = wm.AddWeapon(weaponPrefab);
            if (instance == null) return;

            // 리플렉션 대신 타입 체크로 할당
            if (instance is Pistol pistol) pistol.projectilePrefab = projPrefab;
            else if (instance is PlasmaRifle rifle) rifle.projectilePrefab = projPrefab;
            else if (instance is EMPBomb bomb) bomb.bombPrefab = projPrefab;
            else if (instance is AIDrone drone) drone.projectilePrefab = projPrefab;
            else if (instance is AutoTurret turret) turret.projectilePrefab = projPrefab;
        }
    }
}

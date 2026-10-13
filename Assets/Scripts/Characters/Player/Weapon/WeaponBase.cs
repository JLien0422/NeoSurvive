using System.Collections.Generic;
using UnityEngine;

namespace NeoSurvive.Weapon
{

    [CreateAssetMenu(fileName = "New Weapon", menuName = "Weapon/WeaponBase")]
    /// <summary>
    /// 모든 무기의 기본 클래스
    /// </summary>
    /// xabuna 26-09-15
    public class WeaponBase : ScriptableObject
    {
        [Header("Weapon ID")]
        [Min(1)]
        public int weaponId;

        [Header("Weapon Settings")]
        public string weaponName;

        [Header("Weapon Icon")]
        public Sprite weaponIcon;

        public string description;
        public GameObject weaponPrefab;
        public int level = 1;
        public bool isIndependent = false; // true면 플레이어 자식이 아닌 독립적인 오브젝트로 생성

        // CSV 및 무기 스크립트에서 사용하는 선택적 식별자. 예: linkpistol
        [Header("Run Trait")]
        [Tooltip("CSV 및 무기 스크립트가 사용하는 식별자. 예: linkpistol")]
        public string runtimeWeaponId;

        [Header("Synergy")]
        public List<SynergyTag> synergyTags = new List<SynergyTag>();
    }
}

using UnityEngine;

namespace NeoSurvive.Weapon
{

    [CreateAssetMenu(fileName = "New Weapon", menuName = "Weapon/WeaponBase")]
    /// <summary>
    /// 모든 무기의 기본 클래스
    /// </summary>
    public class WeaponBase : ScriptableObject
    {
        [Header("Weapon Settings")]
        public string weaponName;

        [Header("Weapon Icon")]
        public Sprite weaponIcon;

        public string description;
        public GameObject weaponPrefab;
        public int level = 1;
        public bool isIndependent = false; // true면 플레이어 자식이 아닌 독립적인 오브젝트로 생성
    }
}

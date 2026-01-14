using UnityEngine;

namespace NeoSurvive.Characters
{
    /// <summary>
    /// 캐릭터의 기본 스탯과 정보를 저장하는 ScriptableObject입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "CharacterData", menuName = "NeoSurvive/Character Data", order = 1)]
    public class CharacterData : ScriptableObject
    {
        [Header("캐릭터 정보")]
        public CharacterType characterType;
        public string characterName;
        public string description;
        public Sprite characterIcon;
        public Sprite characterPortrait;

        [Header("기본 스탯")]
        [Tooltip("캐릭터의 기본 체력")]
        public float baseHealth = 100f;

        [Tooltip("캐릭터의 기본 공격력")]
        public float baseAttackDamage = 10f;

        [Tooltip("캐릭터의 기본 공격 범위")]
        public float baseAttackRange = 3f;

        [Tooltip("캐릭터의 기본 공격 속도 (1.0 = 100%)")]
        public float baseAttackSpeed = 1.0f;

        [Tooltip("캐릭터의 이동 속도")]
        public float baseMoveSpeed = 5f;

        [Header("특수 능력")]
        [Tooltip("캐릭터의 고유 능력 설명")]
        [TextArea(3, 5)]
        public string specialAbilityDescription;
    }
}

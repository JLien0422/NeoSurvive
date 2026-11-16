using NeoSurvive.Characters;
using NeoSurvive.Weapon;
using UnityEngine;

[CreateAssetMenu(fileName = "RT_", menuName = "NeoSurvive/Traits/Run Trait Definition")]
public class RunTraitDefinition : ScriptableObject
{
    public string traitId;
    public string displayName;
    public Sprite icon;
    [TextArea] public string description;
    public RunTraitTier tier;
    public CharacterType targetCharacter;
    public bool isCommonTrait;
    [Header("Tag Trait")]
    public bool requiresWeaponTag;
    public SynergyTag requiredTag;
    [Header("Stack")]
    [Min(1)] public int maxStacks = 1;
    [Header("Effect")]
    [Tooltip("효과 설정 SO. 획득 레벨과 실행 상태는 RunTraitManager에서 관리합니다.")]
    public RunTraitEffect[] effect = new RunTraitEffect[0];
}

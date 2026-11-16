using UnityEngine;

[CreateAssetMenu(fileName = "DamageBonusEffect", menuName = "NeoSurvive/Traits/Effects/Damage Bonus")]
public class DamageBonusEffect : RunTraitEffect
{
    [SerializeField, Range(0f, 10f)] private float bonusPerStack = 0.1f;
    public float BonusPerStack => bonusPerStack;

    public override float GetDamageBonus(int stacks)
    {
        return Mathf.Max(0f, bonusPerStack) * Mathf.Max(0, stacks);
    }
}

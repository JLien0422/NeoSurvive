using System;
using UnityEngine;

[CreateAssetMenu(fileName = "WeaponStatEffect", menuName = "NeoSurvive/Traits/Effects/Weapon Stat Modifier")]
public class WeaponStatModifierEffect : RunTraitEffect
{
    [Serializable]
    public struct Modifier
    {
        public RunTraitStat stat;
        [Tooltip("장당 가산 비율. +10%는 0.1, 주기 -10%는 -0.1입니다.")]
        public float percentPerStack;
    }

    [SerializeField] private Modifier[] modifiers = new Modifier[0];

    public override float GetStatBonus(RunTraitStat stat, int stacks)
    {
        float bonus = 0f;
        foreach (Modifier modifier in modifiers)
            if (modifier.stat == stat)
                bonus += modifier.percentPerStack * Mathf.Max(0, stacks);
        return bonus;
    }
}

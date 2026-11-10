using System;

/// <summary>소진된 등급의 확률을 낮은 등급에 2/3, 높은 등급에 1/3 배분합니다.</summary>
public readonly struct RunTraitTierChances
{
    public readonly float Silver;
    public readonly float Gold;
    public readonly float Platinum;
    public float Total => Silver + Gold + Platinum;

    private RunTraitTierChances(float silver, float gold, float platinum)
    {
        Silver = silver;
        Gold = gold;
        Platinum = platinum;
    }

    public static RunTraitTierChances Calculate(float silver, float gold, float platinum,
        bool hasSilver, bool hasGold, bool hasPlatinum)
    {
        silver = Math.Max(0f, silver);
        gold = Math.Max(0f, gold);
        platinum = Math.Max(0f, platinum);
        float total = silver + gold + platinum;
        int availableCount = (hasSilver ? 1 : 0) + (hasGold ? 1 : 0) + (hasPlatinum ? 1 : 0);
        if (availableCount == 0)
            return default;
        if (availableCount == 1)
            return new RunTraitTierChances(hasSilver ? total : 0f,
                hasGold ? total : 0f, hasPlatinum ? total : 0f);
        if (availableCount == 3)
            return new RunTraitTierChances(silver, gold, platinum);
        if (!hasSilver)
            return new RunTraitTierChances(0f, gold + silver * (2f / 3f), platinum + silver / 3f);
        if (!hasGold)
            return new RunTraitTierChances(silver + gold * (2f / 3f), 0f, platinum + gold / 3f);
        return new RunTraitTierChances(silver + platinum * (2f / 3f), gold + platinum / 3f, 0f);
    }
}

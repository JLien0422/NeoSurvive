using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using NeoSurvive.Weapon;
using NeoSurvive.Characters;

/// <summary>
/// Xabuna 26-09-15
/// 
/// 런 특성 관리
/// 런 시작 시 InitializeRun() 호출
/// 런 중 자판기에서 GenerateOffers() 호출
/// 플레이어가 선택 시 TryAcquire() 호출
/// 런 중 무기 피해 계산 시 GetTagDamageMultiplier() 호출
/// </summary>

[CreateAssetMenu(
    fileName = "RT_",
    menuName = "NeoSurvive/Traits/Run Trait Definition")]
public class RunTraitDefinition : ScriptableObject
{
    public string traitId;
    public string displayName;

    [TextArea]
    public string description;

    public RunTraitTier tier;
    public CharacterType targetCharacter; // Hacker / Cyborg
    public bool isCommonTrait;

    [Header("Tag Trait")]
    public bool requiresWeaponTag;
    public WeaponSynergyTag requiredTag;

    [Header("Stack")]
    [Min(1)] public int maxStacks = 1;

    [Header("Simple Effect")]
    [Range(-1f, 10f)]
    public float damagePercentPerStack = 0.05f;
}

[CreateAssetMenu(
    fileName = "RunTraitCatalog",
    menuName = "NeoSurvive/Traits/Run Trait Catalog")]
public class RunTraitCatalog : ScriptableObject
{
    public List<RunTraitDefinition> traits = new();
}

public class RunTraitManager : MonoBehaviour
{
    public static RunTraitManager Instance { get; private set; }

    [SerializeField] private RunTraitCatalog catalog;
    [SerializeField] private CharacterType characterType;

    [Header("Tier chance")]
    [Range(0f, 100f)] [SerializeField] private float silverChance = 45f;
    [Range(0f, 100f)] [SerializeField] private float goldChance = 30f;
    [Range(0f, 100f)] [SerializeField] private float platinumChance = 25f;

    // 이번 런에서 남은 카드 수
    private readonly Dictionary<string, int> remainingCopies = new();

    // 이번 런에서 얻은 특성 레벨
    private readonly Dictionary<string, int> traitLevels = new();

    public event Action<RunTraitDefinition, int> OnTraitAcquired;

    private void Awake()
    {
        Instance = this;
    }

    public void InitializeRun(CharacterType selectedCharacter)
    {
        characterType = selectedCharacter;
        remainingCopies.Clear();
        traitLevels.Clear();

        foreach (RunTraitDefinition trait in catalog.traits)
        {
            if (trait == null || string.IsNullOrEmpty(trait.traitId))
                continue;

            if (!trait.isCommonTrait && trait.targetCharacter != characterType)
                continue;

            // 플래티넘은 무조건 한 장
            int copies = trait.tier == RunTraitTier.Platinum
                ? 1
                : Mathf.Max(1, trait.maxStacks);

            remainingCopies[trait.traitId] = copies;
        }
    }

    // 자판기: 3개 선택지 생성
    public List<RunTraitDefinition> GenerateOffers(int count = 3)
    {
        List<RunTraitDefinition> offers = new();
        HashSet<string> offeredIds = new();

        for (int i = 0; i < count; i++)
        {
            RunTraitTier tier = RollAvailableTier();
            RunTraitDefinition trait = RollTraitInTier(tier, offeredIds);

            if (trait == null)
                break;

            offers.Add(trait);
            offeredIds.Add(trait.traitId);
        }

        return offers;
    }

    public bool TryAcquire(RunTraitDefinition trait)
    {
        if (trait == null || !remainingCopies.TryGetValue(trait.traitId, out int copies))
            return false;

        if (copies <= 0)
            return false;

        int currentLevel = GetTraitLevel(trait.traitId);

        if (currentLevel >= trait.maxStacks)
            return false;

        // 플래티넘 중복 방지
        if (trait.tier == RunTraitTier.Platinum && currentLevel > 0)
            return false;

        traitLevels[trait.traitId] = currentLevel + 1;
        remainingCopies[trait.traitId]--;

        OnTraitAcquired?.Invoke(trait, traitLevels[trait.traitId]);
        return true;
    }

    public int GetTraitLevel(string traitId)
    {
        return traitLevels.TryGetValue(traitId, out int level) ? level : 0;
    }

    // 특정 태그 무기의 피해 배율
    public float GetTagDamageMultiplier(WeaponSynergyTag tag)
    {
        float bonus = 0f;

        foreach (RunTraitDefinition trait in catalog.traits)
        {
            if (trait == null || !trait.requiresWeaponTag)
                continue;

            if (trait.requiredTag != tag)
                continue;

            bonus += GetTraitLevel(trait.traitId) * trait.damagePercentPerStack;
        }

        return 1f + bonus;
    }

    private RunTraitTier RollAvailableTier()
    {
        List<RunTraitTier> availableTiers = new();

        foreach (RunTraitTier tier in Enum.GetValues(typeof(RunTraitTier)))
        {
            bool hasTrait = catalog.traits.Any(trait =>
                trait != null &&
                trait.tier == tier &&
                remainingCopies.TryGetValue(trait.traitId, out int copies) &&
                copies > 0);

            if (hasTrait)
                availableTiers.Add(tier);
        }

        if (availableTiers.Count == 1)
            return availableTiers[0];

        float silver = availableTiers.Contains(RunTraitTier.Silver) ? silverChance : 0f;
        float gold = availableTiers.Contains(RunTraitTier.Gold) ? goldChance : 0f;
        float platinum = availableTiers.Contains(RunTraitTier.Platinum) ? platinumChance : 0f;

        // 비어 있는 등급의 확률을 현재 남은 등급에 단순 재분배
        float total = silver + gold + platinum;
        float roll = UnityEngine.Random.Range(0f, total);

        if (roll < silver)
            return RunTraitTier.Silver;

        if (roll < silver + gold)
            return RunTraitTier.Gold;

        return RunTraitTier.Platinum;
    }

    private RunTraitDefinition RollTraitInTier(
        RunTraitTier tier,
        HashSet<string> excludedIds)
    {
        List<RunTraitDefinition> candidates = catalog.traits
            .Where(trait =>
                trait != null &&
                trait.tier == tier &&
                !excludedIds.Contains(trait.traitId) &&
                remainingCopies.TryGetValue(trait.traitId, out int copies) &&
                copies > 0)
            .ToList();

        if (candidates.Count == 0)
            return null;

        // 남은 중첩 수 자체가 가중치
        int totalWeight = candidates.Sum(
            trait => remainingCopies[trait.traitId]);

        int roll = UnityEngine.Random.Range(0, totalWeight);

        foreach (RunTraitDefinition trait in candidates)
        {
            roll -= remainingCopies[trait.traitId];

            if (roll < 0)
                return trait;
        }

        return candidates[0];
    }
}
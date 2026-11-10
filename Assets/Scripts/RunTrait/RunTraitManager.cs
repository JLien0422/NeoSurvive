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
    private readonly List<RunTraitDefinition> acquiredTraits = new();

    public event Action<RunTraitDefinition, int> OnTraitAcquired;
    public event Action OnTraitsChanged;
    public bool IsInitialized { get; private set; }
    public bool HasCatalog => catalog != null;
    public int RunVersion { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>같은 런에서 자판기를 다시 열어도 획득 상태를 초기화하지 않습니다.</summary>
    public bool EnsureInitialized(CharacterType selectedCharacter, RunTraitCatalog fallbackCatalog = null)
    {
        if (IsInitialized)
            return true;
        if (catalog == null)
            catalog = fallbackCatalog;
        if (catalog == null || catalog.traits == null)
            return false;
        InitializeRun(selectedCharacter);
        return IsInitialized;
    }

    public void InitializeRun(CharacterType selectedCharacter)
    {
        RunVersion++;
        characterType = selectedCharacter;
        remainingCopies.Clear();
        traitLevels.Clear();
        acquiredTraits.Clear();
        IsInitialized = false;
        OnTraitsChanged?.Invoke();
        if (catalog == null || catalog.traits == null)
            return;

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
        IsInitialized = true;
    }

    // 자판기: 3개 선택지 생성
    public List<RunTraitDefinition> GenerateOffers(int count = 3)
    {
        List<RunTraitDefinition> offers = new();
        HashSet<string> offeredIds = new();
        if (!IsInitialized)
            return offers;

        for (int i = 0; i < count; i++)
        {
            if (!TryRollAvailableTier(offeredIds, out RunTraitTier tier))
                break;
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
        if (!IsInitialized || trait == null || string.IsNullOrEmpty(trait.traitId)
            || !catalog.traits.Contains(trait)
            || !remainingCopies.TryGetValue(trait.traitId, out int copies))
            return false;

        if (copies <= 0)
            return false;

        int currentLevel = GetTraitLevel(trait.traitId);

        if (currentLevel >= Mathf.Max(1, trait.maxStacks))
            return false;

        // 플래티넘 중복 방지
        if (trait.tier == RunTraitTier.Platinum && currentLevel > 0)
            return false;

        traitLevels[trait.traitId] = currentLevel + 1;
        remainingCopies[trait.traitId]--;
        if (currentLevel == 0)
            acquiredTraits.Add(trait);

        OnTraitAcquired?.Invoke(trait, traitLevels[trait.traitId]);
        OnTraitsChanged?.Invoke();
        return true;
    }

    /// <summary>26-10-22 Xabuna: 한 중첩을 회수하고 같은 런의 자판기 후보 수량도 복구합니다.</summary>
    public bool TryRemove(RunTraitDefinition trait)
    {
        if (!IsInitialized || trait == null || string.IsNullOrEmpty(trait.traitId)
            || catalog == null || !catalog.traits.Contains(trait)
            || !remainingCopies.ContainsKey(trait.traitId)) return false;
        int level = GetTraitLevel(trait.traitId);
        if (level <= 0) return false;
        if (level == 1)
        {
            traitLevels.Remove(trait.traitId);
            acquiredTraits.Remove(trait);
        }
        else traitLevels[trait.traitId] = level - 1;
        remainingCopies[trait.traitId]++;
        OnTraitsChanged?.Invoke();
        return true;
    }

    /// <summary>특성만 초기화합니다. 플레이어/무기/자판기 사용 여부는 변경하지 않습니다.</summary>
    public int ResetAcquiredTraits()
    {
        if (!IsInitialized) return 0;
        int removed = traitLevels.Values.Sum();
        if (removed > 0) InitializeRun(characterType);
        return removed;
    }

    public bool HasEffect(RunTraitEffect effect)
    {
        if (!IsInitialized || effect == null) return false;
        return acquiredTraits.Any(trait => trait != null && GetTraitLevel(trait.traitId) > 0
            && trait.effect != null && Array.IndexOf(trait.effect, effect) >= 0);
    }

    public int GetTraitLevel(string traitId)
    {
        return traitLevels.TryGetValue(traitId, out int level) ? level : 0;
    }

    // 특정 태그 무기의 피해 배율
    public float GetTagDamageMultiplier(SynergyTag tag)
    {
        float bonus = 0f;
        if (!IsInitialized)
            return 1f;

        foreach (RunTraitDefinition trait in acquiredTraits)
        {
            if (trait == null || trait.effect == null || !trait.requiresWeaponTag)
                continue;

            if (trait.requiredTag != tag)
                continue;

            foreach (RunTraitEffect effect in trait.effect)
                if (effect != null)
                    bonus += effect.GetDamageBonus(GetTraitLevel(trait.traitId));
        }

        return 1f + bonus;
    }

    /// <summary>대상 태그가 일치하는 획득 효과를 각각 한 번 합산합니다.</summary>
    public float GetWeaponDamageMultiplier(WeaponBase weapon)
    {
        if (!IsInitialized || weapon == null)
            return 1f;

        float bonus = 0f;
        foreach (RunTraitDefinition trait in acquiredTraits)
        {
            if (trait == null || trait.effect == null)
                continue;
            if (trait.requiresWeaponTag && (weapon.synergyTags == null
                || !weapon.synergyTags.Contains(trait.requiredTag)))
                continue;
            foreach (RunTraitEffect effect in trait.effect)
                if (effect != null)
                    bonus += effect.GetDamageBonus(GetTraitLevel(trait.traitId));
        }
        return Mathf.Max(0f, 1f + bonus);
    }

    public float GetWeaponStatMultiplier(WeaponBase weapon, RunTraitStat stat)
    {
        bool isInterval = stat == RunTraitStat.AttackInterval
            || stat == RunTraitStat.DeploymentCooldown || stat == RunTraitStat.HitInterval;
        return Mathf.Max(isInterval ? 0.1f : 0f, 1f + GetWeaponStatBonus(weapon, stat));
    }

    public float GetWeaponStatBonus(WeaponBase weapon, RunTraitStat stat)
    {
        if (!IsInitialized || weapon == null)
            return 0f;
        float bonus = 0f;
        foreach (RunTraitDefinition trait in acquiredTraits)
        {
            if (trait == null || trait.effect == null)
                continue;
            if (trait.requiresWeaponTag && (weapon.synergyTags == null
                || !weapon.synergyTags.Contains(trait.requiredTag)))
                continue;
            foreach (RunTraitEffect effect in trait.effect)
                if (effect != null)
                    bonus += effect.GetStatBonus(stat, GetTraitLevel(trait.traitId));
        }
        return bonus;
    }

    public RunTraitHitSettings GetHitSettings(WeaponBase weapon)
    {
        RunTraitHitSettings settings = default;
        if (!IsInitialized || weapon == null) return settings;
        foreach (RunTraitDefinition trait in acquiredTraits)
        {
            if (trait == null || trait.effect == null) continue;
            if (trait.requiresWeaponTag && (weapon.synergyTags == null
                || !weapon.synergyTags.Contains(trait.requiredTag))) continue;
            foreach (RunTraitEffect effect in trait.effect)
                if (effect != null)
                    effect.ConfigureHit(ref settings, GetTraitLevel(trait.traitId));
        }
        return settings;
    }

    public SpecialTraitEffect GetSpecialEffect(WeaponBase weapon, RunTraitSpecialKind kind)
    {
        if (!IsInitialized || weapon == null) return null;
        foreach (var trait in acquiredTraits)
        {
            if (trait == null || trait.effect == null || GetTraitLevel(trait.traitId) <= 0) continue;
            if (trait.requiresWeaponTag && (weapon.synergyTags == null || !weapon.synergyTags.Contains(trait.requiredTag))) continue;
            foreach (var effect in trait.effect)
                if (effect is SpecialTraitEffect special && special.kind == kind) return special;
        }
        return null;
    }

    private bool TryRollAvailableTier(HashSet<string> excludedIds, out RunTraitTier selectedTier)
    {
        List<RunTraitTier> availableTiers = new();

        foreach (RunTraitTier tier in Enum.GetValues(typeof(RunTraitTier)))
        {
            bool hasTrait = catalog.traits.Any(trait =>
                trait != null &&
                !string.IsNullOrEmpty(trait.traitId) &&
                trait.tier == tier &&
                !excludedIds.Contains(trait.traitId) &&
                remainingCopies.TryGetValue(trait.traitId, out int copies) &&
                copies > 0);

            if (hasTrait)
                availableTiers.Add(tier);
        }

        selectedTier = default;
        if (availableTiers.Count == 0)
            return false;
        if (availableTiers.Count == 1)
        {
            selectedTier = availableTiers[0];
            return true;
        }

        RunTraitTierChances chances = RunTraitTierChances.Calculate(
            silverChance, goldChance, platinumChance,
            availableTiers.Contains(RunTraitTier.Silver),
            availableTiers.Contains(RunTraitTier.Gold),
            availableTiers.Contains(RunTraitTier.Platinum));
        float total = chances.Total;
        if (total <= 0f)
        {
            selectedTier = availableTiers[UnityEngine.Random.Range(0, availableTiers.Count)];
            return true;
        }
        float roll = UnityEngine.Random.Range(0f, total);

        selectedTier = roll < chances.Silver ? RunTraitTier.Silver
            : roll < chances.Silver + chances.Gold ? RunTraitTier.Gold
            : availableTiers[availableTiers.Count - 1];
        return true;
    }

    private RunTraitDefinition RollTraitInTier(
        RunTraitTier tier,
        HashSet<string> excludedIds)
    {
        List<RunTraitDefinition> candidates = catalog.traits
            .Where(trait =>
                trait != null &&
                !string.IsNullOrEmpty(trait.traitId) &&
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

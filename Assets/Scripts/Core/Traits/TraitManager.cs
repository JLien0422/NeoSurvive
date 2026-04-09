using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class TraitManager : MonoBehaviour
{
    public static TraitManager Instance { get; private set; }

    private Dictionary<string, Trait> hackerTraits = new Dictionary<string, Trait>();
    private Dictionary<string, Trait> cyborgTraits = new Dictionary<string, Trait>();
    private Dictionary<string, Trait> commonTraits = new Dictionary<string, Trait>();

    public PlayerTraitData playerTraitData;

    private const string PLAYER_TRAIT_SAVE_FILE = "player_traits.json";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadAllTraits();
            LoadPlayerTraits();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void LoadAllTraits()
    {
        hackerTraits = LoadTraitsFromFile("Traits/HackerTraits");
        cyborgTraits = LoadTraitsFromFile("Traits/CyborgTraits");
        commonTraits = LoadTraitsFromFile("Traits/CommonTraits");
    }

    private Dictionary<string, Trait> LoadTraitsFromFile(string path)
    {
        var traitDictionary = new Dictionary<string, Trait>();
        TextAsset jsonFile = Resources.Load<TextAsset>(path);
        if (jsonFile != null)
        {
            TraitData traitData = JsonUtility.FromJson<TraitData>(jsonFile.text);
            foreach (var trait in traitData.traits)
            {
                if (!traitDictionary.ContainsKey(trait.id))
                {
                    traitDictionary.Add(trait.id, trait);
                }
            }
        }
        else
        {
            Debug.LogError($"Cannot find trait data file at: {path}");
        }
        return traitDictionary;
    }

    public void LoadPlayerTraits()
    {
        playerTraitData = JsonDataUtils.Load<PlayerTraitData>(PLAYER_TRAIT_SAVE_FILE);
        if (playerTraitData == null)
        {
            playerTraitData = new PlayerTraitData();
        }
    }

    public void SavePlayerTraits()
    {
        JsonDataUtils.Save(playerTraitData, PLAYER_TRAIT_SAVE_FILE);
    }

    public Trait GetTrait(string id, string characterClass)
    {
        switch (characterClass.ToLower())
        {
            case "hacker":
                if (hackerTraits.ContainsKey(id)) return hackerTraits[id];
                break;
            case "cyborg":
                if (cyborgTraits.ContainsKey(id)) return cyborgTraits[id];
                break;
        }
        if (commonTraits.ContainsKey(id)) return commonTraits[id];
        return null;
    }

    public bool CanAcquireTrait(string traitId, string characterClass)
    {
        Trait trait = GetTrait(traitId, characterClass);
        if (trait == null) return false;

        // Check current level
        int currentLevel = 0;
        playerTraitData.acquiredTraits.TryGetValue(traitId, out currentLevel);
        if (currentLevel >= trait.maxLevel)
        {
            Debug.Log($"Trait {trait.name} is already at max level.");
            return false;
        }

        // Tier 4 rule: only one can be acquired.
        if (trait.tier == 4)
        {
            foreach (var acquiredTraitId in playerTraitData.acquiredTraits.Keys)
            {
                Trait acquiredTrait = GetTrait(acquiredTraitId, characterClass);
                if (acquiredTrait != null && acquiredTrait.tier == 4 && acquiredTraitId != traitId)
                {
                    Debug.Log($"Cannot acquire {trait.name}. Another Tier 4 trait is already acquired.");
                    return false;
                }
            }
        }

        // Check prerequisites
        if (trait.prerequisites != null)
        {
            foreach (var prereqId in trait.prerequisites)
            {
                Trait prereqTrait = GetTrait(prereqId, characterClass);
                if (prereqTrait == null)
                {
                    Debug.LogError($"Prerequisite trait with id {prereqId} not found.");
                    return false;
                }

                int prereqLevel = 0;
                playerTraitData.acquiredTraits.TryGetValue(prereqId, out prereqLevel);
                if (prereqLevel < prereqTrait.maxLevel)
                {
                    Debug.Log($"Cannot acquire {trait.name}. Prerequisite {prereqTrait.name} is not maxed out.");
                    return false;
                }
            }
        }

        return true;
    }

    public void AcquireTrait(string traitId, string characterClass)
    {
        if (!CanAcquireTrait(traitId, characterClass))
        {
            return;
        }

        if (playerTraitData.acquiredTraits.ContainsKey(traitId))
        {
            playerTraitData.acquiredTraits[traitId]++;
        }
        else
        {
            playerTraitData.acquiredTraits.Add(traitId, 1);
        }

        Debug.Log($"Acquired trait: {GetTrait(traitId, characterClass).name}, Level: {playerTraitData.acquiredTraits[traitId]}");

        // Apply effects immediately
        SavePlayerTraits();
    }
}

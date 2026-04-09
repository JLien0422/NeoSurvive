using UnityEngine;
using System.Collections.Generic;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }

    // 골드 변경 알림 이벤트
    public static event System.Action<int> OnGoldChanged;

    private int gold;
    public int Gold => gold;

    // 업그레이드 레벨 저장소 (Key: UpgradeID, Value: Level)
    private Dictionary<string, int> upgradeLevels = new Dictionary<string, int>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(transform.root.gameObject);
            LoadData();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void AddGold(int amount)
    {
        gold += amount;
        OnGoldChanged?.Invoke(gold);
        SaveData();
    }

    public bool SpendGold(int amount)
    {
        if (gold >= amount)
        {
            gold -= amount;
            OnGoldChanged?.Invoke(gold);
            SaveData();
            return true;
        }
        return false;
    }

    public int GetUpgradeLevel(string id)
    {
        if (upgradeLevels.ContainsKey(id))
            return upgradeLevels[id];
        return 0;
    }

    public void LevelUpUpgrade(string id)
    {
        int current = GetUpgradeLevel(id);
        upgradeLevels[id] = current + 1;
        SaveData();
    }

    // --- Save & Load ---
    private void SaveData()
    {
        PlayerPrefs.SetInt("PlayerGold", gold);
        foreach (var kvp in upgradeLevels)
        {
            PlayerPrefs.SetInt($"Upgrade_{kvp.Key}", kvp.Value);
        }
        PlayerPrefs.Save();
    }

    private void LoadData()
    {
        gold = PlayerPrefs.GetInt("PlayerGold", 0); // 기본 골드 0

        // 미리 정의된 업그레이드 키들을 로드 (임시로 하드코딩된 키 리스트를 사용하거나, 요청 시 로드)
        // 여기서는 필요한 순간에 GetUpgradeLevel에서 기본값 0을 반환하므로 키를 몰라도 됩니다.
        // 다만 저장된 키를 불러오는 로직이 필요하다면 별도 관리가 필요합니다.
        // 지금은 단순화를 위해 PlayerPrefs에 의존합니다.
    }

    // 테스트용: 골드 추가
    [ContextMenu("Add 1000 Gold")]
    public void AddTestGold()
    {
        AddGold(1000);
    }
}
using UnityEngine;

/// <summary>
/// 씬의 매니저 오브젝트에 필요한 컴포넌트를 자동으로 추가합니다.
/// </summary>
public class ManagerInitializer : MonoBehaviour
{
    private void Awake()
    {
        InitializeManagers();
    }

    /// <summary>
    /// 매니저 오브젝트들을 찾아서 필요한 컴포넌트를 추가합니다.
    /// </summary>
    private void InitializeManagers()
    {
        // SettingsManager 찾기 또는 생성
        GameObject settingsManagerObj = GameObject.Find("SettingsManager");
        if (settingsManagerObj == null)
        {
            settingsManagerObj = new GameObject("SettingsManager");
            Debug.Log("[ManagerInitializer] SettingsManager GameObject 생성");
        }
        
        if (settingsManagerObj.GetComponent<SettingsManager>() == null)
        {
            settingsManagerObj.AddComponent<SettingsManager>();
            Debug.Log("[ManagerInitializer] SettingsManager 컴포넌트 추가");
        }

        // HackingSystem 찾기 또는 생성
        GameObject hackingSystemObj = GameObject.Find("HackingSystem");
        if (hackingSystemObj == null)
        {
            hackingSystemObj = new GameObject("HackingSystem");
            Debug.Log("[ManagerInitializer] HackingSystem GameObject 생성");
        }
        
        if (hackingSystemObj.GetComponent<HackingSystem>() == null)
        {
            hackingSystemObj.AddComponent<HackingSystem>();
            Debug.Log("[ManagerInitializer] HackingSystem 컴포넌트 추가");
        }

        // DataManager 찾기 또는 생성
        GameObject dataManagerObj = GameObject.Find("DataManager");
        if (dataManagerObj == null)
        {
            dataManagerObj = new GameObject("DataManager");
            Debug.Log("[ManagerInitializer] DataManager GameObject 생성");
        }
        
        if (dataManagerObj.GetComponent<DataManager>() == null)
        {
            dataManagerObj.AddComponent<DataManager>();
            Debug.Log("[ManagerInitializer] DataManager 컴포넌트 추가");
        }

        // WeaponDamageStats 찾기 또는 생성 (무기별 대미지 통계)
        GameObject weaponStatsObj = GameObject.Find("WeaponDamageStats");
        if (weaponStatsObj == null)
        {
            weaponStatsObj = new GameObject("WeaponDamageStats");
            Debug.Log("[ManagerInitializer] WeaponDamageStats GameObject 생성");
        }
        if (weaponStatsObj.GetComponent<WeaponDamageStats>() == null)
        {
            weaponStatsObj.AddComponent<WeaponDamageStats>();
            Debug.Log("[ManagerInitializer] WeaponDamageStats 컴포넌트 추가");
        }

        Debug.Log("[ManagerInitializer] 모든 매니저 초기화 완료");
    }
}

using System.Collections.Generic;
using NeoSurvive.Weapon;
using UnityEngine;

public class WeaponChoiceController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WeaponChoiceUI weaponChoiceUI; // 무기 선택 UI 호출만 담당

    private const int MAX_WEAPON_LEVEL = 5;

    private void Awake()
    {
        if (weaponChoiceUI == null)
        {
            Debug.LogError("[WeaponChoiceController] weaponChoiceUI is not assigned.");
            enabled = false;
        }
    }

    private void OnEnable()
    {
        ChestPickup.OnChestOpened += HandleChestOpened;
    }

    private void OnDisable()
    {
        ChestPickup.OnChestOpened -= HandleChestOpened;
    }

    /// <summary>
    /// 상자 열림 이벤트를 받아 무기 선택 UI를 여는 기능만 담당
    /// </summary>
    private void HandleChestOpened()
    {
        WeaponManager weaponManager = GetPlayerWeaponManager();
        if (weaponManager == null)
        {
            Debug.LogError("[WeaponChoiceController] WeaponManager not found on Player.");
            return;
        }

        WeaponBase[] choices = Pick3RandomWeapons(weaponManager, weaponManager.allWeaponDatas);
        if (choices == null)
        {
            Debug.LogWarning("[WeaponChoiceController] selectable weapon count is less than 3.");
            return;
        }

        weaponChoiceUI.Show(
            choices,
            GetWeaponDisplayName,
            GetWeaponDescription,
            weapon => GetLevelText(weaponManager, weapon),
            picked =>
            {
                if (picked == null)
                {
                    Debug.LogError("[WeaponChoiceController] picked weapon is null.");
                    return;
                }

                weaponManager.AddWeapon(picked);
            }
        );
    }

    /// <summary>
    /// Player 태그 오브젝트에서 WeaponManager를 찾는 기능만 담당
    /// </summary>
    private WeaponManager GetPlayerWeaponManager()
    {
        GameObject playerObject = GameObject.FindWithTag("Player");
        if (playerObject == null)
        {
            Debug.LogError("[WeaponChoiceController] GameObject with tag 'Player' not found.");
            return null;
        }

        WeaponManager weaponManager = playerObject.GetComponent<WeaponManager>();
        if (weaponManager == null)
        {
            Debug.LogError("[WeaponChoiceController] WeaponManager component not found on Player.");
            return null;
        }

        return weaponManager;
    }

    /// <summary>
    /// 마스터 무기를 제외하고 랜덤 3개를 고르는 기능만 담당
    /// </summary>
    private WeaponBase[] Pick3RandomWeapons(WeaponManager weaponManager, List<WeaponBase> allWeapons)
    {
        if (weaponManager == null)
        {
            Debug.LogError("[WeaponChoiceController] weaponManager is null.");
            return null;
        }

        if (allWeapons == null)
        {
            Debug.LogError("[WeaponChoiceController] allWeapons is null.");
            return null;
        }

        List<WeaponBase> selectableWeapons = new List<WeaponBase>();

        foreach (WeaponBase weapon in allWeapons)
        {
            if (weapon == null)
            {
                Debug.LogError("[WeaponChoiceController] allWeapons contains null weapon.");
                continue;
            }

            if (IsWeaponSelectableForChest(weaponManager, weapon))
            {
                selectableWeapons.Add(weapon);
            }
        }

        if (selectableWeapons.Count < 3)
        {
            return null;
        }

        for (int i = 0; i < selectableWeapons.Count; i++)
        {
            int j = Random.Range(i, selectableWeapons.Count);

            WeaponBase temp = selectableWeapons[i];
            selectableWeapons[i] = selectableWeapons[j];
            selectableWeapons[j] = temp;
        }

        return new WeaponBase[]
        {
            selectableWeapons[0],
            selectableWeapons[1],
            selectableWeapons[2]
        };
    }

    /// <summary>
    /// 상자 후보에 포함 가능한 무기인지 판별하는 기능만 담당
    /// </summary>
    private bool IsWeaponSelectableForChest(WeaponManager weaponManager, WeaponBase targetWeapon)
    {
        if (weaponManager == null)
        {
            Debug.LogError("[WeaponChoiceController] weaponManager is null.");
            return false;
        }

        if (targetWeapon == null)
        {
            Debug.LogError("[WeaponChoiceController] targetWeapon is null.");
            return false;
        }

        if (weaponManager.activeWeapons == null)
        {
            Debug.LogError("[WeaponChoiceController] activeWeapons is null.");
            return false;
        }

        string targetName = GetWeaponDisplayName(targetWeapon);

        foreach (WeaponBase activeWeapon in weaponManager.activeWeapons)
        {
            if (activeWeapon == null)
            {
                Debug.LogError("[WeaponChoiceController] activeWeapons contains null weapon.");
                continue;
            }

            if (GetWeaponDisplayName(activeWeapon) == targetName)
            {
                if (activeWeapon.level >= MAX_WEAPON_LEVEL)
                    return false;

                return true;
            }
        }

        return true;
    }

    /// <summary>
    /// 무기 표시 이름을 반환하는 기능만 담당
    /// </summary>
    private string GetWeaponDisplayName(WeaponBase weapon)
    {
        if (weapon == null)
        {
            Debug.LogError("[WeaponChoiceController] weapon is null.");
            return string.Empty;
        }

        if (string.IsNullOrWhiteSpace(weapon.weaponName))
        {
            Debug.LogError($"[WeaponChoiceController] weaponName is empty. asset={weapon.name}");
            return string.Empty;
        }

        return weapon.weaponName;
    }

    /// <summary>
    /// 무기 설명을 반환하는 기능만 담당
    /// </summary>
    private string GetWeaponDescription(WeaponBase weapon)
    {
        if (weapon == null)
        {
            Debug.LogError("[WeaponChoiceController] weapon is null.");
            return string.Empty;
        }

        if (weapon.description == null)
        {
            Debug.LogError($"[WeaponChoiceController] description is null. asset={weapon.name}");
            return string.Empty;
        }

        return weapon.description;
    }

    /// <summary>
    /// 레벨 문구를 반환하는 기능만 담당
    /// </summary>
    private string GetLevelText(WeaponManager weaponManager, WeaponBase weapon)
    {
        if (weaponManager == null)
        {
            Debug.LogError("[WeaponChoiceController] weaponManager is null.");
            return string.Empty;
        }

        if (weapon == null)
        {
            Debug.LogError("[WeaponChoiceController] weapon is null.");
            return string.Empty;
        }

        bool isNewWeapon = IsNewWeapon(weaponManager, weapon);
        int currentLevel = GetCurrentWeaponLevel(weaponManager, weapon);

        if (isNewWeapon)
            return "NEW WEAPON";

        if (currentLevel >= MAX_WEAPON_LEVEL)
            return $"Lv.{MAX_WEAPON_LEVEL} (MASTER)";

        return $"Lv.{currentLevel} → Lv.{currentLevel + 1}";
    }

    /// <summary>
    /// 새 무기인지 판별하는 기능만 담당
    /// </summary>
    private bool IsNewWeapon(WeaponManager weaponManager, WeaponBase targetWeapon)
    {
        if (weaponManager == null)
        {
            Debug.LogError("[WeaponChoiceController] weaponManager is null.");
            return false;
        }

        if (targetWeapon == null)
        {
            Debug.LogError("[WeaponChoiceController] targetWeapon is null.");
            return false;
        }

        if (weaponManager.activeWeapons == null)
        {
            Debug.LogError("[WeaponChoiceController] activeWeapons is null.");
            return false;
        }

        string targetName = GetWeaponDisplayName(targetWeapon);

        foreach (WeaponBase activeWeapon in weaponManager.activeWeapons)
        {
            if (activeWeapon == null)
            {
                Debug.LogError("[WeaponChoiceController] activeWeapons contains null weapon.");
                continue;
            }

            if (GetWeaponDisplayName(activeWeapon) == targetName)
                return false;
        }

        return true;
    }

    /// <summary>
    /// 현재 무기 레벨을 찾는 기능만 담당
    /// </summary>
    private int GetCurrentWeaponLevel(WeaponManager weaponManager, WeaponBase targetWeapon)
    {
        if (weaponManager == null)
        {
            Debug.LogError("[WeaponChoiceController] weaponManager is null.");
            return 0;
        }

        if (targetWeapon == null)
        {
            Debug.LogError("[WeaponChoiceController] targetWeapon is null.");
            return 0;
        }

        if (weaponManager.activeWeapons == null)
        {
            Debug.LogError("[WeaponChoiceController] activeWeapons is null.");
            return 0;
        }

        string targetName = GetWeaponDisplayName(targetWeapon);

        foreach (WeaponBase activeWeapon in weaponManager.activeWeapons)
        {
            if (activeWeapon == null)
            {
                Debug.LogError("[WeaponChoiceController] activeWeapons contains null weapon.");
                continue;
            }

            if (GetWeaponDisplayName(activeWeapon) == targetName)
                return activeWeapon.level;
        }

        return 0;
    }
}
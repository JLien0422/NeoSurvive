using System.Collections.Generic;
using System.Linq;
using NeoSurvive.Weapon;
using UnityEngine;

public class WeaponChoiceController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WeaponChoiceUI weaponChoiceUI; // 무기 선택 UI 호출만 담당

    private const int MAX_WEAPON_COUNT = 6;
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

        WeaponBase[] choices = PickRandomWeaponsForChest(weaponManager);
        if (choices == null || choices.Length == 0)
        {
            Debug.LogWarning("[WeaponChoiceController] selectable weapon count is zero.");
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
    /// 상자에서 보여줄 무기 후보를 만든 뒤 최대 3개를 랜덤으로 반환한다.
    /// </summary>
    private WeaponBase[] PickRandomWeaponsForChest(WeaponManager weaponManager)
    {
        if (weaponManager == null)
        {
            Debug.LogError("[WeaponChoiceController] weaponManager is null.");
            return null;
        }

        List<WeaponBase> selectableWeapons = BuildSelectableWeapons(weaponManager);

        if (selectableWeapons.Count == 0)
            return new WeaponBase[0];

        for (int i = 0; i < selectableWeapons.Count; i++)
        {
            int j = Random.Range(i, selectableWeapons.Count);

            WeaponBase temp = selectableWeapons[i];
            selectableWeapons[i] = selectableWeapons[j];
            selectableWeapons[j] = temp;
        }

        int count = Mathf.Min(3, selectableWeapons.Count);
        return selectableWeapons.Take(count).ToArray();
    }

    /// <summary>
    /// ★ 수정:
    /// 무기 6개 미만이면 전체 무기 목록에서 후보를 만든다.
    /// 무기 6개 이상이면 현재 보유 중인 6개 무기에서만 후보를 만든다.
    ///
    /// ★ 추가:
    /// WeaponChoice에 같은 무기가 2개 이상 나오지 않도록
    /// weaponName 기준으로 중복 후보를 제거한다.
    /// </summary>
    private List<WeaponBase> BuildSelectableWeapons(WeaponManager weaponManager)
    {
        List<WeaponBase> selectableWeapons = new List<WeaponBase>();
        HashSet<string> addedWeaponNames = new HashSet<string>();

        if (weaponManager.allWeaponDatas == null)
        {
            Debug.LogError("[WeaponChoiceController] allWeaponDatas is null.");
            return selectableWeapons;
        }

        if (weaponManager.activeWeapons == null)
        {
            Debug.LogError("[WeaponChoiceController] activeWeapons is null.");
            return selectableWeapons;
        }

        List<WeaponBase> sourceWeapons;

        // ★ 수정:
        // 무기 6개 이상부터는 새 무기를 더 이상 후보에 넣지 않고,
        // 현재 가진 6개 무기 중 Lv.5 미만 무기만 후보로 사용한다.
        if (weaponManager.activeWeapons.Count >= MAX_WEAPON_COUNT)
        {
            sourceWeapons = weaponManager.activeWeapons;
        }
        // ★ 수정:
        // 무기 6개 미만일 때는 전체 무기 목록에서 후보를 만든다.
        // 이 상태에서는 새 무기도 나오고, 이미 가진 무기도 레벨업 후보로 나올 수 있다.
        else
        {
            sourceWeapons = weaponManager.allWeaponDatas;
        }

        foreach (WeaponBase weapon in sourceWeapons)
        {
            if (weapon == null)
            {
                Debug.LogError("[WeaponChoiceController] sourceWeapons contains null weapon.");
                continue;
            }

            string weaponName = GetWeaponDisplayName(weapon);

            if (string.IsNullOrWhiteSpace(weaponName))
                continue;

            // ★ 추가:
            // 같은 이름의 무기가 이미 후보에 들어갔다면 다시 넣지 않는다.
            // 무기가 6개가 되기 전까지 WeaponChoice에 같은 무기가 2개 이상 뜨는 문제를 막는다.
            if (addedWeaponNames.Contains(weaponName))
                continue;

            if (IsWeaponSelectableForChest(weaponManager, weapon))
            {
                selectableWeapons.Add(weapon);
                addedWeaponNames.Add(weaponName);
            }
        }

        return selectableWeapons;
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
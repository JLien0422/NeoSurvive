using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using NeoSurvive.Weapon;

/// <summary>
/// 설정(ESC) 패널의 무기별 대미지 통계를 한 줄씩(아이콘 + 이름 + 총대미지 + DPM) 표시합니다.
/// WeaponStatRows 아래에 "한 줄 템플릿"을 두고, 무기 개수만큼 복제해서 채웁니다.
/// </summary>
public class WeaponStatsPanelUI : MonoBehaviour
{
    [Header("연결")]
    [Tooltip("무기 한 줄씩 쌓을 부모 (Vertical Layout 권장)")]
    public Transform weaponStatRowsRoot;

    [Tooltip("한 줄 템플릿: Horizontal Layout + [Image(아이콘), Text(이름), Text(총대미지), Text(DPM)]. 비어 있으면 weaponStatRowsRoot의 첫 번째 자식을 템플릿으로 씀")]
    public GameObject rowTemplate;

    [Header("선택")]
    [Tooltip("비어 있으면 씬에서 WeaponManager 찾음")]
    public WeaponManager weaponManager;

    private void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (weaponStatRowsRoot == null) return;

        var stats = WeaponDamageStats.Instance;

        if (weaponManager == null)
            Debug.LogError("[WeaponStatsPanelUI] WeaponManager가 할당되지 않았습니다. 씬에서 WeaponManager를 찾습니다.");

        GameObject template = rowTemplate;
        if (template == null && weaponStatRowsRoot.childCount > 0)
            template = weaponStatRowsRoot.GetChild(0).gameObject;

        if (template == null)
            return;

        template.SetActive(false);

        for (int i = weaponStatRowsRoot.childCount - 1; i >= 0; i--)
        {
            var child = weaponStatRowsRoot.GetChild(i).gameObject;
            if (child == template) continue;
            Destroy(child);
        }

        foreach (var weapon in weaponManager.activeWeapons)
        {
            if (weapon == null) continue;

            float total = stats.GetTotalDamage(weapon);
            float dpm = stats.GetDPM(weapon);

            Debug.Log($"[WeaponStatsPanelUI] 무기: {weapon.weaponName}, 총대미지: {total:F1}, DPM: {dpm:F1}");

            var row = Instantiate(template, weaponStatRowsRoot);
            row.SetActive(true);

            var images = row.GetComponentsInChildren<Image>(true);
            var texts = row.GetComponentsInChildren<TextMeshProUGUI>(true);

            if (images != null && images.Length > 0 && weapon.weaponIcon != null)
            {
                images[0].sprite = weapon.weaponIcon;
                images[0].enabled = true;
            }

            // 모든 텍스트 필드를 먼저 비워서 이전 값이 남지 않도록 함
            if (texts != null)
            {
                foreach (var t in texts)
                    t.text = "";
            }

            if (texts != null && texts.Length >= 3)
            {
                texts[0].text = weapon.weaponName ?? "(무기)";
                texts[1].text = total.ToString("F0");
                texts[2].text = dpm.ToString("F0");
            }
        }

        Debug.Log("[WeaponStatsPanelUI] Refresh 완료. 무기 개수: " + weaponManager.activeWeapons.Count);
    }
}

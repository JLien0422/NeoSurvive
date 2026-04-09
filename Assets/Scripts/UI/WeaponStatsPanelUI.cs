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
        // 이 패널 배경이 레이캐스트를 막지 않도록 → 탭/버튼 클릭이 가능해짐
        if (TryGetComponent<Image>(out var img))
            img.raycastTarget = false;
        Refresh();
        StartCoroutine(AutoRefresh());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    private IEnumerator AutoRefresh()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(0.5f);
            Refresh();
        }
    }

    public void Refresh()
    {
        if (weaponStatRowsRoot == null) return;

        var stats = WeaponDamageStats.Instance;
        var wm = weaponManager != null ? weaponManager : Object.FindObjectOfType<WeaponManager>();
        if (stats == null || wm == null || wm.activeWeapons == null)
            return;

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
            Object.Destroy(child);
        }

        foreach (var weapon in wm.activeWeapons)
        {
            if (weapon == null) continue;

            float total = stats.GetTotalDamage(weapon);
            float dpm = stats.GetDPM(weapon);

            var row = Object.Instantiate(template, weaponStatRowsRoot);
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
    }
}

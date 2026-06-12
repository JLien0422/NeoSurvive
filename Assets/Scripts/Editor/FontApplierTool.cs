using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using System.IO;

/// <summary>
/// Tools → Apply Font 메뉴에서 폰트를 선택하면
/// 현재 씬의 모든 TextMeshProUGUI 컴포넌트에 해당 폰트를 일괄 적용합니다.
/// </summary>
public static class FontApplierTool
{
    private const string FontResourcesPath = "Assets/Resources/Fonts";
    private const string MenuRoot = "Tools/Apply Font To All TMP/";

    [MenuItem(MenuRoot + "[ 현재 씬 전체 적용 ] ──────────────", priority = 0)]
    private static void Separator() { }

    [MenuItem(MenuRoot + "[ 현재 씬 전체 적용 ] ──────────────", true)]
    private static bool SeparatorValidate() => false;

    // ─── 폰트 메뉴 항목은 Unity가 static initializer를 지원하지 않으므로
    //     각 폰트를 직접 MenuItem으로 등록합니다. ───

    [MenuItem(MenuRoot + "Maplestory Light SDF", priority = 10)]
    private static void ApplyMaplestoryLight() => Apply("Fonts/Maplestory Light SDF");

    [MenuItem(MenuRoot + "Maplestory Bold SDF", priority = 11)]
    private static void ApplyMaplestoryBold() => Apply("Fonts/Maplestory Bold SDF");

    [MenuItem(MenuRoot + "UmdotMono16 SDF", priority = 12)]
    private static void ApplyUmdot() => Apply("Fonts/Umdot/UmdotMono16 SDF");

    [MenuItem(MenuRoot + "quaver SDF", priority = 13)]
    private static void ApplyQuaver() => Apply("Fonts/quaver SDF");

    [MenuItem(MenuRoot + "GalmuriMono7 SDF", priority = 14)]
    private static void ApplyGalmuriMono7() => Apply("Fonts/Galmuri-v2.40.3/GalmuriMono7 SDF");

    [MenuItem(MenuRoot + "Mulmaru", priority = 15)]
    private static void ApplyMulmaru() => Apply("Fonts/Mulmaru/Mulmaru SDF");

    [MenuItem(MenuRoot + "Stardust", priority = 16)]
    private static void ApplyStardust() => Apply("Fonts/Stardust/PF스타더스트 3.0 SDF");

    [MenuItem(MenuRoot + "Stardust Bold", priority = 17)]
    private static void ApplyStardustBold() => Apply("Fonts/Stardust/PF스타더스트 3.0 Bold SDF");

    [MenuItem(MenuRoot + "Stardust ExtraBold", priority = 18)]
    private static void ApplyStardustExtraBold() => Apply("Fonts/Stardust/PF스타더스트 3.0 ExtraBold SDF");

    // ─── 공통 적용 로직 ───────────────────────────────────────────────────

    private static void Apply(string resourcePath)
    {
        TMP_FontAsset font = Resources.Load<TMP_FontAsset>(resourcePath);
        if (font == null)
        {
            EditorUtility.DisplayDialog(
                "폰트 로드 실패",
                $"Resources 경로에서 폰트를 찾을 수 없습니다:\n{resourcePath}\n\nAssets/Resources/ 하위에 폰트 어셋이 있는지 확인해주세요.",
                "확인"
            );
            return;
        }

        var allTMPs = Object.FindObjectsOfType<TextMeshProUGUI>(true);
        if (allTMPs.Length == 0)
        {
            EditorUtility.DisplayDialog("완료", "씬에 TextMeshProUGUI 컴포넌트가 없습니다.", "확인");
            return;
        }

        int count = 0;
        foreach (var tmp in allTMPs)
        {
            Undo.RecordObject(tmp, "Apply Font");
            tmp.font = font;
            EditorUtility.SetDirty(tmp);
            count++;
        }

        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        EditorUtility.DisplayDialog(
            "폰트 일괄 적용 완료",
            $"폰트: {font.name}\n적용된 TextMeshProUGUI 수: {count}개\n\nCtrl+Z로 되돌릴 수 있습니다.",
            "확인"
        );

        Debug.Log($"[FontApplierTool] '{font.name}' 폰트를 {count}개의 TextMeshProUGUI에 적용했습니다.");
    }
}

using UnityEditor;
using UnityEngine;

namespace NeoSurvive.Editor.FolderDock
{
    /// <summary>
    /// 별도 패널은 쓰지 않는다. 메뉴는 Project 창 고정 안내 + Pin ON.
    /// </summary>
    public sealed class FolderDockWindow : EditorWindow
    {
        [MenuItem("Tools/NeoSurvive/Folder Dock")]
        private static void OpenFromMenu()
        {
            // 남아 있는 옛 패널이 있으면 닫는다.
            var windows = UnityEngine.Resources.FindObjectsOfTypeAll<FolderDockWindow>();
            for (var i = 0; i < windows.Length; i++)
                windows[i].Close();

            VFavorites.FolderDockPinToggle.IsOnDefaultPage = true;
            VFavorites.FolderDockPinToggle.ApplyPinned(true);

            EditorUtility.DisplayDialog(
                "Folder Dock (Project 창)",
                "별도 패널이 아니라 Project 창에서 동작합니다.\n\n"
                    + "1) DefaultPage: 일반 폴더/파일 탐색\n"
                    + "2) ▶ 로 Page 1… (vFavorites 즐겨찾기)\n"
                    + "3) 우하단「고정 창」: Alt 없이 하단 바 유지\n\n"
                    + "Project 창을 클릭한 뒤 하단을 확인하세요.",
                "확인"
            );
        }
    }
}

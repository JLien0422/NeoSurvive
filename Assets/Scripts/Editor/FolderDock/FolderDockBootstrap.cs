using UnityEditor;

namespace NeoSurvive.Editor.FolderDock
{
    /// <summary>
    /// Unity 기동 시 Project 창 고정을 요청한다. (별도 패널 없음)
    /// </summary>
    [InitializeOnLoad]
    internal static class FolderDockBootstrap
    {
        static FolderDockBootstrap()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                    return;

                VFavorites.FolderDockPinToggle.RequestEnsurePinnedOnLoad();
            };
        }
    }
}

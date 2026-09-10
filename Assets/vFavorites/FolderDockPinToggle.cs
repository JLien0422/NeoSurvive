#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace VFavorites
{
    /// <summary>
    /// Project 창(vFavorites)용 고정/DefaultPage 상태.
    /// 별도 EditorWindow가 아니라 Project 브라우저 Lock으로 고정한다.
    /// </summary>
    public static class FolderDockPinToggle
    {
        public const string PrefKey = "NeoSurvive.FolderDock.Pinned";
        public const string DefaultPagePrefKey = "NeoSurvive.FolderDock.OnDefaultPage";

        public static bool IsPinned
        {
            get => EditorPrefs.GetBool(PrefKey, true);
            set => EditorPrefs.SetBool(PrefKey, value);
        }

        public static bool IsOnDefaultPage
        {
            get => EditorPrefs.GetBool(DefaultPagePrefKey, true);
            set => EditorPrefs.SetBool(DefaultPagePrefKey, value);
        }

        public static bool PinRequested { get; set; }
        public static bool UnpinRequested { get; set; }
        public static bool EnsurePinnedRequested { get; set; }

        public static Rect LastToggleRect { get; private set; }

        public static Rect GetToggleRect(Rect totalRectGroupSpace)
        {
            const float width = 72f;
            const float height = 20f;
            return new Rect(
                totalRectGroupSpace.xMax - width - 6f,
                totalRectGroupSpace.yMax - height - 10f,
                width,
                height
            );
        }

        public static void Draw(Rect totalRectGroupSpace, float opacity)
        {
            // DefaultPage에서는 숨기고, vFavorites Page에서만 표시
            if (IsOnDefaultPage)
                return;

            if (opacity < 0.35f)
                return;

            LastToggleRect = GetToggleRect(totalRectGroupSpace);
            var e = Event.current;

            // ▶ 보다 토글 우선
            if ((e.type == EventType.MouseDown || e.type == EventType.MouseUp)
                && LastToggleRect.Contains(e.mousePosition))
            {
                if (e.type == EventType.MouseUp && e.button == 0)
                {
                    ApplyPinned(!IsPinned);
                    GUI.changed = true;
                }

                e.Use();
            }

            var prev = GUI.color;
            GUI.color = Color.white;
            var style = new GUIStyle(EditorStyles.miniButton)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            GUI.Toggle(LastToggleRect, IsPinned, "고정 창", style);
            GUI.color = prev;
        }

        public static void ApplyPinned(bool pinned)
        {
            IsPinned = pinned;
            if (pinned)
            {
                UnpinRequested = false;
                PinRequested = true;
                EnsurePinnedRequested = true;
            }
            else
            {
                PinRequested = false;
                UnpinRequested = true;
            }
        }

        public static void RequestEnsurePinnedOnLoad()
        {
            if (!IsPinned)
                return;
            EnsurePinnedRequested = true;
        }
    }
}
#endif

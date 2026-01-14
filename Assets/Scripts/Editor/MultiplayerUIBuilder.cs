using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using NeoSurvive.UI;
using UnityEngine.EventSystems;

namespace NeoSurvive.Editor
{
    public class MultiplayerUIBuilder : EditorWindow
    {
        [MenuItem("Tools/NeoSurvive/Setup Multiplayer UI")]
        public static void SetupUI()
        {
            // 1. Canvas 찾기 또는 생성
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
                Undo.RegisterCreatedObjectUndo(canvasObj, "Create Canvas");
            }

            // EventSystem 확인
            if (FindObjectOfType<EventSystem>() == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                esObj.AddComponent<EventSystem>();
                esObj.AddComponent<StandaloneInputModule>();
                Undo.RegisterCreatedObjectUndo(esObj, "Create EventSystem");
            }

            // 2. 리더보드 UI 생성
            LeaderboardUI leaderboardUI = CreateLeaderboardUI(canvas.transform);

            // 3. 닉네임 변경 UI 생성
            NicknameChangeUI nicknameUI = CreateNicknameUI(canvas.transform);

            // 4. CharacterSelector 연결 및 버튼 생성
            ConnectToSelector(canvas.transform, leaderboardUI, nicknameUI);

            Debug.Log("Multiplayer UI Setup Complete!");
        }

        private static LeaderboardUI CreateLeaderboardUI(Transform parent)
        {
            // 패널 생성
            GameObject panelObj = CreatePanel("LeaderboardPanel", parent, new Vector2(800, 600));
            
            // 타이틀
            CreateText("Title", panelObj.transform, "Leaderboard", new Vector2(0, 250), 48);

            // 닫기 버튼
            Button closeBtn = CreateButton("CloseButton", panelObj.transform, "Close", new Vector2(350, 270), new Vector2(80, 40));

            // 새로고침 버튼
            Button refreshBtn = CreateButton("RefreshButton", panelObj.transform, "Refresh", new Vector2(250, 270), new Vector2(100, 40));

            // Scroll View 생성 (간략화)
            GameObject scrollObj = new GameObject("ScrollView");
            scrollObj.transform.SetParent(panelObj.transform, false);
            RectTransform scrollRect = scrollObj.AddComponent<RectTransform>();
            scrollRect.sizeDelta = new Vector2(700, 450);
            scrollRect.anchoredPosition = new Vector2(0, -30);
            ScrollRect sr = scrollObj.AddComponent<ScrollRect>();
            
            // Viewport
            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollObj.transform, false);
            RectTransform viewRect = viewport.AddComponent<RectTransform>();
            viewRect.anchorMin = Vector2.zero;
            viewRect.anchorMax = Vector2.one;
            viewRect.sizeDelta = Vector2.zero;
            viewport.AddComponent<RectMask2D>();
            Image viewImg = viewport.AddComponent<Image>();
            viewImg.color = new Color(1, 1, 1, 0.1f);

            // Content
            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 0); // 높이는 VerticalLayoutGroup에 의해 조절됨
            VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.childControlHeight = false;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 5;
            ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            sr.content = contentRect;
            sr.viewport = viewRect;

            // Entry Prefab (Template) 생성
            GameObject entryTemplate = CreateLeaderboardEntryTemplate(content.transform);
            entryTemplate.SetActive(false); // 템플릿은 숨김

            // LeaderboardUI 컴포넌트 설정
            LeaderboardUI ui = panelObj.AddComponent<LeaderboardUI>();
            // private 필드들에 접근하기 위해 SerializedObject 사용
            SerializedObject so = new SerializedObject(ui);
            so.FindProperty("leaderboardPanel").objectReferenceValue = panelObj;
            so.FindProperty("contentRoot").objectReferenceValue = content.transform;
            so.FindProperty("entryPrefab").objectReferenceValue = entryTemplate;
            so.FindProperty("closeButton").objectReferenceValue = closeBtn;
            so.FindProperty("refreshButton").objectReferenceValue = refreshBtn;
            so.ApplyModifiedProperties();

            panelObj.SetActive(false); // 기본은 숨김
            return ui;
        }

        private static GameObject CreateLeaderboardEntryTemplate(Transform parent)
        {
            GameObject entry = new GameObject("EntryTemplate");
            entry.transform.SetParent(parent, false);
            RectTransform rt = entry.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 50); // 높이 50
            Image img = entry.AddComponent<Image>();
            img.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            HorizontalLayoutGroup hlg = entry.AddComponent<HorizontalLayoutGroup>();
            hlg.childControlWidth = true;
            hlg.childForceExpandWidth = true;
            hlg.padding = new RectOffset(10, 10, 5, 5);

            CreateText("Rank", entry.transform, "1", Vector2.zero, 24).alignment = TextAlignmentOptions.Left;
            CreateText("Name", entry.transform, "Player", Vector2.zero, 24).alignment = TextAlignmentOptions.Center;
            CreateText("Level", entry.transform, "Lv.1", Vector2.zero, 24).alignment = TextAlignmentOptions.Center;
            CreateText("Time", entry.transform, "00:00", Vector2.zero, 24).alignment = TextAlignmentOptions.Right;

            return entry;
        }

        private static NicknameChangeUI CreateNicknameUI(Transform parent)
        {
            GameObject panelObj = CreatePanel("NicknamePanel", parent, new Vector2(400, 300));
            
            CreateText("Title", panelObj.transform, "닉네임 변경", new Vector2(0, 100), 36);

            // InputField
            GameObject inputObj = new GameObject("InputField");
            inputObj.transform.SetParent(panelObj.transform, false);
            RectTransform inputRect = inputObj.AddComponent<RectTransform>();
            inputRect.sizeDelta = new Vector2(300, 50);
            Image inputImg = inputObj.AddComponent<Image>();
            inputImg.color = Color.white;
            
            GameObject textArea = new GameObject("TextArea");
            textArea.transform.SetParent(inputObj.transform, false);
            RectTransform areaRect = textArea.AddComponent<RectTransform>();
            areaRect.anchorMin = Vector2.zero;
            areaRect.anchorMax = Vector2.one;
            areaRect.offsetMin = new Vector2(10, 0);
            areaRect.offsetMax = new Vector2(-10, 0);
            
            TextMeshProUGUI inputText = CreateText("Text", textArea.transform, "", Vector2.zero, 24);
            inputText.color = Color.black;
            
            TMP_InputField inputField = inputObj.AddComponent<TMP_InputField>();
            inputField.textViewport = areaRect;
            inputField.textComponent = inputText;
            
            // Placeholder
            GameObject placeholderObj = new GameObject("Placeholder");
            placeholderObj.transform.SetParent(textArea.transform, false);
            RectTransform phRect = placeholderObj.AddComponent<RectTransform>();
            phRect.anchorMin = Vector2.zero;
            phRect.anchorMax = Vector2.one;
            TextMeshProUGUI phText = placeholderObj.AddComponent<TextMeshProUGUI>();
            phText.text = "새 닉네임 입력...";
            phText.fontSize = 24;
            phText.color = Color.gray;
            inputField.placeholder = phText;

            // Buttons
            Button confirmBtn = CreateButton("ConfirmButton", panelObj.transform, "확인", new Vector2(-70, -80), new Vector2(120, 50));
            Button cancelBtn = CreateButton("CancelButton", panelObj.transform, "취소", new Vector2(70, -80), new Vector2(120, 50));

            // Status Text
            TextMeshProUGUI statusText = CreateText("Status", panelObj.transform, "", new Vector2(0, -30), 20);
            statusText.color = Color.yellow;

            // Component Setup
            NicknameChangeUI ui = panelObj.AddComponent<NicknameChangeUI>();
            SerializedObject so = new SerializedObject(ui);
            so.FindProperty("panel").objectReferenceValue = panelObj;
            so.FindProperty("nicknameInput").objectReferenceValue = inputField;
            so.FindProperty("confirmButton").objectReferenceValue = confirmBtn;
            so.FindProperty("cancelButton").objectReferenceValue = cancelBtn;
            so.FindProperty("statusText").objectReferenceValue = statusText;
            so.ApplyModifiedProperties();

            panelObj.SetActive(false);
            return ui;
        }

        private static void ConnectToSelector(Transform parent, LeaderboardUI lbUI, NicknameChangeUI nickUI)
        {
            CharacterSelector selector = FindObjectOfType<CharacterSelector>();
            if (selector == null)
            {
                Debug.LogWarning("CharacterSelector를 찾지 못했습니다.");
                return;
            }

            // Buttons creation in UI layer (under Canvas, top corner usually)
            // 리더보드 버튼
            Button lbBtn = CreateButton("LeaderboardButton", parent, "랭킹", new Vector2(400, 350), new Vector2(100, 50));
            // 위치 조정 (Top Right)
            RectTransform lbRect = lbBtn.GetComponent<RectTransform>();
            lbRect.anchorMin = new Vector2(1, 1);
            lbRect.anchorMax = new Vector2(1, 1);
            lbRect.anchoredPosition = new Vector2(-60, -60);

            // 닉네임 버튼
            Button nickBtn = CreateButton("NicknameButton", parent, "닉변", new Vector2(400, 300), new Vector2(100, 50));
            RectTransform nickRect = nickBtn.GetComponent<RectTransform>();
            nickRect.anchorMin = new Vector2(1, 1);
            nickRect.anchorMax = new Vector2(1, 1);
            nickRect.anchoredPosition = new Vector2(-170, -60);


            SerializedObject so = new SerializedObject(selector);
            so.FindProperty("leaderboardUI").objectReferenceValue = lbUI;
            so.FindProperty("nicknameUI").objectReferenceValue = nickUI;
            so.FindProperty("leaderboardButton").objectReferenceValue = lbBtn;
            so.FindProperty("nicknameButton").objectReferenceValue = nickBtn;
            so.ApplyModifiedProperties();
        }

        // Helpers
        private static GameObject CreatePanel(string name, Transform parent, Vector2 size)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.sizeDelta = size;
            Image img = obj.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0.9f);
            return obj;
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, string content, Vector2 pos, int fontSize)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(300, 60); // 기본 사이즈
            TextMeshProUGUI txt = obj.AddComponent<TextMeshProUGUI>();
            txt.text = content;
            txt.fontSize = fontSize;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color = Color.white;
            return txt;
        }

        private static Button CreateButton(string name, Transform parent, string label, Vector2 pos, Vector2 size)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            
            Image img = obj.AddComponent<Image>();
            img.color = new Color(0.3f, 0.5f, 1f);
            
            Button btn = obj.AddComponent<Button>();
            
            CreateText("Label", obj.transform, label, Vector2.zero, 20).rectTransform.sizeDelta = size;
            
            return btn;
        }
    }
}
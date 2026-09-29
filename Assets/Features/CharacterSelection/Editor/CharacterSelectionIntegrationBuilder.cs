#if UNITY_EDITOR
using NeoSurvive.UI.CharacterSelection;
using UIEffectDemo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NeoSurvive.Editor
{
    public static class CharacterSelectionIntegrationBuilder
    {
        private const string ReferenceScenePath = "Assets/Features/CharacterSelection/Reference/CharacterSelectionReference.unity";
        private const string PrefabPath = "Assets/Features/CharacterSelection/Prefabs/CharacterSelectionPanel.prefab";
        private const string LobbyScenePath = "Assets/Scenes/LobbyScene.unity";
        private const string SourceRootName = "UIEffect_CardSelection_Demo";
        private const string IntegratedRootName = "IntegratedCharacterSelection";
        private const string MainThemeFontPath = "Assets/Resources/fonts/Stardust/PF스타더스트 3.0 ExtraBold.ttf";
        private const string BackPanelSpritePath = "Assets/Features/CharacterSelection/Art/Panel.png";

        private static readonly Color PrimaryTextColor = new Color(0.94f, 0.97f, 1f, 1f);
        private static readonly Color AccentTextColor = new Color(0.594f, 0.909f, 1f, 1f);
        private static readonly Color ActionTextColor = new Color(0.973f, 0.933f, 0.04f, 1f);

        [MenuItem("Tools/NeoSurvive/Build Integrated Character Selection")]
        public static void Build()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            BuildPrefabFromReferenceScene();
            AttachPrefabToLobbyScene();
            AssetDatabase.SaveAssets();
            Debug.Log("[CharacterSelection] UIEffect 캐릭터 선택 UI 통합 완료");
        }

        [InitializeOnLoadMethod]
        private static void UpgradeIntegratedPrefabIfNeeded()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                    return;

                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
                if (prefab != null && FindChild(prefab.transform, "BackPanel") == null)
                    Build();
            };
        }

        private static void BuildPrefabFromReferenceScene()
        {
            Scene referenceScene = EditorSceneManager.OpenScene(ReferenceScenePath, OpenSceneMode.Single);
            GameObject sourceRoot = FindInScene(referenceScene, SourceRootName);
            if (sourceRoot == null)
                throw new MissingReferenceException($"{ReferenceScenePath}에서 {SourceRootName}을 찾지 못했습니다.");

            ApplyMainTheme(sourceRoot);
            EnsureOriginalBackground(sourceRoot);
            if (sourceRoot.GetComponent<CanvasGroup>() == null)
                sourceRoot.AddComponent<CanvasGroup>();
            if (sourceRoot.GetComponent<CharacterSelectionTabPresentation>() == null)
                sourceRoot.AddComponent<CharacterSelectionTabPresentation>();
            PrefabUtility.SaveAsPrefabAsset(sourceRoot, PrefabPath);
        }

        private static void AttachPrefabToLobbyScene()
        {
            Scene lobbyScene = EditorSceneManager.OpenScene(LobbyScenePath, OpenSceneMode.Single);
            GameObject tab = FindInScene(lobbyScene, "CharacterSelectionTab");
            if (tab == null)
                throw new MissingReferenceException("LobbyScene에서 CharacterSelectionTab을 찾지 못했습니다.");

            HorizontalLayoutGroup legacyLayout = tab.GetComponent<HorizontalLayoutGroup>();
            if (legacyLayout != null)
                Object.DestroyImmediate(legacyLayout);

            Image legacyBackground = tab.GetComponent<Image>();
            if (legacyBackground != null)
            {
                legacyBackground.sprite = null;
                legacyBackground.color = Color.clear;
                legacyBackground.raycastTarget = false;
            }

            RemoveLegacyObject(tab.transform, "TitleText");
            RemoveLegacyObject(tab.transform, "CharactersContainer");
            RemoveLegacyObject(tab.transform, "StartGameButton");

            Transform previous = tab.transform.Find(IntegratedRootName);
            if (previous != null)
                Object.DestroyImmediate(previous.gameObject);

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab, lobbyScene) as GameObject;
            if (instance == null)
                throw new MissingReferenceException("캐릭터 선택 프리팹을 생성하지 못했습니다.");

            instance.name = IntegratedRootName;
            instance.transform.SetParent(tab.transform, false);
            instance.transform.SetAsLastSibling();

            RectTransform rect = instance.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image background = FindChildComponent<Image>(instance.transform, "Background");
            if (background != null)
                background.raycastTarget = true;

            CardSelectionController controller = instance.GetComponent<CardSelectionController>();
            LobbyCharacterSelector lobbySelector = Object.FindObjectOfType<LobbyCharacterSelector>(true);
            NeoCharacterSelectionBridge bridge = instance.GetComponent<NeoCharacterSelectionBridge>();
            if (bridge == null)
                bridge = instance.AddComponent<NeoCharacterSelectionBridge>();
            bridge.Configure(controller, lobbySelector);

            EditorSceneManager.MarkSceneDirty(lobbyScene);
            EditorSceneManager.SaveScene(lobbyScene);
        }

        private static GameObject FindInScene(Scene scene, string objectName)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform match = FindChild(root.transform, objectName);
                if (match != null)
                    return match.gameObject;
            }

            return null;
        }

        private static Transform FindChild(Transform root, string objectName)
        {
            if (root.name == objectName)
                return root;

            for (int index = 0; index < root.childCount; index++)
            {
                Transform match = FindChild(root.GetChild(index), objectName);
                if (match != null)
                    return match;
            }

            return null;
        }

        private static T FindChildComponent<T>(Transform root, string objectName) where T : Component
        {
            Transform child = FindChild(root, objectName);
            return child != null ? child.GetComponent<T>() : null;
        }

        private static void RemoveLegacyObject(Transform tab, string objectName)
        {
            Transform child = tab.Find(objectName);
            if (child != null)
                Object.DestroyImmediate(child.gameObject);
        }

        private static void ApplyMainTheme(GameObject root)
        {
            Font mainFont = AssetDatabase.LoadAssetAtPath<Font>(MainThemeFontPath);
            if (mainFont == null)
                throw new MissingReferenceException($"메인 테마 폰트를 찾지 못했습니다: {MainThemeFontPath}");

            foreach (Text text in root.GetComponentsInChildren<Text>(true))
            {
                text.font = mainFont;
                text.color = PrimaryTextColor;

                if (text.text == "Characters")
                {
                    text.text = "캐릭터 선택";
                    text.color = AccentTextColor;
                }
                else if (text.text == "Start")
                {
                    text.text = "게임 시작";
                    text.color = ActionTextColor;
                }
                else if (text.text == "Cybug")
                {
                    text.text = "Cyborg";
                }
            }
        }

        private static void EnsureOriginalBackground(GameObject root)
        {
            Transform previous = FindChild(root.transform, "BackPanel");
            if (previous != null)
                Object.DestroyImmediate(previous.gameObject);

            Transform background = FindChild(root.transform, "Background");
            if (background != null)
                background.SetAsFirstSibling();

            Sprite panelSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BackPanelSpritePath);
            if (panelSprite == null)
                throw new MissingReferenceException($"뒤 패널 이미지를 찾지 못했습니다: {BackPanelSpritePath}");

            GameObject panelObject = new GameObject("BackPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panelObject.transform.SetParent(root.transform, false);
            panelObject.transform.SetSiblingIndex(1);

            RectTransform rect = panelObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, -4f);
            rect.sizeDelta = new Vector2(1460f, 820f);

            Image image = panelObject.GetComponent<Image>();
            image.sprite = panelSprite;
            image.preserveAspect = true;
            image.color = new Color(0.74f, 0.82f, 0.86f, 0.72f);
            image.raycastTarget = false;
        }
    }
}
#endif

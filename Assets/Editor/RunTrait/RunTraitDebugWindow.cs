using System;
using NeoSurvive.Characters;
using NeoSurvive.Weapon;
using UnityEditor;
using UnityEngine;

/// <summary>26-10-22 Xabuna: 씬/프리팹 연결 없이 현재 런의 특성을 직접 획득하는 Editor 전용 도구.</summary>
public class RunTraitDebugWindow : EditorWindow
{
    private const string DefaultCatalogPath = "Assets/Data/RunTraitCatalog.asset";
    private static readonly string[] TierFilters = { "모든 등급", "실버", "골드", "플래티넘" };
    [SerializeField] private RunTraitCatalog fallbackCatalog;
    [SerializeField] private string search = "";
    [SerializeField] private int tierFilter;
    [SerializeField] private bool currentCharacterOnly = true;
    private Vector2 scroll;
    private string feedback;
    private MessageType feedbackType;

    [MenuItem("Tools/NeoSurvive/특성 테스트")]
    public static void Open()
    {
        var window = GetWindow<RunTraitDebugWindow>("특성 테스트");
        window.minSize = new Vector2(460f, 420f);
        window.Show();
    }

    private void OnEnable()
    {
        if (fallbackCatalog == null)
            fallbackCatalog = AssetDatabase.LoadAssetAtPath<RunTraitCatalog>(DefaultCatalogPath);
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private void OnDisable() => EditorApplication.playModeStateChanged -= OnPlayModeChanged;
    private void OnInspectorUpdate() => Repaint();
    private void OnPlayModeChanged(PlayModeStateChange state)
    {
        feedback = null;
        Repaint();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("런 특성 적용 / 제거", EditorStyles.boldLabel);
        bool playing = EditorApplication.isPlaying && !EditorApplication.isCompiling;
        Player player = playing ? Player.Instance : null;
        RunTraitManager manager = playing ? RunTraitManager.Instance : null;
        RunTraitCatalog activeCatalog = ReadCatalog(manager);
        using (new EditorGUI.DisabledScope(activeCatalog != null))
        {
            var selected = (RunTraitCatalog)EditorGUILayout.ObjectField("특성 카탈로그",
                activeCatalog != null ? activeCatalog : fallbackCatalog, typeof(RunTraitCatalog), false);
            if (activeCatalog == null) fallbackCatalog = selected;
        }
        RunTraitCatalog catalog = activeCatalog != null ? activeCatalog : fallbackCatalog;

        if (!playing) EditorGUILayout.HelpBox("Play Mode를 실행하면 현재 플레이어에게 특성을 적용할 수 있습니다.", MessageType.Info);
        else if (player == null) EditorGUILayout.HelpBox("플레이어 생성을 기다리는 중입니다.", MessageType.Info);
        else EditorGUILayout.LabelField("현재 캐릭터", player.CharacterType == CharacterType.Hacker ? "해커" : "사이보그");
        if (player != null && player.IsDead)
            EditorGUILayout.HelpBox("플레이어가 사망했습니다. 새 플레이를 시작하면 특성을 적용할 수 있습니다.", MessageType.Info);

        bool selectionOpen = playing && SelectionIsOpen();
        if (selectionOpen) EditorGUILayout.HelpBox("자판기의 특성 선택 창을 닫으면 직접 적용할 수 있습니다.", MessageType.Info);
        bool characterMatches = manager == null || !manager.IsInitialized || player == null
            || ReadCharacter(manager) == player.CharacterType;
        if (!characterMatches) EditorGUILayout.HelpBox("플레이어와 런의 캐릭터가 다릅니다. Play Mode를 다시 시작하세요.", MessageType.Warning);

        search = EditorGUILayout.TextField("이름 / ID 검색", search);
        tierFilter = EditorGUILayout.Popup("등급", tierFilter, TierFilters);
        currentCharacterOnly = EditorGUILayout.ToggleLeft("현재 캐릭터의 특성만 표시", currentCharacterOnly);
        EditorGUILayout.LabelField("+1: 한 단계 획득  |  최대: 최대 중첩  |  -1: 한 단계 제거", EditorStyles.wordWrappedMiniLabel);

        if (catalog == null || catalog.traits == null)
        {
            EditorGUILayout.HelpBox("RunTraitCatalog 에셋을 연결하세요.", MessageType.Warning);
            return;
        }

        bool canApply = playing && player != null && !player.IsDead && !selectionOpen && characterMatches;
        using (new EditorGUI.DisabledScope(!canApply || manager == null || !manager.IsInitialized))
        {
            if (GUILayout.Button("획득한 특성 전체 초기화"))
            {
                int removed = RunTraitDebugActions.Reset(manager);
                feedback = removed > 0 ? $"특성 {removed}중첩을 모두 초기화했습니다." : "초기화할 특성이 없습니다.";
                feedbackType = MessageType.Info;
                Debug.Log($"[RunTraitDebug] {feedback}", player);
            }
        }
        int shown = 0;
        string query = (search ?? "").Trim();
        scroll = EditorGUILayout.BeginScrollView(scroll);
        foreach (RunTraitDefinition trait in catalog.traits)
        {
            if (trait == null || string.IsNullOrEmpty(trait.traitId)) continue;
            bool eligible = player != null && (trait.isCommonTrait || trait.targetCharacter == player.CharacterType);
            if (currentCharacterOnly && player != null && !eligible) continue;
            if (tierFilter > 0 && (int)trait.tier != tierFilter - 1) continue;
            if (query.Length > 0 && !Contains(trait.displayName, query) && !Contains(trait.traitId, query)) continue;
            shown++;
            int level = manager != null ? manager.GetTraitLevel(trait.traitId) : 0;
            int max = trait.tier == RunTraitTier.Platinum ? 1 : Mathf.Max(1, trait.maxStacks);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(string.IsNullOrEmpty(trait.displayName) ? trait.traitId : trait.displayName,
                        EditorStyles.boldLabel);
                    GUILayout.Label($"Lv.{level}/{max}", GUILayout.Width(64f));
                    using (new EditorGUI.DisabledScope(!canApply || !eligible || level <= 0))
                    {
                        if (GUILayout.Button("-1", GUILayout.Width(38f))) Remove(player, manager, trait);
                    }
                    using (new EditorGUI.DisabledScope(!canApply || !eligible || level >= max))
                    {
                        if (GUILayout.Button("+1", GUILayout.Width(44f))) Apply(player, catalog, trait, false);
                        if (GUILayout.Button("최대", GUILayout.Width(44f))) Apply(player, catalog, trait, true);
                    }
                }
                string tier = trait.tier == RunTraitTier.Silver ? "실버" : trait.tier == RunTraitTier.Gold ? "골드" : "플래티넘";
                EditorGUILayout.LabelField($"{tier} · {(trait.requiresWeaponTag ? TagName(trait.requiredTag) : "공통")} · {trait.traitId}",
                    EditorStyles.miniLabel);
                if (!string.IsNullOrEmpty(trait.description))
                    EditorGUILayout.LabelField(trait.description, EditorStyles.wordWrappedMiniLabel);
            }
        }
        EditorGUILayout.EndScrollView();
        if (shown == 0) EditorGUILayout.HelpBox("검색 조건에 맞는 특성이 없습니다.", MessageType.Info);
        if (!string.IsNullOrEmpty(feedback)) EditorGUILayout.HelpBox(feedback, feedbackType);
        EditorGUILayout.LabelField("획득 상태는 이번 런에 적용됩니다. 관련 무기를 보유해야 효과를 확인할 수 있습니다.", EditorStyles.wordWrappedMiniLabel);
        EditorGUILayout.LabelField("제거 시 특성 보호막·추가 분신은 정리됩니다. 이미 적중한 피해와 걸린 상태이상은 되돌리지 않습니다. 설치 시 정해지는 수치는 재설치 후 비교하세요.", EditorStyles.wordWrappedMiniLabel);
    }

    private void Remove(Player player, RunTraitManager manager, RunTraitDefinition trait)
    {
        if (!EditorApplication.isPlaying || player == null || player.IsDead || SelectionIsOpen()) return;
        bool removed = RunTraitDebugActions.Remove(manager, trait);
        feedbackType = removed ? MessageType.Info : MessageType.Warning;
        feedback = removed ? $"{trait.displayName}: -1, 현재 Lv.{manager.GetTraitLevel(trait.traitId)}"
            : "제거할 특성이 없습니다.";
        if (removed) Debug.Log($"[RunTraitDebug] {feedback}", player);
        Repaint();
    }

    private void Apply(Player player, RunTraitCatalog catalog, RunTraitDefinition trait, bool fillToMax)
    {
        if (!EditorApplication.isPlaying || player == null || player.IsDead || SelectionIsOpen()) return;
        RunTraitManager manager = RunTraitManager.Instance;
        if (manager == null) manager = player.gameObject.AddComponent<RunTraitManager>();
        if (!manager.EnsureInitialized(player.CharacterType, catalog))
        {
            feedback = "런 특성 관리자를 초기화하지 못했습니다. 카탈로그를 확인하세요.";
            feedbackType = MessageType.Error;
            return;
        }
        int added = RunTraitDebugActions.Acquire(manager, trait, fillToMax);
        feedbackType = added > 0 ? MessageType.Info : MessageType.Warning;
        feedback = added > 0 ? $"{trait.displayName}: +{added}, 현재 Lv.{manager.GetTraitLevel(trait.traitId)}"
            : "획득하지 못했습니다. 중첩 제한과 현재 런 카탈로그를 확인하세요.";
        if (added > 0) Debug.Log($"[RunTraitDebug] {feedback}", player);
        Repaint();
    }

    private static RunTraitCatalog ReadCatalog(RunTraitManager manager)
    {
        if (manager == null) return null;
        using (var serialized = new SerializedObject(manager))
            return serialized.FindProperty("catalog")?.objectReferenceValue as RunTraitCatalog;
    }

    private static CharacterType ReadCharacter(RunTraitManager manager)
    {
        using (var serialized = new SerializedObject(manager))
            return (CharacterType)(serialized.FindProperty("characterType")?.enumValueIndex ?? 0);
    }

    private static bool SelectionIsOpen()
    {
        foreach (var ui in UnityEngine.Object.FindObjectsOfType<TraitChoiceUI>(true))
            if (ui.IsOpen) return true;
        return false;
    }

    private static bool Contains(string text, string query) => text != null
        && text.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;

    private static string TagName(SynergyTag tag)
    {
        switch (tag)
        {
            case SynergyTag.Shooting: return "사격";
            case SynergyTag.Area: return "광역";
            case SynergyTag.Disruption: return "교란";
            case SynergyTag.Amplification: return "증폭";
            case SynergyTag.Deployment: return "설치";
            case SynergyTag.Melee: return "근접";
            case SynergyTag.Ranged: return "원거리";
            case SynergyTag.Physical: return "물리";
            case SynergyTag.Energy: return "에너지";
            case SynergyTag.ForceField: return "역장";
            default: return tag.ToString();
        }
    }
}

using System.Collections.Generic;
using NeoSurvive.Characters;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

/// <summary>
/// 가까이서 E를 누르면 런 특성 후보를 최대 3개 제시하고 하나를 획득합니다.
/// TraitChoiceUI에 특성 후보를 표시합니다.
/// RunTraitManager에 획득 상태를 보관하며 골드나 영구 저장을 사용하지 않습니다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(HackInteractPromptAnchor))]
public class TraitVendingMachine : MonoBehaviour, IHackPromptProvider
{
    [Header("상호작용")]
    [SerializeField, Min(0.1f)] private float interactRange = 3f;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private bool singleUse = true;
    [SerializeField] private Player player;

    [Header("특성 후보")]
    [SerializeField] private RunTraitCatalog traitCatalog;
    [SerializeField] private RunTraitManager runTraitManager;

    [Header("선택 UI 연결")]
    [Tooltip("씬에 배치한 선택 UI. 비워두면 아래 프리팹으로 생성합니다.")]
    [SerializeField] private TraitChoiceUI choiceUI;
    [Tooltip("TraitChoicePanel 프리팹의 TraitChoiceUI를 연결하세요.")]
    [SerializeField] private TraitChoiceUI choiceUIPrefab;
    [SerializeField] private Canvas uiCanvas;
    [Tooltip("기존 선택 UI에 후보를 표시한 뒤 호출됩니다. 추가 연출을 연결하세요.")]
    [SerializeField] private UnityEvent onChoicesOpened = new UnityEvent();
    [SerializeField] private UnityEvent onChoicesClosed = new UnityEvent();
    [FormerlySerializedAs("onTraitPurchased")]
    [SerializeField] private UnityEvent<string> onTraitAcquired = new UnityEvent<string>();
    [FormerlySerializedAs("onPurchaseFailed")]
    [Tooltip("중첩 제한 등으로 획득에 실패하면 호출됩니다. 선택 UI는 유지됩니다.")]
    [SerializeField] private UnityEvent onAcquireFailed = new UnityEvent();

    private const int ChoiceCount = 3;
    private readonly List<RunTraitDefinition> choices = new List<RunTraitDefinition>(ChoiceCount);
    private IReadOnlyList<RunTraitDefinition> readOnlyChoices;
    private static TraitVendingMachine activeMachine;
    private bool isSelecting;
    private bool isUsed;
    private bool isAcquiring;
    private bool ownsChoiceUI;
    private float nextPlayerSearchTime;

    private void Start()
    {
        if (choiceUI == null)
            choiceUI = FindObjectOfType<TraitChoiceUI>(true);
    }

    public IReadOnlyList<RunTraitDefinition> CurrentChoices =>
        readOnlyChoices ??= choices.AsReadOnly();
    public bool IsSelecting => isSelecting;
    public Transform PromptAnchorRoot => transform;
    public float HackInteractRange => interactRange;
    public bool IsHackInteractionComplete => singleUse && isUsed;
    public bool IsHackInteractionEnabled => isActiveAndEnabled
        && !IsHackInteractionComplete && !isSelecting
        && activeMachine == null && Time.timeScale > 0f
        && (choiceUI != null ? !choiceUI.IsOpen : choiceUIPrefab != null)
        && (HackingSystem.Instance == null || !HackingSystem.Instance.IsHacking);
    public bool ShouldShowHackPrompt => IsHackInteractionEnabled && IsPlayerInRange();

    private void Update()
    {
        // 플레이어가 나중에 생성되는 씬도 지원하되 매 프레임 검색하지 않습니다.
        if (player == null && Time.unscaledTime >= nextPlayerSearchTime)
        {
            player = HackPromptBoundsUtility.FindPlayer();
            nextPlayerSearchTime = Time.unscaledTime + 1f;
        }

        if (isSelecting)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                CancelSelection();
            return;
        }

        if (Input.GetKeyDown(interactKey) && ShouldShowHackPrompt)
            OpenChoices();
    }

    public void OpenChoices()
    {
        if (!ShouldShowHackPrompt)
            return;

        if (runTraitManager == null)
        {
            runTraitManager = RunTraitManager.Instance;
            if (runTraitManager == null && traitCatalog != null)
                runTraitManager = player.gameObject.AddComponent<RunTraitManager>();
        }
        if (runTraitManager == null || !runTraitManager.EnsureInitialized(player.CharacterType, traitCatalog))
        {
            Debug.LogWarning("[TraitVendingMachine] RunTraitCatalog를 연결하세요.", this);
            return;
        }

        choices.Clear();
        choices.AddRange(runTraitManager.GenerateOffers(ChoiceCount));

        if (choices.Count == 0)
        {
            Debug.Log("[TraitVendingMachine] 선택 가능한 특성이 없습니다.", this);
            return;
        }

        if (!EnsureChoiceUI())
        {
            choices.Clear();
            return;
        }

        activeMachine = this;
        isSelecting = true;
        if (!choiceUI.Show(CurrentChoices, GetTraitLevelText,
            TrySelectChoice, CancelSelection))
        {
            CancelSelection();
            Debug.LogWarning("[TraitVendingMachine] 선택 UI의 패널과 카드 참조를 확인하세요.", this);
            return;
        }
        onChoicesOpened.Invoke();
    }

    /// <summary>외부에서 후보를 선택할 때 사용합니다. 공유 UI 버튼은 자동 연결됩니다.</summary>
    public void SelectChoice(int index)
    {
        TrySelectChoice(index);
    }

    private bool TrySelectChoice(int index)
    {
        if (!isActiveAndEnabled || !isSelecting || isAcquiring
            || index < 0 || index >= choices.Count || runTraitManager == null)
            return false;

        string traitId = choices[index].traitId;
        isAcquiring = true;
        bool acquired;
        try
        {
            acquired = runTraitManager.TryAcquire(choices[index]);
        }
        finally
        {
            isAcquiring = false;
        }

        if (!acquired)
        {
            onAcquireFailed.Invoke();
            return false;
        }

        isUsed = true;
        CancelSelection();
        onTraitAcquired.Invoke(traitId);
        return true;
    }

    public void CancelSelection()
    {
        if (!isSelecting)
            return;

        isSelecting = false;
        if (activeMachine == this)
            activeMachine = null;
        if (choiceUI != null && choiceUI.IsOpen)
            choiceUI.Hide();
        choices.Clear();
        onChoicesClosed.Invoke();
    }

    private bool EnsureChoiceUI()
    {
        if (choiceUI != null && choiceUI.gameObject.scene.IsValid())
        {
            // 씬 루트에 배치된 UI도 Canvas 아래에서 표시되도록 합니다.
            if (choiceUI.GetComponentInParent<Canvas>() == null)
            {
                if (uiCanvas == null)
                    uiCanvas = FindObjectOfType<Canvas>();
                if (uiCanvas == null)
                    return false;
                choiceUI.transform.SetParent(uiCanvas.transform, false);
                if (choiceUI.transform is RectTransform rect)
                    rect.anchoredPosition = Vector2.zero;
            }
            return true;
        }
        TraitChoiceUI prefab = choiceUI != null ? choiceUI : choiceUIPrefab;
        if (uiCanvas == null)
            uiCanvas = FindObjectOfType<Canvas>();
        if (prefab == null || uiCanvas == null)
        {
            Debug.LogWarning("[TraitVendingMachine] 선택 UI 프리팹과 Canvas를 확인하세요.", this);
            return false;
        }
        choiceUI = Instantiate(prefab, uiCanvas.transform, false);
        choiceUI.gameObject.SetActive(false);
        ownsChoiceUI = true;
        return true;
    }

    private string GetTraitLevelText(RunTraitDefinition trait)
    {
        int level = runTraitManager.GetTraitLevel(trait.traitId);
        return $"{trait.tier}  |  Lv.{level} → Lv.{level + 1}";
    }

    private bool IsPlayerInRange()
    {
        return player != null
            && ((Vector2)(player.transform.position - transform.position)).sqrMagnitude
                <= interactRange * interactRange;
    }

    private void OnDisable()
    {
        CancelSelection();
    }

    private void OnDestroy()
    {
        if (ownsChoiceUI && choiceUI != null)
            Destroy(choiceUI.gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}

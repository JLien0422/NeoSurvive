using UnityEngine;

/// <summary>
/// HackingSystem의 프롬프트 프리팹을 붙여 해킹 가능 오브젝트 우상단에 E 버튼을 표시합니다.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(100)]
public class HackInteractPromptAnchor : MonoBehaviour
{
    private IHackPromptProvider provider;
    private GameObject promptInstance;
    private Transform promptTransform;
    private SpriteRenderer promptSprite;
    private Canvas promptCanvas;

    private void Start()
    {
        ResolveProvider();
        CreatePromptIfNeeded();
        UpdatePrompt();
    }

    private void LateUpdate()
    {
        if (provider == null)
            ResolveProvider();

        if (promptInstance == null)
            CreatePromptIfNeeded();

        if (provider == null || promptInstance == null)
            return;

        UpdatePrompt();
    }

    private void OnDestroy()
    {
        if (promptInstance != null)
            Destroy(promptInstance);
    }

    private void ResolveProvider()
    {
        IHackPromptProvider preferred = null;
        IHackPromptProvider fallback = null;

        MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour is not IHackPromptProvider candidate)
                continue;

            if (behaviour is HackableObject)
            {
                fallback ??= candidate;
                continue;
            }

            preferred = candidate;
            break;
        }

        provider = preferred ?? fallback;
    }

    private void CreatePromptIfNeeded()
    {
        if (provider == null || promptInstance != null || HackingSystem.Instance == null)
            return;

        promptInstance = HackingSystem.Instance.InstantiateHackInteractPrompt();
        if (promptInstance == null)
            return;

        promptSprite = promptInstance.GetComponent<SpriteRenderer>();
        promptCanvas = promptInstance.GetComponentInChildren<Canvas>(true);
        promptTransform = promptSprite != null
            ? promptSprite.transform
            : promptCanvas != null
                ? promptCanvas.transform
                : promptInstance.transform;

        if (promptSprite != null)
        {
            HackingSystem.Instance.ApplyPromptHierarchySorting(
                promptInstance,
                provider.PromptAnchorRoot
            );
        }
        else if (promptCanvas != null)
        {
            HackingSystem.Instance.ApplyPromptCanvasSorting(
                promptCanvas,
                provider.PromptAnchorRoot
            );
        }

        promptInstance.SetActive(false);
    }

    private void UpdatePrompt()
    {
        bool show =
            provider.IsHackInteractionEnabled
            && !provider.IsHackInteractionComplete
            && provider.ShouldShowHackPrompt;

        promptInstance.SetActive(show);
        if (!show || promptTransform == null)
            return;

        Vector2 offset = HackingSystem.Instance != null
            ? HackingSystem.Instance.PromptWorldOffset
            : Vector2.zero;

        Vector3 position = HackPromptBoundsUtility.GetTopRightWorld(
            provider.PromptAnchorRoot,
            offset
        );

        float depthOffset = HackingSystem.Instance != null
            ? HackingSystem.Instance.PromptDepthOffset
            : 0f;

        position.z = provider.PromptAnchorRoot.position.z + depthOffset;
        promptTransform.position = position;

        if (promptSprite != null && HackingSystem.Instance != null)
        {
            float scale = HackingSystem.Instance.PromptWorldScale;
            promptSprite.transform.localScale = Vector3.one * scale;
        }
    }
}

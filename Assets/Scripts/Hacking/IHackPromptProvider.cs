using UnityEngine;

/// <summary>
/// 해킹 상호작용 E 버튼 프롬프트 표시 여부·범위를 제공합니다.
/// </summary>
public interface IHackPromptProvider
{
    Transform PromptAnchorRoot { get; }

    /// <summary>실제 E키 해킹이 가능한 거리(월드 유닛).</summary>
    float HackInteractRange { get; }

    /// <summary>해킹 완료 등으로 프롬프트를 더 이상 표시하지 않을 때 true.</summary>
    bool IsHackInteractionComplete { get; }

    /// <summary>해킹이 비활성화된 경우(기믹 설정 등).</summary>
    bool IsHackInteractionEnabled { get; }

    /// <summary>상호작용 가능 범위 안에서 E 버튼을 표시할지 (실제 E키와 동일 조건).</summary>
    bool ShouldShowHackPrompt { get; }
}

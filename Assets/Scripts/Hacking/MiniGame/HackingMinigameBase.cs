using UnityEngine;

/// <summary>
/// 해킹 미니게임 기본 클래스
/// 모든 미니게임은 이 클래스를 상속받아 구현합니다.
/// </summary>
public abstract class HackingMinigameBase : MonoBehaviour
{
    protected System.Action onSuccess;
    protected System.Action onFailure;

    /// <summary>
    /// 미니게임 초기화
    /// </summary>
    public virtual void Initialize(System.Action onSuccessCallback, System.Action onFailureCallback)
    {
        onSuccess = onSuccessCallback;
        onFailure = onFailureCallback;
    }

    /// <summary>
    /// 미니게임 성공
    /// </summary>
    public virtual void OnSuccess()
    {
        onSuccess?.Invoke();
    }

    /// <summary>
    /// 미니게임 실패
    /// </summary>
    public virtual void OnFailure()
    {
        onFailure?.Invoke();
    }
}

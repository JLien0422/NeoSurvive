using System.Collections;
using UnityEngine;

/// <summary>
/// 보스 캐릭터 클래스. Character를 상속받아 기본 HP/피격 처리를 공유합니다.
/// </summary>
public class Boss : Character
{
    [Header("보스 이름 설정")]
    [Tooltip("HP바/클리어 화면에 표시될 보스 이름 (비워두면 프리팹 오브젝트 이름 사용)")]
    [SerializeField] private string bossNameForUI = "";

    [Header("사망 애니메이션")]
    [SerializeField] private string deathAnimationTrigger = "Map1Boss Die";
    [SerializeField] private float defeatEventDelay = 1f;

    // 보스 처치 시 발생하는 이벤트 (GameClearUI 등에서 구독)
    public static event System.Action OnBossDefeated;

    private Animator bossAnimator;

    /// <summary>
    /// UI에 표시할 보스 이름을 반환합니다.
    /// </summary>
    public string GetBossDisplayName()
    {
        if (!string.IsNullOrWhiteSpace(bossNameForUI))
            return bossNameForUI;

        return gameObject.name;
    }

    protected override void Awake()
    {
        base.Awake();
        bossAnimator = GetComponentInChildren<Animator>(true);
    }

    /// <summary>
    /// 보스 사망 처리 - 게임 클리어 이벤트를 발생시킵니다.
    /// </summary>
    protected override void Die()
    {
        if (IsDead) return;

        base.Die();

        // 로그에 표시할 이름은 UI 표시 이름 기준으로 통일합니다.
        Debug.Log($"[Boss] 보스 {GetBossDisplayName()} 처치! 게임 클리어!");

        // 킬 카운트 증가
        if (GameManager.Instance != null)
            GameManager.Instance.AddKill();
        GameAnalyticsTracker.TrackBossDefeated(this);

        PlayDeathAnimation();
        StartCoroutine(CompleteDefeatAfterAnimation());
    }

    private IEnumerator CompleteDefeatAfterAnimation()
    {
        if (defeatEventDelay > 0f)
            yield return new WaitForSeconds(defeatEventDelay);

        OnBossDefeated?.Invoke();
        Destroy(gameObject);
    }

    private void PlayDeathAnimation()
    {
        if (bossAnimator == null || string.IsNullOrEmpty(deathAnimationTrigger))
            return;

        if (!HasTriggerParameter(deathAnimationTrigger))
            return;

        bossAnimator.ResetTrigger(deathAnimationTrigger);
        bossAnimator.SetTrigger(deathAnimationTrigger);
    }

    private bool HasTriggerParameter(string triggerName)
    {
        foreach (AnimatorControllerParameter parameter in bossAnimator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Trigger &&
                parameter.name == triggerName)
            {
                return true;
            }
        }

        return false;
    }
}

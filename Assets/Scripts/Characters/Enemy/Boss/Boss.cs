using UnityEngine;

/// <summary>
/// 보스 캐릭터 클래스. Character를 상속받아 기본 HP/피격 처리를 공유합니다.
/// </summary>
public class Boss : Character
{
    [Header("보스 이름 설정")]
    [Tooltip("HP바/클리어 화면에 표시될 보스 이름 (비워두면 프리팹 오브젝트 이름 사용)")]
    [SerializeField] private string bossNameForUI = "";

    // 보스 처치 시 발생하는 이벤트 (GameClearUI 등에서 구독)
    public static event System.Action OnBossDefeated;

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

        // 게임 클리어 이벤트 발생 → GameClearUI가 구독해서 처리
        OnBossDefeated?.Invoke();

        // 보스 오브젝트 제거
        Destroy(gameObject);
    }
}

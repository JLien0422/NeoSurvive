using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 화면 하단에 보스 HP바를 표시합니다.
/// Boss.OnBossDefeated 이벤트를 구독해 보스 사망 시 자동으로 숨깁니다.
/// </summary>
public class BossHealthBar : MonoBehaviour
{
    [Header("UI 참조")]
    [Tooltip("보스 HP 슬라이더")]
    [SerializeField] private Slider hpSlider;

    [Tooltip("보스 이름 텍스트")]
    [SerializeField] private TextMeshProUGUI bossNameText;

    [Tooltip("HP 수치 텍스트 (예: 1500 / 2000)")]
    [SerializeField] private TextMeshProUGUI hpValueText;

    // 현재 연결된 보스
    private Boss currentBoss;

    private void Awake()
    {
        // 비활성화는 인스펙터에서 직접 설정 - Awake에서 끄면 SetBoss() 활성화 시 재실행되어 꺼짐
    }

    private void OnEnable()
    {
        Boss.OnBossDefeated += HideBossHealthBar;
    }

    private void OnDisable()
    {
        Boss.OnBossDefeated -= HideBossHealthBar;
    }

    private void OnDestroy()
    {
        Boss.OnBossDefeated -= HideBossHealthBar;
    }

    /// <summary>
    /// 보스 스폰 시 호출 - 보스 HP바를 보스에 연결하고 표시합니다.
    /// BossSpawner에서 보스 생성 후 호출합니다.
    /// </summary>
    public void SetBoss(Boss boss)
    {
        if (boss == null) return;

        // 이전 보스 구독 해제
        if (currentBoss != null)
            currentBoss.CurrentHP.onValueChanged -= UpdateHP;

        currentBoss = boss;

        // HP 변경 이벤트 구독
        currentBoss.CurrentHP.onValueChanged += UpdateHP;

        // 초기 HP 설정
        float maxHP = currentBoss.MaxHP.GetValue();
        float curHP = currentBoss.CurrentHP.CurrentValue;

        if (hpSlider != null)
        {
            hpSlider.maxValue = maxHP;
            hpSlider.value = curHP;
        }

        // 보스 이름 표시: Boss에서 지정한 이름(bossNameForUI)을 우선 사용
        if (bossNameText != null)
            bossNameText.text = boss.GetBossDisplayName();

        UpdateHPValueText(curHP, maxHP);

        gameObject.SetActive(true);
    }

    /// <summary>
    /// HP 변경 시 슬라이더 및 텍스트 업데이트
    /// </summary>
    private void UpdateHP(float current, float max)
    {
        if (hpSlider != null)
        {
            hpSlider.maxValue = max;
            hpSlider.value = current;
        }

        UpdateHPValueText(current, max);
    }

    private void UpdateHPValueText(float current, float max)
    {
        if (hpValueText != null)
            hpValueText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
    }

    /// <summary>
    /// 보스 처치 시 HP바 숨김
    /// </summary>
    private void HideBossHealthBar()
    {
        if (currentBoss != null)
        {
            currentBoss.CurrentHP.onValueChanged -= UpdateHP;
            currentBoss = null;
        }

        gameObject.SetActive(false);
    }

}

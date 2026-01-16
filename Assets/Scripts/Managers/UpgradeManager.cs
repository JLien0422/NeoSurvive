using UnityEngine;
using System.Collections.Generic;

// UpgradeManager 클래스는 플레이어의 영구적인 능력치 업그레이드를 관리합니다.
// 이 Manager도 싱글톤 패턴으로 구현하여 어디서든 쉽게 접근할 수 있게 합니다.
public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance { get; private set; }

    // 업그레이드 레벨 저장 키 (Easy Save용)
    private const string UPGRADE_HEALTH_LEVEL_KEY = "HealthUpgradeLevel";
    private const string UPGRADE_DAMAGE_LEVEL_KEY = "DamageUpgradeLevel";
    private const string UPGRADE_MOVESPEED_LEVEL_KEY = "MoveSpeedUpgradeLevel";

    // 모든 업그레이드의 고정 비용
    private const int UPGRADE_COST = 100;

    [Header("업그레이드 정보")]
    [SerializeField] private int healthUpgradeLevel = 0;
    [SerializeField] private int damageUpgradeLevel = 0;
    [SerializeField] private int moveSpeedUpgradeLevel = 0;

    [Header("업그레이드 스탯 보너스 (레벨당)")]
    [SerializeField] private float healthPerLevel = 10f; // 레벨당 체력 증가량
    [SerializeField] private float damagePerLevel = 2f;  // 레벨당 공격력 증가량
    [SerializeField] private float moveSpeedPerLevel = 0.5f; // 레벨당 이동속도 증가량

    // 현재 총 골드를 UI 등에서 쉽게 참조할 수 있도록 public 프로퍼티로 노출
    // GameManager 인스턴스가 없을 경우를 대비하여 null 체크 추가
    public int TotalGold => GameManager.Instance != null ? GameManager.Instance.TotalGold : 0;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadUpgrades(); // 저장된 업그레이드 레벨 불러오기
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 모든 업그레이드 레벨을 불러옵니다.
    private void LoadUpgrades()
    {
        healthUpgradeLevel = ES3.Load(UPGRADE_HEALTH_LEVEL_KEY, 0);
        damageUpgradeLevel = ES3.Load(UPGRADE_DAMAGE_LEVEL_KEY, 0);
        moveSpeedUpgradeLevel = ES3.Load(UPGRADE_MOVESPEED_LEVEL_KEY, 0);
        Debug.Log($"업그레이드 레벨 불러오기: 체력 {healthUpgradeLevel}, 공격력 {damageUpgradeLevel}, 이동속도 {moveSpeedUpgradeLevel}");
    }

    // 모든 업그레이드 레벨을 저장합니다.
    private void SaveUpgrades()
    {
        ES3.Save(UPGRADE_HEALTH_LEVEL_KEY, healthUpgradeLevel);
        ES3.Save(UPGRADE_DAMAGE_LEVEL_KEY, damageUpgradeLevel);
        ES3.Save(UPGRADE_MOVESPEED_LEVEL_KEY, moveSpeedUpgradeLevel);
        Debug.Log($"업그레이드 레벨 저장 완료.");
    }

    // ===================================================
    // 체력 업그레이드
    // ===================================================
    public bool BuyHealthUpgrade()
    {
        if (TotalGold >= UPGRADE_COST)
        {
            GameManager.Instance.AddGold(-UPGRADE_COST); // 골드 차감
            healthUpgradeLevel++;
            SaveUpgrades(); // 업그레이드 레벨 저장
            Debug.Log($"체력 업그레이드 완료! 레벨: {healthUpgradeLevel}, 남은 골드: {TotalGold}");
            return true;
        }
        Debug.Log($"체력 업그레이드 실패: 골드 부족! 필요 골드: {UPGRADE_COST}, 현재 골드: {TotalGold}");
        return false;
    }

    public int GetHealthUpgradeLevel() => healthUpgradeLevel;
    public float GetHealthUpgradeBonus() => healthUpgradeLevel * healthPerLevel;
    public int GetHealthUpgradeCost() => UPGRADE_COST;

    // ===================================================
    // 공격력 업그레이드
    // ===================================================
    public bool BuyDamageUpgrade()
    {
        if (TotalGold >= UPGRADE_COST)
        {
            GameManager.Instance.AddGold(-UPGRADE_COST); // 골드 차감
            damageUpgradeLevel++;
            SaveUpgrades();
            Debug.Log($"공격력 업그레이드 완료! 레벨: {damageUpgradeLevel}, 남은 골드: {TotalGold}");
            return true;
        }
        Debug.Log($"공격력 업그레이드 실패: 골드 부족! 필요 골드: {UPGRADE_COST}, 현재 골드: {TotalGold}");
        return false;
    }

    public int GetDamageUpgradeLevel() => damageUpgradeLevel;
    public float GetDamageUpgradeBonus() => damageUpgradeLevel * damagePerLevel;
    public int GetDamageUpgradeCost() => UPGRADE_COST;

    // ===================================================
    // 이동속도 업그레이드
    // ===================================================
    public bool BuyMoveSpeedUpgrade()
    {
        if (TotalGold >= UPGRADE_COST)
        {
            GameManager.Instance.AddGold(-UPGRADE_COST); // 골드 차감
            moveSpeedUpgradeLevel++;
            SaveUpgrades();
            Debug.Log($"이동속도 업그레이드 완료! 레벨: {moveSpeedUpgradeLevel}, 남은 골드: {TotalGold}");
            return true;
        }
        Debug.Log($"이동속도 업그레이드 실패: 골드 부족! 필요 골드: {UPGRADE_COST}, 현재 골드: {TotalGold}");
        return false;
    }

    public int GetMoveSpeedUpgradeLevel() => moveSpeedUpgradeLevel;
    public float GetMoveSpeedUpgradeBonus() => moveSpeedUpgradeLevel * moveSpeedPerLevel;
    public int GetMoveSpeedUpgradeCost() => UPGRADE_COST;

    public void OpenUpgradeWindow()
    {
        Debug.Log("업그레이드 창 열기");
    }
}

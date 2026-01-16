using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 레벨업 보상 상자
/// - 플레이어가 근처에 1.5초 머무르면 자동으로 열림
/// - 3개의 보상 중 1개 선택
/// </summary>
public class RewardChest : MonoBehaviour
{
    [Header("Pick (Stay to absorb)")]
    [SerializeField] private float pickDelay = 1.5f; // ★ 고정 머무는 시간 (1.5초)

    private float stayTimer = 0f;     // 플레이어 머문 시간
    private bool opened = false;

    private List<RewardItemDefinition> rewardOptions;
    private RewardSelectUI rewardUI;
    private Collider2D col;

    private void Awake()
    {
        col = GetComponent<Collider2D>();
    }

    /// <summary>
    /// 상자 생성 직후 호출
    /// </summary>
    public void Setup(
        List<RewardItemDefinition> options,
        RewardSelectUI ui)
    {
        rewardOptions = options;
        rewardUI = ui;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        stayTimer = 0f; // 들어오면 초기화
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (opened) return;
        if (!other.CompareTag("Player")) return;

        stayTimer += Time.deltaTime;

        if (stayTimer >= pickDelay)
        {
            Open();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        stayTimer = 0f; // 나가면 다시 0
    }

    private void Open()
    {
        if (opened) return;
        opened = true;

        // 중복 트리거 방지
        if (col != null)
            col.enabled = false;

        // 보상 선택 UI 표시
        if (rewardUI != null && rewardOptions != null && rewardOptions.Count == 3)
        {
            rewardUI.Show(rewardOptions, OnRewardSelected);
        }
        else
        {
            Debug.LogWarning("[RewardChest] 보상 UI 또는 옵션이 잘못됨");
            Destroy(gameObject);
        }
    }

    private void OnRewardSelected(RewardItemDefinition selected)
    {
        // TODO: 선택된 보상 효과 적용
        Debug.Log($"[RewardChest] 선택된 보상: {selected.displayName} ({selected.id})");

        // ExpOrb처럼 상자 제거
        Destroy(gameObject);
    }
}

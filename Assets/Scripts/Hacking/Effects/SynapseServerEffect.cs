using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NeoSurvive.Buff;

/// <summary>
/// 시냅스 과부하 서버 해킹 성공 효과
/// 화면 내 적의 절반을 즉시 해킹하여 아군으로 전환 → 나머지 적을 공격하게 만듭니다.
/// 해킹된 적은 StatusFlags.frenzy = true 상태가 되어 서로 싸웁니다.
/// </summary>
public class SynapseServerEffect : MonoBehaviour
{
    [Header("아군화 설정")]
    [SerializeField] private float frenzyDuration = 10f;   // 아군화(광란) 지속 시간 (초)

    /// <summary>
    /// 외부(HackableObject)에서 호출하여 효과를 시작합니다.
    /// </summary>
    public void Activate()
    {
        StartCoroutine(HackHalfEnemies());
    }

    /// <summary>
    /// 화면 내 적 절반을 광란 상태로 만드는 코루틴
    /// </summary>
    private IEnumerator HackHalfEnemies()
    {
        // 카메라 기준 화면 범위 계산
        Camera cam = Camera.main;
        if (cam == null) yield break;

        float worldHeight = cam.orthographicSize * 2f;
        float worldWidth  = worldHeight * cam.aspect;
        Vector3 screenCenter = cam.transform.position;

        // 화면 내 모든 적 검색
        Collider2D[] cols = Physics2D.OverlapBoxAll(screenCenter, new Vector2(worldWidth, worldHeight), 0f);

        // HashSet으로 중복 제거 (적 1마리에 콜라이더가 여러 개일 경우 중복 방지)
        HashSet<EnemyController> seen = new HashSet<EnemyController>();
        List<EnemyController> enemiesInScreen = new List<EnemyController>();
        foreach (var col in cols)
        {
            if (!col.CompareTag("Enemy")) continue;
            EnemyController ec = col.GetComponent<EnemyController>();
            if (ec != null && seen.Add(ec)) // Add가 false면 이미 추가된 것 → 중복 스킵
                enemiesInScreen.Add(ec);
        }

        if (enemiesInScreen.Count == 0) yield break;

        // 목록을 랜덤하게 섞은 뒤 절반 선택
        // CeilToInt로 올림 처리 → 3마리면 2마리, 5마리면 3마리 보장
        ShuffleList(enemiesInScreen);
        int hackCount = Mathf.CeilToInt(enemiesInScreen.Count / 2f);

        for (int i = 0; i < hackCount; i++)
        {
            EnemyController ec = enemiesInScreen[i];
            if (ec == null) continue;

            // StatusFlags.frenzy = true 로 광란 상태 부여
            // → EnemyController.UpdateTarget()에서 가장 가까운 적(Enemy)을 타겟으로 삼음
            StatusFlags flags = ec.GetComponent<StatusFlags>();
            if (flags == null) flags = ec.gameObject.AddComponent<StatusFlags>();
            flags.frenzy = true;

            // 시각적 표시: 해킹된 적을 녹색으로 변경
            SpriteRenderer sr = ec.GetComponentInChildren<SpriteRenderer>();
            Color originalColor = sr != null ? sr.color : Color.white;
            if (sr != null) sr.color = new Color(0f, 1f, 0.4f, 1f);

            // frenzyDuration 이후 광란 상태 + 색상 복원
            StartCoroutine(RemoveFrenzyAfterDelay(flags, sr, originalColor, frenzyDuration));
        }

        Debug.Log($"[SynapseServerEffect] {hackCount}마리 아군화 완료 (전체 {enemiesInScreen.Count}마리)");

        yield return null;
        Destroy(this);
    }

    /// <summary>
    /// 지정 시간 후 광란 상태 해제 + 스프라이트 색상 원복
    /// </summary>
    private IEnumerator RemoveFrenzyAfterDelay(StatusFlags flags, SpriteRenderer sr, Color originalColor, float delay)
    {
        yield return new WaitForSeconds(delay);

        // 광란 상태 해제
        if (flags != null)
            flags.frenzy = false;

        // 색상 원복
        if (sr != null)
            sr.color = originalColor;
    }

    /// <summary>
    /// 리스트를 Fisher-Yates 알고리즘으로 무작위 섞습니다.
    /// </summary>
    private void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }
}

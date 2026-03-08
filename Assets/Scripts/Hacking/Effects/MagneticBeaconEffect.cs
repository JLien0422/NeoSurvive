using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NeoSurvive.Buff;

/// <summary>
/// 마그네틱 비컨 해킹 성공 효과
/// 화면 내 모든 적을 우측(3시 방향) 끝으로 강제 견인하여 한곳에 몰아버립니다.
/// StatusFlags.isPulled 플래그를 사용해 EnemyController의 AI 이동을 우선 차단합니다.
/// </summary>
public class MagneticBeaconEffect : MonoBehaviour
{
    [Header("견인 설정")]
    [SerializeField] private float pullSpeed    = 20f;  // 적 견인 속도
    [SerializeField] private float pullDuration = 3f;   // 견인 지속 시간 (초)

    /// <summary>
    /// 외부(HackableObject)에서 호출하여 효과를 시작합니다.
    /// </summary>
    public void Activate()
    {
        StartCoroutine(PullAllEnemiesToRight());
    }

    /// <summary>
    /// 화면 내 모든 적을 우측 끝으로 강제 이동시키는 코루틴
    /// StatusFlags.isPulled = true로 설정하여 EnemyController.FixedUpdate()가
    /// AI 이동 대신 pullVelocity를 적용하도록 합니다.
    /// </summary>
    private IEnumerator PullAllEnemiesToRight()
    {
        // 우측 끝 좌표 계산 (카메라 기준 화면 오른쪽 밖)
        Camera cam = Camera.main;
        float rightEdgeX = cam != null
            ? cam.transform.position.x + cam.orthographicSize * cam.aspect + 2f
            : 20f;

        // 견인 시작 시점 스냅샷 + StatusFlags 수집
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        List<StatusFlags> pulledFlags = new List<StatusFlags>();

        foreach (GameObject enemy in enemies)
        {
            if (enemy == null) continue;
            StatusFlags flags = enemy.GetComponent<StatusFlags>();
            if (flags == null) flags = enemy.AddComponent<StatusFlags>();

            // 견인 플래그 ON → FixedUpdate가 pullVelocity를 사용하도록 신호 전송
            flags.isPulled = true;
            pulledFlags.Add(flags);
        }

        float elapsed = 0f;
        while (elapsed < pullDuration)
        {
            elapsed += Time.deltaTime;

            // 매 프레임 각 적의 pullVelocity를 갱신
            for (int i = 0; i < enemies.Length; i++)
            {
                GameObject enemy = enemies[i];
                if (enemy == null) continue;

                Vector2 targetPos = new Vector2(rightEdgeX, enemy.transform.position.y);
                Vector2 direction = (targetPos - (Vector2)enemy.transform.position).normalized;

                pulledFlags[i].pullVelocity = direction * pullSpeed;
            }

            yield return null;
        }

        // 견인 종료: 플래그 해제 및 속도 초기화
        foreach (StatusFlags flags in pulledFlags)
        {
            if (flags == null) continue;
            flags.isPulled      = false;
            flags.pullVelocity  = Vector2.zero;

            Rigidbody2D rb = flags.GetComponent<Rigidbody2D>();
            if (rb != null) rb.velocity = Vector2.zero;
        }

        Destroy(this);
    }
}

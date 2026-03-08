using System.Collections;
using UnityEngine;

/// <summary>
/// 새틀라이트 통신기 해킹 성공 효과
/// 가장 가까운 적을 자동 추적하는 레이저 빔이 하늘에서 내려와 연속 데미지를 줍니다.
/// 적이 없으면 플레이어 주변을 천천히 공전합니다.
/// </summary>
public class SatelliteUplinkEffect : MonoBehaviour
{
    [Header("레이저 설정")]
    [SerializeField] private float trackSpeed    = 300f;  // 적을 향해 회전하는 속도 (deg/s)
    [SerializeField] private float orbitRadius   = 4f;    // 플레이어 기준 레이저 도달 반경
    [SerializeField] private float damagePerHit  = 30f;   // 레이저 1회 타격 데미지
    [SerializeField] private float hitInterval   = 0.1f;  // 타격 판정 간격 (초)
    [SerializeField] private float duration      = 10f;   // 전체 지속 시간 (초)
    [SerializeField] private float laserWidth    = 0.5f;  // 레이저 타격 범위 반경

    [Header("비주얼")]
    [SerializeField] private Color laserColor = new Color(0f, 1f, 1f, 1f); // 청록 레이저

    /// <summary>
    /// 외부(HackableObject)에서 호출하여 효과를 시작합니다.
    /// </summary>
    public void Activate()
    {
        StartCoroutine(TrackingLaser());
    }

    /// <summary>
    /// 가장 가까운 적을 자동 추적하는 레이저 코루틴
    /// 적이 있으면 해당 방향으로 빠르게 회전하고, 없으면 느리게 공전합니다.
    /// </summary>
    private IEnumerator TrackingLaser()
    {
        Player player = FindObjectOfType<Player>();
        if (player == null) yield break;

        GameObject laserObj = CreateLaserVisual();
        LineRenderer lr = laserObj.GetComponent<LineRenderer>();

        float elapsed  = 0f;
        float angle    = 0f;   // 현재 레이저 방향 각도 (도)
        float hitTimer = 0f;

        while (elapsed < duration)
        {
            elapsed  += Time.deltaTime;
            hitTimer += Time.deltaTime;

            if (player == null) break;

            // 가장 가까운 적 찾기
            Transform nearestEnemy = FindNearestEnemy(player.transform.position);

            if (nearestEnemy != null)
            {
                // 적이 있으면 해당 방향 각도 계산 후 빠르게 추적
                Vector2 toEnemy = (Vector2)(nearestEnemy.position - player.transform.position);
                float targetAngle = Mathf.Atan2(toEnemy.y, toEnemy.x) * Mathf.Rad2Deg;

                // 현재 각도에서 목표 각도로 trackSpeed로 부드럽게 회전
                angle = Mathf.MoveTowardsAngle(angle, targetAngle, trackSpeed * Time.deltaTime);
            }
            else
            {
                // 적이 없으면 느리게 공전 (fallback)
                angle += 60f * Time.deltaTime;
            }

            // 레이저 끝점: 플레이어 → angle 방향으로 orbitRadius 거리
            float rad = angle * Mathf.Deg2Rad;
            Vector3 laserEnd = player.transform.position + new Vector3(
                Mathf.Cos(rad) * orbitRadius,
                Mathf.Sin(rad) * orbitRadius,
                0f
            );

            // 레이저 시작점: 끝점 바로 위 하늘에서 내려오는 연출
            Vector3 laserStart = laserEnd + Vector3.up * 10f;

            if (lr != null)
            {
                lr.SetPosition(0, laserStart);
                lr.SetPosition(1, laserEnd);
            }

            if (hitTimer >= hitInterval)
            {
                hitTimer = 0f;
                DamageEnemiesAtPoint(laserEnd, laserWidth);
            }

            yield return null;
        }

        if (laserObj != null) Destroy(laserObj);
        Destroy(this);
    }

    /// <summary>
    /// 플레이어 위치를 기준으로 가장 가까운 적을 반환합니다.
    /// </summary>
    private Transform FindNearestEnemy(Vector3 origin)
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        Transform nearest    = null;
        float minDist        = float.MaxValue;

        foreach (GameObject enemy in enemies)
        {
            if (enemy == null) continue;
            float dist = Vector2.Distance(origin, enemy.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = enemy.transform;
            }
        }
        return nearest;
    }

    /// <summary>
    /// 지정 위치 주변의 적에게 데미지를 줍니다.
    /// </summary>
    private void DamageEnemiesAtPoint(Vector3 point, float radius)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(point, radius);
        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Enemy")) continue;
            Character c = hit.GetComponent<Character>();
            if (c != null) c.TakeDamage(damagePerHit);
        }
    }

    /// <summary>
    /// LineRenderer를 이용한 레이저 시각화 오브젝트를 생성합니다.
    /// </summary>
    private GameObject CreateLaserVisual()
    {
        GameObject obj = new GameObject("SatelliteLaser");

        LineRenderer lr = obj.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.startWidth    = 0.15f;
        lr.endWidth      = 0.05f;
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.startColor    = laserColor;
        lr.endColor      = new Color(laserColor.r, laserColor.g, laserColor.b, 0f); // 끝으로 갈수록 투명
        lr.sortingOrder  = 10; // 다른 스프라이트 위에 렌더링

        return obj;
    }
}

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
    [SerializeField] private float trackSpeed = 300f;
    [SerializeField] private float orbitRadius = 4f;
    [SerializeField] private float damagePerHit = 30f;
    [SerializeField] private float hitInterval = 0.1f;
    [SerializeField] private float duration = 10f;
    [SerializeField] private float laserWidth = 0.5f;

    [Header("LaserBeam VFX")]
    [SerializeField] private GameObject laserBeamPrefab;
    [SerializeField] private float laserBeamScale = 1f;
    [SerializeField] private Vector3 laserBeamOffset = Vector3.zero;

    public void Activate()
    {
        StartCoroutine(TrackingLaser());
    }

    private IEnumerator TrackingLaser()
    {
        Player player = FindObjectOfType<Player>();
        if (player == null)
            yield break;

        GameObject laserObj = CreateLaserBeamVisual();

        float elapsed = 0f;
        float angle = 0f;
        float hitTimer = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            hitTimer += Time.deltaTime;

            if (player == null)
                break;

            Transform nearestEnemy =
                FindNearestEnemy(player.transform.position);

            if (nearestEnemy != null)
            {
                Vector2 toEnemy =
                    (Vector2)(nearestEnemy.position - player.transform.position);

                float targetAngle =
                    Mathf.Atan2(toEnemy.y, toEnemy.x) * Mathf.Rad2Deg;

                angle =
                    Mathf.MoveTowardsAngle(
                        angle,
                        targetAngle,
                        trackSpeed * Time.deltaTime
                    );
            }
            else
            {
                angle += 60f * Time.deltaTime;
            }

            float rad = angle * Mathf.Deg2Rad;

            Vector3 laserPoint =
                player.transform.position +
                new Vector3(
                    Mathf.Cos(rad) * orbitRadius,
                    Mathf.Sin(rad) * orbitRadius,
                    0f
                );

            UpdateLaserBeamVisual(
                laserObj,
                laserPoint
            );

            if (hitTimer >= hitInterval)
            {
                hitTimer = 0f;

                DamageEnemiesAtPoint(
                    laserPoint,
                    laserWidth
                );
            }

            yield return null;
        }

        if (laserObj != null)
            Destroy(laserObj);

        Destroy(this);
    }

    private Transform FindNearestEnemy(Vector3 origin)
    {
        GameObject[] enemies =
            GameObject.FindGameObjectsWithTag("Enemy");

        Transform nearest = null;
        float minDist = float.MaxValue;

        foreach (GameObject enemy in enemies)
        {
            if (enemy == null)
                continue;

            float dist =
                Vector2.Distance(
                    origin,
                    enemy.transform.position
                );

            if (dist < minDist)
            {
                minDist = dist;
                nearest = enemy.transform;
            }
        }

        return nearest;
    }

    private void DamageEnemiesAtPoint(
        Vector3 point,
        float radius)
    {
        Collider2D[] hits =
            Physics2D.OverlapCircleAll(
                point,
                radius
            );

        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Enemy"))
                continue;

            Character c =
                hit.GetComponent<Character>();

            if (c != null)
                c.TakeDamage(damagePerHit);
        }
    }

    private GameObject CreateLaserBeamVisual()
    {
        if (laserBeamPrefab == null)
        {
            Debug.LogWarning(
                "[SatelliteUplinkEffect] laserBeamPrefab이 없습니다."
            );

            return null;
        }

        GameObject obj =
            Instantiate(
                laserBeamPrefab,
                transform.position,
                Quaternion.identity
            );

        obj.transform.localScale =
            Vector3.one * laserBeamScale;

        return obj;
    }

    private void UpdateLaserBeamVisual(
        GameObject laserObj,
        Vector3 laserPoint)
    {
        if (laserObj == null)
            return;

        laserObj.transform.position =
            laserPoint + laserBeamOffset;

        laserObj.transform.rotation =
            Quaternion.identity;

        // 중요:
        // 프리팹 원본 애니메이션 크기 유지
        // 화면 높이/거리 기준으로 늘리지 않음
        laserObj.transform.localScale =
            Vector3.one * laserBeamScale;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;

        Gizmos.DrawWireSphere(
            transform.position,
            orbitRadius
        );
    }
#endif
}
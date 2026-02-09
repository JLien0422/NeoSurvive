using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 해킹 오브젝트(기획서 5종) 보상 전용 시스템
/// - 미니게임과 완전히 분리됨
/// - HackingSystem의 성공/실패 이벤트만 구독
/// </summary>
public class HackingObjectRewardSystem : MonoBehaviour
{
    public static HackingObjectRewardSystem Instance { get; private set; }

    /* ===============================
     * Security Turret 설정 (총알 발사)
     * =============================== */
    [Header("Security Turret")]
    [SerializeField] private GameObject securityTurretPrefab;
    [SerializeField] private GameObject turretBulletPrefab;
    [SerializeField] private float turretDuration = 8f;
    [SerializeField] private float turretRange = 12f;
    [SerializeField] private float turretFireRate = 0.15f;
    [SerializeField] private float turretDamage = 8f;
    [SerializeField] private float bulletSpeed = 18f;
    [SerializeField] private float bulletLifeTime = 2f;

    /* ===============================
     * Electric Fence 설정 (사각 벽 4개 생성)
     * =============================== */
    [Header("Electric Fence")]
    [SerializeField] private GameObject electricFenceSegmentPrefab;
    [SerializeField] private float fenceHalfWidth = 8f;     // 사각형 가로 반
    [SerializeField] private float fenceHalfHeight = 5f;    // 사각형 세로 반
    [SerializeField] private float fenceDuration = 6f;
    [SerializeField] private float fenceDps = 25f;
    [SerializeField] private float fenceThickness = 0.6f;   // 벽 두께(스케일)

    /* ===============================
     * Satellite Uplink 설정 (범위 연사 틱딜)
     * =============================== */
    [Header("Satellite Uplink")]
    [SerializeField] private float satelliteRadius = 16f;
    [SerializeField] private float satelliteDuration = 3.0f;
    [SerializeField] private float satelliteTick = 0.12f;
    [SerializeField] private float satelliteDamage = 6f;

    /* ===============================
     * Synapse Overload Server 설정 (절반 아군화)
     * =============================== */
    [Header("Synapse Overload Server")]
    [SerializeField] private float synapseRadius = 14f;
    [SerializeField] private float synapseConvertRatio = 0.5f;
    [SerializeField] private float synapseDuration = 8f;

    /* ===============================
     * Magnetic Beacon 설정 (3시 방향 견인)
     * =============================== */
    [Header("Magnetic Beacon")]
    [SerializeField] private float magneticRadius = 25f;
    [SerializeField] private float magneticDuration = 1.0f;
    [SerializeField] private float magneticForce = 40f;
    [SerializeField] private Vector2 pullDirection = Vector2.right;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnEnable()
    {
        HackingSystem.OnHackSuccess += HandleHackSuccess;
        HackingSystem.OnHackFail += HandleHackFail;
    }

    private void OnDisable()
    {
        HackingSystem.OnHackSuccess -= HandleHackSuccess;
        HackingSystem.OnHackFail -= HandleHackFail;
    }

    /* ===============================
     * 이벤트 핸들러
     * =============================== */
    private void HandleHackSuccess(HackableObject hackableObject)
    {
        ApplyObjectReward(hackableObject);

        if (hackableObject != null)
            Destroy(hackableObject.gameObject);
    }

    private void HandleHackFail(HackableObject hackableObject)
    {
        // 실패 패널티는 HackingSystem에서 처리
        if (hackableObject != null)
            Destroy(hackableObject.gameObject);
    }

    /* ===============================
     * 오브젝트 보상 진입점
     * =============================== */
    public void ApplyObjectReward(HackableObject hackableObject)
    {
        if (hackableObject == null) return;

        Vector3 center = hackableObject.transform.position;

        switch (hackableObject.hackableType)
        {
            case HackableType.SecurityTurret:
                Debug.Log("[HackingObjectRewardSystem] 보안 터렛 발동");
                StartCoroutine(ActivateSecurityTurret(center));
                break;

            case HackableType.ElectricFence:
                Debug.Log("[HackingObjectRewardSystem] 전기 울타리 발동");
                StartCoroutine(ActivateElectricFence(center));
                break;

            case HackableType.SatelliteUplink:
                Debug.Log("[HackingObjectRewardSystem] 새틀라이트 통신기 발동");
                StartCoroutine(ActivateSatelliteUplink(center));
                break;

            case HackableType.SynapseOverloadServer:
                Debug.Log("[HackingObjectRewardSystem] 시냅스 과부하 서버 발동");
                StartCoroutine(TriggerSynapseOverload(center));
                break;

            case HackableType.MagneticBeacon:
                Debug.Log("[HackingObjectRewardSystem] 마그네틱 비컨 발동");
                StartCoroutine(ActivateMagneticBeacon(center));
                break;
        }
    }

    /* ===============================
     * 1) Security Turret (총알 발사)
     * =============================== */
    private IEnumerator ActivateSecurityTurret(Vector3 center)
    {
        if (securityTurretPrefab == null || turretBulletPrefab == null)
        {
            Debug.LogError("[SecurityTurret] Prefab이 설정되지 않았습니다!");
            yield break;
        }

        GameObject turretObj = Instantiate(securityTurretPrefab, center, Quaternion.identity);
        SecurityTurretDummy turret = turretObj.GetComponent<SecurityTurretDummy>();

        if (turret == null)
        {
            Debug.LogError("[SecurityTurret] SecurityTurretDummy가 없습니다!");
            Destroy(turretObj);
            yield break;
        }

        float elapsed = 0f;
        float fireTimer = 0f;

        while (elapsed < turretDuration)
        {
            fireTimer += Time.deltaTime;

            Enemy target = FindClosestEnemy(turretObj.transform.position, turretRange);
            if (target != null)
            {
                // 조준(회전)
                Vector2 dir = target.transform.position - turretObj.transform.position;
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                turretObj.transform.rotation = Quaternion.Euler(0, 0, angle);

                // 발사
                if (fireTimer >= turretFireRate)
                {
                    fireTimer = 0f;

                    Transform firePoint = turret.firePoint != null ? turret.firePoint : turretObj.transform;

                    GameObject bulletObj = Instantiate(
                        turretBulletPrefab,
                        firePoint.position,
                        firePoint.rotation
                    );

                    TurretBullet bullet = bulletObj.GetComponent<TurretBullet>();
                    if (bullet != null)
                    {
                        bullet.Init(turretDamage, bulletSpeed, bulletLifeTime);
                    }
                }
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(turretObj);
    }

    /* ===============================
     * 2) Electric Fence (사각 벽 4개)
     * =============================== */
    private IEnumerator ActivateElectricFence(Vector3 center)
    {
        if (electricFenceSegmentPrefab == null)
        {
            Debug.LogError("[ElectricFence] electricFenceSegmentPrefab이 없습니다!");
            yield break;
        }

        List<GameObject> segments = new List<GameObject>(4);

        // 상단(가로)
        segments.Add(SpawnFenceSegment(
            position: center + new Vector3(0f, fenceHalfHeight, 0f),
            size: new Vector2(fenceHalfWidth * 2f, fenceThickness)
        ));

        // 하단(가로)
        segments.Add(SpawnFenceSegment(
            position: center + new Vector3(0f, -fenceHalfHeight, 0f),
            size: new Vector2(fenceHalfWidth * 2f, fenceThickness)
        ));

        // 우측(세로)
        segments.Add(SpawnFenceSegment(
            position: center + new Vector3(fenceHalfWidth, 0f, 0f),
            size: new Vector2(fenceThickness, fenceHalfHeight * 2f)
        ));

        // 좌측(세로)
        segments.Add(SpawnFenceSegment(
            position: center + new Vector3(-fenceHalfWidth, 0f, 0f),
            size: new Vector2(fenceThickness, fenceHalfHeight * 2f)
        ));

        yield return new WaitForSeconds(fenceDuration);

        foreach (var seg in segments)
        {
            if (seg != null) Destroy(seg);
        }
    }

    private GameObject SpawnFenceSegment(Vector3 position, Vector2 size)
    {
        GameObject obj = Instantiate(electricFenceSegmentPrefab, position, Quaternion.identity);

        // 프리팹 기준 스케일이 (1,1,1)이라고 가정하고 size대로 맞춤
        obj.transform.localScale = new Vector3(size.x, size.y, 1f);

        // 데미지 주입
        ElectricFenceSegment seg = obj.GetComponent<ElectricFenceSegment>();
        if (seg != null)
        {
            seg.SetDps(fenceDps);
        }

        return obj;
    }

    /* ===============================
     * 3) Satellite Uplink
     * =============================== */
    private IEnumerator ActivateSatelliteUplink(Vector3 center)
    {
        float elapsed = 0f;

        while (elapsed < satelliteDuration)
        {
            Collider2D[] cols = Physics2D.OverlapCircleAll(center, satelliteRadius);

            foreach (var col in cols)
            {
                if (!col.CompareTag("Enemy")) continue;

                Enemy e = col.GetComponent<Enemy>();
                if (e != null)
                    e.TakeDamage(satelliteDamage);
            }

            elapsed += satelliteTick;
            yield return new WaitForSeconds(satelliteTick);
        }
    }

    /* ===============================
     * 4) Synapse Overload Server
     * =============================== */
    private IEnumerator TriggerSynapseOverload(Vector3 center)
    {
        Collider2D[] cols = Physics2D.OverlapCircleAll(center, synapseRadius);

        List<Enemy> enemies = new List<Enemy>();
        foreach (var col in cols)
        {
            if (!col.CompareTag("Enemy")) continue;

            Enemy e = col.GetComponent<Enemy>();
            if (e != null) enemies.Add(e);
        }

        if (enemies.Count == 0) yield break;

        // 셔플
        for (int i = 0; i < enemies.Count; i++)
        {
            int j = Random.Range(i, enemies.Count);
            (enemies[i], enemies[j]) = (enemies[j], enemies[i]);
        }

        int convertCount = Mathf.RoundToInt(enemies.Count * synapseConvertRatio);
        convertCount = Mathf.Clamp(convertCount, 1, enemies.Count);

        List<Enemy> converted = new List<Enemy>();
        for (int i = 0; i < convertCount; i++)
        {
            if (enemies[i] == null) continue;
            ConvertToAlly(enemies[i]);
            converted.Add(enemies[i]);
        }

        yield return new WaitForSeconds(synapseDuration);

        foreach (var e in converted)
            if (e != null) RevertToEnemy(e);
    }

    private void ConvertToAlly(Enemy enemy)
    {
        enemy.gameObject.tag = "Ally";

        EnemyController controller = enemy.GetComponent<EnemyController>();
        if (controller != null)
            controller.SetTargetToEnemies();

        SpriteRenderer sr = enemy.GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.color = new Color(0.5f, 1f, 0.5f);
    }

    private void RevertToEnemy(Enemy enemy)
    {
        enemy.gameObject.tag = "Enemy";

        EnemyController controller = enemy.GetComponent<EnemyController>();
        if (controller != null)
            controller.SetTargetToPlayer();

        SpriteRenderer sr = enemy.GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.color = Color.white;
    }

    /* ===============================
     * 5) Magnetic Beacon
     * =============================== */
    private IEnumerator ActivateMagneticBeacon(Vector3 center)
    {
        float t = 0f;

        while (t < magneticDuration)
        {
            Collider2D[] cols = Physics2D.OverlapCircleAll(center, magneticRadius);

            foreach (var col in cols)
            {
                if (!col.CompareTag("Enemy")) continue;

                Rigidbody2D rb = col.GetComponent<Rigidbody2D>();
                if (rb != null)
                    rb.AddForce(pullDirection.normalized * magneticForce, ForceMode2D.Force);
            }

            t += Time.deltaTime;
            yield return null;
        }
    }

    /* ===============================
     * 유틸: 가장 가까운 적 찾기
     * =============================== */
    private Enemy FindClosestEnemy(Vector3 center, float range)
    {
        Collider2D[] cols = Physics2D.OverlapCircleAll(center, range);

        Enemy closest = null;
        float minDist = float.MaxValue;

        foreach (var col in cols)
        {
            if (!col.CompareTag("Enemy")) continue;

            Enemy e = col.GetComponent<Enemy>();
            if (e == null) continue;

            float dist = Vector3.Distance(center, e.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = e;
            }
        }

        return closest;
    }
}

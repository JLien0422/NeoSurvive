using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 해킹 미니게임 성공 시 보상을 적용하는 시스템
/// - 적 오브젝트를 아군으로 전환 (자동포탑, 거대로봇 등)
/// - 맵 패턴을 중립적으로 변경 (전기 차단막, 유독물질 발산장치 등)
/// </summary>
public class HackingRewardSystem : MonoBehaviour
{
    public static HackingRewardSystem Instance { get; private set; }

    [Header("아군 전환 설정")]
    [SerializeField]
    [Tooltip("아군으로 전환할 적의 태그")]
    private string[] enemyTagsToConvert = { "Enemy" };

    [SerializeField]
    [Tooltip("아군 전환 범위 (해킹 오브젝트 기준)")]
    private float conversionRange = 10f;

    [SerializeField]
    [Tooltip("아군 전환 지속 시간 (0이면 영구)")]
    private float conversionDuration = 30f;

    [Header("맵 패턴 중립화 설정")]
    [SerializeField]
    [Tooltip("중립화할 맵 오브젝트 태그")]
    private string[] mapObjectTagsToNeutralize = { "ElectricBarrier", "PoisonEmitter", "Hazard" };

    [SerializeField]
    [Tooltip("중립화 범위 (해킹 오브젝트 기준)")]
    private float neutralizationRange = 15f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 해킹 성공 보상 적용
    /// </summary>
    /// <param name="hackableObject">해킹한 오브젝트</param>
    /// <param name="minigameType">성공한 미니게임 타입</param>
    public void ApplyHackingReward(HackableObject hackableObject, HackingMinigameType minigameType)
    {
        if (hackableObject == null)
        {
            Debug.LogError("[HackingRewardSystem] hackableObject가 null입니다!");
            return;
        }

        Vector3 hackPosition = hackableObject.transform.position;

        switch (minigameType)
        {
            case HackingMinigameType.CommandBypass:
                // 시스템 과부하: 맵의 모든 문이나 전자기기가 폭발하며 주변 적에게 대미지
                ApplySystemOverload(hackPosition);
                break;

            case HackingMinigameType.NumberSequence:
                // 보안 봇 탈취: 적군 중 기계형 유닛들이 일정 시간 동안 아군이 되어 주변 적을 공격
                ConvertEnemiesToAllies(hackPosition);
                break;

            case HackingMinigameType.NetworkBridge:
                // 에너지 보급: 플레이어의 무기 쿨타임이 즉시 초기화되고, 일정 시간 동안 탄환 제한 없이 공격
                ApplyEnergySupply();
                break;

            case HackingMinigameType.SynapseSync:
                // 데이터 펄스: 플레이어 중심의 강력한 충격파가 발생하여 화면 내 모든 적을 맵 밖으로 밀쳐내고 5초간 기절
                ApplyDataPulse(hackPosition);
                break;

            case HackingMinigameType.FrequencyOverride:
                // 궤도 폭격: 위성에서 강력한 레이저가 쏟아져 내려와 맵에서 가장 체력이 높은 적을 우선적으로 타격
                ApplyOrbitalStrike();
                break;
        }

        // 모든 미니게임 성공 시 공통 효과: 맵 패턴 중립화
        NeutralizeMapPatterns(hackPosition);
    }

    /// <summary>
    /// 시스템 과부하 효과
    /// </summary>
    private void ApplySystemOverload(Vector3 center)
    {
        Debug.Log("[HackingRewardSystem] 시스템 과부하 효과 적용!");

        // 범위 내 전자기기 찾기
        Collider2D[] objects = Physics2D.OverlapCircleAll(center, neutralizationRange);

        foreach (var col in objects)
        {
            // 전자기기 태그 확인 (예: "ElectronicDevice", "Door" 등)
            if (col.CompareTag("ElectronicDevice") || col.CompareTag("Door"))
            {
                // 폭발 효과
                ExplodeDevice(col.gameObject);
            }
        }
    }

    /// <summary>
    /// 적을 아군으로 전환
    /// </summary>
    private void ConvertEnemiesToAllies(Vector3 center)
    {
        Debug.Log($"[HackingRewardSystem] 적을 아군으로 전환 시작! 범위: {conversionRange}");

        List<Enemy> convertedEnemies = new List<Enemy>();

        // 범위 내 모든 적 찾기
        foreach (string tag in enemyTagsToConvert)
        {
            GameObject[] enemies = GameObject.FindGameObjectsWithTag(tag);
            
            foreach (GameObject enemyObj in enemies)
            {
                float distance = Vector3.Distance(center, enemyObj.transform.position);
                if (distance <= conversionRange)
                {
                    Enemy enemy = enemyObj.GetComponent<Enemy>();
                    if (enemy != null)
                    {
                        ConvertToAlly(enemy);
                        convertedEnemies.Add(enemy);
                    }
                }
            }
        }

        Debug.Log($"[HackingRewardSystem] {convertedEnemies.Count}개의 적이 아군으로 전환되었습니다.");

        // 지속 시간이 있으면 일정 시간 후 원래대로 복귀
        if (conversionDuration > 0f)
        {
            StartCoroutine(RevertEnemiesAfterDelay(convertedEnemies));
        }
    }

    /// <summary>
    /// 적을 아군으로 전환
    /// </summary>
    private void ConvertToAlly(Enemy enemy)
    {
        if (enemy == null) return;

        // 태그 변경
        enemy.gameObject.tag = "Ally";

        // EnemyController 수정 (적 대신 적을 공격하도록)
        EnemyController controller = enemy.GetComponent<EnemyController>();
        if (controller != null)
        {
            // 아군으로 전환된 적은 다른 적을 공격하도록 설정
            controller.SetTargetToEnemies();
        }

        // 시각적 효과 (색상 변경 등)
        SpriteRenderer sr = enemy.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = new Color(0.5f, 1f, 0.5f, 1f); // 연한 녹색으로 변경
        }

        Debug.Log($"[HackingRewardSystem] {enemy.name}이(가) 아군으로 전환되었습니다.");
    }

    /// <summary>
    /// 일정 시간 후 적을 원래대로 복귀
    /// </summary>
    private IEnumerator RevertEnemiesAfterDelay(List<Enemy> enemies)
    {
        yield return new WaitForSeconds(conversionDuration);

        foreach (Enemy enemy in enemies)
        {
            if (enemy != null)
            {
                RevertToEnemy(enemy);
            }
        }

        Debug.Log("[HackingRewardSystem] 아군이 다시 적으로 복귀했습니다.");
    }

    /// <summary>
    /// 아군을 다시 적으로 복귀
    /// </summary>
    private void RevertToEnemy(Enemy enemy)
    {
        if (enemy == null) return;

        enemy.gameObject.tag = "Enemy";

        EnemyController controller = enemy.GetComponent<EnemyController>();
        if (controller != null)
        {
            controller.SetTargetToPlayer();
        }

        SpriteRenderer sr = enemy.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = Color.white; // 원래 색상으로 복귀
        }
    }

    /// <summary>
    /// 에너지 보급 효과
    /// </summary>
    private void ApplyEnergySupply()
    {
        Debug.Log("[HackingRewardSystem] 에너지 보급 효과 적용!");

        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj == null) return;

        // 무기 쿨타임 초기화
        NeoSurvive.Weapon.WeaponManager weaponManager = playerObj.GetComponent<NeoSurvive.Weapon.WeaponManager>();
        if (weaponManager != null)
        {
            // TODO: WeaponManager에 쿨타임 초기화 메서드 추가 필요
            Debug.Log("[HackingRewardSystem] 무기 쿨타임 초기화 (구현 필요)");
        }

        // 무한 탄환 효과 (일정 시간)
        StartCoroutine(InfiniteAmmoRoutine(10f)); // 10초간 무한 탄환
    }

    /// <summary>
    /// 무한 탄환 효과 코루틴
    /// </summary>
    private IEnumerator InfiniteAmmoRoutine(float duration)
    {
        // TODO: 무기 시스템에 무한 탄환 플래그 추가 필요
        Debug.Log($"[HackingRewardSystem] {duration}초간 무한 탄환 효과 (구현 필요)");
        yield return new WaitForSeconds(duration);
        Debug.Log("[HackingRewardSystem] 무한 탄환 효과 종료");
    }

    /// <summary>
    /// 데이터 펄스 효과
    /// </summary>
    private void ApplyDataPulse(Vector3 center)
    {
        Debug.Log("[HackingRewardSystem] 데이터 펄스 효과 적용!");

        // 화면 내 모든 적 찾기
        Camera cam = Camera.main;
        if (cam == null) return;

        float worldHeight = cam.orthographicSize * 2f;
        float worldWidth = worldHeight * cam.aspect;
        Vector3 screenCenter = cam.transform.position;

        Collider2D[] enemies = Physics2D.OverlapBoxAll(screenCenter, new Vector2(worldWidth, worldHeight), 0f);

        foreach (var col in enemies)
        {
            if (col.CompareTag("Enemy"))
            {
                // 적을 맵 밖으로 밀쳐내기
                PushEnemyAway(col.gameObject, center, 20f);

                // 5초간 기절
                EnemyController controller = col.GetComponent<EnemyController>();
                if (controller != null)
                {
                    controller.ApplySlow(0f, 5f); // 이동 속도 0으로 기절
                }
            }
        }
    }

    /// <summary>
    /// 적을 밀쳐내기
    /// </summary>
    private void PushEnemyAway(GameObject enemy, Vector3 center, float force)
    {
        Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            Vector2 direction = (enemy.transform.position - center).normalized;
            rb.AddForce(direction * force, ForceMode2D.Impulse);
        }
    }

    /// <summary>
    /// 궤도 폭격 효과
    /// </summary>
    private void ApplyOrbitalStrike()
    {
        Debug.Log("[HackingRewardSystem] 궤도 폭격 효과 적용!");

        // 모든 적 찾기
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        if (enemies.Length == 0) return;

        // 체력이 가장 높은 적 찾기
        Enemy highestHealthEnemy = null;
        float maxHealth = 0f;

        foreach (GameObject enemyObj in enemies)
        {
            Enemy enemy = enemyObj.GetComponent<Enemy>();
            if (enemy != null && enemy.CurrentHealth > maxHealth)
            {
                maxHealth = enemy.CurrentHealth;
                highestHealthEnemy = enemy;
            }
        }

        if (highestHealthEnemy != null)
        {
            // 레이저 폭격 효과
            StartCoroutine(OrbitalStrikeRoutine(highestHealthEnemy.transform.position));
        }
    }

    /// <summary>
    /// 궤도 폭격 코루틴
    /// </summary>
    private IEnumerator OrbitalStrikeRoutine(Vector3 targetPosition)
    {
        // 레이저 이펙트 (위에서 아래로)
        Debug.Log($"[HackingRewardSystem] 궤도 폭격 타겟: {targetPosition}");

        // TODO: 레이저 이펙트 생성 및 대미지 적용
        yield return new WaitForSeconds(1f);

        // 범위 내 적에게 대미지
        Collider2D[] enemies = Physics2D.OverlapCircleAll(targetPosition, 3f);
        foreach (var col in enemies)
        {
            if (col.CompareTag("Enemy"))
            {
                Enemy enemy = col.GetComponent<Enemy>();
                if (enemy != null)
                {
                    enemy.TakeDamage(50f); // 강력한 대미지
                }
            }
        }
    }

    /// <summary>
    /// 맵 패턴 중립화
    /// </summary>
    private void NeutralizeMapPatterns(Vector3 center)
    {
        Debug.Log($"[HackingRewardSystem] 맵 패턴 중립화 시작! 범위: {neutralizationRange}");

        int neutralizedCount = 0;

        // 범위 내 모든 맵 오브젝트 찾기
        Collider2D[] objects = Physics2D.OverlapCircleAll(center, neutralizationRange);

        foreach (var col in objects)
        {
            foreach (string tag in mapObjectTagsToNeutralize)
            {
                if (col.CompareTag(tag))
                {
                    NeutralizeMapObject(col.gameObject);
                    neutralizedCount++;
                }
            }
        }

        Debug.Log($"[HackingRewardSystem] {neutralizedCount}개의 맵 오브젝트가 중립화되었습니다.");
    }

    /// <summary>
    /// 맵 오브젝트 중립화
    /// </summary>
    private void NeutralizeMapObject(GameObject mapObject)
    {
        if (mapObject == null) return;

        // 전기 차단막 비활성화
        if (mapObject.CompareTag("ElectricBarrier"))
        {
            // 전기 효과 컴포넌트 비활성화
            MonoBehaviour[] components = mapObject.GetComponents<MonoBehaviour>();
            foreach (var comp in components)
            {
                if (comp.GetType().Name.Contains("Electric") || comp.GetType().Name.Contains("Barrier"))
                {
                    comp.enabled = false;
                }
            }

            // 콜라이더 비활성화 (통과 가능하게)
            Collider2D col = mapObject.GetComponent<Collider2D>();
            if (col != null)
            {
                col.enabled = false;
            }

            // 시각적 효과 (어둡게)
            SpriteRenderer sr = mapObject.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            }

            Debug.Log($"[HackingRewardSystem] 전기 차단막 비활성화: {mapObject.name}");
        }

        // 유독물질 발산장치 비활성화
        else if (mapObject.CompareTag("PoisonEmitter"))
        {
            // 발산 효과 컴포넌트 비활성화
            MonoBehaviour[] components = mapObject.GetComponents<MonoBehaviour>();
            foreach (var comp in components)
            {
                if (comp.GetType().Name.Contains("Poison") || comp.GetType().Name.Contains("Emitter"))
                {
                    comp.enabled = false;
                }
            }

            // 파티클 시스템 정지
            ParticleSystem ps = mapObject.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Stop();
            }

            Debug.Log($"[HackingRewardSystem] 유독물질 발산장치 비활성화: {mapObject.name}");
        }

        // 기타 위험 오브젝트
        else if (mapObject.CompareTag("Hazard"))
        {
            // 모든 위험 효과 비활성화
            MonoBehaviour[] components = mapObject.GetComponents<MonoBehaviour>();
            foreach (var comp in components)
            {
                if (comp.GetType().Name.Contains("Hazard") || comp.GetType().Name.Contains("Damage"))
                {
                    comp.enabled = false;
                }
            }

            Debug.Log($"[HackingRewardSystem] 위험 오브젝트 비활성화: {mapObject.name}");
        }
    }

    /// <summary>
    /// 전자기기 폭발 효과
    /// </summary>
    private void ExplodeDevice(GameObject device)
    {
        if (device == null) return;

        // 폭발 대미지 (주변 적에게)
        Collider2D[] enemies = Physics2D.OverlapCircleAll(device.transform.position, 3f);
        foreach (var col in enemies)
        {
            if (col.CompareTag("Enemy"))
            {
                Enemy enemy = col.GetComponent<Enemy>();
                if (enemy != null)
                {
                    enemy.TakeDamage(20f);
                }
            }
        }

        // 폭발 이펙트 (TODO: 파티클 시스템 추가)
        Debug.Log($"[HackingRewardSystem] 전자기기 폭발: {device.name}");

        // 오브젝트 제거
        Destroy(device, 0.5f);
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            // 아군 전환 범위 표시
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, conversionRange);

            // 중립화 범위 표시
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, neutralizationRange);
        }
    }
}

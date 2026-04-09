using System.Collections;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// 게임 시간이 15분(900초)이 되면 모든 적을 제거하고 보스를 생성합니다.
/// 매 프레임 체크 대신 남은 시간을 계산해 WaitForSeconds로 정확하게 한 번만 실행합니다.
/// </summary>
public class BossSpawner : MonoBehaviour
{
    [Header("보스 스폰 설정")]
    [Tooltip("맵 번호(1부터) 기준 보스 프리팹 배열\n- index = selectedMapId - 1")]
    public GameObject[] bossPrefabsByMapId;

    [Tooltip("현재 맵 번호 (기본값은 1맵 = 도심외곽)")]
    public int selectedMapId = 1;

    [Tooltip("플레이어 Y축 위 스폰 거리")]
    [SerializeField] private float spawnOffsetY = 5f;

    [Header("DOTween 연출 설정")]
    [Tooltip("보스 스프라이트 페이드인 시간 (초)")]
    [SerializeField] private float fadeInDuration = 1.5f;

    // 게임 클리어 시간 (15분 = 900초)
    private const float BOSS_SPAWN_TIME = 900f;

    private void Start()
    {
        StartCoroutine(WaitAndSpawnBoss());
    }

    /// <summary>
    /// 1초마다 GetGameTime()을 체크해 15분이 되면 보스를 스폰합니다.
    /// 디버그 시간 점프(6번 키 등)에도 정확하게 반응합니다.
    /// </summary>
    private IEnumerator WaitAndSpawnBoss()
    {
        // GameManager가 준비될 때까지 대기
        yield return new WaitUntil(() => GameManager.Instance != null);

        // 1초마다 체크 - 매 프레임보다 훨씬 가볍고 시간 점프에도 대응 가능
        while (GameManager.Instance.GetGameTime() < BOSS_SPAWN_TIME)
        {
            yield return new WaitForSeconds(1f);
        }

        SpawnBoss();
    }

    /// <summary>
    /// 기존 적 전부 제거 → EnemySpawner 중단 → 보스 생성
    /// </summary>
    private void SpawnBoss()
    {
        // EnemySpawner 스폰 중단
        EnemySpawner spawner = FindObjectOfType<EnemySpawner>();
        if (spawner != null)
            spawner.StopSpawning();

        // "Enemy" 태그 오브젝트 전부 제거
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
            Destroy(enemy);

        Debug.Log($"[BossSpawner] 기존 적 {enemies.Length}마리 제거 완료");

        // 보스 스폰 위치: 플레이어 Y축 위
        Vector3 spawnPos = GetSpawnPosition();

        if (bossPrefabsByMapId == null || bossPrefabsByMapId.Length == 0)
        {
            Debug.LogError("[BossSpawner] bossPrefabsByMapId 배열이 비어있습니다. 인스펙터에서 맵별 보스 프리팹을 등록하세요.");
            return;
        }

        int index = selectedMapId - 1; // 맵 번호(1~) -> 배열 인덱스(0~) 변환
        if (index < 0 || index >= bossPrefabsByMapId.Length)
        {
            Debug.LogError($"[BossSpawner] selectedMapId 범위 오류: selectedMapId={selectedMapId}, index={index}, 배열길이={bossPrefabsByMapId.Length}");
            return;
        }

        GameObject prefab = bossPrefabsByMapId[index];
        if (prefab == null)
        {
            Debug.LogError($"[BossSpawner] 해당 맵에 매핑된 보스 프리팹이 null입니다. selectedMapId={selectedMapId}, index={index}");
            return;
        }

        GameObject bossObj = Instantiate(prefab, spawnPos, Quaternion.identity);

        // DOTween 페이드인 연출 (SpriteRenderer alpha 0 → 1)
        // 보스 스프라이트가 루트가 아니라 자식에 있는 경우도 대비해서 children에서 찾는다.
        SpriteRenderer sr = bossObj.GetComponentInChildren<SpriteRenderer>(true);
        if (sr != null)
        {
            Color c = sr.color;
            c.a = 0f;
            sr.color = c;
            sr.DOFade(1f, fadeInDuration).SetEase(Ease.InOutSine);
        }

        // BossHealthBar에 보스 연결
        BossHealthBar healthBar = FindObjectOfType<BossHealthBar>(true);
        if (healthBar != null)
        {
            Boss boss = bossObj.GetComponent<Boss>();
            if (boss != null) healthBar.SetBoss(boss);
        }

        Debug.Log($"[BossSpawner] 보스 스폰 완료! 위치: {spawnPos}");
    }

    /// <summary>
    /// 플레이어 Y축 위 spawnOffsetY 거리에 스폰 위치를 계산합니다.
    /// </summary>
    private Vector3 GetSpawnPosition()
    {
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
            return playerObj.transform.position + new Vector3(0f, spawnOffsetY, 0f);

        // 플레이어를 찾지 못하면 원점 위에 스폰
        Debug.LogWarning("[BossSpawner] 플레이어를 찾지 못했습니다. 원점 위에 스폰합니다.");
        return new Vector3(0f, spawnOffsetY, 0f);
    }
}

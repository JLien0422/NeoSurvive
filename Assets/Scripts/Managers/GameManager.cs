using System.Collections;
using UnityEngine;

// GameManager 클래스는 게임의 전반적인 흐름과 상태를 관리합니다.
public class GameManager : MonoBehaviour
{
    // 싱글톤 인스턴스: 다른 스크립트에서 GameManager에 쉽게 접근할 수 있도록 합니다.
    public static GameManager Instance { get; private set; }

    [Header("적 스폰 설정")]
    // 인스펙터에서 할당할 적 프리팹입니다.
    [SerializeField]
    private GameObject enemyPrefab;
    // 적 생성 주기 (초)
    [SerializeField]
    private float spawnInterval = 3.0f;
    // 플레이어 중심 생성 최소/최대 반경
    [SerializeField]
    private float minSpawnRadius = 5.0f;
    [SerializeField]
    private float maxSpawnRadius = 10.0f;

    [Header("골드 관리")]
    // 이번 판에서 획득한 골드
    [SerializeField]
    private int currentRunGold = 0;
    // 저장된 총 골드
    private int totalGold = 0;

    // 참조
    private Transform playerTransform;
    private const string GOLD_SAVE_KEY = "TotalGold"; // PlayerPrefs 키

    // 컴포넌트가 처음 활성화될 때 호출됩니다.
    private void Awake()
    {
        // 싱글톤 패턴 구현
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬이 바뀌어도 파괴되지 않도록 설정
            LoadGold(); // 게임 시작 시 저장된 골드 불러오기
        }
        else
        {
            Destroy(gameObject); // 이미 인스턴스가 있다면 이 오브젝트는 파괴
        }
    }

    // 게임이 시작될 때 한 번 호출됩니다.
    private void Start()
    {
        GameObject playerObject = GameObject.FindWithTag("Player");
        if (playerObject != null)
        {
            playerTransform = playerObject.transform;
            StartCoroutine(SpawnEnemies());
        }
        else
        {
            Debug.LogError("플레이어를 찾을 수 없습니다! 'Player' 태그가 설정되었는지 확인해주세요.");
        }
    }

    // 골드를 추가하는 공용 메서드
    public void AddGold(int amount)
    {
        currentRunGold += amount;
        Debug.Log($"골드 {amount} 획득! 이번 판 총 골드: {currentRunGold}");
    }
    
    // 플레이어가 죽었을 때 호출될 메서드
    public void OnPlayerDeath()
    {
        totalGold += currentRunGold;
        SaveGold();
        currentRunGold = 0; // 현재 판 골드 초기화
        Debug.Log($"이번 판에 얻은 골드가 총 골드에 합산되었습니다. 현재 총 골드: {totalGold}");
    }

    // 골드를 PlayerPrefs에 저장
    private void SaveGold()
    {
        PlayerPrefs.SetInt(GOLD_SAVE_KEY, totalGold);
        PlayerPrefs.Save(); // 변경사항을 디스크에 즉시 저장
        Debug.Log($"총 골드 {totalGold}를 저장했습니다.");
    }

    // PlayerPrefs에서 골드를 불러옴
    private void LoadGold()
    {
        totalGold = PlayerPrefs.GetInt(GOLD_SAVE_KEY, 0); // 저장된 값이 없으면 0을 기본값으로 사용
        Debug.Log($"저장된 총 골드 {totalGold}를 불러왔습니다.");
    }

    // 일정 주기로 적을 생성하는 코루틴
    private IEnumerator SpawnEnemies()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);
            if (playerTransform != null && enemyPrefab != null)
            {
                SpawnEnemy();
            }
        }
    }

    // 적 하나를 생성하는 메서드
    private void SpawnEnemy()
    {
        float randomAngle = Random.Range(0f, 360f);
        float randomRadius = Random.Range(minSpawnRadius, maxSpawnRadius);
        Vector2 spawnPosition = playerTransform.position + (Vector3)(new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle)) * randomRadius);
        Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
    }
}

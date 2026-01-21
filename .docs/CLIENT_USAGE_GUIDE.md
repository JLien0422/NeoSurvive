# NeoSurvive Co-op 멀티플레이어 구현 가이드

## 📦 Unity 패키지 설치

### 1. NativeWebSocket 설치 (필수)

Package Manager → Add package from git URL:

```
https://github.com/endel/NativeWebSocket.git#upm
```

또는 `Packages/manifest.json`에 추가:

```json
{
  "dependencies": {
    "com.endel.nativewebsocket": "https://github.com/endel/NativeWebSocket.git#upm"
  }
}
```

---

## 🎮 클라이언트 사용 예제

### 1. Co-op 세션 생성 및 참가

```csharp
using UnityEngine;
using NeoSurvive.Network;
using NeoSurvive.Characters;

public class GameLobby : MonoBehaviour
{
    private NetworkManager networkManager;

    private void Start()
    {
        networkManager = NetworkManager.Instance;

        // 이벤트 등록
        networkManager.OnRemotePlayerJoined += OnPlayerJoined;
        networkManager.OnRemotePlayerUpdated += OnPlayerUpdated;
    }

    // 호스트: 세션 생성
    public void CreateSession()
    {
        StartCoroutine(networkManager.CreateCoopSession(CharacterType.Hacker, stage: 1));
    }

    // 클라이언트: 세션 참가
    public void JoinSession(string sessionCode)
    {
        StartCoroutine(networkManager.JoinCoopSession(sessionCode, CharacterType.Cyborg));
    }

    private void OnPlayerJoined(PlayerState player)
    {
        Debug.Log($"플레이어 참가: {player.nickname}");
        // 원격 플레이어 오브젝트 생성
    }

    private void OnPlayerUpdated(PlayerState player)
    {
        // 원격 플레이어 위치/상태 업데이트
    }
}
```

---

### 2. 플레이어 상태 동기화

```csharp
using UnityEngine;
using NeoSurvive.Network;

public class Player : Character
{
    private NetworkManager networkManager;
    private Rigidbody2D rb;

    private void Start()
    {
        networkManager = NetworkManager.Instance;
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        // 로컬 플레이어만 입력 처리
        if (IsLocalPlayer)
        {
            HandleInput();
        }
    }

    // NetworkManager의 Update()에서 자동으로 동기화되지만,
    // 수동으로 보낼 수도 있습니다:
    public void SendPositionManually()
    {
        if (networkManager.IsInCoopSession)
        {
            networkManager.SendLocalPlayerPosition(
                transform.position,
                rb.velocity,
                transform.rotation.eulerAngles.z
            );
        }
    }

    // 전체 상태 전송
    public PlayerState GetPlayerState()
    {
        return new PlayerState
        {
            playerId = networkManager.HttpAPI.PlayerId,
            nickname = "Player1",
            position = transform.position,
            velocity = rb.velocity,
            rotation = transform.rotation.eulerAngles.z,
            health = currentHealth,
            maxHealth = healthStat.GetValue(),
            level = level,
            experience = experience,
            isAlive = isAlive,
            activeWeapons = GetActiveWeapons(),
            activeBuffs = GetActiveBuffs()
        };
    }
}
```

---

### 3. 적 동기화 (호스트만)

```csharp
using UnityEngine;
using NeoSurvive.Network;

public class EnemyManager : MonoBehaviour
{
    private NetworkManager networkManager;

    private void Start()
    {
        networkManager = NetworkManager.Instance;

        // 원격에서 적 스폰 메시지 수신
        networkManager.OnEnemySpawned += OnRemoteEnemySpawned;
        networkManager.OnEnemyDied += OnRemoteEnemyDied;
    }

    // 호스트: 적 스폰
    public void SpawnEnemy(string enemyType, Vector2 position)
    {
        if (networkManager.IsHost)
        {
            // 로컬에 적 생성
            GameObject enemyObj = Instantiate(enemyPrefab, position, Quaternion.identity);
            Enemy enemy = enemyObj.GetComponent<Enemy>();

            // 네트워크로 전송
            var enemyState = new EnemyState
            {
                enemyId = enemy.GetInstanceID(),
                enemyType = enemyType,
                position = position,
                velocity = Vector2.zero,
                rotation = 0f,
                health = enemy.GetHealth(),
                maxHealth = enemy.GetMaxHealth(),
                isAlive = true,
                targetPlayerId = GetRandomPlayerId()
            };

            networkManager.SendEnemySpawn(enemyState);
        }
    }

    // 클라이언트: 원격 적 스폰 처리
    private void OnRemoteEnemySpawned(EnemyState enemyState)
    {
        // 적 오브젝트 생성
        GameObject enemyObj = Instantiate(enemyPrefab, enemyState.position, Quaternion.identity);
        Enemy enemy = enemyObj.GetComponent<Enemy>();
        enemy.InitializeFromNetworkState(enemyState);
    }

    // 적 사망 전송
    public void OnEnemyKilled(int enemyId, int killerPlayerId)
    {
        networkManager.SendEnemyDeath(enemyId, killerPlayerId);
    }

    // 원격 적 사망 처리
    private void OnRemoteEnemyDied(int enemyId)
    {
        // 적 오브젝트 제거
        Enemy enemy = FindEnemyById(enemyId);
        if (enemy != null)
        {
            Destroy(enemy.gameObject);
        }
    }
}
```

---

### 4. 아이템 동기화

```csharp
using UnityEngine;
using NeoSurvive.Network;

public class ItemManager : MonoBehaviour
{
    private NetworkManager networkManager;

    private void Start()
    {
        networkManager = NetworkManager.Instance;

        networkManager.OnItemDropped += OnRemoteItemDropped;
        networkManager.OnItemCollected += OnRemoteItemCollected;
    }

    // 호스트: 아이템 드랍
    public void DropItem(string itemType, Vector2 position, int value)
    {
        if (networkManager.IsHost)
        {
            int itemId = GenerateItemId();

            // 로컬에 아이템 생성
            GameObject itemObj = CreateItemObject(itemType, position);

            // 네트워크로 전송
            var itemState = new DropItemState
            {
                itemId = itemId,
                itemType = itemType,
                position = position,
                value = value,
                isCollected = false
            };

            networkManager.SendItemDrop(itemState);
        }
    }

    // 클라이언트: 원격 아이템 드랍 처리
    private void OnRemoteItemDropped(DropItemState itemState)
    {
        CreateItemObject(itemState.itemType, itemState.position);
    }

    // 아이템 수집
    public void CollectItem(int itemId)
    {
        // 로컬에서 수집 처리
        ProcessItemCollection(itemId);

        // 네트워크로 전송
        networkManager.SendItemCollect(itemId);
    }

    // 원격 아이템 수집 처리
    private void OnRemoteItemCollected(int itemId, int collectorPlayerId)
    {
        // 아이템 오브젝트 제거
        RemoveItemObject(itemId);
    }
}
```

---

## 🔧 추가 설정

### GameManager에서 NetworkManager 초기화

```csharp
// GameManager.cs
private void Awake()
{
    if (Instance == null)
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // NetworkManager 생성
        GameObject networkObj = new GameObject("NetworkManager");
        networkObj.AddComponent<NetworkManager>();
        DontDestroyOnLoad(networkObj);
    }
    else
    {
        Destroy(gameObject);
    }
}
```

---

## 📝 주의사항

1. **WebSocket 패키지 설치 필수**: NativeWebSocket 없이는 실시간 동기화 불가
2. **호스트 권한**: 적/아이템 스폰은 호스트만 가능
3. **동기화 빈도**:
   - 위치: 0.1초 (초당 10회)
   - 전체 상태: 1초 (초당 1회)
4. **ES3 직렬화**: 모든 네트워크 데이터는 ES3SerializationHelper 사용
5. **재연결 처리**: autoReconnect 활성화 권장

---

## 🎯 다음 단계

1. ✅ 패키지 설치 (NativeWebSocket)
2. ⬜ 백엔드 서버 구현 (API 명세 참고)
3. ⬜ Player 클래스에 네트워크 동기화 추가
4. ⬜ EnemyManager 수정
5. ⬜ ItemManager 수정
6. ⬜ UI 추가 (세션 코드 입력, 로비 등)
7. ⬜ 테스트 및 디버깅

---

**문서 위치:** `.docs/BACKEND_API_SPECIFICATION.md`

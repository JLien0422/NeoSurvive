using System.Collections.Generic;
using UnityEngine;

public class MapObjectTileSpawner : MonoBehaviour
{
    public static MapObjectTileSpawner Instance { get; private set; }

    private enum SpawnType
    {
        MapObject,
        EnvironmentGimmick
    }

    [System.Serializable]
    private class SpawnEntry
    {
        public GameObject prefab;

        [Range(0f, 1f)]
        public float spawnChance = 0.05f;

        public SpawnType spawnType = SpawnType.MapObject;

        [Header("위치 랜덤 오프셋")]
        public Vector2 randomOffsetMin = new Vector2(-0.2f, -0.2f);
        public Vector2 randomOffsetMax = new Vector2(0.2f, 0.2f);

        [Header("랜덤 회전")]
        public bool randomRotation = false;
    }

    private class SpawnedObjectInfo
    {
        public GameObject obj;
        public SpawnType spawnType;
        public Vector2Int originTileCoord;
    }

    [Header("Map Objects")]
    [SerializeField] private SpawnEntry[] mapObjectEntries;

    [Header("Environment Gimmicks")]
    [SerializeField] private SpawnEntry[] gimmickEntries;

    [Header("거리 제한")]
    [SerializeField] private float playerSafeRadius = 8f;
    [SerializeField] private float minDistanceBetweenMapObjects = 3f;
    [SerializeField] private float minDistanceBetweenGimmicks = 12f;

    [Header("유지 / 제거 범위")]
    [SerializeField] private float unloadMargin = 18f;
    [SerializeField] private float checkInterval = 0.35f;

    [Header("Boss Phase Cleanup")]
    [SerializeField] private bool clearObjectsOnBossTime = true;
    [SerializeField] private float bossStartTime = 900f;
    [SerializeField] private bool stopSpawningAfterBossTime = true;

    [Header("랜덤 Seed")]
    [SerializeField] private int seed = 12345;

    [Header("참조")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Transform player;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    private readonly Dictionary<Vector2Int, List<SpawnedObjectInfo>> objectsByTile = new();
    private readonly HashSet<Vector2Int> generatedTiles = new();

    private readonly List<Vector3> usedMapObjectPositions = new();
    private readonly List<Vector3> usedGimmickPositions = new();

    private Transform runtimeSpawnRoot;

    private float checkTimer;
    private bool bossCleanupDone = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        CreateRuntimeSpawnRoot();
    }

    private void Start()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (player == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }
    }

    private void Update()
    {
        if (clearObjectsOnBossTime &&
            !bossCleanupDone &&
            GetCurrentGameTime() >= bossStartTime)
        {
            ClearAllSpawnedObjectsForBossPhase();
        }

        if (bossCleanupDone && stopSpawningAfterBossTime)
            return;

        checkTimer += Time.deltaTime;

        if (checkTimer < checkInterval)
            return;

        checkTimer = 0f;
        CleanupFarMapObjects();
    }

    private void CreateRuntimeSpawnRoot()
    {
        GameObject rootObj = new GameObject("[Runtime_MapObjects_And_Gimmicks]");
        rootObj.transform.SetParent(transform);
        rootObj.transform.localPosition = Vector3.zero;
        runtimeSpawnRoot = rootObj.transform;
    }

    private float GetCurrentGameTime()
    {
        if (GameManager.Instance != null)
            return GameManager.Instance.GetGameTime();

        return Time.timeSinceLevelLoad;
    }

    public void OnTileSpawned(Vector2Int coord, Vector3 tileWorldPosition, float tileSize)
    {
        if (bossCleanupDone && stopSpawningAfterBossTime)
            return;

        if (generatedTiles.Contains(coord))
            return;

        generatedTiles.Add(coord);

        TrySpawnFromEntries(coord, tileWorldPosition, tileSize, mapObjectEntries, SpawnType.MapObject);
        TrySpawnFromEntries(coord, tileWorldPosition, tileSize, gimmickEntries, SpawnType.EnvironmentGimmick);
    }

    public void OnTileDespawned(Vector2Int coord)
    {
        // 타일이 사라져도 여기서 즉시 제거하지 않음.
    }

    private void TrySpawnFromEntries(
        Vector2Int coord,
        Vector3 tileWorldPosition,
        float tileSize,
        SpawnEntry[] entries,
        SpawnType spawnType)
    {
        if (bossCleanupDone && stopSpawningAfterBossTime)
            return;

        if (entries == null)
            return;

        foreach (SpawnEntry entry in entries)
        {
            if (entry == null || entry.prefab == null)
                continue;

            System.Random prng = GetPRNG(coord, entry.prefab.name);
            double roll = prng.NextDouble();

            if (roll > entry.spawnChance)
                continue;

            Vector3 spawnPosition = GetRandomPositionInTile(
                prng,
                tileWorldPosition,
                tileSize,
                entry.randomOffsetMin,
                entry.randomOffsetMax
            );

            if (!IsValidSpawnPosition(spawnPosition, spawnType))
                continue;

            Quaternion rotation = Quaternion.identity;

            if (entry.randomRotation)
            {
                float z = (float)(prng.NextDouble() * 360.0);
                rotation = Quaternion.Euler(0f, 0f, z);
            }

            GameObject spawned = Instantiate(entry.prefab, spawnPosition, rotation);

            // 핵심:
            // 이 스포너가 만든 모든 오브젝트를 전용 부모 밑으로 넣음.
            if (runtimeSpawnRoot != null)
                spawned.transform.SetParent(runtimeSpawnRoot, true);

            RegisterSpawnedObject(coord, spawned, spawnType);

            if (spawnType == SpawnType.EnvironmentGimmick)
                usedGimmickPositions.Add(spawnPosition);
            else
                usedMapObjectPositions.Add(spawnPosition);

            if (debugLog)
            {
                Debug.Log(
                    $"[MapObjectTileSpawner] Spawn | type={spawnType}, prefab={entry.prefab.name}, tile={coord}, pos={spawnPosition}"
                );
            }
        }
    }

    private Vector3 GetRandomPositionInTile(
        System.Random prng,
        Vector3 tileWorldPosition,
        float tileSize,
        Vector2 offsetMin,
        Vector2 offsetMax)
    {
        float half = tileSize * 0.5f;

        float x = Mathf.Lerp(-half, half, (float)prng.NextDouble());
        float y = Mathf.Lerp(-half, half, (float)prng.NextDouble());

        float offsetX = Mathf.Lerp(offsetMin.x, offsetMax.x, (float)prng.NextDouble());
        float offsetY = Mathf.Lerp(offsetMin.y, offsetMax.y, (float)prng.NextDouble());

        return new Vector3(
            tileWorldPosition.x + x + offsetX,
            tileWorldPosition.y + y + offsetY,
            0f
        );
    }

    private bool IsValidSpawnPosition(Vector3 position, SpawnType spawnType)
    {
        if (player != null)
        {
            float playerDistance = Vector2.Distance(position, player.position);

            if (playerDistance < playerSafeRadius)
                return false;
        }

        if (spawnType == SpawnType.EnvironmentGimmick)
        {
            foreach (Vector3 usedPos in usedGimmickPositions)
            {
                if (Vector2.Distance(position, usedPos) < minDistanceBetweenGimmicks)
                    return false;
            }

            return true;
        }

        foreach (Vector3 usedPos in usedMapObjectPositions)
        {
            if (Vector2.Distance(position, usedPos) < minDistanceBetweenMapObjects)
                return false;
        }

        return true;
    }

    private void RegisterSpawnedObject(Vector2Int coord, GameObject obj, SpawnType spawnType)
    {
        if (obj == null)
            return;

        if (!objectsByTile.ContainsKey(coord))
            objectsByTile[coord] = new List<SpawnedObjectInfo>();

        objectsByTile[coord].Add(new SpawnedObjectInfo
        {
            obj = obj,
            spawnType = spawnType,
            originTileCoord = coord
        });
    }

    private void CleanupFarMapObjects()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera == null)
            return;

        Rect unloadRect = GetCameraWorldRect(unloadMargin);

        List<Vector2Int> emptyTileKeys = new();

        foreach (var pair in objectsByTile)
        {
            List<SpawnedObjectInfo> list = pair.Value;

            for (int i = list.Count - 1; i >= 0; i--)
            {
                SpawnedObjectInfo info = list[i];

                if (info == null || info.obj == null)
                {
                    list.RemoveAt(i);
                    continue;
                }

                if (info.spawnType == SpawnType.EnvironmentGimmick)
                    continue;

                Vector2 pos = info.obj.transform.position;

                if (unloadRect.Contains(pos))
                    continue;

                Destroy(info.obj);
                list.RemoveAt(i);
            }

            if (list.Count == 0)
                emptyTileKeys.Add(pair.Key);
        }

        foreach (Vector2Int key in emptyTileKeys)
            objectsByTile.Remove(key);
    }

    public void ClearAllSpawnedObjectsForBossPhase()
    {
        if (bossCleanupDone)
            return;

        bossCleanupDone = true;

        int removedCount = 0;

        if (runtimeSpawnRoot != null)
        {
            for (int i = runtimeSpawnRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = runtimeSpawnRoot.GetChild(i);

                if (child == null)
                    continue;

                Destroy(child.gameObject);
                removedCount++;
            }
        }

        objectsByTile.Clear();
        generatedTiles.Clear();
        usedMapObjectPositions.Clear();
        usedGimmickPositions.Clear();

        if (debugLog)
        {
            Debug.Log(
                $"[MapObjectTileSpawner] 보스전 진입 정리 완료 | 제거 시도 수: {removedCount}"
            );
        }
    }

    private Rect GetCameraWorldRect(float margin)
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera == null)
            return new Rect();

        float height = targetCamera.orthographicSize * 2f;
        float width = height * targetCamera.aspect;

        Vector3 camPos = targetCamera.transform.position;

        return new Rect(
            camPos.x - width * 0.5f - margin,
            camPos.y - height * 0.5f - margin,
            width + margin * 2f,
            height + margin * 2f
        );
    }

    private System.Random GetPRNG(Vector2Int coord, string salt)
    {
        unchecked
        {
            int hash = seed;

            hash ^= coord.x * 73856093;
            hash ^= coord.y * 19349663;

            if (!string.IsNullOrEmpty(salt))
            {
                for (int i = 0; i < salt.Length; i++)
                {
                    hash = hash * 31 + salt[i];
                }
            }

            hash = (hash ^ (hash >> 16)) * -2048144789;
            hash = (hash ^ (hash >> 13)) * -1028477387;
            hash ^= hash >> 16;

            return new System.Random(hash);
        }
    }
}
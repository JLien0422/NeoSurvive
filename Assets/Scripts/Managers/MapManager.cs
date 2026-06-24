using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 오브젝트 풀링 기반 무한 타일 매니저
/// 플레이어 주변의 타일만 활성화하고, 범위를 벗어나면 풀로 반합합니다.
/// </summary>
public class MapManager : MonoBehaviour
{
  private const int TileSortingOrder = -10;
  private const int DecoTileSortingOrder = TileSortingOrder + 1;

  [System.Serializable]
  public class WeightedTilePrefab
  {
    public GameObject prefab;
    [Min(0f)] public float weight = 1f;
  }

  [Header("맵 프리팹 설정")]
  [Tooltip("기본 바닥 타일 후보와 가중치. 예: tile 50, 1-1 20, 3 20, 3-1 10")]
  public List<WeightedTilePrefab> baseTilePrefabs = new();

  [Tooltip("낮은 확률로 기본 타일 위에 추가 생성되는 데코 타일 프리팹")]
  public List<GameObject> decoTilePrefabs = new();

  [HideInInspector] public GameObject baseTilePrefab; // 기존 씬 참조 유지용 fallback

  [Header("맵 설정 (Pixel 방식)")]
  public bool autoSizeFromPrefab = true; // 프리팹 스프라이트 크기에 맞춰 자동 조절
  public float tilePixelSize = 32f;
  public float pixelsPerUnit = 100f;
  public float gap = 0f;
  public float tileZValue = 100f; // 절대 수정 금지. 100f로 정의하였음.

  [Header("최적화 및 생성 설정")]
  public int viewDistanceX = 10;
  public int viewDistanceY = 10;

  [Tooltip("타일이 삭제되기 전 추가로 유지되는 거리 (히스테리시스)")]
  public int despawnMargin = 2;
  public int seed = 42;

  [Header("데코 타일 생성 설정")]
  [Range(0f, 1f)] public float decoTileChance = 0.01f;
  [Tooltip("기존 데코 타일 기준 가로/세로 몇 칸 안에 새 데코를 금지할지")]
  [Min(0)] public int decoTileMinSpacing = 3;

  public Transform playerTransform;

  public void SetTarget(Transform target)
  {
    playerTransform = target;
    if (playerTransform != null)
    {
      int currentX = Mathf.FloorToInt(playerTransform.position.x / actualTileSize);
      int currentY = Mathf.FloorToInt(playerTransform.position.y / actualTileSize);
      lastCoord = new Vector2Int(currentX, currentY);
      UpdateTiles(currentX, currentY);
    }
  }

  // 타일 크기 계산용
  private float actualTileSize;
  private Dictionary<Vector2Int, GameObject> activeTiles = new();
  private Dictionary<Vector2Int, GameObject> activeDecoTiles = new();
  private Dictionary<Object, Rect> reservedTileAreas = new();
  private Vector2Int lastCoord = new Vector2Int(int.MinValue, int.MinValue);

  // 프리팹 인덱스별 풀 관리
  private Dictionary<int, Queue<GameObject>> basePool = new();
  private Dictionary<int, Queue<GameObject>> decoPool = new();

  private void Start()
  {
    UpdateActualTileSize();

    Camera cam = Camera.main;
    if (cam != null)
    {
      UpdateScreenSize(new Vector2(cam.orthographicSize * cam.aspect, cam.orthographicSize));
    }

    if (playerTransform != null)
    {
      int currentX = Mathf.FloorToInt(playerTransform.position.x / actualTileSize);
      int currentY = Mathf.FloorToInt(playerTransform.position.y / actualTileSize);
      lastCoord = new Vector2Int(currentX, currentY);
      UpdateTiles(currentX, currentY);
    }
  }

  private void UpdateActualTileSize()
  {
    // 타일 크기를 tilePixelSize/pixelsPerUnit으로 일관되게 계산
    actualTileSize = (tilePixelSize / pixelsPerUnit) + gap;
  }

  private void Update()
  {
    if (playerTransform == null)
    {
      // "Player" 태그로 찾기
      GameObject playerObj = GameObject.FindWithTag("Player");
      if (playerObj != null)
      {
        SetTarget(playerObj.transform);
      }
      else
      {
        return;
      }
    }


    int currentX = Mathf.FloorToInt(playerTransform.position.x / actualTileSize);
    int currentY = Mathf.FloorToInt(playerTransform.position.y / actualTileSize);

    // 플레이어가 새로운 타일 그리드로 이동했을 때만 업데이트하여 성능 최적화
    if (currentX != lastCoord.x || currentY != lastCoord.y)
    {
      lastCoord = new Vector2Int(currentX, currentY);
      UpdateTiles(currentX, currentY);
    }
  }

  public void ReserveTileArea(Object owner, Vector3 center, Vector2 size, float padding)
  {
    if (owner == null)
      return;

    if (actualTileSize <= 0f)
      UpdateActualTileSize();

    Vector2 paddedSize = size + Vector2.one * Mathf.Max(0f, padding) * 2f;
    Rect area = new Rect(
      center.x - paddedSize.x * 0.5f,
      center.y - paddedSize.y * 0.5f,
      paddedSize.x,
      paddedSize.y
    );

    reservedTileAreas[owner] = area;
    SpawnReservedAreaTiles(area);
  }

  public void ReleaseTileArea(Object owner)
  {
    if (owner == null)
      return;

    reservedTileAreas.Remove(owner);

    if (playerTransform == null)
      return;

    int currentX = Mathf.FloorToInt(playerTransform.position.x / actualTileSize);
    int currentY = Mathf.FloorToInt(playerTransform.position.y / actualTileSize);
    UpdateTiles(currentX, currentY);
  }

  void UpdateScreenSize(Vector2 size)
  {
    Camera cam = Camera.main;
    if (cam == null) return;

    // 카메라가 월드 공간에서 바라보는 높이와 너비를 구합니다.
    float worldHeight = cam.orthographicSize * 2f;
    float worldWidth = worldHeight * cam.aspect;

    // 월드 크기를 타일의 실제 월드 크기로 나누어 필요한 타일의 '반지름' 개수를 구합니다.
    // 배경이 보이지 않도록 충분한 여유를 둡니다
    viewDistanceX = (Mathf.CeilToInt((worldWidth / actualTileSize) * 1.0f) + 4) / 2;
    viewDistanceY = (Mathf.CeilToInt((worldHeight / actualTileSize) * 1.0f) + 4) / 2;

    // Debug.Log($"[MapManager] View distances updated: X={viewDistanceX}, Y={viewDistanceY} (Camera World View: {worldWidth:F1}x{worldHeight:F1})");
  }

  private void UpdateTiles(int currentX, int currentY)
  {
    int despawnDistX = viewDistanceX + despawnMargin;
    int despawnDistY = viewDistanceY + despawnMargin;

    List<Vector2Int> toRemove = new();
    foreach (var key in activeTiles.Keys)
    {
      bool outsidePlayerArea = Mathf.Abs(key.x - currentX) > despawnDistX || Mathf.Abs(key.y - currentY) > despawnDistY;
      if (outsidePlayerArea && !IsReservedTileCoord(key))
      {
        toRemove.Add(key);
      }
    }

    foreach (var key in toRemove)
    {
      ReturnTileToPool(key);
    }

    for (int x = currentX - viewDistanceX; x <= currentX + viewDistanceX; x++)
    {
      for (int y = currentY - viewDistanceY; y <= currentY + viewDistanceY; y++)
      {
        Vector2Int coord = new(x, y);
        if (!activeTiles.ContainsKey(coord))
        {
          SpawnTile(coord);
        }
      }
    }

    foreach (Rect area in reservedTileAreas.Values)
    {
      SpawnReservedAreaTiles(area);
    }
  }

  private void SpawnReservedAreaTiles(Rect area)
  {
    GetTileCoordRange(area, out Vector2Int min, out Vector2Int max);

    for (int x = min.x; x <= max.x; x++)
    {
      for (int y = min.y; y <= max.y; y++)
      {
        Vector2Int coord = new(x, y);
        if (!activeTiles.ContainsKey(coord))
        {
          SpawnTile(coord);
        }
      }
    }
  }

  private bool IsReservedTileCoord(Vector2Int coord)
  {
    Vector2 worldPos = new Vector2(coord.x * actualTileSize, coord.y * actualTileSize);
    foreach (Rect area in reservedTileAreas.Values)
    {
      if (area.Contains(worldPos))
        return true;
    }

    return false;
  }

  private void GetTileCoordRange(Rect area, out Vector2Int min, out Vector2Int max)
  {
    min = new Vector2Int(
      Mathf.FloorToInt(area.xMin / actualTileSize),
      Mathf.FloorToInt(area.yMin / actualTileSize)
    );

    max = new Vector2Int(
      Mathf.CeilToInt(area.xMax / actualTileSize),
      Mathf.CeilToInt(area.yMax / actualTileSize)
    );
  }

  private void SpawnTile(Vector2Int coord)
  {
    // 결정론적인 난수 생성기
    System.Random prng = GetPRNG(coord);

    if (!TrySelectBaseTile(prng, out int baseIndex, out GameObject basePrefab))
    {
      Debug.LogError("[MapManager] 생성 가능한 baseTilePrefabs가 없습니다. 인스펙터에 tile, 1-1, 3, 3-1 프리팹을 등록하세요.");
      return;
    }

    GameObject tile = GetFromPool(basePool, baseIndex, basePrefab);
    tile.name = $"BaseTile_{baseIndex}"; // 풀링 식별용 이름
    tile.transform.position = new Vector3(coord.x * actualTileSize, coord.y * actualTileSize, tileZValue);

    ApplyTileVisualSettings(tile, TileSortingOrder);

    tile.transform.rotation = Quaternion.identity;

    tile.SetActive(true);
    activeTiles.Add(coord, tile);

    TrySpawnDecoTile(coord, prng);

    if (MapObjectTileSpawner.Instance != null)
    {
      MapObjectTileSpawner.Instance.OnTileSpawned(
        coord,
        tile.transform.position,
        actualTileSize
      );
    }
  }

  private bool TrySelectBaseTile(System.Random prng, out int selectedIndex, out GameObject selectedPrefab)
  {
    selectedIndex = -1;
    selectedPrefab = null;

    float totalWeight = 0f;
    for (int i = 0; i < baseTilePrefabs.Count; i++)
    {
      WeightedTilePrefab entry = baseTilePrefabs[i];
      if (entry == null || entry.prefab == null || entry.weight <= 0f)
        continue;

      totalWeight += entry.weight;
    }

    if (totalWeight <= 0f)
    {
      if (baseTilePrefab == null)
        return false;

      selectedIndex = 0;
      selectedPrefab = baseTilePrefab;
      return true;
    }

    float roll = (float)(prng.NextDouble() * totalWeight);
    float cumulative = 0f;

    for (int i = 0; i < baseTilePrefabs.Count; i++)
    {
      WeightedTilePrefab entry = baseTilePrefabs[i];
      if (entry == null || entry.prefab == null || entry.weight <= 0f)
        continue;

      cumulative += entry.weight;
      if (roll <= cumulative)
      {
        selectedIndex = i;
        selectedPrefab = entry.prefab;
        return true;
      }
    }

    return false;
  }

  private void TrySpawnDecoTile(Vector2Int coord, System.Random prng)
  {
    if (decoTilePrefabs == null || decoTilePrefabs.Count == 0)
      return;

    if (prng.NextDouble() > decoTileChance)
      return;

    if (IsDecoTileBlocked(coord))
      return;

    int decoIndex = PickValidDecoIndex(prng);
    if (decoIndex < 0)
      return;

    GameObject decoTile = GetFromPool(decoPool, decoIndex, decoTilePrefabs[decoIndex]);
    decoTile.name = $"DecoTile_{decoIndex}";
    decoTile.transform.position = new Vector3(coord.x * actualTileSize, coord.y * actualTileSize, tileZValue);
    decoTile.transform.rotation = Quaternion.identity;
    ApplyTileVisualSettings(decoTile, DecoTileSortingOrder);
    decoTile.SetActive(true);
    activeDecoTiles.Add(coord, decoTile);
  }

  private int PickValidDecoIndex(System.Random prng)
  {
    int validCount = 0;
    for (int i = 0; i < decoTilePrefabs.Count; i++)
    {
      if (decoTilePrefabs[i] != null)
        validCount++;
    }

    if (validCount == 0)
      return -1;

    int selectedValidIndex = prng.Next(0, validCount);
    for (int i = 0; i < decoTilePrefabs.Count; i++)
    {
      if (decoTilePrefabs[i] == null)
        continue;

      if (selectedValidIndex == 0)
        return i;

      selectedValidIndex--;
    }

    return -1;
  }

  private bool IsDecoTileBlocked(Vector2Int coord)
  {
    foreach (Vector2Int decoCoord in activeDecoTiles.Keys)
    {
      if (Mathf.Abs(decoCoord.x - coord.x) <= decoTileMinSpacing &&
          Mathf.Abs(decoCoord.y - coord.y) <= decoTileMinSpacing)
      {
        return true;
      }
    }

    return false;
  }

  private void ApplyTileVisualSettings(GameObject tile, int sortingOrder)
  {
    // 타일이 실제 칸(actualTileSize)을 꽉 채우도록 스케일 조정
    // 1.01배로 약간 크게 만들어서 타일 사이 간격이 보이지 않도록 함
    var sr = tile.GetComponentInChildren<SpriteRenderer>(true);
    if (sr != null)
    {
      sr.sortingOrder = sortingOrder;
    }

    if (sr != null && sr.sprite != null)
    {
      float spriteWorldWidth = sr.sprite.rect.width / sr.sprite.pixelsPerUnit;
      if (spriteWorldWidth > 0)
      {
        float scale = ((actualTileSize - gap) / spriteWorldWidth) * 1.01f;
        tile.transform.localScale = new Vector3(scale, scale, 1f);
        return;
      }
    }

    tile.transform.localScale = Vector3.one;
  }

  private GameObject GetFromPool(Dictionary<int, Queue<GameObject>> poolDict, int index, GameObject prefab)
  {
    if (!poolDict.ContainsKey(index)) poolDict[index] = new Queue<GameObject>();

    if (poolDict[index].Count > 0)
    {
      return poolDict[index].Dequeue();
    }
    return Instantiate(prefab, transform);
  }

  private void ReturnTileToPool(Vector2Int coord)
  {
    if (activeTiles.TryGetValue(coord, out GameObject tile))
    {
      if (MapObjectTileSpawner.Instance != null)
      {
        MapObjectTileSpawner.Instance.OnTileDespawned(coord);
      }

      tile.SetActive(false);
      string[] info = tile.name.Split('_');
      if (info.Length >= 2 && int.TryParse(info[1], out int baseIndex))
      {
        if (!basePool.ContainsKey(baseIndex)) basePool[baseIndex] = new Queue<GameObject>();
        basePool[baseIndex].Enqueue(tile);
      }
      activeTiles.Remove(coord);
    }

    ReturnDecoTileToPool(coord);
  }

  private void ReturnDecoTileToPool(Vector2Int coord)
  {
    if (!activeDecoTiles.TryGetValue(coord, out GameObject decoTile))
      return;

    decoTile.SetActive(false);
    string[] info = decoTile.name.Split('_');
    if (info.Length >= 2 && int.TryParse(info[1], out int decoIndex))
    {
      if (!decoPool.ContainsKey(decoIndex)) decoPool[decoIndex] = new Queue<GameObject>();
      decoPool[decoIndex].Enqueue(decoTile);
    }

    activeDecoTiles.Remove(coord);
  }

  private System.Random GetPRNG(Vector2Int coord)
  {
    unchecked
    {
      // 거대 소수를 사용한 비트 믹싱 해시
      int hash = seed;
      hash ^= (coord.x * 73856093);
      hash ^= (coord.y * 19349663);

      hash = (hash ^ (hash >> 16)) * -2048144789;
      hash = (hash ^ (hash >> 13)) * -1028477387;
      hash ^= (hash >> 16);

      return new System.Random(hash);
    }
  }
}
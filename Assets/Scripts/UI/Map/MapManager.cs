using UnityEngine;
using System.Collections.Generic;

namespace NeoSurvive.UI.Map
{


  /// <summary>
  /// 오브젝트 풀링 기반 무한 타일 매니저
  /// 플레이어 주변의 타일만 활성화하고, 범위를 벗어나면 풀로 반합합니다.
  /// </summary>
  public class MapManager : MonoBehaviour
  {
    [Header("맵 프리팹 설정")]
    public GameObject baseTilePrefab;     // 기본 바닥 타일 (거의 모든 곳)
    public List<GameObject> flowerPrefabs;    // 꽃 장식 타일들
    public List<GameObject> grassPrefabs;     // 풀 장식 타일들

    public const float TILE_Z = 100f;

    [Header("맵 설정 (Pixel 방식)")]
    public float tilePixelSize = 32f;
    public float pixelsPerUnit = 100f;
    public float gap = 0f;

    [Header("최적화 및 생성 설정")]
    public int viewDistanceX = 10;
    public int viewDistanceY = 10;
    [Tooltip("타일이 삭제되기 전 추가로 유지되는 거리 (히스테리시스)")]
    public int despawnMargin = 2;
    public int seed = 42;
    [Range(0f, 1f)] public float flowerChance = 0.1f; // 꽃이 나올 확률
    [Range(0f, 1f)] public float grassChance = 0.2f;  // 풀이 나올 확률
    public Transform playerTransform;

    public void SetTarget(Transform target)
    {
      playerTransform = target;
      UpdateTiles(); // 즉시 타일 업데이트
    }

    // 타일 크기 계산용
    private float actualTileSize;
    private Dictionary<Vector2Int, GameObject> activeTiles = new();

    // 카테고리별 풀 관리를 위한 딕셔너리
    // 0: Base, 1: Flower, 2: Grass
    private Dictionary<int, Queue<GameObject>> basePool = new();
    private Dictionary<int, Queue<GameObject>> flowerPool = new();
    private Dictionary<int, Queue<GameObject>> grassPool = new();

    private void Start()
    {
      actualTileSize = (tilePixelSize / pixelsPerUnit) + gap;

      if (playerTransform != null)
      {
        UpdateTiles();
      }

      Camera cam = Camera.main;
      if (cam != null)
      {
        UpdateScreenSize(new Vector2(cam.pixelWidth, cam.pixelHeight));
      }
    }

    private void Update()
    {
      if (playerTransform == null) return;

      UpdateTiles();
    }

    void UpdateScreenSize(Vector2 size)
    {
      Camera cam = Camera.main;
      if (cam == null) return;

      // 카메라가 월드 공간에서 바라보는 높이와 너비를 구합니다.
      float worldHeight = cam.orthographicSize * 2f;
      float worldWidth = worldHeight * cam.aspect;

      // 월드 크기를 타일의 실제 월드 크기로 나누어 필요한 타일의 '반지름' 개수를 구합니다.
      viewDistanceX = Mathf.CeilToInt((worldWidth / actualTileSize) * 0.5f) + 2;
      viewDistanceY = Mathf.CeilToInt((worldHeight / actualTileSize) * 0.5f) + 2;

      Debug.Log($"[MapManager] View distances updated: X={viewDistanceX}, Y={viewDistanceY} (Camera World View: {worldWidth:F1}x{worldHeight:F1})");
    }

    private void UpdateTiles()
    {
      int currentX = Mathf.RoundToInt(playerTransform.position.x / actualTileSize);
      int currentY = Mathf.RoundToInt(playerTransform.position.y / actualTileSize);

      int despawnDistX = viewDistanceX + despawnMargin;
      int despawnDistY = viewDistanceY + despawnMargin;

      List<Vector2Int> toRemove = new();
      foreach (var key in activeTiles.Keys)
      {
        if (Mathf.Abs(key.x - currentX) > despawnDistX || Mathf.Abs(key.y - currentY) > despawnDistY)
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
    }

    private void SpawnTile(Vector2Int coord)
    {
      // 결정론적인 난수 생성기
      System.Random prng = GetPRNG(coord);
      double roll = prng.NextDouble();

      GameObject tile = null;
      int typeIndex = 0; // 0: Base, 1: Flower, 2: Grass
      int subIndex = 0;

      if (roll < flowerChance && flowerPrefabs.Count > 0)
      {
        typeIndex = 1;
        subIndex = prng.Next(0, flowerPrefabs.Count);
        tile = GetFromPool(flowerPool, subIndex, flowerPrefabs[subIndex]);
      }
      else if (roll < (flowerChance + grassChance) && grassPrefabs.Count > 0)
      {
        typeIndex = 2;
        subIndex = prng.Next(0, grassPrefabs.Count);
        tile = GetFromPool(grassPool, subIndex, grassPrefabs[subIndex]);
      }
      else
      {
        typeIndex = 0;
        subIndex = 0;
        tile = GetFromPool(basePool, subIndex, baseTilePrefab);
      }

      tile.name = $"Type_{typeIndex}_{subIndex}"; // 풀링 식별용 이름
      tile.transform.position = new Vector3(coord.x * actualTileSize, coord.y * actualTileSize, TILE_Z);

      // 혹시라도 이전에 설정된 부모나 스케일 등이 있다면 초기화
      tile.transform.rotation = Quaternion.identity;
      tile.transform.localScale = Vector3.one;

      tile.SetActive(true);
      activeTiles.Add(coord, tile);
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
        tile.SetActive(false);
        string[] info = tile.name.Split('_');
        if (info.Length >= 3)
        {
          int type = int.Parse(info[1]);
          int subIndex = int.Parse(info[2]);

          var targetPool = type == 0 ? basePool : (type == 1 ? flowerPool : grassPool);
          if (!targetPool.ContainsKey(subIndex)) targetPool[subIndex] = new Queue<GameObject>();
          targetPool[subIndex].Enqueue(tile);
        }
        activeTiles.Remove(coord);
      }
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
}

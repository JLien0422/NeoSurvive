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
    public int viewDistance = 3;
    public int seed = 42;
    [Range(0f, 1f)] public float flowerChance = 0.1f; // 꽃이 나올 확률
    [Range(0f, 1f)] public float grassChance = 0.2f;  // 풀이 나올 확률
    public Transform playerTransform;

    private float actualTileSize;
    private Dictionary<Vector2Int, GameObject> activeTiles = new();

    // 카테고리별 풀 관리를 위한 딕셔너리
    // 0: Base, 1: Flower, 2: Grass
    private Dictionary<int, Queue<GameObject>> basePool = new();
    private Dictionary<int, Queue<GameObject>> flowerPool = new();
    private Dictionary<int, Queue<GameObject>> grassPool = new();

    private void Start()
    {
      if (playerTransform == null)
        playerTransform = GameObject.FindWithTag("Player").transform;

      actualTileSize = (tilePixelSize / pixelsPerUnit) + gap;

      UpdateTiles();
    }

    private void Update()
    {
      if (playerTransform == null) return;
      UpdateTiles();
    }

    private void UpdateTiles()
    {
      int currentX = Mathf.RoundToInt(playerTransform.position.x / actualTileSize);
      int currentY = Mathf.RoundToInt(playerTransform.position.y / actualTileSize);

      List<Vector2Int> toRemove = new();
      foreach (var key in activeTiles.Keys)
      {
        if (Mathf.Abs(key.x - currentX) > viewDistance || Mathf.Abs(key.y - currentY) > viewDistance)
        {
          toRemove.Add(key);
        }
      }

      foreach (var key in toRemove)
      {
        ReturnTileToPool(key);
      }

      for (int x = currentX - viewDistance; x <= currentX + viewDistance; x++)
      {
        for (int y = currentY - viewDistance; y <= currentY + viewDistance; y++)
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

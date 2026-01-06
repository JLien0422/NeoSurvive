using UnityEngine;
using UnityEngine.Tilemaps;

namespace NeoSurvive.Map
{
    /// <summary>
    /// 간단한 타일맵 생성기
    /// </summary>
    public class MapGenerator : MonoBehaviour
    {
        public Tilemap floorTilemap;
        public TileBase floorTile;
        public int width = 50;
        public int height = 50;

        void Start()
        {
            GenerateMap();
        }

        public void GenerateMap()
        {
            if (floorTilemap == null || floorTile == null) return;

            floorTilemap.ClearAllTiles();

            for (int x = -width / 2; x < width / 2; x++)
            {
                for (int y = -height / 2; y < height / 2; y++)
                {
                    floorTilemap.SetTile(new Vector3Int(x, y, 0), floorTile);
                }
            }
        }
    }
}

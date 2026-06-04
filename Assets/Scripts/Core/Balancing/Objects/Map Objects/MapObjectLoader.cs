using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeoSurvive.Balancing.Map
{
    public class MapObjectLoader : MonoBehaviour
    {
        public static MapObjectDB DB { get; private set; }

        [Header("CSV - Map1")]
        [SerializeField] private TextAsset map1ObjectCsv;

        [Header("CSV - Map2")]
        [SerializeField] private TextAsset map2ObjectCsv;

        [Header("CSV - Map3")]
        [SerializeField] private TextAsset map3ObjectCsv;

        private void Awake()
        {
            Debug.Log("[MapObjectLoader] Awake");
            DontDestroyOnLoad(gameObject);
            Load();
        }

        public void Load()
        {
            DB = new MapObjectDB();

            LoadMapCsv("Map1", map1ObjectCsv);
            LoadMapCsv("Map2", map2ObjectCsv);
            LoadMapCsv("Map3", map3ObjectCsv);

            Debug.Log($"[MapObjectLoader] 전체 로드 완료: {DB.Count}");
        }

        private void LoadMapCsv(string mapName, TextAsset csvFile)
        {
            if (csvFile == null)
            {
                Debug.Log($"[MapObjectLoader] {mapName} 오브젝트 CSV가 할당되지 않았습니다.");
                return;
            }

            Debug.Log($"[MapObjectLoader] {mapName} csv = {csvFile.name}");
            LoadSingleCSV(csvFile);
        }

        private void LoadSingleCSV(TextAsset csvFile)
        {
            string csv = csvFile.text;

            var parsed = SimpleCsv.Parse(csv);

            foreach (var r in parsed)
            {
                var row = new MapObjectDB.Row();

                // 기본
                SimpleCsv.TryGetString(r, "mapId", out row.mapId);
                SimpleCsv.TryGetString(r, "id", out row.id);
                SimpleCsv.TryGetString(r, "name", out row.name);
                SimpleCsv.TryGetString(r, "type", out row.type);

                // Billboard
                row.autoFit = GetBool(r, "autoFit");
                SimpleCsv.TryGetFloat(r, "triggerPaddingX", out row.triggerPaddingX);
                SimpleCsv.TryGetFloat(r, "triggerPaddingY", out row.triggerPaddingY);
                SimpleCsv.TryGetFloat(r, "reducedOrthographicSize", out row.reducedOrthographicSize);
                SimpleCsv.TryGetFloat(r, "zoomLerpSpeed", out row.zoomLerpSpeed);
                SimpleCsv.TryGetString(r, "sortingLayerName", out row.sortingLayerName);

                if (int.TryParse(GetString(r, "sortingOrder"), out int order))
                    row.sortingOrder = order;

                // Vending
                SimpleCsv.TryGetFloat(r, "maxHp", out row.maxHp);
                row.dropRandomOne = GetBool(r, "dropRandomOne");
                row.dropBoth = GetBool(r, "dropBoth");
                SimpleCsv.TryGetFloat(r, "despawnDistance", out row.despawnDistance);
                SimpleCsv.TryGetFloat(r, "despawnDelayMin", out row.despawnDelayMin);
                SimpleCsv.TryGetFloat(r, "despawnDelayMax", out row.despawnDelayMax);
                row.enableRandomSpark = GetBool(r, "enableRandomSpark");
                SimpleCsv.TryGetFloat(r, "sparkIntervalMin", out row.sparkIntervalMin);
                SimpleCsv.TryGetFloat(r, "sparkIntervalMax", out row.sparkIntervalMax);
                SimpleCsv.TryGetFloat(r, "colliderPaddingX", out row.colliderPaddingX);
                SimpleCsv.TryGetFloat(r, "colliderPaddingY", out row.colliderPaddingY);

                SimpleCsv.TryGetString(r, "notes", out row.notes);

                if (string.IsNullOrWhiteSpace(row.mapId) || string.IsNullOrWhiteSpace(row.id))
                    continue;

                DB.Add(row);

                Debug.Log($"[MapObjectLoader] 로드됨 | {row.mapId} / {row.id} / HP={row.maxHp}");
            }

            Debug.Log($"[MapObjectLoader] CSV 완료: {csvFile.name}");
        }

        // ------------------------
        // 유틸
        // ------------------------

        private bool GetBool(Dictionary<string, string> row, string key)
        {
            if (!row.TryGetValue(key, out var value))
                return false;

            if (string.IsNullOrWhiteSpace(value))
                return false;

            value = value.Trim().ToLower();
            return value == "true" || value == "1";
        }

        private string GetString(Dictionary<string, string> row, string key)
        {
            if (!row.TryGetValue(key, out var value))
                return "";
            return value;
        }
    }
}
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEngine;

namespace NeoSurvive.Balancing.Map
{
    public class MapObjectLoader : MonoBehaviour
    {
        public static MapObjectDB DB { get; private set; }

        private void Awake()
        {
            Debug.Log("[MapObjectLoader] Awake");
            DontDestroyOnLoad(gameObject);
            Load();
        }

        public void Load()
        {
            DB = new MapObjectDB();

            string basePath = Path.Combine(Application.dataPath, "Scripts/Balancing/Map/Map1");
            Debug.Log("[MapObjectLoader] basePath = " + basePath);

            string[] files = Directory.GetFiles(basePath, "*.csv", SearchOption.AllDirectories);

            foreach (var file in files)
            {
                if (!file.Contains("map1_map_objects")) continue;

                LoadSingleCSV(file);
            }

            Debug.Log($"[MapObjectLoader] 전체 로드 완료: {DB.Count}");
        }

        private void LoadSingleCSV(string path)
        {
            string csv = "";

            try
            {
                using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (StreamReader sr = new StreamReader(fs, Encoding.UTF8))
                {
                    csv = sr.ReadToEnd();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[MapObjectLoader] CSV 읽기 실패: {path} | {e.Message}");
                return;
            }

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

            Debug.Log($"[MapObjectLoader] CSV 완료: {Path.GetFileName(path)}");
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
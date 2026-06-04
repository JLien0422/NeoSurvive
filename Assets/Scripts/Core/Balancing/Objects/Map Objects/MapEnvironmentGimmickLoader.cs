using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeoSurvive.Balancing.Map
{
    public class MapEnvironmentGimmickLoader : MonoBehaviour
    {
        public static MapEnvironmentGimmickDB DB { get; private set; }

        [Header("CSV - Map1")]
        [SerializeField] private TextAsset map1EnvironmentGimmickCsv;

        [Header("CSV - Map2")]
        [SerializeField] private TextAsset map2EnvironmentGimmickCsv;

        [Header("CSV - Map3")]
        [SerializeField] private TextAsset map3EnvironmentGimmickCsv;

        private void Awake()
        {
            Debug.Log("[MapLoader] Awake 진입");
            DontDestroyOnLoad(gameObject);
            Load();
            Debug.Log("[MapLoader] Awake 종료");
        }

        public void Load()
        {
            DB = new MapEnvironmentGimmickDB();

            LoadMapCsv("Map1", map1EnvironmentGimmickCsv);
            LoadMapCsv("Map2", map2EnvironmentGimmickCsv);
            LoadMapCsv("Map3", map3EnvironmentGimmickCsv);

            Debug.Log($"[MapLoader] 전체 로드 완료: {DB.Count}");
        }

        private void LoadMapCsv(string mapName, TextAsset csvFile)
        {
            if (csvFile == null)
            {
                Debug.Log($"[MapLoader] {mapName} 환경 기믹 CSV가 할당되지 않았습니다.");
                return;
            }

            Debug.Log($"[MapLoader] {mapName} csv = {csvFile.name}");
            LoadSingleCSV(csvFile);
        }

        private void LoadSingleCSV(TextAsset csvFile)
        {
            string csv = csvFile.text;

            var parsed = SimpleCsv.Parse(csv);

            foreach (var r in parsed)
            {
                var row = new MapEnvironmentGimmickDB.Row();

                // 공통
                SimpleCsv.TryGetString(r, "mapId", out row.mapId);
                SimpleCsv.TryGetString(r, "id", out row.id);

                row.isActive = GetBool(r, "isActive");
                row.canHack = GetBool(r, "canHack");
                row.blink = GetBool(r, "blink");

                // 펄스형 기믹
                SimpleCsv.TryGetFloat(r, "pulseInterval", out row.pulseInterval);
                SimpleCsv.TryGetFloat(r, "damage", out row.damage);
                SimpleCsv.TryGetFloat(r, "hackHoldTime", out row.hackHoldTime);

                SimpleCsv.TryGetString(r, "effectType", out row.effectType);
                SimpleCsv.TryGetFloat(r, "effectValue", out row.effectValue);
                SimpleCsv.TryGetFloat(r, "effectDuration", out row.effectDuration);

                SimpleCsv.TryGetString(r, "hackedEffectType", out row.hackedEffectType);
                SimpleCsv.TryGetFloat(r, "hackedEffectValue", out row.hackedEffectValue);
                SimpleCsv.TryGetFloat(r, "hackedEffectDuration", out row.hackedEffectDuration);

                // 크레인 / 범위형 기믹
                SimpleCsv.TryGetFloat(r, "areaWidth", out row.areaWidth);
                SimpleCsv.TryGetFloat(r, "areaHeight", out row.areaHeight);

                SimpleCsv.TryGetFloat(r, "firstDelay", out row.firstDelay);
                SimpleCsv.TryGetFloat(r, "warningDuration", out row.warningDuration);
                SimpleCsv.TryGetFloat(r, "intervalMin", out row.intervalMin);
                SimpleCsv.TryGetFloat(r, "intervalMax", out row.intervalMax);

                SimpleCsv.TryGetFloat(r, "blinkSpeed", out row.blinkSpeed);

                SimpleCsv.TryGetFloat(r, "impactWidth", out row.impactWidth);
                SimpleCsv.TryGetFloat(r, "impactHeight", out row.impactHeight);
                SimpleCsv.TryGetFloat(r, "playerDamage", out row.playerDamage);
                SimpleCsv.TryGetFloat(r, "enemyDamage", out row.enemyDamage);
                SimpleCsv.TryGetFloat(r, "impactDestroyAfter", out row.impactDestroyAfter);

                if (string.IsNullOrWhiteSpace(row.mapId) || string.IsNullOrWhiteSpace(row.id))
                    continue;

                DB.Add(row);

                Debug.Log(
                    $"[MapLoader] 로드됨 | {row.mapId} / {row.id} / " +
                    $"isActive={row.isActive} / canHack={row.canHack} / " +
                    $"blink={row.blink} / playerDamage={row.playerDamage}"
                );
            }

            Debug.Log($"[MapLoader] CSV 로드 완료: {csvFile.name}");
        }

        private bool GetBool(Dictionary<string, string> row, string key)
        {
            if (!row.TryGetValue(key, out var value))
                return false;

            if (string.IsNullOrWhiteSpace(value))
                return false;

            value = value.Trim().ToLower();
            return value == "true" || value == "1";
        }
    }
}
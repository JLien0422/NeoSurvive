using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEngine;

namespace NeoSurvive.Balancing.Map
{
    public class MapEnvironmentGimmickLoader : MonoBehaviour
    {
        public static MapEnvironmentGimmickDB DB { get; private set; }

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

            string basePath = Path.Combine(Application.dataPath, "Scripts/Balancing/Map/Map1");
            Debug.Log("[MapLoader] basePath = " + basePath);

            if (!Directory.Exists(basePath))
            {
                Debug.LogWarning("[MapLoader] Map1 폴더 없음: " + basePath);
                return;
            }

            string[] files = Directory.GetFiles(basePath, "*.csv", SearchOption.AllDirectories);

            foreach (string file in files)
            {
                LoadSingleCSV(file);
            }

            Debug.Log($"[MapLoader] 전체 로드 완료: {DB.Count}");
        }

        private void LoadSingleCSV(string path)
        {
            string csv = null;

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
                Debug.LogError($"[MapLoader] CSV 읽기 실패: {path} | {e.Message}");
                return;
            }

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

            Debug.Log($"[MapLoader] CSV 로드 완료: {Path.GetFileName(path)}");
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
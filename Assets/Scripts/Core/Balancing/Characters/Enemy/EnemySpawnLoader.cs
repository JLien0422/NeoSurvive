using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class EnemySpawnLoader : MonoBehaviour
{
    public static EnemySpawnDB DB { get; private set; }

    [Header("CSV")]
    [SerializeField] private TextAsset enemySpawnCsv;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        Load();
    }

    private void Load()
    {
        DB = new EnemySpawnDB();

        if (enemySpawnCsv == null)
        {
            Debug.LogError("[EnemySpawnLoader] enemySpawnCsv가 연결되지 않았습니다.");
            return;
        }

        var table = SimpleCsv.Parse(enemySpawnCsv.text);

        foreach (var row in table)
        {
            float phaseFloat;
            if (!SimpleCsv.TryGetFloat(row, "phase", out phaseFloat))
            {
                Debug.LogWarning("[EnemySpawnLoader] phase 값을 읽지 못했습니다.");
                continue;
            }

            int phase = Mathf.RoundToInt(phaseFloat);

            EnemySpawnDB.Row data = new EnemySpawnDB.Row();

            data.phase = phase;

            // ★ 일반 EnemySpawner 값
            data.minSpawnInterval = GetFloat(row, "minspawninterval", 2f);
            data.maxSpawnInterval = GetFloat(row, "maxspawninterval", 2f);

            // ★ 일반 스폰 + SiegeEvent가 공유하는 적 비율
            data.basicWeight = GetFloat(row, "basicweight", 100f);
            data.shooterWeight = GetFloat(row, "shooterweight", 0f);
            data.rusherWeight = GetFloat(row, "rusherweight", 0f);
            data.bomberWeight = GetFloat(row, "bomberweight", 0f);
            data.tankerWeight = GetFloat(row, "tankerweight", 0f);

            // ★ SiegeEvent 전용 값
            data.siegeInnerRadius = GetFloat(row, "siegeinnerradius", 9.5f);
            data.siegeOuterRadius = GetFloat(row, "siegeouterradius", 14.5f);
            data.siegeOuterCount = Mathf.RoundToInt(GetFloat(row, "siegeoutercount", 10f));
            data.siegeInnerCount = Mathf.RoundToInt(GetFloat(row, "siegeinnercount", 18f));
            data.siegeDuration = GetFloat(row, "siegeduration", 10f);
            data.freezeOuterShooters = GetBool(row, "freezeoutershooters", true);

            DB.rows[phase] = data;
        }

        Debug.Log($"[EnemySpawnLoader] 로드 완료 | count={DB.rows.Count}");
    }

    private float GetFloat(Dictionary<string, string> row, string key, float defaultValue)
    {
        float value;
        if (SimpleCsv.TryGetFloat(row, key, out value))
            return value;

        return defaultValue;
    }

    // ★ 추가: SimpleCsv에는 bool 파싱이 없으므로 Loader 안에서 처리
    private bool GetBool(Dictionary<string, string> row, string key, bool defaultValue)
    {
        if (!row.TryGetValue(key, out var value)) return defaultValue;
        if (string.IsNullOrWhiteSpace(value)) return defaultValue;

        value = value.Trim().ToLowerInvariant();

        if (value == "true") return true;
        if (value == "false") return false;
        if (value == "1") return true;
        if (value == "0") return false;

        return defaultValue;
    }
}
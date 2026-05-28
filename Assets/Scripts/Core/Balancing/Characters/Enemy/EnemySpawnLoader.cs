using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class EnemySpawnLoader : MonoBehaviour
{
    public static EnemySpawnDB DB { get; private set; }

    [Header("CSV")]
    [SerializeField] private TextAsset enemySpawnCsv;
    [SerializeField] private TextAsset phase1SegmentsCsv;
    [SerializeField] private TextAsset phase2To5Csv;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        Load();
    }

    private void Load()
    {
        DB = new EnemySpawnDB();

        bool loadedSplitCsv = false;

        if (phase1SegmentsCsv != null)
        {
            LoadPhase1Segments(phase1SegmentsCsv);
            loadedSplitCsv = true;
        }

        if (phase2To5Csv != null)
        {
            LoadPhaseRows(phase2To5Csv);
            loadedSplitCsv = true;
        }

        if (!loadedSplitCsv)
        {
            if (enemySpawnCsv == null)
            {
                Debug.LogError("[EnemySpawnLoader] 스폰 CSV가 연결되지 않았습니다.");
                return;
            }

            LoadPhaseRows(enemySpawnCsv);
        }
        else if (enemySpawnCsv != null)
        {
            LoadMissingLegacyRows(enemySpawnCsv);
        }

        DB.phase1Segments.Sort((a, b) => a.startTime.CompareTo(b.startTime));

        Debug.Log($"[EnemySpawnLoader] 로드 완료 | phaseRows={DB.rows.Count}, phase1Segments={DB.phase1Segments.Count}");
    }

    private void LoadPhaseRows(TextAsset csvAsset)
    {
        var table = SimpleCsv.Parse(csvAsset.text);

        foreach (var row in table)
        {
            float phaseFloat;
            if (!SimpleCsv.TryGetFloat(row, "phase", out phaseFloat))
            {
                Debug.LogWarning("[EnemySpawnLoader] phase 값을 읽지 못했습니다.");
                continue;
            }

            int phase = Mathf.RoundToInt(phaseFloat);

            EnemySpawnDB.Row data = BuildPhaseRow(row, phase);
            DB.rows[phase] = data;
        }
    }

    private void LoadPhase1Segments(TextAsset csvAsset)
    {
        var table = SimpleCsv.Parse(csvAsset.text);

        foreach (var row in table)
        {
            float segmentFloat;
            if (!SimpleCsv.TryGetFloat(row, "segment", out segmentFloat))
            {
                Debug.LogWarning("[EnemySpawnLoader] phase1 segment 값을 읽지 못했습니다.");
                continue;
            }

            var data = new EnemySpawnDB.Phase1SegmentRow();
            data.phase = 1;
            data.segment = Mathf.RoundToInt(segmentFloat);
            data.startTime = GetFloat(row, "starttime", 0f);
            data.endTime = GetFloat(row, "endtime", data.startTime + 30f);
            FillCommonSpawnFields(row, data);

            DB.phase1Segments.Add(data);
        }
    }

    private void LoadMissingLegacyRows(TextAsset csvAsset)
    {
        var table = SimpleCsv.Parse(csvAsset.text);

        foreach (var row in table)
        {
            float phaseFloat;
            if (!SimpleCsv.TryGetFloat(row, "phase", out phaseFloat))
                continue;

            int phase = Mathf.RoundToInt(phaseFloat);
            if (DB.rows.ContainsKey(phase))
                continue;

            DB.rows[phase] = BuildPhaseRow(row, phase);
        }
    }

    private EnemySpawnDB.Row BuildPhaseRow(Dictionary<string, string> row, int phase)
    {
        EnemySpawnDB.Row data = new EnemySpawnDB.Row();
        data.phase = phase;
        FillCommonSpawnFields(row, data);
        return data;
    }

    private void FillCommonSpawnFields(Dictionary<string, string> row, EnemySpawnDB.Row data)
    {
        // 일반 EnemySpawner 값
        data.minSpawnInterval = GetFloat(row, "minspawninterval", 2f);
        data.maxSpawnInterval = GetFloat(row, "maxspawninterval", data.minSpawnInterval);
        data.minSpawnCount = Mathf.Max(1, Mathf.RoundToInt(GetFloat(row, "minspawncount", GetFloat(row, "spawncount", 1f))));
        data.maxSpawnCount = Mathf.Max(data.minSpawnCount, Mathf.RoundToInt(GetFloat(row, "maxspawncount", data.minSpawnCount)));

        // 일반 스폰 + SiegeEvent가 공유하는 적 비율
        data.basicWeight = GetFloat(row, "basicweight", 100f);
        data.shooterWeight = GetFloat(row, "shooterweight", 0f);
        data.rusherWeight = GetFloat(row, "rusherweight", 0f);
        data.bomberWeight = GetFloat(row, "bomberweight", 0f);
        data.tankerWeight = GetFloat(row, "tankerweight", 0f);

        // SiegeEvent 전용 값
        data.siegeInnerRadius = GetFloat(row, "siegeinnerradius", 9.5f);
        data.siegeOuterRadius = GetFloat(row, "siegeouterradius", 14.5f);
        data.siegeOuterCount = Mathf.RoundToInt(GetFloat(row, "siegeoutercount", 10f));
        data.siegeInnerCount = Mathf.RoundToInt(GetFloat(row, "siegeinnercount", 18f));
        data.siegeDuration = GetFloat(row, "siegeduration", 10f);
        data.freezeOuterShooters = GetBool(row, "freezeoutershooters", true);
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
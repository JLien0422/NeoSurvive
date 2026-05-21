using UnityEngine;

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
            float phaseValue;
            if (!SimpleCsv.TryGetFloat(row, "phase", out phaseValue))
            {
                Debug.LogWarning("[EnemySpawnLoader] phase 값을 읽지 못했습니다.");
                continue;
            }

            int phase = Mathf.RoundToInt(phaseValue);

            EnemySpawnDB.Row data = new EnemySpawnDB.Row();

            data.phase = phase;

            data.spawnInterval = GetFloat(row, "spawninterval", 2f);
            data.minSpawnRadius = GetFloat(row, "minspawnradius", 5f);
            data.maxSpawnRadius = GetFloat(row, "maxspawnradius", 10f);

            data.basicWeight = GetFloat(row, "basicweight", 100f);
            data.shooterWeight = GetFloat(row, "shooterweight", 0f);
            data.rusherWeight = GetFloat(row, "rusherweight", 0f);
            data.bomberWeight = GetFloat(row, "bomberweight", 0f);
            data.tankerWeight = GetFloat(row, "tankerweight", 0f);

            DB.rows[phase] = data;
        }

        Debug.Log($"[EnemySpawnLoader] 로드 완료 | count={DB.rows.Count}");
    }

    private float GetFloat(System.Collections.Generic.Dictionary<string, string> row, string key, float defaultValue)
    {
        float value;
        if (SimpleCsv.TryGetFloat(row, key, out value))
        {
            return value;
        }

        return defaultValue;
    }
}
using UnityEngine;

public class EnemyStatLoader : MonoBehaviour
{
    public static EnemyStatDB DB { get; private set; }

    [Header("CSV")]
    [SerializeField] private TextAsset enemyStatCsv;
    [SerializeField] private TextAsset enemyHpScaleCsv;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        LoadEnemyStats();
        LoadHpScale();
    }

    private void LoadEnemyStats()
    {
        DB = new EnemyStatDB();

        if (enemyStatCsv == null)
        {
            Debug.LogWarning("[EnemyStatLoader] enemyStatCsv가 할당되지 않았습니다.");
            return;
        }

        Debug.Log($"[EnemyStatLoader] csv = {enemyStatCsv.name}");

        string csv = enemyStatCsv.text;

        var parsed = SimpleCsv.Parse(csv);

        foreach (var r in parsed)
        {
            if (!SimpleCsv.TryGetString(r, "enemyid", out string id)) continue;

            id = id.Trim().ToLower();

            var row = new EnemyStatDB.Row();

            SimpleCsv.TryGetFloat(r, "maxhp", out row.maxhp);
            SimpleCsv.TryGetFloat(r, "movespeed", out row.movespeed);
            SimpleCsv.TryGetFloat(r, "attackdamage", out row.attackdamage);

            DB.rows[id] = row;
        }

        Debug.Log($"[EnemyStatLoader] 로드 완료: {DB.rows.Count} rows");

        if (DB.rows.ContainsKey("basic"))
        {
            Debug.Log($"[EnemyStatLoader] basic maxhp: {DB.rows["basic"].maxhp}");
        }
    }

    private void LoadHpScale()
    {
        if (DB == null)
            DB = new EnemyStatDB();

        if (enemyHpScaleCsv == null)
        {
            Debug.LogWarning("[EnemyStatLoader] enemyHpScaleCsv가 없어 페이즈 HP 배율은 1.0으로 둡니다.");
            return;
        }

        var parsed = SimpleCsv.Parse(enemyHpScaleCsv.text);

        foreach (var r in parsed)
        {
            if (!SimpleCsv.TryGetFloat(r, "phase", out float phaseFloat))
            {
                Debug.LogWarning("[EnemyStatLoader] hp scale phase 값을 읽지 못했습니다.");
                continue;
            }

            int phase = Mathf.RoundToInt(phaseFloat);
            float multiplier = 1f;
            SimpleCsv.TryGetFloat(r, "hpmultiplier", out multiplier);

            if (multiplier <= 0f)
                multiplier = 1f;

            DB.hpMultipliers[phase] = multiplier;
        }

        Debug.Log($"[EnemyStatLoader] hp scale 로드 완료: {DB.hpMultipliers.Count} phases");
    }
}

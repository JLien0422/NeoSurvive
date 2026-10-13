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
            Debug.LogWarning("[EnemyStatLoader] enemyHpScaleCsv가 없어 기본 시간 HP 곡선을 씁니다.");
            return;
        }

        var parsed = SimpleCsv.Parse(enemyHpScaleCsv.text);
        if (parsed.Count == 0)
        {
            Debug.LogWarning("[EnemyStatLoader] enemy_hp_scale.csv가 비어 있어 기본 시간 HP 곡선을 씁니다.");
            return;
        }

        var r = parsed[0];
        if (!SimpleCsv.TryGetFloat(r, "durationsec", out float durationSec)
            || !SimpleCsv.TryGetFloat(r, "exponent", out float exponent)
            || !SimpleCsv.TryGetFloat(r, "startmultiplier", out float startMul)
            || !SimpleCsv.TryGetFloat(r, "endmultiplier", out float endMul))
        {
            Debug.LogWarning("[EnemyStatLoader] enemy_hp_scale.csv 컬럼을 읽지 못해 기본 시간 HP 곡선을 씁니다.");
            return;
        }

        if (durationSec <= 0f || exponent <= 0f || startMul <= 0f || endMul <= 0f)
        {
            Debug.LogWarning("[EnemyStatLoader] enemy_hp_scale.csv 값이 0 이하라 기본 시간 HP 곡선을 씁니다.");
            return;
        }

        DB.hpScaleDurationSec = durationSec;
        DB.hpScaleExponent = exponent;
        DB.hpScaleStartMultiplier = startMul;
        DB.hpScaleEndMultiplier = endMul;

        Debug.Log(
            $"[EnemyStatLoader] hp scale 로드 완료: duration={durationSec}, exponent={exponent}, start={startMul}, end={endMul}"
        );
    }
}

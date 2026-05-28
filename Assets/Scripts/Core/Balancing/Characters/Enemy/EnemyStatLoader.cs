using UnityEngine;

public class EnemyStatLoader : MonoBehaviour
{
    public static EnemyStatDB DB { get; private set; }

    [Header("CSV")]
    [SerializeField] private TextAsset enemyStatCsv;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        LoadEnemyStats();
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
}

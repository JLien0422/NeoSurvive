using UnityEngine;

public class DropTableLoader : MonoBehaviour
{
    public static DropTableDB DB { get; private set; }

    [Header("CSV")]
    [SerializeField] private TextAsset dropTableCsv;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        LoadDropTable();
    }

    private void LoadDropTable()
    {
        DB = new DropTableDB();

        if (dropTableCsv == null)
        {
            Debug.LogWarning("[DropTableLoader] dropTableCsv가 할당되지 않았습니다.");
            return;
        }

        Debug.Log($"[DropTableLoader] csv = {dropTableCsv.name}");

        string csv = dropTableCsv.text;

        var parsed = SimpleCsv.Parse(csv);

        foreach (var r in parsed)
        {
            if (!SimpleCsv.TryGetString(r, "enemyId", out string enemyId)) continue;

            enemyId = enemyId.Trim().ToLower();

            var row = new DropTableDB.Row();

            if (SimpleCsv.TryGetFloat(r, "experienceToGive", out float exp))
                row.experienceToGive = Mathf.RoundToInt(exp);

            if (SimpleCsv.TryGetFloat(r, "dropCount", out float count))
                row.dropCount = Mathf.RoundToInt(count);

            if (SimpleCsv.TryGetFloat(r, "scatterRadius", out float radius))
                row.scatterRadius = radius;

            if (SimpleCsv.TryGetFloat(r, "goldDropChance", out float goldChance))
                row.goldDropChance = goldChance;

            if (SimpleCsv.TryGetFloat(r, "psychoCorruptionDropChance", out float psychoChance))
                row.psychoCorruptionDropChance = psychoChance;

            if (SimpleCsv.TryGetFloat(r, "dataChipDropChance", out float chipChance))
                row.dataChipDropChance = chipChance;

            DB.rows[enemyId] = row;
        }

        Debug.Log($"[DropTableLoader] 로드 완료: {DB.rows.Count} rows");

        if (DB.rows.ContainsKey("basic"))
        {
            Debug.Log($"[DropTableLoader] basic exp = {DB.rows["basic"].experienceToGive}");
        }
    }
}

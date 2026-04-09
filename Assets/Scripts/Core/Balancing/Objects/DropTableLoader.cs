using System;
using System.IO;
using System.Text;
using UnityEngine;

public class DropTableLoader : MonoBehaviour
{
    public static DropTableDB DB { get; private set; }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        LoadDropTable();
    }

    private void LoadDropTable()
    {
        DB = new DropTableDB();

        string path = Path.Combine(Application.dataPath, "Scripts/Balancing/Objects/enemy_drops.csv");
        Debug.Log("[DropTableLoader] path = " + path);

        if (!File.Exists(path))
        {
            Debug.LogWarning($"[DropTableLoader] enemy_drops.csv 없음: {path}");
            return;
        }

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
            Debug.LogError($"[DropTableLoader] CSV 읽기 실패: {e.Message}");
            return;
        }

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

using System;
using System.IO;
using System.Text;
using UnityEngine;

public class EnemyStatLoader : MonoBehaviour
{
    public static EnemyStatDB DB { get; private set; }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        LoadEnemyStats();
    }

    private void LoadEnemyStats()
    {
        DB = new EnemyStatDB();

        string path = Path.Combine(Application.dataPath, "Scripts/Balancing/Enemy/enemy_stats.csv");
        Debug.Log("[EnemyStatLoader] path = " + path);

        if (!File.Exists(path))
        {
            Debug.LogWarning($"[EnemyStatLoader] enemy_stats.csv 없음: {path}");
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
            Debug.LogError($"[EnemyStatLoader] CSV 읽기 실패: {e.Message}");
            return;
        }

        var parsed = SimpleCsv.Parse(csv);

        foreach (var r in parsed)
        {
            if (!SimpleCsv.TryGetString(r, "enemyid", out string id)) continue;

            id = id.Trim().ToLower();

            var row = new EnemyStatDB.Row();

            SimpleCsv.TryGetFloat(r, "maxhp", out row.maxhp);
            SimpleCsv.TryGetFloat(r, "movespeed", out row.movespeed);
            SimpleCsv.TryGetFloat(r, "attackdamage", out row.attackdamage);
            SimpleCsv.TryGetFloat(r, "attackrange", out row.attackrange);

            DB.rows[id] = row;
        }

        Debug.Log($"[EnemyStatLoader] 로드 완료: {DB.rows.Count} rows");

        if (DB.rows.ContainsKey("basic"))
        {
            Debug.Log($"[EnemyStatLoader] basic maxhp: {DB.rows["basic"].maxhp}");
        }
    }
}

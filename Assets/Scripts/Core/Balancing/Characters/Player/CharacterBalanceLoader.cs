using System.IO;
using UnityEngine;

public class CharacterBalanceLoader : MonoBehaviour
{
    public static CharacterBalanceDB DB { get; private set; }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        LoadCharacters();
    }

    private void LoadCharacters()
    {
        DB = new CharacterBalanceDB();

        string path = Path.Combine(Application.dataPath, "Scripts/Balancing/Player/characters.csv");
        Debug.Log("[CharacterBalanceLoader] path = " + path);

        if (!File.Exists(path))
        {
            Debug.LogWarning($"[CharacterBalanceLoader] characters.csv 없음: {path}");
            return;
        }

        string csv = File.ReadAllText(path);
        var parsed = SimpleCsv.Parse(csv);

        foreach (var r in parsed)
        {
            if (!SimpleCsv.TryGetString(r, "id", out string id)) continue;

            id = id.Trim().ToLower();

            var row = new CharacterBalanceDB.Row();

            SimpleCsv.TryGetFloat(r, "baseHealth", out row.baseHealth);
            SimpleCsv.TryGetFloat(r, "baseAttackDamage", out row.baseAttackDamage);
            SimpleCsv.TryGetFloat(r, "baseAttackRange", out row.baseAttackRange);
            SimpleCsv.TryGetFloat(r, "baseAttackSpeed", out row.baseAttackSpeed);
            SimpleCsv.TryGetFloat(r, "baseMoveSpeed", out row.baseMoveSpeed);

            DB.rows[id] = row;
        }

        Debug.Log($"[CharacterBalanceLoader] 로드 완료: {DB.rows.Count} rows");

        if (DB.rows.ContainsKey("hacker"))
            Debug.Log($"[CharacterBalanceLoader] baseHealth: {DB.rows["hacker"].baseHealth}");
    }
}

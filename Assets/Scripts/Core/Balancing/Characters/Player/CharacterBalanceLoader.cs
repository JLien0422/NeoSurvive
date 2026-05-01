using UnityEngine;

public class CharacterBalanceLoader : MonoBehaviour
{
    public static CharacterBalanceDB DB { get; private set; }

    [Header("CSV")]
    [SerializeField] private TextAsset characterCsv;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        LoadCharacters();
    }

    private void LoadCharacters()
    {
        DB = new CharacterBalanceDB();

        if (characterCsv == null)
        {
            Debug.LogWarning("[CharacterBalanceLoader] characterCsv가 할당되지 않았습니다.");
            return;
        }

        Debug.Log($"[CharacterBalanceLoader] csv = {characterCsv.name}");

        string csv = characterCsv.text;
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

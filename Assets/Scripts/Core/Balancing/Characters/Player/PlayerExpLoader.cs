using UnityEngine;

namespace NeoSurvive.Balancing.Player
{
    /*
     * [파일 설명]
     * player_exp_table.csv(TextAsset)을 읽어서
     * PlayerExpDB에 레벨별 경험치 데이터를 저장하는 Loader
     *
     * [기존 방식과 차이]
     * - File.ReadAllText → ❌ 제거
     * - TextAsset → ✅ 사용 (Unity 표준 방식)
     *
     * [연결 흐름]
     * player_exp_table.csv (TextAsset)
     *   -> SimpleCsv.Parse()
     *   -> PlayerExpDB.Row 생성
     *   -> DB.rows[level] 저장
     *   -> Player.cs에서 GetRequiredExp(level)로 사용
     *
     * [주의]
     * 반드시 인스펙터에서 playerExpCsv를 할당해야 한다.
     */

    public class PlayerExpLoader : MonoBehaviour
    {
        public static PlayerExpDB DB { get; private set; }

        [Header("CSV")]
        [SerializeField] private TextAsset playerExpCsv;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            LoadExpTable();
        }

        private void LoadExpTable()
        {
            DB = new PlayerExpDB();

            if (playerExpCsv == null)
            {
                Debug.LogWarning("[PlayerExpLoader] playerExpCsv가 할당되지 않았습니다.");
                return;
            }

            Debug.Log($"[PlayerExpLoader] csv = {playerExpCsv.name}");

            string csv = playerExpCsv.text;

            /*
             * SimpleCsv.Parse 결과:
             * row["level"] = "1"
             * row["requiredExp"] = "125"
             * row["totalExp"] = "0"
             */
            var parsed = SimpleCsv.Parse(csv);

            foreach (var r in parsed)
            {
                // level은 반드시 필요
                if (!SimpleCsv.TryGetString(r, "level", out string levelStr)) continue;

                if (!int.TryParse(levelStr, out int level))
                {
                    Debug.LogWarning($"[PlayerExpLoader] level 파싱 실패: {levelStr}");
                    continue;
                }

                var row = new PlayerExpDB.Row
                {
                    level = level
                };

                // requiredExp
                if (SimpleCsv.TryGetString(r, "requiredExp", out string reqStr))
                    int.TryParse(reqStr, out row.requiredExp);

                // totalExp
                if (SimpleCsv.TryGetString(r, "totalExp", out string totalStr))
                    int.TryParse(totalStr, out row.totalExp);

                DB.Add(row);
            }

            Debug.Log($"[PlayerExpLoader] 로드 완료: {DB.Count} rows");

            // 디버그 확인용
            if (DB.TryGet(1, out var first))
            {
                Debug.Log($"[PlayerExpLoader] Lv1 requiredExp = {first.requiredExp}");
            }
        }

        // ===================== Player에서 사용하는 API =====================

        public static int GetRequiredExp(int level, int fallback = 0)
        {
            if (DB == null)
            {
                Debug.LogWarning("[PlayerExpLoader] DB null");
                return fallback;
            }

            return DB.GetRequiredExp(level, fallback);
        }

        public static int GetTotalExp(int level, int fallback = 0)
        {
            if (DB == null)
            {
                Debug.LogWarning("[PlayerExpLoader] DB null");
                return fallback;
            }

            return DB.GetTotalExp(level, fallback);
        }

        public static int GetMaxLevel(int fallback = 30)
        {
            if (DB == null)
            {
                Debug.LogWarning("[PlayerExpLoader] DB null");
                return fallback;
            }

            int max = DB.GetMaxLevel();
            return max > 0 ? max : fallback;
        }
    }
}
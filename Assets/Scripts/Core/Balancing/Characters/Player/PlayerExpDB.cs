using System.Collections.Generic;

namespace NeoSurvive.Balancing.Player
{
    /*
     * player_exp_table.csv의 레벨별 경험치 데이터를 저장하는 DB.
     *
     * 연결 흐름:
     * player_exp_table.csv
     * -> PlayerExpLoader가 로드
     * -> PlayerExpDB에 level 기준 저장
     * -> Player.cs가 현재 level의 requiredExp를 조회
     */

    public class PlayerExpDB
    {
        public class Row
        {
            public int level;
            public int requiredExp;
            public int totalExp;
        }

        private readonly Dictionary<int, Row> rows = new Dictionary<int, Row>();

        public int Count => rows.Count;

        public void Add(Row row)
        {
            if (row == null) return;
            rows[row.level] = row;
        }

        public bool TryGet(int level, out Row row)
        {
            return rows.TryGetValue(level, out row);
        }

        public int GetRequiredExp(int level, int fallback = 0)
        {
            return rows.TryGetValue(level, out var row) ? row.requiredExp : fallback;
        }

        public int GetTotalExp(int level, int fallback = 0)
        {
            return rows.TryGetValue(level, out var row) ? row.totalExp : fallback;
        }

        public int GetMaxLevel()
        {
            int max = 0;

            foreach (var kv in rows)
            {
                if (kv.Key > max)
                    max = kv.Key;
            }

            return max;
        }

        public IEnumerable<Row> AllRows => rows.Values;
    }
}
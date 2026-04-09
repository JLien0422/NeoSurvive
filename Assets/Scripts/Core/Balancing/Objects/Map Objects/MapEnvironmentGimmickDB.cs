using System.Collections.Generic;

namespace NeoSurvive.Balancing.Map
{
    public class MapEnvironmentGimmickDB
    {
        public class Row
        {
            public string mapId;
            public string id;

            public bool isActive;
            public bool canHack;

            public float pulseInterval;
            public float damage;
            public float hackHoldTime;

            public string effectType;
            public float effectValue;
            public float effectDuration;

            public string hackedEffectType;
            public float hackedEffectValue;
            public float hackedEffectDuration;

            public float areaWidth;
            public float areaHeight;

            public float firstDelay;
            public float warningDuration;
            public float intervalMin;
            public float intervalMax;

            public bool blink;
            public float blinkSpeed;

            public float impactWidth;
            public float impactHeight;
            public float playerDamage;
            public float enemyDamage;
            public float impactDestroyAfter;
        }

        // 🔥 핵심 구조 (mapId + id)
        private Dictionary<string, Row> rows = new Dictionary<string, Row>();

        private string MakeKey(string mapId, string id)
        {
            return $"{mapId}_{id}".ToLower();
        }

        public void Add(Row row)
        {
            string key = MakeKey(row.mapId, row.id);
            rows[key] = row;
        }

        public Row Get(string mapId, string id)
        {
            string key = MakeKey(mapId, id);

            if (rows.TryGetValue(key, out var row))
                return row;

            return null;
        }

        public int Count => rows.Count;
    }
}
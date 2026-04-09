using System.Collections.Generic;

namespace NeoSurvive.Balancing.Map
{
    public class MapObjectDB
    {
        public class Row
        {
            public string mapId;
            public string id;
            public string name;
            public string type;

            // Billboard
            public bool autoFit;
            public float triggerPaddingX;
            public float triggerPaddingY;
            public float reducedOrthographicSize;
            public float zoomLerpSpeed;
            public string sortingLayerName;
            public int sortingOrder;

            // Vending Machine
            public float maxHp;
            public bool dropRandomOne;
            public bool dropBoth;
            public float despawnDistance;
            public float despawnDelayMin;
            public float despawnDelayMax;
            public bool enableRandomSpark;
            public float sparkIntervalMin;
            public float sparkIntervalMax;
            public float colliderPaddingX;
            public float colliderPaddingY;

            public string notes;
        }

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
using System.Collections.Generic;

public class EnemySpawnDB
{
    public class Row
    {
        public int phase;

        public float spawnInterval;
        public float minSpawnRadius;
        public float maxSpawnRadius;

        public float basicWeight;
        public float shooterWeight;
        public float rusherWeight;
        public float bomberWeight;
        public float tankerWeight;
    }

    public readonly Dictionary<int, Row> rows = new Dictionary<int, Row>();

    public bool TryGetRow(int phase, out Row row)
    {
        return rows.TryGetValue(phase, out row);
    }
}

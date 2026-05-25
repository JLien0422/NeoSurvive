using System.Collections.Generic;

public class EnemySpawnDB
{
    public class Row
    {
        public int phase;

        public float minSpawnInterval;
        public float maxSpawnInterval;

        public float basicWeight;
        public float shooterWeight;
        public float rusherWeight;
        public float bomberWeight;
        public float tankerWeight;

        // ★ 추가: SiegeEvent 전용 값
        public float siegeInnerRadius;
        public float siegeOuterRadius;
        public int siegeOuterCount;
        public int siegeInnerCount;
        public float siegeDuration;
        public bool freezeOuterShooters;
    }

    public readonly Dictionary<int, Row> rows = new();

    public bool TryGetRow(int phase, out Row row)
    {
        return rows.TryGetValue(phase, out row);
    }
}
using System.Collections.Generic;

public class EnemySpawnDB
{
    public class Row
    {
        public int phase;

        public float minSpawnInterval;
        public float maxSpawnInterval;
        public int minSpawnCount = 1;
        public int maxSpawnCount = 1;

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

    public class Phase1SegmentRow : Row
    {
        public int segment;
        public float startTime;
        public float endTime;
    }

    public readonly Dictionary<int, Row> rows = new();
    public readonly List<Phase1SegmentRow> phase1Segments = new();

    public bool TryGetRow(int phase, out Row row)
    {
        return rows.TryGetValue(phase, out row);
    }

    public bool TryGetPhase1Segment(float gameTime, out Phase1SegmentRow row)
    {
        row = null;

        if (phase1Segments.Count == 0)
            return false;

        for (int i = 0; i < phase1Segments.Count; i++)
        {
            Phase1SegmentRow candidate = phase1Segments[i];

            if (gameTime >= candidate.startTime && gameTime < candidate.endTime)
            {
                row = candidate;
                return true;
            }
        }

        row = gameTime < phase1Segments[0].startTime
            ? phase1Segments[0]
            : phase1Segments[phase1Segments.Count - 1];

        return true;
    }
}
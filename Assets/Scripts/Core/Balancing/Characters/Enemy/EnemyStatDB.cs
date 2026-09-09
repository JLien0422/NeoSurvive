using System.Collections.Generic;

public class EnemyStatDB
{
    public class Row
    {
        public float maxhp;
        public float movespeed;
        public float attackdamage;
    }

    public Dictionary<string, Row> rows = new Dictionary<string, Row>();
    public readonly Dictionary<int, float> hpMultipliers = new Dictionary<int, float>();

    public float GetHpMultiplier(int phase)
    {
        if (hpMultipliers.TryGetValue(phase, out float multiplier) && multiplier > 0f)
            return multiplier;

        return 1f;
    }
}

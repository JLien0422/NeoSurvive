using System.Collections.Generic;

public class DropTableDB
{
    public class Row
    {
        public int experienceToGive;
        public int dropCount;
        public float scatterRadius;

        public float goldDropChance;
        public float psychoCorruptionDropChance;
        public float dataChipDropChance;
    }

    public Dictionary<string, Row> rows = new Dictionary<string, Row>();
}

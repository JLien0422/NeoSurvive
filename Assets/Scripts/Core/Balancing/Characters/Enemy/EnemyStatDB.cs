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
}

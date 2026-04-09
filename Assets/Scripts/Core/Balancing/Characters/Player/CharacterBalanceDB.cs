using System.Collections.Generic;

public class CharacterBalanceDB
{
    public class Row
    {
        public float baseHealth;
        public float baseAttackDamage;
        public float baseAttackRange;
        public float baseAttackSpeed;
        public float baseMoveSpeed;
    }

    public Dictionary<string, Row> rows = new Dictionary<string, Row>();
}

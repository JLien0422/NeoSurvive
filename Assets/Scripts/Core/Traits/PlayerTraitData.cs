using System;
using System.Collections.Generic;

[Serializable]
public class PlayerTraitData
{
    public Dictionary<string, int> acquiredTraits;

    public PlayerTraitData()
    {
        acquiredTraits = new Dictionary<string, int>();
    }
}

using System;
using System.Collections.Generic;

[Serializable]
public class Trait
{
    public string id;
    public string name;
    public string description;
    public int tier;
    public int maxLevel;
    public List<string> prerequisites;
    public TraitEffect effect;
}

[Serializable]
public class TraitEffect
{
    public string stat;
    public float value;
    public string type; // "Flat" or "Percentage"
}

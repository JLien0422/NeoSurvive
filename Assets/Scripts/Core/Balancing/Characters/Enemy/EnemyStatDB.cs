using System;
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

    public float hpScaleDurationSec = 900f;
    public float hpScaleExponent = 2f;
    public float hpScaleStartMultiplier = 0.416667f;
    public float hpScaleEndMultiplier = 8.75f;

    public float GetHpMultiplier(float gameTime)
    {
        if (hpScaleDurationSec <= 0f)
            return hpScaleStartMultiplier > 0f ? hpScaleStartMultiplier : 1f;

        float t = gameTime / hpScaleDurationSec;
        if (t < 0f)
            t = 0f;
        else if (t > 1f)
            t = 1f;

        float curve = (float)Math.Pow(t, hpScaleExponent);
        float multiplier = hpScaleStartMultiplier
            + (hpScaleEndMultiplier - hpScaleStartMultiplier) * curve;

        return multiplier > 0f ? multiplier : 1f;
    }
}

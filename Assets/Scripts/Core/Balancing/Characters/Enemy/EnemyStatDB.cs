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

    // 전시용 임시. 1이면 원래 체력. 되돌릴 때는 이 값만 1로 바꾸면 됩니다.
    public const float ExhibitionHpMultiplier = 0.5f;

    public float GetHpMultiplier(float gameTime)
    {
        if (hpScaleDurationSec <= 0f)
        {
            float fallback = hpScaleStartMultiplier > 0f ? hpScaleStartMultiplier : 1f;
            return fallback * ExhibitionHpMultiplier;
        }

        float t = gameTime / hpScaleDurationSec;
        if (t < 0f)
            t = 0f;
        else if (t > 1f)
            t = 1f;

        float curve = (float)Math.Pow(t, hpScaleExponent);
        float multiplier = hpScaleStartMultiplier
            + (hpScaleEndMultiplier - hpScaleStartMultiplier) * curve;

        float resolved = multiplier > 0f ? multiplier : 1f;
        return resolved * ExhibitionHpMultiplier;
    }
}

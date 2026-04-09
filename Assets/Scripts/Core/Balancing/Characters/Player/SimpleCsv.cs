using System;
using System.Collections.Generic;
using System.Globalization;

public static class SimpleCsv
{
    public static List<Dictionary<string, string>> Parse(string csv)
    {
        var rows = new List<Dictionary<string, string>>();
        var lines = csv.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length <= 1) return rows;

        var headers = lines[0].Split(',');

        for (int i = 1; i < lines.Length; i++)
        {
            var cols = lines[i].Split(',');
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            for (int c = 0; c < headers.Length; c++)
            {
                var key = headers[c].Trim();
                var val = (c < cols.Length ? cols[c] : "").Trim();
                dict[key] = val;
            }
            rows.Add(dict);
        }

        return rows;
    }

    // 한국/윈도우 환경에서 소수점 파싱 문제 방지(4.5 같은 점 표기 고정)
    public static bool TryGetFloat(Dictionary<string, string> row, string key, out float value)
    {
        value = 0f;
        if (!row.TryGetValue(key, out var s)) return false;
        if (string.IsNullOrWhiteSpace(s)) return false;

        return float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    public static bool TryGetString(Dictionary<string, string> row, string key, out string value)
    {
        value = null;
        if (!row.TryGetValue(key, out var s)) return false;
        if (string.IsNullOrWhiteSpace(s)) return false;

        value = s.Trim();
        return true;
    }
}

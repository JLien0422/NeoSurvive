using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

public static class SimpleCsv
{
    public static List<Dictionary<string, string>> Parse(string csv)
    {
        var rows = new List<Dictionary<string, string>>();
        var table = ParseRows(csv);
        if (table.Count <= 1)
            return rows;

        var headers = table[0];
        if (headers.Count > 0)
            headers[0] = headers[0].TrimStart('\uFEFF');

        for (int i = 1; i < table.Count; i++)
        {
            var cols = table[i];
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            for (int c = 0; c < headers.Count; c++)
            {
                var key = headers[c].Trim();
                var val = (c < cols.Count ? cols[c] : "").Trim();
                dict[key] = val;
            }
            rows.Add(dict);
        }

        return rows;
    }

    /// <summary>
    /// RFC 4180 형식의 CSV를 행/열로 파싱합니다.
    /// 따옴표 필드, 필드 안의 쉼표와 줄바꿈, 연속 따옴표("")를 지원합니다.
    /// </summary>
    public static List<List<string>> ParseRows(string csv)
    {
        var rows = new List<List<string>>();
        if (string.IsNullOrEmpty(csv))
            return rows;

        var row = new List<string>();
        var field = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < csv.Length; i++)
        {
            char current = csv[i];

            if (inQuotes)
            {
                if (current == '"')
                {
                    bool isEscapedQuote = i + 1 < csv.Length && csv[i + 1] == '"';
                    if (isEscapedQuote)
                    {
                        field.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    field.Append(current);
                }

                continue;
            }

            if (current == '"')
            {
                if (field.Length != 0)
                    throw new FormatException($"CSV {i + 1}번째 문자에 잘못된 따옴표가 있습니다.");

                inQuotes = true;
                continue;
            }

            if (current == ',')
            {
                row.Add(field.ToString());
                field.Clear();
                continue;
            }

            if (current == '\r' || current == '\n')
            {
                row.Add(field.ToString());
                field.Clear();
                AddRowUnlessBlank(rows, row);
                row = new List<string>();

                if (current == '\r' && i + 1 < csv.Length && csv[i + 1] == '\n')
                    i++;

                continue;
            }

            field.Append(current);
        }

        if (inQuotes)
            throw new FormatException("CSV의 따옴표 필드가 닫히지 않았습니다.");

        if (field.Length > 0 || row.Count > 0)
        {
            row.Add(field.ToString());
            AddRowUnlessBlank(rows, row);
        }

        return rows;
    }

    private static void AddRowUnlessBlank(List<List<string>> rows, List<string> row)
    {
        if (row.Count == 1 && string.IsNullOrWhiteSpace(row[0]))
            return;

        rows.Add(row);
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

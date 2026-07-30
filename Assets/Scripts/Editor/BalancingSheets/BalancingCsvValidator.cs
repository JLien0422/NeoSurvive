using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace NeoSurvive.Editor.BalancingSheets
{
    internal sealed class BalancingCsvValidationResult
    {
        public string SheetName { get; }
        public string NormalizedCsv { get; }
        public List<string> Errors { get; } = new List<string>();
        public List<string> Warnings { get; } = new List<string>();
        public bool IsValid => Errors.Count == 0;

        public BalancingCsvValidationResult(string sheetName, string normalizedCsv)
        {
            SheetName = sheetName;
            NormalizedCsv = normalizedCsv;
        }
    }

    internal static class BalancingCsvValidator
    {
        private static readonly HashSet<string> TextColumns =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "id",
                "enemyid",
                "weaponid",
                "mapid",
                "name",
                "type",
                "sortinglayername",
                "effecttype",
                "hackedeffecttype",
                "notes",
            };

        private static readonly HashSet<string> BooleanColumns =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "siegefreezeoutershooters",
                "autofit",
                "droprandomone",
                "dropboth",
                "enablerandomspark",
                "isactive",
                "canhack",
                "blink",
            };

        private static readonly HashSet<string> OptionalColumns =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "notes" };

        public static BalancingCsvValidationResult Validate(
            BalancingSheetDefinition definition,
            string downloadedCsv,
            string currentLocalCsv
        )
        {
            List<List<string>> downloadedRows;
            List<List<string>> localRows;

            try
            {
                downloadedRows = SimpleCsv.ParseRows(downloadedCsv);
            }
            catch (FormatException exception)
            {
                var failed = new BalancingCsvValidationResult(definition.SheetName, string.Empty);
                failed.Errors.Add($"CSV 형식 오류: {exception.Message}");
                return failed;
            }

            try
            {
                localRows = SimpleCsv.ParseRows(currentLocalCsv);
            }
            catch (FormatException exception)
            {
                var failed = new BalancingCsvValidationResult(definition.SheetName, string.Empty);
                failed.Errors.Add($"현재 로컬 CSV 형식 오류: {exception.Message}");
                return failed;
            }

            string normalizedCsv = Serialize(downloadedRows);
            var result = new BalancingCsvValidationResult(definition.SheetName, normalizedCsv);
            if (downloadedRows.Count < 2)
            {
                result.Errors.Add("헤더와 데이터 행이 모두 필요합니다.");
                return result;
            }

            if (localRows.Count == 0)
            {
                result.Errors.Add("비교할 현재 로컬 CSV가 비어 있습니다.");
                return result;
            }

            List<string> downloadedHeaders = TrimTrailingBlankColumns(downloadedRows[0]);
            List<string> expectedHeaders = TrimTrailingBlankColumns(localRows[0]);
            if (expectedHeaders.Count != localRows[0].Count)
                result.Warnings.Add("현재 로컬 CSV의 마지막 빈 헤더는 동기화할 때 제거됩니다.");

            ValidateHeaders(downloadedHeaders, expectedHeaders, result);
            if (!result.IsValid)
                return result;

            var headerLookup = downloadedHeaders
                .Select((header, index) => new { Header = header.Trim(), Index = index })
                .ToDictionary(item => item.Header, item => item.Index, StringComparer.OrdinalIgnoreCase);

            ValidateKeyColumns(definition, headerLookup, result);
            if (!result.IsValid)
                return result;

            ValidateRows(definition, downloadedRows, downloadedHeaders, headerLookup, result);
            return result;
        }

        public static string Normalize(string csv)
        {
            return Serialize(SimpleCsv.ParseRows(csv));
        }

        private static void ValidateHeaders(
            IReadOnlyList<string> downloaded,
            IReadOnlyList<string> expected,
            BalancingCsvValidationResult result
        )
        {
            if (downloaded.Any(string.IsNullOrWhiteSpace))
            {
                result.Errors.Add("중간에 비어 있는 헤더가 있습니다.");
                return;
            }

            if (downloaded.Count != expected.Count)
            {
                result.Errors.Add(
                    $"헤더 개수가 다릅니다. 시트 {downloaded.Count}개 / 로컬 {expected.Count}개"
                );
                return;
            }

            for (int i = 0; i < expected.Count; i++)
            {
                if (
                    !string.Equals(
                        downloaded[i].Trim(),
                        expected[i].Trim(),
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    result.Errors.Add(
                        $"{GetColumnName(i)}열 헤더가 다릅니다. 시트 '{downloaded[i]}' / 로컬 '{expected[i]}'"
                    );
                }
            }

            var duplicateHeaders = downloaded
                .GroupBy(header => header.Trim(), StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key);
            foreach (string duplicate in duplicateHeaders)
                result.Errors.Add($"중복 헤더가 있습니다: {duplicate}");
        }

        private static void ValidateKeyColumns(
            BalancingSheetDefinition definition,
            IReadOnlyDictionary<string, int> headerLookup,
            BalancingCsvValidationResult result
        )
        {
            foreach (string keyColumn in definition.KeyColumns)
            {
                if (!headerLookup.ContainsKey(keyColumn))
                    result.Errors.Add($"필수 키 헤더가 없습니다: {keyColumn}");
            }
        }

        private static void ValidateRows(
            BalancingSheetDefinition definition,
            IReadOnlyList<List<string>> rows,
            IReadOnlyList<string> headers,
            IReadOnlyDictionary<string, int> headerLookup,
            BalancingCsvValidationResult result
        )
        {
            var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var levelsByGroup = new Dictionary<string, SortedSet<int>>(StringComparer.OrdinalIgnoreCase);

            for (int rowIndex = 1; rowIndex < rows.Count; rowIndex++)
            {
                List<string> row = rows[rowIndex];
                int displayRow = rowIndex + 1;
                if (HasNonBlankExtraColumns(row, headers.Count))
                {
                    result.Errors.Add($"{displayRow}행에 헤더보다 많은 값이 있습니다.");
                    continue;
                }

                ValidateCellTypes(row, headers, displayRow, result);
                string key = BuildKey(definition, row, headerLookup, displayRow, result);
                if (!string.IsNullOrEmpty(key) && !seenKeys.Add(key))
                    result.Errors.Add($"{displayRow}행의 키가 중복됩니다: {key}");

                CollectLevel(row, headerLookup, displayRow, levelsByGroup, result);
            }

            foreach (KeyValuePair<string, SortedSet<int>> pair in levelsByGroup)
            {
                if (pair.Value.Count == 0)
                    continue;

                if (pair.Value.Min != 1)
                {
                    result.Errors.Add($"{pair.Key} 레벨은 Lv1부터 시작해야 합니다.");
                    continue;
                }

                int expected = 1;
                foreach (int level in pair.Value)
                {
                    if (level != expected)
                    {
                        result.Errors.Add(
                            $"{pair.Key} 레벨이 연속적이지 않습니다. Lv{expected}가 필요합니다."
                        );
                        break;
                    }

                    expected++;
                }
            }
        }

        private static void ValidateCellTypes(
            IReadOnlyList<string> row,
            IReadOnlyList<string> headers,
            int displayRow,
            BalancingCsvValidationResult result
        )
        {
            for (int columnIndex = 0; columnIndex < headers.Count; columnIndex++)
            {
                string value = GetValue(row, columnIndex).Trim();
                string header = headers[columnIndex].Trim();
                if (string.IsNullOrEmpty(value))
                {
                    if (!OptionalColumns.Contains(header))
                    {
                        result.Errors.Add(
                            $"{GetColumnName(columnIndex)}{displayRow} '{header}' 값이 비어 있습니다."
                        );
                    }

                    continue;
                }

                if (TextColumns.Contains(header))
                    continue;

                if (BooleanColumns.Contains(header))
                {
                    if (!IsBoolean(value))
                    {
                        result.Errors.Add(
                            $"{GetColumnName(columnIndex)}{displayRow} '{header}'는 TRUE/FALSE 또는 1/0이어야 합니다: {value}"
                        );
                    }

                    continue;
                }

                if (
                    !double.TryParse(
                        value,
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out double number
                    )
                    || double.IsNaN(number)
                    || double.IsInfinity(number)
                )
                {
                    result.Errors.Add(
                        $"{GetColumnName(columnIndex)}{displayRow} '{header}'는 숫자여야 합니다: {value}"
                    );
                }
            }
        }

        private static string BuildKey(
            BalancingSheetDefinition definition,
            IReadOnlyList<string> row,
            IReadOnlyDictionary<string, int> headerLookup,
            int displayRow,
            BalancingCsvValidationResult result
        )
        {
            var keyParts = new List<string>();
            foreach (string keyColumn in definition.KeyColumns)
            {
                string value = GetValue(row, headerLookup[keyColumn]).Trim();
                if (string.IsNullOrEmpty(value))
                {
                    result.Errors.Add($"{displayRow}행 필수 키 '{keyColumn}'가 비어 있습니다.");
                    return string.Empty;
                }

                keyParts.Add(value);
            }

            return string.Join(" / ", keyParts);
        }

        private static void CollectLevel(
            IReadOnlyList<string> row,
            IReadOnlyDictionary<string, int> headerLookup,
            int displayRow,
            IDictionary<string, SortedSet<int>> levelsByGroup,
            BalancingCsvValidationResult result
        )
        {
            if (!headerLookup.TryGetValue("level", out int levelIndex))
                return;

            string levelText = GetValue(row, levelIndex).Trim();
            if (!int.TryParse(levelText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int level))
            {
                result.Errors.Add($"{displayRow}행 level은 정수여야 합니다: {levelText}");
                return;
            }

            string group = "전체";
            if (headerLookup.TryGetValue("weaponid", out int weaponIndex))
                group = GetValue(row, weaponIndex).Trim();

            if (!levelsByGroup.TryGetValue(group, out SortedSet<int> levels))
            {
                levels = new SortedSet<int>();
                levelsByGroup[group] = levels;
            }

            levels.Add(level);
        }

        private static bool IsBoolean(string value)
        {
            return
                string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "false", StringComparison.OrdinalIgnoreCase)
                || value == "1"
                || value == "0";
        }

        private static bool HasNonBlankExtraColumns(IReadOnlyList<string> row, int headerCount)
        {
            for (int index = headerCount; index < row.Count; index++)
            {
                if (!string.IsNullOrWhiteSpace(row[index]))
                    return true;
            }

            return false;
        }

        private static string GetValue(IReadOnlyList<string> row, int index)
        {
            return index < row.Count ? row[index] : string.Empty;
        }

        private static List<string> TrimTrailingBlankColumns(IReadOnlyList<string> row)
        {
            int count = row.Count;
            while (count > 0 && string.IsNullOrWhiteSpace(row[count - 1]))
                count--;

            return row.Take(count).ToList();
        }

        private static string Serialize(IReadOnlyList<List<string>> rows)
        {
            if (rows.Count == 0)
                return string.Empty;

            int columnCount = TrimTrailingBlankColumns(rows[0]).Count;
            var builder = new StringBuilder();
            for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                if (rowIndex > 0)
                    builder.Append('\n');

                IReadOnlyList<string> row = rows[rowIndex];
                for (int columnIndex = 0; columnIndex < columnCount; columnIndex++)
                {
                    if (columnIndex > 0)
                        builder.Append(',');

                    builder.Append(Escape(GetValue(row, columnIndex)));
                }
            }

            builder.Append('\n');
            return builder.ToString();
        }

        private static string Escape(string value)
        {
            if (
                value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0
                && value == value.Trim()
            )
            {
                return value;
            }

            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        private static string GetColumnName(int zeroBasedIndex)
        {
            int index = zeroBasedIndex + 1;
            var name = new StringBuilder();
            while (index > 0)
            {
                index--;
                name.Insert(0, (char)('A' + index % 26));
                index /= 26;
            }

            return name.ToString();
        }
    }
}

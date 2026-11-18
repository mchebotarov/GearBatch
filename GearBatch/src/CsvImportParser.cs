using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace GearBatch.src
{
    public sealed record ImportRow(
        string Sku, string ProductName, string Category, int Quantity, decimal UnitCostCad);

    public sealed record CsvRowError(int LineNumber, string Code);

    public sealed record CsvParseResult(
        string? FileError,
        IReadOnlyList<ImportRow> AcceptedRows,
        IReadOnlyList<CsvRowError> RowErrors);

    public static class CsvImportParser
    {
        private static readonly string[] ExpectedHeader =
        {
            "sku", "productName", "category", "quantity", "unitCostCad"
        };

        private static readonly UTF8Encoding StrictUtf8 = new(false, true);

        // Match one quoted or unquoted field, followed by a comma or the end of the line.
        private static readonly Regex CsvField = new(
            """\G(?:"(?<quoted>(?:[^"\r\n]|"")*)"|(?<plain>[^",\r\n]*))(?<comma>,|\z)""");

        private static readonly Regex SkuFormat = new(@"\A[A-Z0-9][A-Z0-9-]{0,31}\z");
        private static readonly Regex CostFormat = new(@"\A[0-9]+(?:\.[0-9]{1,2})?\z");

        public static bool HasExpectedHeader(IReadOnlyList<string> columns)
        {
            return columns.SequenceEqual(ExpectedHeader, StringComparer.Ordinal);
        }

        public static CsvParseResult Parse(ReadOnlySpan<byte> bytes)
        {
            if (bytes.StartsWith(Encoding.UTF8.Preamble))
            {
                bytes = bytes[Encoding.UTF8.Preamble.Length..];
            }

            string text;
            try
            {
                text = StrictUtf8.GetString(bytes);
            }
            catch (DecoderFallbackException)
            {
                return FileFailure("FILE_INVALID_ENCODING");
            }

            var lines = text.Replace("\r\n", "\n").Split('\n');
            var header = ParseLine(lines[0]);
            if (header is null || !HasExpectedHeader(header))
            {
                return FileFailure("FILE_INVALID_HEADER");
            }

            var accepted = new List<ImportRow>();
            var errors = new List<CsvRowError>();
            var acceptedSkus = new HashSet<string>(StringComparer.Ordinal);
            var recordCount = 0;

            for (var index = 1; index < lines.Length; index++)
            {
                if (lines[index].Length == 0)
                {
                    continue;
                }

                var fields = ParseLine(lines[index]);
                if (fields is null || fields.Count != ExpectedHeader.Length)
                {
                    return FileFailure("FILE_INVALID_CSV");
                }

                recordCount++;
                if (recordCount > 10_000)
                {
                    return FileFailure("FILE_TOO_MANY_ROWS");
                }

                var (row, error) = ValidateRow(fields);
                if (row is null)
                {
                    errors.Add(new CsvRowError(index + 1, error!));
                    continue;
                }

                if (!acceptedSkus.Add(row.Sku))
                {
                    errors.Add(new CsvRowError(index + 1, "DUPLICATE_SKU"));
                    continue;
                }

                accepted.Add(row);
            }

            return recordCount == 0
                ? FileFailure("FILE_NO_DATA")
                : new CsvParseResult(null, accepted, errors);
        }

        private static List<string>? ParseLine(string line)
        {
            var fields = new List<string>();
            var position = 0;

            while (true)
            {
                var match = CsvField.Match(line, position);
                if (!match.Success)
                {
                    return null;
                }

                var quoted = match.Groups["quoted"];
                fields.Add(quoted.Success
                    ? quoted.Value.Replace("\"\"", "\"")
                    : match.Groups["plain"].Value);

                if (match.Groups["comma"].Length == 0)
                {
                    return fields;
                }

                position += match.Length;
            }
        }

        private static (ImportRow? Row, string? Error) ValidateRow(IReadOnlyList<string> fields)
        {
            var sku = fields[0].Trim(' ');
            if (!SkuFormat.IsMatch(sku))
            {
                return (null, "INVALID_SKU");
            }

            var productName = fields[1].Trim();
            if (productName.Length is < 1 or > 120 || fields[1].Any(char.IsControl))
            {
                return (null, "INVALID_PRODUCT_NAME");
            }

            var category = fields[2].Trim();
            if (category is not ("Packs" or "Tents" or "Footwear" or "Accessories"))
            {
                return (null, "INVALID_CATEGORY");
            }

            var quantityText = fields[3].Trim();
            if (!quantityText.All(char.IsAsciiDigit) ||
                !int.TryParse(quantityText, NumberStyles.None, CultureInfo.InvariantCulture, out var quantity) ||
                quantity > 1_000_000)
            {
                return (null, "INVALID_QUANTITY");
            }

            var costText = fields[4].Trim();
            if (!CostFormat.IsMatch(costText) ||
                !decimal.TryParse(costText, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var cost) ||
                cost is <= 0 or > 99_999.99m)
            {
                return (null, "INVALID_UNIT_COST");
            }

            return (new ImportRow(sku, productName, category, quantity, cost), null);
        }

        private static CsvParseResult FileFailure(string code)
        {
            return new CsvParseResult(code, Array.Empty<ImportRow>(), Array.Empty<CsvRowError>());
        }
    }
}
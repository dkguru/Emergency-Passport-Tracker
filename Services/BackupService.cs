using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Emergency_Passport_Tracker.Models;

namespace Emergency_Passport_Tracker.Services
{
    public class BackupFormatException : Exception
    {
        public BackupFormatException(string message) : base(message) { }
    }

    /// <summary>
    /// Plain-text CSV backup and restore.
    ///
    /// WARNING: the files this produces are NOT encrypted. They are intended to be written
    /// straight onto an encrypted drive. Anyone who can read the file can read every record.
    ///
    /// Two files are written per backup:
    ///   EPT-Records-yyyyMMdd-HHmmss.csv
    ///   EPT-Audit-yyyyMMdd-HHmmss.csv
    ///
    /// Values are RFC 4180 quoted, dates are ISO-8601 and culture-invariant, so a backup taken
    /// on one machine restores identically on another regardless of regional settings.
    /// </summary>
    public static class BackupService
    {
        private const string RecordHeader = "PassportNumber,IssuedTo,DateIssued,Notes,Status,Locked";
        private const string AuditHeader = "Timestamp,Action,PassportNumber";

        private static readonly UTF8Encoding Utf8WithBom = new(encoderShouldEmitUTF8Identifier: true);

        // ------------------------------------------------------------- export

        public static (string RecordsPath, string AuditPath) Export(
            string folder,
            IReadOnlyList<PassportRecord> records,
            IReadOnlyList<AuditEntry> audit)
        {
            ArgumentNullException.ThrowIfNull(records);
            ArgumentNullException.ThrowIfNull(audit);

            if (string.IsNullOrWhiteSpace(folder))
                throw new ArgumentException("A destination folder is required.", nameof(folder));

            Directory.CreateDirectory(folder);

            string stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
            string recordsPath = Path.Combine(folder, $"EPT-Records-{stamp}.csv");
            string auditPath = Path.Combine(folder, $"EPT-Audit-{stamp}.csv");

            var sb = new StringBuilder();
            sb.AppendLine(RecordHeader);

            foreach (PassportRecord r in records)
            {
                sb.Append(Escape(r.PassportNumber)).Append(',');
                sb.Append(Escape(r.IssuedTo)).Append(',');
                sb.Append(Escape(r.DateIssued?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))).Append(',');
                sb.Append(Escape(r.Notes)).Append(',');
                sb.Append(Escape(r.Status)).Append(',');
                sb.Append(r.Locked ? "TRUE" : "FALSE");
                sb.AppendLine();
            }

            File.WriteAllText(recordsPath, sb.ToString(), Utf8WithBom);

            sb.Clear();
            sb.AppendLine(AuditHeader);

            foreach (AuditEntry a in audit)
            {
                sb.Append(Escape(a.Timestamp.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture))).Append(',');
                sb.Append(Escape(a.Action)).Append(',');
                sb.Append(Escape(a.PassportNumber));
                sb.AppendLine();
            }

            File.WriteAllText(auditPath, sb.ToString(), Utf8WithBom);

            return (recordsPath, auditPath);
        }

        // ------------------------------------------------------------ import

        /// <summary>
        /// Reads a records CSV, and its matching audit CSV when one is supplied.
        /// Throws <see cref="BackupFormatException"/> rather than importing partial data.
        /// </summary>
        public static (List<PassportRecord> Records, List<AuditEntry> Audit) Import(
            string recordsPath,
            string? auditPath)
        {
            if (!File.Exists(recordsPath))
                throw new BackupFormatException($"Backup file not found: {recordsPath}");

            List<string[]> rows = ParseCsv(File.ReadAllText(recordsPath, Encoding.UTF8));

            if (rows.Count == 0)
                throw new BackupFormatException("The records backup file is empty.");

            string[] header = rows[0];
            if (header.Length < 6 ||
                !header[0].Trim().Equals("PassportNumber", StringComparison.OrdinalIgnoreCase))
            {
                throw new BackupFormatException(
                    "This does not look like an Emergency Passport Tracker records backup." +
                    Environment.NewLine +
                    "Expected the first column to be 'PassportNumber'.");
            }

            var records = new List<PassportRecord>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 1; i < rows.Count; i++)
            {
                string[] row = rows[i];
                if (IsBlankRow(row))
                    continue;

                if (row.Length < 6)
                    throw new BackupFormatException(
                        $"Line {i + 1} of the records backup has {row.Length} columns; 6 were expected.");

                string number = row[0].Trim();
                if (number.Length == 0)
                    throw new BackupFormatException($"Line {i + 1} of the records backup has no passport number.");

                number = number.PadLeft(8, '0');

                if (!seen.Add(number))
                    throw new BackupFormatException(
                        $"The records backup contains passport number {number} more than once (line {i + 1}).");

                string status = row[4].Trim().ToUpperInvariant();
                if (status.Length == 0)
                    status = "A";

                if (status != "A" && status != "B" && status != "C" && status != "D")
                    throw new BackupFormatException(
                        $"Line {i + 1} of the records backup has an unknown status '{row[4]}'. Expected A, B, C or D.");

                DateTime? dateIssued = null;
                string rawDate = row[2].Trim();

                if (rawDate.Length > 0)
                {
                    if (DateTime.TryParse(rawDate, CultureInfo.InvariantCulture,
                            DateTimeStyles.None, out DateTime parsed) ||
                        DateTime.TryParse(rawDate, CultureInfo.CurrentCulture,
                            DateTimeStyles.None, out parsed))
                    {
                        dateIssued = parsed;
                    }
                    else
                    {
                        throw new BackupFormatException(
                            $"Line {i + 1} of the records backup has an unreadable date '{rawDate}'.");
                    }
                }

                records.Add(new PassportRecord
                {
                    PassportNumber = number,
                    IssuedTo = NullIfEmpty(row[1]),
                    DateIssued = dateIssued,
                    Notes = NullIfEmpty(row[3]),
                    Status = status,
                    Locked = ParseBool(row[5])
                });
            }

            var audit = new List<AuditEntry>();

            if (!string.IsNullOrWhiteSpace(auditPath) && File.Exists(auditPath))
            {
                List<string[]> auditRows = ParseCsv(File.ReadAllText(auditPath, Encoding.UTF8));

                for (int i = 1; i < auditRows.Count; i++)
                {
                    string[] row = auditRows[i];
                    if (IsBlankRow(row) || row.Length < 3)
                        continue;

                    DateTime.TryParse(row[0].Trim(), CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out DateTime timestamp);

                    audit.Add(new AuditEntry
                    {
                        Timestamp = timestamp,
                        Action = row[1].Trim(),
                        PassportNumber = row[2].Trim()
                    });
                }
            }

            return (records, audit);
        }

        /// <summary>Guesses the audit file that belongs to a records file by matching the timestamp suffix.</summary>
        public static string? GuessAuditPath(string recordsPath)
        {
            string? folder = Path.GetDirectoryName(recordsPath);
            string name = Path.GetFileName(recordsPath);

            if (folder == null || !name.StartsWith("EPT-Records-", StringComparison.OrdinalIgnoreCase))
                return null;

            string candidate = Path.Combine(folder, "EPT-Audit-" + name.Substring("EPT-Records-".Length));
            return File.Exists(candidate) ? candidate : null;
        }

        // ------------------------------------------------------------ csv core

        private static string Escape(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            bool needsQuotes = value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0
                               || value != value.Trim();

            if (!needsQuotes)
                return value;

            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        /// <summary>RFC 4180 parser: handles quoted fields, escaped quotes and embedded newlines.</summary>
        private static List<string[]> ParseCsv(string text)
        {
            var rows = new List<string[]>();
            var fields = new List<string>();
            var field = new StringBuilder();

            bool inQuotes = false;
            int i = 0;

            // Strip a UTF-8 byte order mark if the reader left one in place.
            if (text.Length > 0 && text[0] == '\uFEFF')
                i = 1;

            for (; i < text.Length; i++)
            {
                char c = text[i];

                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < text.Length && text[i + 1] == '"')
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
                        field.Append(c);
                    }

                    continue;
                }

                switch (c)
                {
                    case '"':
                        inQuotes = true;
                        break;

                    case ',':
                        fields.Add(field.ToString());
                        field.Clear();
                        break;

                    case '\r':
                        // handled by the \n case; a lone \r also ends the row
                        if (i + 1 < text.Length && text[i + 1] == '\n')
                            break;

                        fields.Add(field.ToString());
                        field.Clear();
                        rows.Add(fields.ToArray());
                        fields.Clear();
                        break;

                    case '\n':
                        fields.Add(field.ToString());
                        field.Clear();
                        rows.Add(fields.ToArray());
                        fields.Clear();
                        break;

                    default:
                        field.Append(c);
                        break;
                }
            }

            if (field.Length > 0 || fields.Count > 0)
            {
                fields.Add(field.ToString());
                rows.Add(fields.ToArray());
            }

            return rows;
        }

        private static bool IsBlankRow(string[] row)
        {
            foreach (string value in row)
            {
                if (!string.IsNullOrWhiteSpace(value))
                    return false;
            }

            return true;
        }

        private static string? NullIfEmpty(string value)
        {
            value = value.Trim();
            return value.Length == 0 ? null : value;
        }

        private static bool ParseBool(string value)
        {
            value = value.Trim();

            return value.Equals("TRUE", StringComparison.OrdinalIgnoreCase)
                   || value.Equals("YES", StringComparison.OrdinalIgnoreCase)
                   || value == "1";
        }
    }
}

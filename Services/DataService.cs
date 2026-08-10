using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Emergency_Passport_Tracker.Models;
using Emergency_Passport_Tracker.Security;

namespace Emergency_Passport_Tracker.Services
{
    /// <summary>
    /// Thrown when the supplied code does not unlock the data file.
    /// The caller must NOT continue with empty data when this is thrown.
    /// </summary>
    public class InvalidPinException : Exception
    {
        public InvalidPinException(string message) : base(message) { }
    }

    /// <summary>Thrown when the file unlocked correctly but its contents are unusable.</summary>
    public class DataFileCorruptException : Exception
    {
        public DataFileCorruptException(string message) : base(message) { }
        public DataFileCorruptException(string message, Exception inner) : base(message, inner) { }
    }

    /// <summary>
    /// Encrypted storage for passport records and the audit log.
    ///
    /// File format (current, "EPT2"):
    ///   [4]  ASCII magic "EPT2"
    ///   [1]  salt length
    ///   [1]  iv length
    ///   [n]  salt
    ///   [n]  iv
    ///   [32] HMAC-SHA256 over (magic || saltLen || ivLen || salt || iv || ciphertext)
    ///   [..] AES-256-CBC ciphertext
    ///
    /// Formats still readable:
    ///   "EPT1": magic || saltLen || ivLen || salt || iv || ciphertext   (no integrity tag)
    ///   legacy: salt(16) || iv(16) || ciphertext                        (no magic, no tag)
    ///
    /// Files in an old format are transparently upgraded to EPT2 on the next save.
    /// </summary>
    public class DataService
    {
        public const string DefaultFileName = "eptdata.enc";

        private static readonly byte[] MagicV2 = Encoding.ASCII.GetBytes("EPT2");
        private static readonly byte[] MagicV1 = Encoding.ASCII.GetBytes("EPT1");
        private const int MacSize = 32;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = false
        };

        public string DataFilePath { get; }

        public string DataDirectory => Path.GetDirectoryName(DataFilePath) ?? AppContext.BaseDirectory;

        public bool DataFileExists => File.Exists(DataFilePath);

        public DataService() : this(ResolveDefaultPath()) { }

        public DataService(string dataFilePath)
        {
            if (string.IsNullOrWhiteSpace(dataFilePath))
                throw new ArgumentException("Data file path is required.", nameof(dataFilePath));

            DataFilePath = Path.GetFullPath(dataFilePath);
            Directory.CreateDirectory(DataDirectory);
        }

        /// <summary>
        /// Keeps using a data file next to the executable if one is already there (so existing
        /// installations are untouched); otherwise stores under %LOCALAPPDATA%, which stays
        /// writable when the app is installed into Program Files.
        /// </summary>
        private static string ResolveDefaultPath()
        {
            string beside = Path.Combine(AppContext.BaseDirectory, DefaultFileName);
            if (File.Exists(beside))
                return beside;

            string appData = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "EmergencyPassportTracker");

            return Path.Combine(appData, DefaultFileName);
        }

        // ---------------------------------------------------------------- save

        public void Save(List<PassportRecord> records, List<AuditEntry> audit, string pin)
        {
            ArgumentNullException.ThrowIfNull(records);
            ArgumentNullException.ThrowIfNull(audit);

            if (string.IsNullOrEmpty(pin))
                throw new ArgumentException("A code is required to encrypt the data file.", nameof(pin));

            string json = JsonSerializer.Serialize(new DataWrapper { Records = records, Audit = audit }, JsonOptions);

            byte[] salt = RandomNumberGenerator.GetBytes(SecurityHelper.SaltSize);
            byte[] keys = SecurityHelper.DeriveKeys(pin, salt);

            try
            {
                byte[] aesKey = keys.AsSpan(0, SecurityHelper.KeySize).ToArray();
                byte[] macKey = keys.AsSpan(SecurityHelper.KeySize, SecurityHelper.KeySize).ToArray();

                byte[] iv;
                byte[] ciphertext;

                try
                {
                    using Aes aes = Aes.Create();
                    aes.Key = aesKey;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;
                    aes.GenerateIV();
                    iv = aes.IV;

                    byte[] plaintext = Encoding.UTF8.GetBytes(json);
                    try
                    {
                        using ICryptoTransform encryptor = aes.CreateEncryptor();
                        ciphertext = encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);
                    }
                    finally
                    {
                        CryptographicOperations.ZeroMemory(plaintext);
                    }
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(aesKey);
                }

                byte[] header = BuildHeader(MagicV2, salt, iv);
                byte[] mac;

                try
                {
                    mac = ComputeMac(macKey, header, ciphertext);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(macKey);
                }

                using var ms = new MemoryStream(header.Length + mac.Length + ciphertext.Length);
                ms.Write(header, 0, header.Length);
                ms.Write(mac, 0, mac.Length);
                ms.Write(ciphertext, 0, ciphertext.Length);

                WriteAtomic(ms.ToArray());
            }
            finally
            {
                CryptographicOperations.ZeroMemory(keys);
            }
        }

        private static byte[] BuildHeader(byte[] magic, byte[] salt, byte[] iv)
        {
            byte[] header = new byte[magic.Length + 2 + salt.Length + iv.Length];
            Buffer.BlockCopy(magic, 0, header, 0, magic.Length);
            header[magic.Length] = (byte)salt.Length;
            header[magic.Length + 1] = (byte)iv.Length;
            Buffer.BlockCopy(salt, 0, header, magic.Length + 2, salt.Length);
            Buffer.BlockCopy(iv, 0, header, magic.Length + 2 + salt.Length, iv.Length);
            return header;
        }

        private static byte[] ComputeMac(byte[] macKey, byte[] header, byte[] ciphertext)
        {
            using var hmac = new HMACSHA256(macKey);
            hmac.TransformBlock(header, 0, header.Length, null, 0);
            hmac.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
            return hmac.Hash!;
        }

        /// <summary>
        /// Writes via a temporary file so an interrupted save can never leave a half-written
        /// (permanently unreadable) data file behind. The previous version is kept as .bak.
        /// </summary>
        private void WriteAtomic(byte[] payload)
        {
            string tempPath = DataFilePath + ".tmp";
            string backupPath = DataFilePath + ".bak";

            File.WriteAllBytes(tempPath, payload);

            if (File.Exists(DataFilePath))
            {
                File.Replace(tempPath, DataFilePath, backupPath, ignoreMetadataErrors: true);
            }
            else
            {
                File.Move(tempPath, DataFilePath, overwrite: true);
            }
        }

        // ---------------------------------------------------------------- load

        /// <summary>
        /// Loads and decrypts the data file.
        /// Returns empty lists only when there is genuinely no data file yet.
        /// Throws <see cref="InvalidPinException"/> when the code is wrong - callers must
        /// never fall through and save over the file in that case.
        /// </summary>
        public (List<PassportRecord> Records, List<AuditEntry> Audit) Load(string pin)
        {
            if (!File.Exists(DataFilePath))
                return (new List<PassportRecord>(), new List<AuditEntry>());

            byte[] data = File.ReadAllBytes(DataFilePath);

            // A zero-length file is treated as "no data yet" rather than an error.
            if (data.Length == 0)
                return (new List<PassportRecord>(), new List<AuditEntry>());

            if (string.IsNullOrEmpty(pin))
                throw new InvalidPinException("A code is required to open the data file.");

            byte[] salt;
            byte[] iv;
            byte[]? mac = null;
            int payloadOffset;

            if (StartsWith(data, MagicV2) || StartsWith(data, MagicV1))
            {
                bool hasMac = StartsWith(data, MagicV2);
                int magicLen = MagicV2.Length;

                if (data.Length < magicLen + 2)
                    throw new DataFileCorruptException("The data file is truncated and cannot be read.");

                int saltLen = data[magicLen];
                int ivLen = data[magicLen + 1];
                int headerLen = magicLen + 2;

                if (saltLen == 0 || ivLen == 0 ||
                    data.Length < headerLen + saltLen + ivLen + (hasMac ? MacSize : 0))
                {
                    throw new DataFileCorruptException("The data file header is damaged and cannot be read.");
                }

                salt = Slice(data, headerLen, saltLen);
                iv = Slice(data, headerLen + saltLen, ivLen);
                payloadOffset = headerLen + saltLen + ivLen;

                if (hasMac)
                {
                    mac = Slice(data, payloadOffset, MacSize);
                    payloadOffset += MacSize;
                }
            }
            else
            {
                // Original layout: salt(16) + iv(16) + ciphertext
                if (data.Length < 32)
                    throw new DataFileCorruptException("The data file is too small to be valid.");

                salt = Slice(data, 0, 16);
                iv = Slice(data, 16, 16);
                payloadOffset = 32;
            }

            byte[] ciphertext = Slice(data, payloadOffset, data.Length - payloadOffset);
            byte[] keys = SecurityHelper.DeriveKeys(pin, salt);

            try
            {
                // Authenticated format: verify first. A mismatch means wrong code (or a
                // tampered/damaged file) and is detected with certainty, never by guesswork.
                if (mac != null)
                {
                    byte[] macKey = keys.AsSpan(SecurityHelper.KeySize, SecurityHelper.KeySize).ToArray();
                    byte[] expected;

                    try
                    {
                        expected = ComputeMac(macKey, BuildHeader(MagicV2, salt, iv), ciphertext);
                    }
                    finally
                    {
                        CryptographicOperations.ZeroMemory(macKey);
                    }

                    if (!CryptographicOperations.FixedTimeEquals(mac, expected))
                    {
                        throw new InvalidPinException(
                            "Incorrect code. The data file was not opened and nothing has been changed." +
                            Environment.NewLine + Environment.NewLine +
                            "If you are certain the code is correct, the file may have been damaged or altered.");
                    }
                }

                byte[] aesKey = keys.AsSpan(0, SecurityHelper.KeySize).ToArray();
                string json;

                try
                {
                    using Aes aes = Aes.Create();
                    aes.Key = aesKey;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;
                    aes.IV = iv;

                    using ICryptoTransform decryptor = aes.CreateDecryptor();
                    byte[] plaintext = decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
                    json = Encoding.UTF8.GetString(plaintext);
                    CryptographicOperations.ZeroMemory(plaintext);
                }
                catch (CryptographicException ex)
                {
                    // Unauthenticated legacy formats can only fail here.
                    if (mac == null)
                        throw new InvalidPinException(
                            "Incorrect code. The data file was not opened and nothing has been changed.");

                    throw new DataFileCorruptException("The data file could not be decrypted.", ex);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(aesKey);
                }

                if (string.IsNullOrWhiteSpace(json))
                    return (new List<PassportRecord>(), new List<AuditEntry>());

                DataWrapper? wrapper;

                try
                {
                    wrapper = JsonSerializer.Deserialize<DataWrapper>(json);
                }
                catch (JsonException ex)
                {
                    // With no integrity tag, a wrong code has roughly a 1-in-256 chance of
                    // producing valid padding and garbage plaintext, which lands here.
                    if (mac == null)
                        throw new InvalidPinException(
                            "Incorrect code. The data file was not opened and nothing has been changed.");

                    throw new DataFileCorruptException("The data file contents could not be read.", ex);
                }

                if (wrapper == null)
                    throw new DataFileCorruptException("The data file contents could not be read.");

                return (wrapper.Records ?? new List<PassportRecord>(),
                        wrapper.Audit ?? new List<AuditEntry>());
            }
            finally
            {
                CryptographicOperations.ZeroMemory(keys);
            }
        }

        // ------------------------------------------------------- encrypted copy

        /// <summary>Copies the encrypted data file into a Backups sub-folder. Still encrypted.</summary>
        public string? BackupEncrypted()
        {
            if (!File.Exists(DataFilePath))
                return null;

            string folder = Path.Combine(DataDirectory, "Backups");
            Directory.CreateDirectory(folder);

            string target = Path.Combine(
                folder,
                $"eptdata-{DateTime.Now:yyyyMMdd-HHmmss}.enc");

            File.Copy(DataFilePath, target, overwrite: false);
            return target;
        }

        /// <summary>Takes a safety copy of the current encrypted file before a destructive action.</summary>
        public string? SnapshotBeforeRestore()
        {
            if (!File.Exists(DataFilePath))
                return null;

            string folder = Path.Combine(DataDirectory, "Backups");
            Directory.CreateDirectory(folder);

            string target = Path.Combine(
                folder,
                $"eptdata-before-restore-{DateTime.Now:yyyyMMdd-HHmmss}.enc");

            File.Copy(DataFilePath, target, overwrite: false);
            return target;
        }

        // ------------------------------------------------------------- helpers

        private static bool StartsWith(byte[] data, byte[] prefix)
        {
            if (data.Length < prefix.Length)
                return false;

            for (int i = 0; i < prefix.Length; i++)
            {
                if (data[i] != prefix[i])
                    return false;
            }

            return true;
        }

        private static byte[] Slice(byte[] source, int offset, int length)
        {
            byte[] result = new byte[length];
            Buffer.BlockCopy(source, offset, result, 0, length);
            return result;
        }

        private sealed class DataWrapper
        {
            public List<PassportRecord>? Records { get; set; }
            public List<AuditEntry>? Audit { get; set; }
        }
    }
}

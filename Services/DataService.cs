using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;

using EmergencyPassportTracker.Models;
using EmergencyPassportTracker.Security;

namespace EmergencyPassportTracker.Services
{
    public class DataService
    {
        private const string FileName = "eptdata.enc";

        // File format:
        // - If new format:
        //   [4 bytes ASCII magic "EPT1"]
        //   [1 byte salt length]
        //   [1 byte iv length]
        //   [salt bytes]
        //   [iv bytes]
        //   [ciphertext bytes...]
        // - If old format (no magic present) fallback to:
        //   [16 bytes salt][16 bytes iv][ciphertext...]
        private static readonly byte[] Magic = Encoding.ASCII.GetBytes("EPT1");

        public void Save(List<PassportRecord> records, List<AuditEntry> audit, string pin)
        {
            var wrapper = new { Records = records, Audit = audit };
            string json = JsonSerializer.Serialize(wrapper);

            byte[] salt = RandomNumberGenerator.GetBytes(16);

            using Aes aes = Aes.Create();
            aes.Key = SecurityHelper.DeriveKey(pin, salt);
            aes.GenerateIV();

            using MemoryStream ms = new();

            // Write header (magic + lengths) so we can support versioning later
            ms.Write(Magic, 0, Magic.Length);
            ms.WriteByte((byte)salt.Length); // salt length (1 byte)
            ms.WriteByte((byte)aes.IV.Length); // iv length (1 byte)

            // Write parameters
            ms.Write(salt, 0, salt.Length);
            ms.Write(aes.IV, 0, aes.IV.Length);

            // IMPORTANT: ensure CryptoStream/StreamWriter are disposed (flushed) before calling ms.ToArray()
            using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
            using (var sw = new StreamWriter(cs, Encoding.UTF8))
            {
                sw.Write(json);
            }

            File.WriteAllBytes(FileName, ms.ToArray());
        }

        public (List<PassportRecord>, List<AuditEntry>) Load(string pin)
        {
            DataWrapper wrapper = null;

            if (!File.Exists(FileName))
                return (new(), new());

            byte[] data = File.ReadAllBytes(FileName);
            if (data == null || data.Length < 1)
                return (new(), new());

            byte[] salt;
            byte[] iv;
            int payloadOffset;

            // Detect new format by magic header
            if (data.Length >= Magic.Length && data.Take(Magic.Length).SequenceEqual(Magic))
            {
                // Need at least magic + 2 length bytes
                if (data.Length < Magic.Length + 2)
                    return (new(), new());

                int saltLen = data[Magic.Length];
                int ivLen = data[Magic.Length + 1];

                int headerLen = Magic.Length + 2;
                if (data.Length < headerLen + saltLen + ivLen)
                    return (new(), new());

                salt = data.Skip(headerLen).Take(saltLen).ToArray();
                iv = data.Skip(headerLen + saltLen).Take(ivLen).ToArray();
                payloadOffset = headerLen + saltLen + ivLen;
            }
            else
            {
                // Fallback to original layout: salt(16) + iv(16) + ciphertext
                if (data.Length < 32)
                    return (new(), new());

                salt = data.Take(16).ToArray();
                iv = data.Skip(16).Take(16).ToArray();
                payloadOffset = 32;
            }

            using Aes aes = Aes.Create();
            aes.Key = SecurityHelper.DeriveKey(pin, salt);
            aes.IV = iv;

            try
            {
                using MemoryStream ms = new(data, payloadOffset, data.Length - payloadOffset);
                using CryptoStream cs = new(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
                using StreamReader sr = new(cs, Encoding.UTF8);

                string json = sr.ReadToEnd();
                if (!string.IsNullOrWhiteSpace(json))
                    wrapper = JsonSerializer.Deserialize<DataWrapper>(json);
            }
            catch (CryptographicException ex)
            {
                MessageBox.Show("Decryption failed. Possibly wrong PIN or corrupted data: " + ex.Message);
            }
            catch (JsonException ex)
            {
                MessageBox.Show("Failed to parse data file: " + ex.Message);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Error loading data: " + ex.Message);
            }

            return (wrapper?.Records ?? new(), wrapper?.Audit ?? new());
        }
        public void BackupData()
        {
            File.Copy(FileName, "backup_" + DateTime.Now.Ticks + ".enc");
        }

        private void RestoreData(string file)
        {
            File.Copy(file, FileName, true);
            //LoadData();
            //RefreshGrid();
        }

        private class DataWrapper
        {
            public List<PassportRecord> Records { get; set; }
            public List<AuditEntry> Audit { get; set; }
        }
    }
}
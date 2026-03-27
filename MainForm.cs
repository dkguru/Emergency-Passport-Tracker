using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;
using System.Drawing.Printing;

namespace Emergency_Passport_Tracker
{
    public partial class MainForm : Form
    {
        private List<PassportRecord> records = [];
        private readonly string dataFile = "ept_data.enc";
        private Label? lblRange;
        private Button? BtnAdd;
        private Button? BtnPrint;
        private TextBox? txtStart;
        private TextBox? txtEnd;
        private DataGridView? dataGrid;
        private PictureBox? logoBox;
        private Label? lblTitle;
        private string pin = "";

        public MainForm()
        {
            InitializeComponent();
            PromptForPin();
        }

        private void PromptForPin()
        {
            pin = Microsoft.VisualBasic.Interaction.InputBox("Enter PIN:", "Security", "");

            if (File.Exists(dataFile))
                LoadData();
        }

        private void LoadData()
        {
            try
            {
                byte[] encrypted = File.ReadAllBytes(dataFile);
                string json = Decrypt(encrypted, pin);
                records = JsonSerializer.Deserialize<List<PassportRecord>>(json) ?? [];
                RefreshGrid();
            }
            catch
            {
                MessageBox.Show("Invalid PIN or corrupted file.");
                Environment.Exit(0);
            }
        }

        private void SaveData()
        {
            string json = JsonSerializer.Serialize(records);
            byte[] encrypted = Encrypt(json, pin);
            File.WriteAllBytes(dataFile, encrypted);
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            //if (!string.IsNullOrWhiteSpace(txtSingle.Text))
            //{
            //    AddRecord(txtSingle.Text);
            //}
            //else
            if (!string.IsNullOrWhiteSpace(txtStart.Text) && !string.IsNullOrWhiteSpace(txtEnd.Text))
            {
                int start = int.Parse(txtStart.Text);
                int end = int.Parse(txtEnd.Text);

                for (int i = start; i <= end; i++)
                    AddRecord(i.ToString("D8"));
            }

            SaveData();
            RefreshGrid();
        }

        private void AddRecord(string number)
        {
            records.Add(new PassportRecord
            {
                PassportNumber = number,
                IssuedTo = "",
                DateIssued = "",
                Notes = ""
            });
        }

        private void RefreshGrid()
        {
            dataGrid.DataSource = null;
            dataGrid.DataSource = records;
        }

        // 🔐 Encryption
        private static byte[] Encrypt(string plainText, string pin)
        {
            if (plainText is null)
                throw new ArgumentNullException(nameof(plainText));

            if (string.IsNullOrWhiteSpace(pin))
                throw new ArgumentException("PIN cannot be empty.", nameof(pin));

            using Aes aes = Aes.Create();
            aes.Key = SHA256.HashData(Encoding.UTF8.GetBytes(pin));
            aes.GenerateIV();

            using MemoryStream ms = new();

            // Write IV first so Decrypt() can recover it later
            ms.Write(aes.IV, 0, aes.IV.Length);

            // Important: dispose these before calling ms.ToArray()
            using (CryptoStream cs = new(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
            using (StreamWriter sw = new(cs, Encoding.UTF8))
            {
                sw.Write(plainText);
            }

            return ms.ToArray();
        }

        private static string Decrypt(byte[] cipher, string pin)
        {
            if (cipher is null)
                throw new ArgumentNullException(nameof(cipher));

            if (string.IsNullOrWhiteSpace(pin))
                throw new ArgumentException("PIN cannot be empty.", nameof(pin));

            using Aes aes = Aes.Create();
            aes.Key = SHA256.HashData(Encoding.UTF8.GetBytes(pin));

            int ivLength = aes.BlockSize / 8; // usually 16 bytes for AES

            if (cipher.Length < ivLength)
                throw new CryptographicException("Encrypted data is too short.");

            byte[] iv = new byte[ivLength];
            Array.Copy(cipher, 0, iv, 0, ivLength);
            aes.IV = iv;

            try
            {
                using MemoryStream ms = new(cipher, ivLength, cipher.Length - ivLength);
                using CryptoStream cs = new(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
                using StreamReader sr = new(cs, Encoding.UTF8);

                return sr.ReadToEnd();
            }
            catch (CryptographicException ex)
            {
                throw new CryptographicException("Invalid PIN or corrupted encrypted data.", ex);
            }
        }

        private static byte[] OldEncrypt(string plainText, string pin)
        {
            using Aes aes = Aes.Create();
            aes.Key = SHA256.HashData(Encoding.UTF8.GetBytes(pin));
            aes.GenerateIV();

            using MemoryStream ms = new();
            ms.Write(aes.IV);

            using CryptoStream cs = new(ms, aes.CreateEncryptor(), CryptoStreamMode.Write);
            using StreamWriter sw = new(cs);
            sw.Write(plainText);

            return ms.ToArray();
        }

        private static string OldDecrypt(byte[] cipher, string pin)
        {
            using Aes aes = Aes.Create();
            aes.Key = SHA256.HashData(Encoding.UTF8.GetBytes(pin));

            byte[] iv = new byte[16];
            Array.Copy(cipher, iv, 16);
            aes.IV = iv;

            using MemoryStream ms = new(cipher, 16, cipher.Length - 16);
            using CryptoStream cs = new(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
            using StreamReader sr = new(cs);

            return sr.ReadToEnd();
        }

        // 🖨️ Print
        private void BtnPrint_Click(object sender, EventArgs e)
        {
            PrintDocument pd = new();
            pd.PrintPage += PrintPage;
            PrintPreviewDialog preview = new() { Document = pd };
            preview.ShowDialog();
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            lblRange = new Label();
            BtnAdd = new Button();
            BtnPrint = new Button();
            txtStart = new TextBox();
            txtEnd = new TextBox();
            dataGrid = new DataGridView();
            logoBox = new PictureBox();
            lblTitle = new Label();
            ((System.ComponentModel.ISupportInitialize)dataGrid).BeginInit();
            ((System.ComponentModel.ISupportInitialize)logoBox).BeginInit();
            SuspendLayout();
            // 
            // lblRange
            // 
            lblRange.AutoSize = true;
            lblRange.Location = new Point(12, 54);
            lblRange.Name = "lblRange";
            lblRange.Size = new Size(155, 15);
            lblRange.TabIndex = 1;
            lblRange.Text = "Add passport number range";
            // 
            // BtnAdd
            // 
            BtnAdd.Location = new Point(403, 50);
            BtnAdd.Name = "BtnAdd";
            BtnAdd.Size = new Size(132, 23);
            BtnAdd.TabIndex = 10;
            BtnAdd.Text = "Add Passports";
            BtnAdd.Click += BtnAdd_Click;
            // 
            // BtnPrint
            // 
            BtnPrint.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            BtnPrint.Location = new Point(595, 51);
            BtnPrint.Name = "BtnPrint";
            BtnPrint.Size = new Size(75, 23);
            BtnPrint.TabIndex = 3;
            BtnPrint.Text = "Print Log";
            BtnPrint.UseVisualStyleBackColor = true;
            BtnPrint.Click += BtnPrint_Click;
            // 
            // txtStart
            // 
            txtStart.Location = new Point(191, 51);
            txtStart.Name = "txtStart";
            txtStart.Size = new Size(100, 23);
            txtStart.TabIndex = 5;
            // 
            // txtEnd
            // 
            txtEnd.Location = new Point(297, 51);
            txtEnd.Name = "txtEnd";
            txtEnd.Size = new Size(100, 23);
            txtEnd.TabIndex = 6;
            // 
            // dataGrid
            // 
            dataGrid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dataGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGrid.Location = new Point(12, 93);
            dataGrid.Name = "dataGrid";
            dataGrid.Size = new Size(779, 264);
            dataGrid.TabIndex = 7;
            dataGrid.CellEndEdit += DataGrid_CellEndEdit;
            // 
            // logoBox
            // 
            logoBox.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            logoBox.Image = (Image)resources.GetObject("logoBox.Image");
            logoBox.Location = new Point(716, 12);
            logoBox.Name = "logoBox";
            logoBox.Size = new Size(75, 75);
            logoBox.TabIndex = 8;
            logoBox.TabStop = false;
            // 
            // lblTitle
            // 
            lblTitle.AutoSize = true;
            lblTitle.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblTitle.Location = new Point(12, 9);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(223, 21);
            lblTitle.TabIndex = 9;
            lblTitle.Text = "Emergency Passport Tracker";
            // 
            // MainForm
            // 
            ClientSize = new Size(803, 384);
            Controls.Add(lblTitle);
            Controls.Add(logoBox);
            Controls.Add(dataGrid);
            Controls.Add(txtEnd);
            Controls.Add(txtStart);
            Controls.Add(BtnPrint);
            Controls.Add(BtnAdd);
            Controls.Add(lblRange);
            Name = "MainForm";
            Text = "Emergency Passport Tracker";
            ((System.ComponentModel.ISupportInitialize)dataGrid).EndInit();
            ((System.ComponentModel.ISupportInitialize)logoBox).EndInit();
            ResumeLayout(false);
            PerformLayout();

        }

        private void PrintPage(object sender, PrintPageEventArgs e)
        {
            float y = 50;
            Font font = new("Arial", 10);

            e.Graphics.DrawString("INVENTORY LOG OF EMERGENCY PASSPORTS",
                new Font("Arial", 14, FontStyle.Bold), Brushes.Black, 50, y);
            y += 40;

            e.Graphics.DrawString("Emergency passport #    Issued to    Date issued    Notes/comments",
                font, Brushes.Black, 50, y);
            y += 30;

            foreach (var r in records)
            {
                e.Graphics.DrawString(
                    $"{r.PassportNumber}    {r.IssuedTo}    {r.DateIssued}    {r.Notes}",
                    font, Brushes.Black, 50, y);

                y += 20;
            }

            y += 40;
            e.Graphics.DrawString("Printed name and signature of Honorary Consul:",
                font, Brushes.Black, 50, y);

            y += 40;
            e.Graphics.DrawString("Date: ___________________", font, Brushes.Black, 50, y);
        }

        private void DataGrid_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            SaveData();
        }

    }

    public class PassportRecord
    {
        public string? PassportNumber { get; set; }
        public string? IssuedTo { get; set; }
        public string? DateIssued { get; set; }
        public string? Notes { get; set; }
    }
}
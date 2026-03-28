using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using EmergencyPassportTracker.Models;
using EmergencyPassportTracker.Services;

namespace EmergencyPassportTracker
{
    public partial class MainForm : Form
    {
        private List<PassportRecord> records = new();
        private List<AuditEntry> audit = new();
        private DataService dataService = new();
        private string pin = "";

        private Label? lblRange;
        private Button? BtnAdd;
        private Button? BtnPrint;
        private TextBox? txtStart;
        private TextBox? txtEnd;
        private DataGridView? dataGrid;
        private PictureBox? logoBox;
        private TextBox? txtSearch;
        private Button? btnPDF;
        private Button? btnSave;
        private Button? btnSearch;
        private Button btnAudit;
        private Label? lblTitle;

        public MainForm()
        {
            InitializeComponent();
            Login();
        }

        private void Login()
        {
            pin = Microsoft.VisualBasic.Interaction.InputBox("Enter PIN:");

            var data = dataService.Load(pin);
            records = data.Item1;
            audit = data.Item2;

            RefreshGrid();
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
            txtSearch = new TextBox();
            btnPDF = new Button();
            btnSave = new Button();
            btnSearch = new Button();
            btnAudit = new Button();
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
            BtnAdd.Size = new Size(92, 23);
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
            dataGrid.MultiSelect = false;
            dataGrid.Name = "dataGrid";
            dataGrid.Size = new Size(779, 325);
            dataGrid.TabIndex = 7;
            dataGrid.CellBeginEdit += DataGrid_CellBeginEdit;
            dataGrid.CellClick += DataGrid_CellClick;
            dataGrid.CellEndEdit += DataGrid_CellEndEdit;
            dataGrid.SelectionChanged += DataGrid_SelectionChanged;
            // 
            // logoBox
            // 
            logoBox.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            logoBox.Image = (System.Drawing.Image)resources.GetObject("logoBox.Image");
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
            // txtSearch
            // 
            txtSearch.Location = new Point(498, 12);
            txtSearch.Name = "txtSearch";
            txtSearch.Size = new Size(172, 23);
            txtSearch.TabIndex = 11;
            // 
            // btnPDF
            // 
            btnPDF.Location = new Point(514, 51);
            btnPDF.Name = "btnPDF";
            btnPDF.Size = new Size(75, 23);
            btnPDF.TabIndex = 12;
            btnPDF.Text = "PDF";
            btnPDF.UseVisualStyleBackColor = true;
            btnPDF.Click += btnPDF_Click;
            // 
            // btnSave
            // 
            btnSave.Location = new Point(255, 12);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(75, 23);
            btnSave.TabIndex = 13;
            btnSave.Text = "Save";
            btnSave.UseVisualStyleBackColor = true;
            btnSave.Click += btnSave_Click;
            // 
            // btnSearch
            // 
            btnSearch.Location = new Point(417, 12);
            btnSearch.Name = "btnSearch";
            btnSearch.Size = new Size(75, 23);
            btnSearch.TabIndex = 14;
            btnSearch.Text = "Search";
            btnSearch.UseVisualStyleBackColor = true;
            btnSearch.Click += btnSearch_Click;
            // 
            // btnAudit
            // 
            btnAudit.Location = new Point(336, 12);
            btnAudit.Name = "btnAudit";
            btnAudit.Size = new Size(75, 23);
            btnAudit.TabIndex = 15;
            btnAudit.Text = "Audit";
            btnAudit.UseVisualStyleBackColor = true;
            btnAudit.Click += btnAudit_Click;
            // 
            // MainForm
            // 
            ClientSize = new Size(803, 445);
            Controls.Add(btnAudit);
            Controls.Add(btnSearch);
            Controls.Add(btnSave);
            Controls.Add(btnPDF);
            Controls.Add(txtSearch);
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

        private void AddRecord(string number)
        {
            number = number.PadLeft(8, '0');
            if (records.Exists(r => r.PassportNumber == number))
                return;
            records.Add(new PassportRecord { PassportNumber = number });
            audit.Add(new AuditEntry { Timestamp = DateTime.Now, Action = "Added", PassportNumber = number });
        }

        private void UpdateRecord(PassportRecord record)
        {
            if (record.Locked)
            {
                MessageBox.Show("Record is locked and cannot be modified.");
                return;
            }

            if (record.Status == "B") // Issued
            {
                record.Locked = true;
            }

            audit.Add(new AuditEntry { Timestamp = DateTime.Now, Action = "Updated", PassportNumber = record.PassportNumber });

            SaveData();
        }

        private void OldUpdateRecord(PassportRecord record)
        {
            if (record.Locked)
            {
                MessageBox.Show("Record is locked and cannot be modified.");
                return;
            }

            if (record.Status == "B") // Issued
            {
                record.Locked = true;
            }

            audit.Add(new AuditEntry
            {
                Timestamp = DateTime.Now,
                Action = "Updated",
                PassportNumber = record.PassportNumber
            });

            SaveData();
        }

        private bool IsValidStatus(string status)
        {
            return status == "A" || status == "B" || status == "C" || status == "D";
        }

        private void ApplyFilter()
        {
            string search = txtSearch.Text.ToLower();

            var filtered = records.FindAll(r =>
                r.PassportNumber.Contains(search) ||
                (r.IssuedTo ?? "").ToLower().Contains(search) ||
                r.Status.Contains(search)
            );

            dataGrid.DataSource = null;
            dataGrid.DataSource = filtered;
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(txtStart.Text) && !string.IsNullOrWhiteSpace(txtEnd.Text))
            {
                int start = int.Parse(txtStart.Text);
                int end = int.Parse(txtEnd.Text);

                for (int i = start; i <= end; i++)
                    AddRecord(i.ToString("D8"));
            }

            //    SaveData();
            dataService.Save(records, audit, pin);
            RefreshGrid();
        }

        private void RefreshGrid()
        {
            dataGrid.DataSource = null;
            dataGrid.DataSource = records;
        }

        private void BtnPrint_Click(object sender, EventArgs e)
        {
            PrintDocument pd = new();
            pd.PrintPage += PrintPage;
            PrintPreviewDialog preview = new() { Document = pd };
            preview.ShowDialog();
        }

        private void PrintPage(object sender, PrintPageEventArgs e)
        {
            float y = 40;
            Font header = new("Arial", 14, FontStyle.Bold);
            Font font = new("Arial", 10);

            e.Graphics.DrawString("INVENTORY LOG OF EMERGENCY PASSPORTS", header, Brushes.Black, 50, y);
            y += 30;

            e.Graphics.DrawString("Passport #", font, Brushes.Black, 50, y);
            e.Graphics.DrawString("Issued to", font, Brushes.Black, 200, y);
            e.Graphics.DrawString("Date issued", font, Brushes.Black, 350, y);
            e.Graphics.DrawString("Notes", font, Brushes.Black, 480, y);
            e.Graphics.DrawString("Status", font, Brushes.Black, 600, y);

            y += 20;

            foreach (var r in records)
            {
                e.Graphics.DrawString(r.PassportNumber, font, Brushes.Black, 50, y);
                e.Graphics.DrawString(r.IssuedTo, font, Brushes.Black, 200, y);
                e.Graphics.DrawString(r.DateIssued?.ToShortDateString(), font, Brushes.Black, 350, y);
                e.Graphics.DrawString(r.Notes, font, Brushes.Black, 480, y);
                e.Graphics.DrawString(r.Status, font, Brushes.Black, 600, y);

                y += 20;
            }

            y += 40;
            e.Graphics.DrawString("Printed name and signature of Honorary Consul:", font, Brushes.Black, 50, y);

            y += 40;
            e.Graphics.DrawString("Date: ____________________", font, Brushes.Black, 50, y);
        }


        private void DataGrid_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return; // 🔥 prevents header crash

            var record = dataGrid.Rows[e.RowIndex].DataBoundItem as PassportRecord;
            if (record == null)
                return;

            // Optional: do something with record
            UpdateRecord(record);
        }

        private void DataGrid_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            if (e.RowIndex < 0) return; // 🔥 prevents header crash

            var record = dataGrid.Rows[e.RowIndex].DataBoundItem as PassportRecord;
            if (record == null)
                return;

            if (record.Locked)
            {
                dataGrid.CancelEdit();
                return;
            }
        }

        private void DataGrid_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return; // 🔥 prevents header crash

            var record = dataGrid.Rows[e.RowIndex].DataBoundItem as PassportRecord;
            if (record == null)
                return;

            // Optional: do something with record
            UpdateRecord(record);
        }

        private void DataGrid_SelectionChanged(object sender, EventArgs e)
        {
            if (dataGrid.CurrentRow == null || dataGrid.CurrentRow.Index < 0)
                return;

            var record = dataGrid.CurrentRow.DataBoundItem as PassportRecord;
            if (record == null) return;

            // Optional: do something
        }

        private void SaveData()
        {
            dataService.Save(records, audit, pin);
        }

        public class DataWrapper
        {
            public List<PassportRecord> Records { get; set; }
            public List<AuditEntry> Audit { get; set; }
        }

        private void ExportPdf(string path)
        {
            using var writer = new PdfWriter(path);
            using var pdf = new PdfDocument(writer);
            var doc = new Document(pdf);

            doc.Add(new Paragraph("INVENTORY LOG OF EMERGENCY PASSPORTS"));

            foreach (var r in records)
            {
                doc.Add(new Paragraph($"{r.PassportNumber} | {r.IssuedTo} | {r.DateIssued} | {r.Status} | {r.Notes}"));
            }

            doc.Close();
        }

        private void btnPDF_Click(object sender, EventArgs e)
        {
            ExportPdf("EPTLog.PDF");
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            SaveData();
            dataService.BackupData();
        }

        private void ShowAuditLog()
        {
            var sb = new StringBuilder();

            foreach (var a in audit)
            {
                sb.AppendLine($"{a.Timestamp} | {a.Action} | {a.PassportNumber}");
            }

            MessageBox.Show(sb.ToString(), "Audit Log");
        }

        private void btnAudit_Click(object sender, EventArgs e)
        {
            ShowAuditLog();
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            ApplyFilter();
        }
    }
}
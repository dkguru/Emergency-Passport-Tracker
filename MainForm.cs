using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using Emergency_Passport_Tracker.Models;
using Emergency_Passport_Tracker.Services;
using Emergency_Passport_Tracker.UI;

namespace Emergency_Passport_Tracker
{
    public partial class MainForm : Form
    {
        private const int MaxAddRange = 10_000;

        private readonly DataService _dataService;
        private readonly List<PassportRecord> _records;
        private readonly List<AuditEntry> _audit;
        private readonly string _pin;

        private readonly BindingSource _binding = new();
        private bool _dirty;

        // Print state - kept across pages so long logs are not truncated.
        private int _printIndex;
        private int _printPage;

        // Remembers the value a cell had before an edit so an invalid entry can be undone.
        private object? _cellValueBeforeEdit;

        private Label lblRange = null!;
        private Label lblTitle = null!;
        private Label lblStatus = null!;
        private Button BtnAdd = null!;
        private Button BtnPrint = null!;
        private Button btnPDF = null!;
        private Button btnSave = null!;
        private Button btnSearch = null!;
        private Button btnAudit = null!;
        private Button btnBackup = null!;
        private Button btnRestore = null!;
        private TextBox txtStart = null!;
        private TextBox txtEnd = null!;
        private TextBox txtSearch = null!;
        private DataGridView dataGrid = null!;
        private PictureBox logoBox = null!;
        private DataGridViewComboBoxColumn colStatus = null!;

        public MainForm(DataService dataService, string pin, List<PassportRecord> records, List<AuditEntry> audit)
        {
            _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
            _pin = pin ?? throw new ArgumentNullException(nameof(pin));
            _records = records ?? new List<PassportRecord>();
            _audit = audit ?? new List<AuditEntry>();

            InitializeComponent();

            NormaliseRecords();
            SortRecords();
            RefreshGrid();
        }

        /// <summary>
        /// Tidies data coming off disk so the grid can display it: trims and upper-cases the
        /// status, defaults blanks to A, and makes room in the status list for any unexpected
        /// value rather than throwing a data error on every repaint.
        /// </summary>
        private void NormaliseRecords()
        {
            foreach (PassportRecord r in _records)
            {
                r.PassportNumber = (r.PassportNumber ?? string.Empty).Trim();

                string status = (r.Status ?? string.Empty).Trim().ToUpperInvariant();
                r.Status = status.Length == 0 ? "A" : status;

                if (!colStatus.Items.Contains(r.Status))
                    colStatus.Items.Add(r.Status);
            }
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources =
                new System.ComponentModel.ComponentResourceManager(typeof(MainForm));

            lblRange = new Label();
            lblTitle = new Label();
            lblStatus = new Label();
            BtnAdd = new Button();
            BtnPrint = new Button();
            btnPDF = new Button();
            btnSave = new Button();
            btnSearch = new Button();
            btnAudit = new Button();
            btnBackup = new Button();
            btnRestore = new Button();
            txtStart = new TextBox();
            txtEnd = new TextBox();
            txtSearch = new TextBox();
            dataGrid = new DataGridView();
            logoBox = new PictureBox();

            ((System.ComponentModel.ISupportInitialize)dataGrid).BeginInit();
            ((System.ComponentModel.ISupportInitialize)logoBox).BeginInit();
            SuspendLayout();
            //
            // lblTitle
            //
            lblTitle.AutoSize = true;
            lblTitle.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblTitle.Location = new Point(12, 9);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(223, 21);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "Emergency Passport Tracker";
            //
            // btnSave
            //
            btnSave.Location = new Point(255, 12);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(75, 23);
            btnSave.TabIndex = 1;
            btnSave.Text = "Save";
            btnSave.UseVisualStyleBackColor = true;
            btnSave.Click += btnSave_Click;
            //
            // btnAudit
            //
            btnAudit.Location = new Point(336, 12);
            btnAudit.Name = "btnAudit";
            btnAudit.Size = new Size(75, 23);
            btnAudit.TabIndex = 2;
            btnAudit.Text = "Audit";
            btnAudit.UseVisualStyleBackColor = true;
            btnAudit.Click += btnAudit_Click;
            //
            // btnSearch
            //
            btnSearch.Location = new Point(417, 12);
            btnSearch.Name = "btnSearch";
            btnSearch.Size = new Size(75, 23);
            btnSearch.TabIndex = 3;
            btnSearch.Text = "Search";
            btnSearch.UseVisualStyleBackColor = true;
            btnSearch.Click += btnSearch_Click;
            //
            // txtSearch
            //
            txtSearch.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            txtSearch.Location = new Point(498, 12);
            txtSearch.Name = "txtSearch";
            txtSearch.PlaceholderText = "Search number, name or status";
            txtSearch.Size = new Size(172, 23);
            txtSearch.TabIndex = 4;
            txtSearch.TextChanged += txtSearch_TextChanged;
            txtSearch.KeyDown += txtSearch_KeyDown;
            //
            // logoBox
            //
            logoBox.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            logoBox.Image = (System.Drawing.Image)resources.GetObject("logoBox.Image");
            logoBox.Location = new Point(716, 12);
            logoBox.Name = "logoBox";
            logoBox.Size = new Size(75, 75);
            logoBox.SizeMode = PictureBoxSizeMode.Zoom;
            logoBox.TabIndex = 5;
            logoBox.TabStop = false;
            //
            // lblRange
            //
            lblRange.AutoSize = true;
            lblRange.Location = new Point(12, 54);
            lblRange.Name = "lblRange";
            lblRange.Size = new Size(155, 15);
            lblRange.TabIndex = 6;
            lblRange.Text = "Add passport number range";
            //
            // txtStart
            //
            txtStart.Location = new Point(191, 51);
            txtStart.Name = "txtStart";
            txtStart.PlaceholderText = "From";
            txtStart.Size = new Size(100, 23);
            txtStart.TabIndex = 7;
            //
            // txtEnd
            //
            txtEnd.Location = new Point(297, 51);
            txtEnd.Name = "txtEnd";
            txtEnd.PlaceholderText = "To";
            txtEnd.Size = new Size(100, 23);
            txtEnd.TabIndex = 8;
            //
            // BtnAdd
            //
            BtnAdd.Location = new Point(403, 50);
            BtnAdd.Name = "BtnAdd";
            BtnAdd.Size = new Size(92, 23);
            BtnAdd.TabIndex = 9;
            BtnAdd.Text = "Add Passports";
            BtnAdd.UseVisualStyleBackColor = true;
            BtnAdd.Click += BtnAdd_Click;
            //
            // btnPDF
            //
            btnPDF.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnPDF.Location = new Point(514, 51);
            btnPDF.Name = "btnPDF";
            btnPDF.Size = new Size(75, 23);
            btnPDF.TabIndex = 10;
            btnPDF.Text = "PDF";
            btnPDF.UseVisualStyleBackColor = true;
            btnPDF.Click += btnPDF_Click;
            //
            // BtnPrint
            //
            BtnPrint.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            BtnPrint.Location = new Point(595, 51);
            BtnPrint.Name = "BtnPrint";
            BtnPrint.Size = new Size(75, 23);
            BtnPrint.TabIndex = 11;
            BtnPrint.Text = "Print Log";
            BtnPrint.UseVisualStyleBackColor = true;
            BtnPrint.Click += BtnPrint_Click;
            //
            // dataGrid
            //
            dataGrid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dataGrid.AllowUserToAddRows = false;
            dataGrid.AllowUserToDeleteRows = false;
            dataGrid.AutoGenerateColumns = false;
            dataGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGrid.EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2;
            dataGrid.Location = new Point(12, 93);
            dataGrid.MultiSelect = false;
            dataGrid.Name = "dataGrid";
            dataGrid.RowHeadersWidth = 25;
            dataGrid.SelectionMode = DataGridViewSelectionMode.CellSelect;
            dataGrid.Size = new Size(779, 338);
            dataGrid.TabIndex = 12;
            dataGrid.Columns.AddRange(BuildColumns());
            dataGrid.CellBeginEdit += DataGrid_CellBeginEdit;
            dataGrid.CellEndEdit += DataGrid_CellEndEdit;
            dataGrid.DataBindingComplete += DataGrid_DataBindingComplete;
            dataGrid.DataError += DataGrid_DataError;
            //
            // btnBackup
            //
            btnBackup.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnBackup.Location = new Point(12, 439);
            btnBackup.Name = "btnBackup";
            btnBackup.Size = new Size(110, 25);
            btnBackup.TabIndex = 13;
            btnBackup.Text = "Backup to CSV";
            btnBackup.UseVisualStyleBackColor = true;
            btnBackup.Click += btnBackup_Click;
            //
            // btnRestore
            //
            btnRestore.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnRestore.Location = new Point(128, 439);
            btnRestore.Name = "btnRestore";
            btnRestore.Size = new Size(110, 25);
            btnRestore.TabIndex = 14;
            btnRestore.Text = "Restore from CSV";
            btnRestore.UseVisualStyleBackColor = true;
            btnRestore.Click += btnRestore_Click;
            //
            // lblStatus
            //
            lblStatus.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            lblStatus.AutoSize = true;
            lblStatus.ForeColor = SystemColors.GrayText;
            lblStatus.Location = new Point(248, 445);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(0, 15);
            lblStatus.TabIndex = 15;
            //
            // MainForm
            //
            ClientSize = new Size(803, 480);
            MinimumSize = new Size(700, 400);
            Controls.Add(lblStatus);
            Controls.Add(btnRestore);
            Controls.Add(btnBackup);
            Controls.Add(BtnPrint);
            Controls.Add(btnPDF);
            Controls.Add(BtnAdd);
            Controls.Add(txtEnd);
            Controls.Add(txtStart);
            Controls.Add(lblRange);
            Controls.Add(dataGrid);
            Controls.Add(logoBox);
            Controls.Add(txtSearch);
            Controls.Add(btnSearch);
            Controls.Add(btnAudit);
            Controls.Add(btnSave);
            Controls.Add(lblTitle);
            Name = "MainForm";
            Text = "Emergency Passport Tracker";
            FormClosing += MainForm_FormClosing;
            ((System.ComponentModel.ISupportInitialize)dataGrid).EndInit();
            ((System.ComponentModel.ISupportInitialize)logoBox).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        /// <summary>
        /// Explicit columns (rather than auto-generated ones) so key fields cannot be edited
        /// and the status can only be set to a valid value.
        /// </summary>
        private DataGridViewColumn[] BuildColumns()
        {
            var colNumber = new DataGridViewTextBoxColumn
            {
                Name = "colPassportNumber",
                DataPropertyName = nameof(PassportRecord.PassportNumber),
                HeaderText = "Passport #",
                ReadOnly = true,
                Width = 100
            };

            var colIssuedTo = new DataGridViewTextBoxColumn
            {
                Name = "colIssuedTo",
                DataPropertyName = nameof(PassportRecord.IssuedTo),
                HeaderText = "Issued to",
                Width = 170
            };

            var colDateIssued = new DataGridViewTextBoxColumn
            {
                Name = "colDateIssued",
                DataPropertyName = nameof(PassportRecord.DateIssued),
                HeaderText = "Date issued",
                Width = 100
            };
            colDateIssued.DefaultCellStyle.Format = "d";

            var colNotes = new DataGridViewTextBoxColumn
            {
                Name = "colNotes",
                DataPropertyName = nameof(PassportRecord.Notes),
                HeaderText = "Notes",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                MinimumWidth = 120
            };

            colStatus = new DataGridViewComboBoxColumn
            {
                Name = "colStatus",
                DataPropertyName = nameof(PassportRecord.Status),
                HeaderText = "Status",
                Width = 70,
                FlatStyle = FlatStyle.Flat
            };
            colStatus.Items.AddRange("A", "B", "C", "D");
            colStatus.ToolTipText = "A = in possession, B = issued, C = missing, D = destroyed";

            var colLocked = new DataGridViewCheckBoxColumn
            {
                Name = "colLocked",
                DataPropertyName = nameof(PassportRecord.Locked),
                HeaderText = "Locked",
                ReadOnly = true,
                Width = 55
            };

            return new DataGridViewColumn[]
            {
                colNumber, colIssuedTo, colDateIssued, colNotes, colStatus, colLocked
            };
        }

        // ------------------------------------------------------------ grid data

        private void SortRecords()
        {
            _records.Sort((a, b) => string.CompareOrdinal(a.PassportNumber, b.PassportNumber));
        }

        private void RefreshGrid()
        {
            IEnumerable<PassportRecord> view = _records;
            string search = txtSearch.Text.Trim();

            if (search.Length > 0)
                view = _records.Where(r => Matches(r, search));

            var list = new BindingList<PassportRecord>(view.ToList());
            _binding.DataSource = list;

            if (dataGrid.DataSource != _binding)
                dataGrid.DataSource = _binding;

            UpdateStatusLabel(list.Count);
        }

        private static bool Matches(PassportRecord record, string search)
        {
            const StringComparison Ci = StringComparison.OrdinalIgnoreCase;

            return (record.PassportNumber ?? string.Empty).Contains(search, Ci)
                   || (record.IssuedTo ?? string.Empty).Contains(search, Ci)
                   || (record.Notes ?? string.Empty).Contains(search, Ci)
                   || (record.Status ?? string.Empty).Contains(search, Ci);
        }

        private void UpdateStatusLabel(int shown)
        {
            int inPossession = _records.Count(r => r.Status == "A");
            int issued = _records.Count(r => r.Status == "B");
            int missing = _records.Count(r => r.Status == "C");
            int destroyed = _records.Count(r => r.Status == "D");

            string text =
                $"Showing {shown} of {_records.Count}  |  A in possession: {inPossession}   " +
                $"B issued: {issued}   C missing: {missing}   D destroyed: {destroyed}";

            lblStatus.Text = text;
        }

        private void DataGrid_DataBindingComplete(object? sender, DataGridViewBindingCompleteEventArgs e)
        {
            // Locked records are shown greyed out and cannot be edited at all.
            foreach (DataGridViewRow row in dataGrid.Rows)
            {
                if (row.DataBoundItem is not PassportRecord record)
                    continue;

                row.ReadOnly = record.Locked;
                row.DefaultCellStyle.BackColor = record.Locked
                    ? Color.FromArgb(240, 240, 240)
                    : dataGrid.DefaultCellStyle.BackColor;
            }
        }

        private void DataGrid_DataError(object? sender, DataGridViewDataErrorEventArgs e)
        {
            e.ThrowException = false;
            e.Cancel = true;

            // Only report problems caused by something the user just typed. Display-time
            // formatting glitches would otherwise pop a dialog on every repaint.
            bool userEntry = (e.Context & DataGridViewDataErrorContexts.Commit) != 0
                             || (e.Context & DataGridViewDataErrorContexts.Parsing) != 0;

            if (!userEntry)
                return;

            string column = e.ColumnIndex >= 0 && e.ColumnIndex < dataGrid.Columns.Count
                ? dataGrid.Columns[e.ColumnIndex].HeaderText
                : "this column";

            MessageBox.Show(this,
                $"That value is not valid for {column}." + Environment.NewLine +
                "Dates should be entered as a date, for example " +
                DateTime.Today.ToShortDateString() + ".",
                "Emergency Passport Tracker", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        // ------------------------------------------------------------- editing

        private void DataGrid_CellBeginEdit(object? sender, DataGridViewCellCancelEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= dataGrid.Rows.Count)
                return;

            if (dataGrid.Rows[e.RowIndex].DataBoundItem is not PassportRecord record)
                return;

            if (record.Locked)
            {
                e.Cancel = true;
                MessageBox.Show(this,
                    $"Passport {record.PassportNumber} has been issued and is locked. It cannot be changed.",
                    "Record locked", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _cellValueBeforeEdit = dataGrid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;
        }

        private void DataGrid_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= dataGrid.Rows.Count)
                return;

            if (dataGrid.Rows[e.RowIndex].DataBoundItem is not PassportRecord record)
                return;

            string columnName = dataGrid.Columns[e.ColumnIndex].Name;

            if (columnName == "colStatus" && !IsValidStatus(record.Status))
            {
                MessageBox.Show(this,
                    "Status must be A, B, C or D." + Environment.NewLine + Environment.NewLine +
                    "A = in possession   B = issued   C = missing   D = destroyed",
                    "Invalid status", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                record.Status = _cellValueBeforeEdit as string ?? "A";
                dataGrid.InvalidateRow(e.RowIndex);
                return;
            }

            UpdateRecord(record, columnName);
        }

        private static bool IsValidStatus(string? status)
        {
            return status is "A" or "B" or "C" or "D";
        }

        /// <summary>
        /// Records an edit, locks the record once it is fully issued, and saves.
        /// </summary>
        private void UpdateRecord(PassportRecord record, string changedColumn)
        {
            if (record.Locked)
                return;

            bool lockNow = false;

            if (record.Status == "B")
            {
                // The record used to lock the instant the status became B, which made it
                // impossible to then type in who it was issued to. It now locks only once the
                // issue details are present, and only after confirmation.
                bool detailsComplete = !string.IsNullOrWhiteSpace(record.IssuedTo)
                                       && record.DateIssued.HasValue;

                // Only ask when one of the issue fields was the thing that changed, so editing
                // Notes on an unlocked issued record does not re-ask every time.
                bool issueColumn = changedColumn is "colStatus" or "colIssuedTo" or "colDateIssued";

                if (detailsComplete && issueColumn)
                {
                    DialogResult answer = MessageBox.Show(this,
                        $"Passport {record.PassportNumber} is marked as issued to " +
                        $"{record.IssuedTo} on {record.DateIssued:d}." + Environment.NewLine + Environment.NewLine +
                        "Lock this record now? Once locked it can never be changed again.",
                        "Lock record", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                    lockNow = answer == DialogResult.Yes;
                }
                else if (changedColumn == "colStatus")
                {
                    MessageBox.Show(this,
                        "Fill in 'Issued to' and 'Date issued' as well." + Environment.NewLine +
                        "The record will be locked once those details are complete.",
                        "Issue details needed", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }

            _audit.Add(new AuditEntry
            {
                Timestamp = DateTime.Now,
                Action = $"Updated ({dataGrid.Columns[changedColumn]?.HeaderText ?? changedColumn})",
                PassportNumber = record.PassportNumber
            });

            if (lockNow)
            {
                record.Locked = true;
                _audit.Add(new AuditEntry
                {
                    Timestamp = DateTime.Now,
                    Action = "Locked (issued)",
                    PassportNumber = record.PassportNumber
                });
            }

            _dirty = true;

            // Deferred: rebinding the grid from inside CellEndEdit causes a reentrant
            // binding call, so the refresh is posted until after the event finishes.
            if (SaveData())
                BeginInvoke(new Action(RefreshGrid));
        }

        // ---------------------------------------------------------------- add

        private void BtnAdd_Click(object? sender, EventArgs e)
        {
            if (!TryReadNumber(txtStart, "start", out int start) ||
                !TryReadNumber(txtEnd, "end", out int end))
            {
                return;
            }

            if (end < start)
            {
                MessageBox.Show(this, "The end number must not be lower than the start number.",
                    "Check the range", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtEnd.Focus();
                return;
            }

            long count = (long)end - start + 1;

            if (count > MaxAddRange)
            {
                MessageBox.Show(this,
                    $"That range covers {count:N0} passports, which is more than the {MaxAddRange:N0} " +
                    "allowed in one go. Please add a smaller range.",
                    "Range too large", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (count > 100)
            {
                DialogResult answer = MessageBox.Show(this,
                    $"Add {count:N0} passport numbers?", "Confirm",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (answer != DialogResult.Yes)
                    return;
            }

            var existing = new HashSet<string>(
                _records.Select(r => r.PassportNumber), StringComparer.OrdinalIgnoreCase);

            int added = 0;
            int skipped = 0;

            for (int i = start; i <= end; i++)
            {
                string number = i.ToString("D8", CultureInfo.InvariantCulture);

                if (!existing.Add(number))
                {
                    skipped++;
                    continue;
                }

                _records.Add(new PassportRecord { PassportNumber = number });
                _audit.Add(new AuditEntry
                {
                    Timestamp = DateTime.Now,
                    Action = "Added",
                    PassportNumber = number
                });

                added++;
            }

            if (added == 0)
            {
                MessageBox.Show(this,
                    "Those passport numbers are already on file. Nothing was added.",
                    "Nothing to add", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _dirty = true;
            SortRecords();

            if (SaveData())
                RefreshGrid();

            txtStart.Clear();
            txtEnd.Clear();
            txtStart.Focus();

            string message = $"{added:N0} passport number{(added == 1 ? "" : "s")} added.";
            if (skipped > 0)
                message += $" {skipped:N0} were already on file and were skipped.";

            MessageBox.Show(this, message, "Passports added", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private bool TryReadNumber(TextBox box, string which, out int value)
        {
            string text = box.Text.Trim();

            if (!int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out value)
                || value < 1 || value > 99_999_999)
            {
                MessageBox.Show(this,
                    $"Enter a whole {which} number between 1 and 99999999.",
                    "Check the range", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                box.Focus();
                box.SelectAll();
                value = 0;
                return false;
            }

            return true;
        }

        // --------------------------------------------------------------- save

        /// <summary>Saves and reports failures. Returns false when the data was not written.</summary>
        private bool SaveData()
        {
            try
            {
                _dataService.Save(_records, _audit, _pin);
                _dirty = false;
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    "The data could not be saved:" + Environment.NewLine + Environment.NewLine + ex.Message +
                    Environment.NewLine + Environment.NewLine +
                    "Your changes are still on screen. Fix the problem and try Save again.",
                    "Save failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private void btnSave_Click(object? sender, EventArgs e)
        {
            if (!SaveData())
                return;

            try
            {
                string? copy = _dataService.BackupEncrypted();

                MessageBox.Show(this,
                    copy == null
                        ? "Saved."
                        : "Saved. An encrypted copy was placed in:" + Environment.NewLine + copy,
                    "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    "The data was saved, but the encrypted copy could not be written:" +
                    Environment.NewLine + ex.Message,
                    "Saved", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (!_dirty)
                return;

            if (!SaveData())
            {
                DialogResult answer = MessageBox.Show(this,
                    "The latest changes could not be saved. Close anyway and lose them?",
                    "Unsaved changes", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                if (answer != DialogResult.Yes)
                    e.Cancel = true;
            }
        }

        // ------------------------------------------------------ backup/restore

        private void btnBackup_Click(object? sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "Choose where to write the unencrypted CSV backup " +
                              "(use an encrypted drive).",
                UseDescriptionForTitle = true
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            try
            {
                (string recordsPath, string auditPath) = BackupService.Export(
                    dialog.SelectedPath, _records, _audit);

                MessageBox.Show(this,
                    $"{_records.Count:N0} records and {_audit.Count:N0} audit entries were written to:" +
                    Environment.NewLine + Environment.NewLine +
                    Path.GetFileName(recordsPath) + Environment.NewLine +
                    Path.GetFileName(auditPath) + Environment.NewLine + Environment.NewLine +
                    "in " + dialog.SelectedPath + Environment.NewLine + Environment.NewLine +
                    "These files are NOT encrypted. Keep them on an encrypted drive.",
                    "Backup complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    "The backup could not be written:" + Environment.NewLine + Environment.NewLine + ex.Message,
                    "Backup failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnRestore_Click(object? sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Title = "Select the records CSV to restore",
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                CheckFileExists = true
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            List<PassportRecord> records;
            List<AuditEntry> audit;

            try
            {
                string? auditPath = BackupService.GuessAuditPath(dialog.FileName);

                if (auditPath == null)
                {
                    DialogResult pick = MessageBox.Show(this,
                        "No matching audit file was found next to that records file." +
                        Environment.NewLine + Environment.NewLine +
                        "Select the audit CSV as well? Choose No to restore the records only.",
                        "Audit file", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

                    if (pick == DialogResult.Cancel)
                        return;

                    if (pick == DialogResult.Yes)
                    {
                        using var auditDialog = new OpenFileDialog
                        {
                            Title = "Select the audit CSV",
                            Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                            CheckFileExists = true
                        };

                        if (auditDialog.ShowDialog(this) == DialogResult.OK)
                            auditPath = auditDialog.FileName;
                    }
                }

                (records, audit) = BackupService.Import(dialog.FileName, auditPath);
            }
            catch (BackupFormatException ex)
            {
                MessageBox.Show(this, ex.Message, "Backup file not usable",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    "The backup could not be read:" + Environment.NewLine + Environment.NewLine + ex.Message,
                    "Restore failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            DialogResult confirm = MessageBox.Show(this,
                $"This will replace all {_records.Count:N0} records currently in the tracker with " +
                $"the {records.Count:N0} records from the backup." + Environment.NewLine + Environment.NewLine +
                "A copy of the current encrypted data file will be kept first." +
                Environment.NewLine + Environment.NewLine + "Continue?",
                "Confirm restore", MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);

            if (confirm != DialogResult.Yes)
                return;

            string? snapshot;

            try
            {
                snapshot = _dataService.SnapshotBeforeRestore();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    "A safety copy of the current data could not be made, so the restore was " +
                    "cancelled:" + Environment.NewLine + Environment.NewLine + ex.Message,
                    "Restore cancelled", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            _records.Clear();
            _records.AddRange(records);

            _audit.Clear();
            _audit.AddRange(audit);
            _audit.Add(new AuditEntry
            {
                Timestamp = DateTime.Now,
                Action = "Restored from backup " + Path.GetFileName(dialog.FileName),
                PassportNumber = string.Empty
            });

            NormaliseRecords();
            SortRecords();
            _dirty = true;

            if (SaveData())
            {
                txtSearch.Clear();
                RefreshGrid();

                MessageBox.Show(this,
                    $"Restored {_records.Count:N0} records." +
                    (snapshot == null
                        ? string.Empty
                        : Environment.NewLine + Environment.NewLine +
                          "The previous data was kept as:" + Environment.NewLine + snapshot),
                    "Restore complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        // -------------------------------------------------------------- search

        private void btnSearch_Click(object? sender, EventArgs e) => RefreshGrid();

        private void txtSearch_TextChanged(object? sender, EventArgs e) => RefreshGrid();

        private void txtSearch_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                txtSearch.Clear();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Enter)
            {
                RefreshGrid();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        // --------------------------------------------------------------- print

        private void BtnPrint_Click(object? sender, EventArgs e)
        {
            if (_records.Count == 0)
            {
                MessageBox.Show(this, "There is nothing to print yet.", "Print log",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var document = new PrintDocument();
            document.DocumentName = "Inventory log of emergency passports";
            document.BeginPrint += (_, _) => { _printIndex = 0; _printPage = 0; };
            document.PrintPage += PrintPage;

            using var preview = new PrintPreviewDialog
            {
                Document = document,
                Width = 900,
                Height = 700,
                StartPosition = FormStartPosition.CenterParent
            };

            try
            {
                preview.ShowDialog(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    "The log could not be printed:" + Environment.NewLine + Environment.NewLine + ex.Message,
                    "Print failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Draws one page. Continues across pages via HasMorePages - the previous version drew
        /// every record on a single page, so anything past the bottom was silently lost.
        /// </summary>
        private void PrintPage(object? sender, PrintPageEventArgs e)
        {
            Graphics? g = e.Graphics;
            if (g == null)
                return;

            using var headerFont = new Font("Arial", 14, FontStyle.Bold);
            using var columnFont = new Font("Arial", 9, FontStyle.Bold);
            using var font = new Font("Arial", 9);

            Rectangle bounds = e.MarginBounds;
            float left = bounds.Left;
            float y = bounds.Top;

            _printPage++;

            const float SignatureBlockHeight = 110f;
            const float RowHeight = 18f;

            float[] columns =
            {
                left,
                left + 90,
                left + 290,
                left + 390,
                left + 640
            };

            g.DrawString("INVENTORY LOG OF EMERGENCY PASSPORTS", headerFont, Brushes.Black, left, y);
            y += 26;

            g.DrawString($"Page {_printPage}    Printed {DateTime.Now:g}", font, Brushes.Black, left, y);
            y += 20;

            g.DrawString("Passport #", columnFont, Brushes.Black, columns[0], y);
            g.DrawString("Issued to", columnFont, Brushes.Black, columns[1], y);
            g.DrawString("Date issued", columnFont, Brushes.Black, columns[2], y);
            g.DrawString("Notes", columnFont, Brushes.Black, columns[3], y);
            g.DrawString("Status", columnFont, Brushes.Black, columns[4], y);
            y += 16;

            g.DrawLine(Pens.Black, left, y, bounds.Right, y);
            y += 6;

            while (_printIndex < _records.Count && y + RowHeight < bounds.Bottom - SignatureBlockHeight)
            {
                PassportRecord r = _records[_printIndex];

                g.DrawString(r.PassportNumber, font, Brushes.Black, columns[0], y);
                g.DrawString(Truncate(r.IssuedTo, 28), font, Brushes.Black, columns[1], y);
                g.DrawString(r.DateIssued?.ToShortDateString() ?? string.Empty, font, Brushes.Black, columns[2], y);
                g.DrawString(Truncate(r.Notes, 34), font, Brushes.Black, columns[3], y);
                g.DrawString(r.Status, font, Brushes.Black, columns[4], y);

                y += RowHeight;
                _printIndex++;
            }

            bool more = _printIndex < _records.Count;
            e.HasMorePages = more;

            if (!more)
            {
                y += 30;
                g.DrawString("Printed name and signature of Honorary Consul:", font, Brushes.Black, left, y);
                y += 34;
                g.DrawString("_______________________________________", font, Brushes.Black, left, y);
                y += 26;
                g.DrawString("Date: ____________________", font, Brushes.Black, left, y);
            }
        }

        private static string Truncate(string? value, int max)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            return value.Length <= max ? value : value.Substring(0, max - 3) + "...";
        }

        // ----------------------------------------------------------------- pdf

        private void btnPDF_Click(object? sender, EventArgs e)
        {
            if (_records.Count == 0)
            {
                MessageBox.Show(this, "There is nothing to export yet.", "Export PDF",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var dialog = new SaveFileDialog
            {
                Title = "Export the log as PDF",
                Filter = "PDF files (*.pdf)|*.pdf",
                FileName = $"EPT-Log-{DateTime.Now:yyyyMMdd}.pdf",
                OverwritePrompt = true
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            try
            {
                ExportPdf(dialog.FileName);

                MessageBox.Show(this, "Exported to:" + Environment.NewLine + dialog.FileName,
                    "Export complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (IOException ex)
            {
                MessageBox.Show(this,
                    "The PDF could not be written. If it is already open in a PDF reader, close it " +
                    "and try again." + Environment.NewLine + Environment.NewLine + ex.Message,
                    "Export failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    "The PDF could not be written:" + Environment.NewLine + Environment.NewLine + ex.Message,
                    "Export failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ExportPdf(string path)
        {
            // Document.Close() closes the PdfDocument and the writer, so these are deliberately
            // not wrapped in 'using' - that would dispose them a second time.
            var writer = new PdfWriter(path);
            var pdf = new PdfDocument(writer);
            var document = new Document(pdf, PageSize.A4);

            try
            {
                document.Add(new Paragraph("INVENTORY LOG OF EMERGENCY PASSPORTS")
                    .SetBold()
                    .SetFontSize(14));

                document.Add(new Paragraph($"Printed {DateTime.Now:g}").SetFontSize(8));

                var table = new Table(UnitValue.CreatePercentArray(new float[] { 16, 26, 15, 33, 10 }))
                    .UseAllAvailableWidth();

                foreach (string heading in new[] { "Passport #", "Issued to", "Date issued", "Notes", "Status" })
                {
                    table.AddHeaderCell(new Cell().Add(new Paragraph(heading).SetBold().SetFontSize(9)));
                }

                foreach (PassportRecord r in _records)
                {
                    table.AddCell(MakeCell(r.PassportNumber));
                    table.AddCell(MakeCell(r.IssuedTo));
                    table.AddCell(MakeCell(r.DateIssued?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
                    table.AddCell(MakeCell(r.Notes));
                    table.AddCell(MakeCell(r.Status));
                }

                document.Add(table);

                document.Add(new Paragraph("\nPrinted name and signature of Honorary Consul: " +
                                           "_______________________________").SetFontSize(9));
                document.Add(new Paragraph("Date: ____________________").SetFontSize(9));
            }
            finally
            {
                document.Close();
            }
        }

        private static Cell MakeCell(string? text)
        {
            return new Cell().Add(new Paragraph(text ?? string.Empty).SetFontSize(9));
        }

        // --------------------------------------------------------------- audit

        private void btnAudit_Click(object? sender, EventArgs e) => ShowAuditLog();

        private void ShowAuditLog()
        {
            if (_audit.Count == 0)
            {
                MessageBox.Show(this, "The audit log is empty.", "Audit log",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var sb = new StringBuilder();

            // Newest first, and capped so a long history cannot overflow the dialog.
            const int Limit = 500;
            int shown = 0;

            for (int i = _audit.Count - 1; i >= 0 && shown < Limit; i--, shown++)
            {
                AuditEntry a = _audit[i];
                sb.AppendLine($"{a.Timestamp:yyyy-MM-dd HH:mm}  |  {a.Action}  |  {a.PassportNumber}");
            }

            if (_audit.Count > Limit)
                sb.AppendLine($"... and {_audit.Count - Limit:N0} older entries.");

            ShowTextWindow("Audit log", sb.ToString());
        }

        /// <summary>Scrollable, selectable text window - MessageBox truncates long content.</summary>
        private void ShowTextWindow(string title, string text)
        {
            using var form = new Form
            {
                Text = title,
                ClientSize = new Size(620, 460),
                StartPosition = FormStartPosition.CenterParent,
                MinimizeBox = false,
                ShowInTaskbar = false
            };

            var box = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Both,
                WordWrap = false,
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 9),
                Text = text
            };

            form.Controls.Add(box);
            form.ShowDialog(this);
        }
    }
}

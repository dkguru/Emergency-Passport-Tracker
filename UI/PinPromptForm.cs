using System;
using System.Drawing;
using System.Windows.Forms;

namespace Emergency_Passport_Tracker.UI
{
    /// <summary>
    /// Masked code entry dialog. Replaces Microsoft.VisualBasic.Interaction.InputBox, which
    /// displayed the code in clear text on screen.
    /// </summary>
    public class PinPromptForm : Form
    {
        private readonly TextBox _txtPin;
        private readonly TextBox? _txtConfirm;

        public string Pin => _txtPin.Text;

        private PinPromptForm(string title, string prompt, bool confirm)
        {
            var lblPrompt = new Label
            {
                AutoSize = false,
                Location = new Point(12, 12),
                Size = new Size(330, confirm ? 34 : 20),
                Text = prompt
            };

            _txtPin = new TextBox
            {
                Location = new Point(12, lblPrompt.Bottom + 6),
                Size = new Size(330, 23),
                UseSystemPasswordChar = true,
                TabIndex = 0
            };

            Controls.Add(lblPrompt);
            Controls.Add(_txtPin);

            int nextTop = _txtPin.Bottom + 8;

            if (confirm)
            {
                var lblConfirm = new Label
                {
                    AutoSize = true,
                    Location = new Point(12, nextTop),
                    Text = "Re-enter the code to confirm:"
                };

                _txtConfirm = new TextBox
                {
                    Location = new Point(12, lblConfirm.Bottom + 4),
                    Size = new Size(330, 23),
                    UseSystemPasswordChar = true,
                    TabIndex = 1
                };

                Controls.Add(lblConfirm);
                Controls.Add(_txtConfirm);
                nextTop = _txtConfirm.Bottom + 12;
            }

            var btnOk = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Location = new Point(186, nextTop),
                Size = new Size(75, 25),
                TabIndex = 2
            };

            var btnCancel = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(267, nextTop),
                Size = new Size(75, 25),
                TabIndex = 3
            };

            btnOk.Click += (_, _) =>
            {
                if (string.IsNullOrEmpty(_txtPin.Text))
                {
                    MessageBox.Show(this, "Please enter a code.", "Emergency Passport Tracker",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    DialogResult = DialogResult.None;
                    return;
                }

                if (_txtConfirm != null && _txtPin.Text != _txtConfirm.Text)
                {
                    MessageBox.Show(this, "The two codes do not match. Please try again.",
                        "Emergency Passport Tracker", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    _txtConfirm.Clear();
                    _txtConfirm.Focus();
                    DialogResult = DialogResult.None;
                }
            };

            Controls.Add(btnOk);
            Controls.Add(btnCancel);

            AcceptButton = btnOk;
            CancelButton = btnCancel;
            ClientSize = new Size(354, nextTop + 47);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterScreen;
            Text = title;
        }

        /// <summary>Asks for an existing code. Returns null if the user cancels.</summary>
        public static string? Ask(string prompt, string title = "Emergency Passport Tracker")
        {
            using var dialog = new PinPromptForm(title, prompt, confirm: false);
            return dialog.ShowDialog() == DialogResult.OK ? dialog.Pin : null;
        }

        /// <summary>Asks for a new code twice. Returns null if the user cancels.</summary>
        public static string? AskNew(string prompt, string title = "Emergency Passport Tracker")
        {
            using var dialog = new PinPromptForm(title, prompt, confirm: true);
            return dialog.ShowDialog() == DialogResult.OK ? dialog.Pin : null;
        }
    }
}

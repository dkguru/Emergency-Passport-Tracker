using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Emergency_Passport_Tracker.Models;
using Emergency_Passport_Tracker.Services;
using Emergency_Passport_Tracker.UI;

namespace Emergency_Passport_Tracker
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (_, e) =>
                MessageBox.Show(
                    "An unexpected error occurred:" + Environment.NewLine + Environment.NewLine + e.Exception.Message,
                    "Emergency Passport Tracker", MessageBoxButtons.OK, MessageBoxIcon.Error);

            DataService dataService;

            try
            {
                dataService = new DataService();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "The data folder could not be opened:" + Environment.NewLine + Environment.NewLine + ex.Message,
                    "Emergency Passport Tracker", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!TryUnlock(dataService, out string pin, out var records, out var audit))
                return;

            Application.Run(new MainForm(dataService, pin, records, audit));
        }

        /// <summary>
        /// Prompts for the code until the data file opens, or the user cancels.
        ///
        /// A wrong code re-prompts. It never falls through to an empty data set, which is what
        /// previously caused the next save to overwrite the real records with nothing.
        /// </summary>
        private static bool TryUnlock(
            DataService dataService,
            out string pin,
            out List<PassportRecord> records,
            out List<AuditEntry> audit)
        {
            pin = string.Empty;
            records = new List<PassportRecord>();
            audit = new List<AuditEntry>();

            // First run: no data file yet, so set the code up front (entered twice).
            if (!dataService.DataFileExists)
            {
                string? newPin = PinPromptForm.AskNew(
                    "No data file was found, so a new one will be created." + Environment.NewLine +
                    "Choose the code that will protect it. There is no way to recover it if it is lost.",
                    "Set access code");

                if (string.IsNullOrEmpty(newPin))
                    return false;

                pin = newPin;
                return true;
            }

            string message = "Enter the access code:";

            while (true)
            {
                string? entered = PinPromptForm.Ask(message, "Emergency Passport Tracker");

                if (entered == null)
                    return false; // cancelled - the data file is left untouched

                try
                {
                    var loaded = dataService.Load(entered);
                    records = loaded.Records;
                    audit = loaded.Audit;
                    pin = entered;
                    return true;
                }
                catch (InvalidPinException)
                {
                    message = "Incorrect code. Please try again:";
                }
                catch (DataFileCorruptException ex)
                {
                    MessageBox.Show(
                        ex.Message + Environment.NewLine + Environment.NewLine +
                        "The data file has not been changed. A recent copy may be available in the " +
                        "Backups folder next to it:" + Environment.NewLine + dataService.DataDirectory,
                        "Emergency Passport Tracker", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "The data file could not be opened:" + Environment.NewLine + Environment.NewLine + ex.Message,
                        "Emergency Passport Tracker", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            }
        }
    }
}

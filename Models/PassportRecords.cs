using System;

namespace Emergency_Passport_Tracker.Models
{
    public class PassportRecord
    {
        public string PassportNumber { get; set; } = string.Empty;
        public string? IssuedTo { get; set; }
        public DateTime? DateIssued { get; set; }
        public string? Notes { get; set; }

        /// <summary>A = in possession, B = issued, C = missing, D = destroyed.</summary>
        public string Status { get; set; } = "A";

        public bool Locked { get; set; }
    }
}

using System;

namespace Emergency_Passport_Tracker.Models
{
    public class AuditEntry
    {
        public DateTime Timestamp { get; set; }
        public string Action { get; set; } = string.Empty;
        public string PassportNumber { get; set; } = string.Empty;
    }
}

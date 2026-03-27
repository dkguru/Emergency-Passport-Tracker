using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmergencyPassportTracker.Models
{
    public class AuditEntry
    {
        public DateTime Timestamp { get; set; }
        public string Action { get; set; }
        public string PassportNumber { get; set; }
    }
}
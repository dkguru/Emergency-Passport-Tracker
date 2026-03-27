using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmergencyPassportTracker.Models
{
    public class PassportRecord
    {
        public string PassportNumber { get; set; }
        public string IssuedTo { get; set; }
        public DateTime? DateIssued { get; set; }
        public string Notes { get; set; }
        public string Status { get; set; } = "A"; // A, B, C, D
        public bool Locked { get; set; } = false;
    }
}
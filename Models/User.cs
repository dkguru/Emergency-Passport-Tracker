using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmergencyPassportTracker.Models
{
    public enum UserRole
    {
        Admin,
        Staff,
        Viewer
    }

    public class User
    {
        public string Username { get; set; }
        public byte[] PinHash { get; set; }
        public byte[] Salt { get; set; }
        public UserRole Role { get; set; }
    }
}
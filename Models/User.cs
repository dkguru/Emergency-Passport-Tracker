using System;

namespace Emergency_Passport_Tracker.Models
{
    public enum UserRole
    {
        Admin,
        Staff,
        Viewer
    }

    public class User
    {
        public string Username { get; set; } = string.Empty;
        public byte[] PinHash { get; set; } = Array.Empty<byte>();
        public byte[] Salt { get; set; } = Array.Empty<byte>();
        public UserRole Role { get; set; }
    }
}

using System;

namespace calendarrrrrrrrrr.Models
{
    // ═══════════════════════════════════════
    //                 ROOM
    // ═══════════════════════════════════════
    public class Room
    {
        public int RoomId { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public string RoomType { get; set; } = string.Empty;       // Standard, Deluxe, Suite, Family
        public int Floor { get; set; }
        public int Capacity { get; set; }
        public decimal PricePerNight { get; set; }
        public string Status { get; set; } = string.Empty;         // Available, Occupied, Cleaning, Maintenance, Reserved
        public string Description { get; set; } = string.Empty;
        public string Amenities { get; set; } = string.Empty;

        public override string ToString() => $"Room {RoomNumber} — {RoomType}";
    }

    // ═══════════════════════════════════════
    //                 GUEST
    // ═══════════════════════════════════════
    public class Guest
    {
        public int GuestId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Nationality { get; set; } = string.Empty;
        public string IdType { get; set; } = string.Empty;         // Passport, Driver's License, National ID
        public string IdNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public string FullName => $"{FirstName} {LastName}";
        public override string ToString() => FullName;
    }

    // ═══════════════════════════════════════
    //              RESERVATION
    // ═══════════════════════════════════════
    public class Reservation
    {
        public int ReservationId { get; set; }
        public int GuestId { get; set; }
        public int RoomId { get; set; }
        public DateTime CheckIn { get; set; }
        public DateTime CheckOut { get; set; }
        public int GuestCount { get; set; }
        public string Status { get; set; } = string.Empty;         // Pending, Confirmed, CheckedIn, CheckedOut, Cancelled
        public decimal TotalAmount { get; set; }
        public string SpecialRequests { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        // Navigation (joined from DB)
        public string GuestName { get; set; } = string.Empty;
        public string RoomNumber { get; set; } = string.Empty;
        public string RoomType { get; set; } = string.Empty;
        public decimal PricePerNight { get; set; }

        public int Nights => (CheckOut - CheckIn).Days;
        public string CheckInStr => CheckIn.ToString("MMM dd, yyyy");
        public string CheckOutStr => CheckOut.ToString("MMM dd, yyyy");
    }

    // ═══════════════════════════════════════
    //               PAYMENT
    // ═══════════════════════════════════════
    public class Payment
    {
        public int PaymentId { get; set; }
        public int ReservationId { get; set; }
        public decimal Amount { get; set; }
        public string Method { get; set; } = string.Empty;         // Cash, Card, GCash, Bank Transfer
        public string Status { get; set; } = string.Empty;         // Paid, Partial, Unpaid
        public DateTime PaidAt { get; set; }
        public string Notes { get; set; } = string.Empty;

        // Navigation
        public string GuestName { get; set; } = string.Empty;
        public string RoomNumber { get; set; } = string.Empty;
    }

    // ═══════════════════════════════════════
    //           HOUSEKEEPING TASK
    // ═══════════════════════════════════════
    public class HousekeepingTask
    {
        public int TaskId { get; set; }
        public int RoomId { get; set; }
        public string AssignedTo { get; set; } = string.Empty;
        public string TaskType { get; set; } = string.Empty;       // Cleaning, Turndown, Maintenance, Inspection
        public string Priority { get; set; } = string.Empty;       // Low, Normal, High, Urgent
        public string Status { get; set; } = string.Empty;         // Pending, InProgress, Done
        public DateTime DueDate { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string Notes { get; set; } = string.Empty;

        // Navigation
        public string RoomNumber { get; set; } = string.Empty;
    }

    // ═══════════════════════════════════════
    //                 USER
    // ═══════════════════════════════════════
    public class User
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;           // Admin, Receptionist, Housekeeping
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

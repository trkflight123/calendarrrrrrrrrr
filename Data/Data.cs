#nullable disable

using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.Sqlite;
using System.IO;
using calendarrrrrrrrrr.Models;

namespace calendarrrrrrrrrr.Data
{
    public class DatabaseService
    {
        private static readonly string DbPath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "HotelYncierto.db");

        private static string ConnectionString =>
            $"Data Source={DbPath};";

        private static bool _initialized;

        // ═══════════════════════════════════════
        //             INITIALIZATION
        // ═══════════════════════════════════════

        public static void Initialize()
        {
            using (var conn = new SqliteConnection(ConnectionString))
            {
                conn.Open();
                if (!TableExists(conn, "Rooms"))
                {
                    CreateTables(conn);
                    SeedData(conn);
                }
            }
            _initialized = true;
        }

        private static void EnsureInitialized()
        {
            if (_initialized)
                return;

            Initialize();
        }

        private static bool TableExists(SqliteConnection conn, string tableName)
        {
            using var cmd = new SqliteCommand(
                "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=@name", conn);
            cmd.Parameters.AddWithValue("@name", tableName);
            return Convert.ToInt64(cmd.ExecuteScalar()) > 0;
        }

        private static void CreateTables(SqliteConnection conn)
        {
            string[] statements =
            {
                @"CREATE TABLE IF NOT EXISTS Rooms (
                    RoomId INTEGER PRIMARY KEY AUTOINCREMENT,
                    RoomNumber TEXT NOT NULL UNIQUE,
                    RoomType TEXT NOT NULL,
                    Floor INTEGER NOT NULL DEFAULT 1,
                    Capacity INTEGER NOT NULL DEFAULT 2,
                    PricePerNight REAL NOT NULL DEFAULT 0,
                    Status TEXT NOT NULL DEFAULT 'Available',
                    Description TEXT,
                    Amenities TEXT);",
                @"CREATE TABLE IF NOT EXISTS Guests (
                    GuestId INTEGER PRIMARY KEY AUTOINCREMENT,
                    FirstName TEXT NOT NULL,
                    LastName TEXT NOT NULL,
                    Email TEXT,
                    Phone TEXT,
                    Nationality TEXT,
                    IdType TEXT,
                    IdNumber TEXT,
                    Address TEXT,
                    CreatedAt TEXT NOT NULL);",
                @"CREATE TABLE IF NOT EXISTS Reservations (
                    ReservationId INTEGER PRIMARY KEY AUTOINCREMENT,
                    GuestId INTEGER NOT NULL,
                    RoomId INTEGER NOT NULL,
                    CheckIn TEXT NOT NULL,
                    CheckOut TEXT NOT NULL,
                    GuestCount INTEGER NOT NULL DEFAULT 1,
                    Status TEXT NOT NULL DEFAULT 'Pending',
                    TotalAmount REAL NOT NULL DEFAULT 0,
                    SpecialRequests TEXT,
                    CreatedAt TEXT NOT NULL,
                    FOREIGN KEY (GuestId) REFERENCES Guests(GuestId),
                    FOREIGN KEY (RoomId) REFERENCES Rooms(RoomId));",
                @"CREATE TABLE IF NOT EXISTS Payments (
                    PaymentId INTEGER PRIMARY KEY AUTOINCREMENT,
                    ReservationId INTEGER NOT NULL,
                    Amount REAL NOT NULL,
                    Method TEXT NOT NULL DEFAULT 'Cash',
                    Status TEXT NOT NULL DEFAULT 'Paid',
                    PaidAt TEXT NOT NULL,
                    Notes TEXT,
                    FOREIGN KEY (ReservationId) REFERENCES Reservations(ReservationId));",
                @"CREATE TABLE IF NOT EXISTS HousekeepingTasks (
                    TaskId INTEGER PRIMARY KEY AUTOINCREMENT,
                    RoomId INTEGER NOT NULL,
                    AssignedTo TEXT,
                    TaskType TEXT NOT NULL DEFAULT 'Cleaning',
                    Priority TEXT NOT NULL DEFAULT 'Normal',
                    Status TEXT NOT NULL DEFAULT 'Pending',
                    DueDate TEXT NOT NULL,
                    CompletedAt TEXT,
                    Notes TEXT,
                    FOREIGN KEY (RoomId) REFERENCES Rooms(RoomId));",
                @"CREATE TABLE IF NOT EXISTS Users (
                    UserId INTEGER PRIMARY KEY AUTOINCREMENT,
                    Username TEXT NOT NULL UNIQUE,
                    PasswordHash TEXT NOT NULL,
                    FullName TEXT NOT NULL,
                    Role TEXT NOT NULL DEFAULT 'Receptionist',
                    IsActive INTEGER NOT NULL DEFAULT 1,
                    CreatedAt TEXT NOT NULL);"
            };

            foreach (var sql in statements)
            {
                using var cmd = new SqliteCommand(sql, conn);
                cmd.ExecuteNonQuery();
            }
        }

        private static void SeedData(SqliteConnection conn)
        {
            // FIX: Changed SQLiteCommand to SqliteCommand
            using (var cmd = new SqliteCommand("SELECT COUNT(*) FROM Rooms", conn))
            {
                long count = (long)cmd.ExecuteScalar();
                if (count > 0) return;
            }

            var seed = @"
                INSERT INTO Rooms (RoomNumber, RoomType, Floor, Capacity, PricePerNight, Status, Description, Amenities)
                VALUES
                ('101', 'Standard', 1, 2, 1500, 'Available', 'Cozy standard room with garden view', 'WiFi, AC, TV, Hot Shower'),
                ('102', 'Standard', 1, 2, 1500, 'Available', 'Cozy standard room with garden view', 'WiFi, AC, TV, Hot Shower'),
                ('103', 'Standard', 1, 2, 1500, 'Cleaning', 'Cozy standard room with garden view', 'WiFi, AC, TV, Hot Shower'),
                ('104', 'Deluxe', 1, 3, 2500, 'Available', 'Spacious deluxe room with balcony', 'WiFi, AC, TV, Mini Bar, Hot Shower, Balcony'),
                ('105', 'Deluxe', 1, 3, 2500, 'Maintenance', 'Spacious deluxe room with balcony', 'WiFi, AC, TV, Mini Bar, Hot Shower, Balcony'),
                ('201', 'Standard', 2, 2, 1500, 'Available', 'Standard room on 2nd floor', 'WiFi, AC, TV, Hot Shower'),
                ('202', 'Standard', 2, 2, 1500, 'Available', 'Standard room on 2nd floor', 'WiFi, AC, TV, Hot Shower'),
                ('203', 'Deluxe', 2, 3, 2500, 'Available', 'Deluxe room with mountain view', 'WiFi, AC, TV, Mini Bar, Hot Shower, Balcony'),
                ('204', 'Deluxe', 2, 3, 2500, 'Occupied', 'Deluxe room with mountain view', 'WiFi, AC, TV, Mini Bar, Hot Shower, Balcony'),
                ('205', 'Suite', 2, 4, 4500, 'Available', 'Luxury suite with living area', 'WiFi, AC, TV, Mini Bar, Hot Shower, Living Room, Bathtub'),
                ('301', 'Suite', 3, 4, 4500, 'Available', 'Premium suite with panoramic view', 'WiFi, AC, TV, Mini Bar, Hot Shower, Living Room, Bathtub, Jacuzzi'),
                ('302', 'Family', 3, 6, 3500, 'Available', 'Spacious family room', 'WiFi, AC, TV, Hot Shower, Extra Beds'),
                ('303', 'Family', 3, 6, 3500, 'Available', 'Spacious family room', 'WiFi, AC, TV, Hot Shower, Extra Beds');

                INSERT INTO Users (Username, PasswordHash, FullName, Role, IsActive, CreatedAt)
                VALUES ('admin', 'admin123', 'Administrator', 'Admin', 1, datetime('now'));

                INSERT INTO Guests (FirstName, LastName, Email, Phone, Nationality, IdType, IdNumber, Address, CreatedAt)
                VALUES
                ('Maria', 'Santos', 'maria@email.com', '09171234567', 'Filipino', 'National ID', 'PH-001-001', 'Davao City', datetime('now')),
                ('Juan', 'Dela Cruz', 'juan@email.com', '09181234567', 'Filipino', 'Passport', 'P123456A', 'Manila', datetime('now')),
                ('Ana', 'Reyes', 'ana@email.com', '09191234567', 'Filipino', 'Driver''s License', 'DL-123456', 'Cebu City', datetime('now'));
            ";

            // FIX: Changed SQLiteCommand to SqliteCommand
            using (var cmd = new SqliteCommand(seed, conn))
                cmd.ExecuteNonQuery();
        }

        // ═══════════════════════════════════════
        //               ROOMS CRUD
        // ═══════════════════════════════════════

        public static List<Room> GetAllRooms()
        {
            EnsureInitialized();
            var list = new List<Room>();
            using (var conn = new SqliteConnection(ConnectionString))
            {
                conn.Open();
                // FIX: Changed SQLiteCommand to SqliteCommand
                using (var cmd = new SqliteCommand("SELECT * FROM Rooms ORDER BY RoomNumber", conn))
                using (var reader = cmd.ExecuteReader())
                    while (reader.Read())
                        list.Add(MapRoom(reader));
            }
            return list;
        }

        public static List<Room> GetAvailableRooms(DateTime checkIn, DateTime checkOut)
        {
            var list = new List<Room>();
            using (var conn = new SqliteConnection(ConnectionString))
            {
                conn.Open();
                var sql = @"
                    SELECT * FROM Rooms
                    WHERE Status NOT IN ('Maintenance')
                    AND RoomId NOT IN (
                        SELECT RoomId FROM Reservations
                        WHERE Status NOT IN ('Cancelled', 'CheckedOut')
                        AND NOT (CheckOut <= @CheckIn OR CheckIn >= @CheckOut)
                    )
                    ORDER BY RoomNumber";
                // FIX: Changed SQLiteCommand to SqliteCommand
                using (var cmd = new SqliteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@CheckIn", checkIn.ToString("yyyy-MM-dd"));
                    cmd.Parameters.AddWithValue("@CheckOut", checkOut.ToString("yyyy-MM-dd"));
                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                            list.Add(MapRoom(reader));
                }
            }
            return list;
        }

        public static void AddRoom(Room r)
        {
            using (var conn = new SqliteConnection(ConnectionString))
            {
                conn.Open();
                var sql = @"INSERT INTO Rooms (RoomNumber, RoomType, Floor, Capacity, PricePerNight, Status, Description, Amenities)
                            VALUES (@Num, @Type, @Floor, @Cap, @Price, @Status, @Desc, @Amen)";
                // FIX: Changed SQLiteCommand to SqliteCommand
                using (var cmd = new SqliteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Num", r.RoomNumber);
                    cmd.Parameters.AddWithValue("@Type", r.RoomType);
                    cmd.Parameters.AddWithValue("@Floor", r.Floor);
                    cmd.Parameters.AddWithValue("@Cap", r.Capacity);
                    cmd.Parameters.AddWithValue("@Price", r.PricePerNight);
                    cmd.Parameters.AddWithValue("@Status", r.Status);
                    cmd.Parameters.AddWithValue("@Desc", r.Description ?? "");
                    cmd.Parameters.AddWithValue("@Amen", r.Amenities ?? "");
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static void UpdateRoom(Room r)
        {
            using (var conn = new SqliteConnection(ConnectionString))
            {
                conn.Open();
                var sql = @"UPDATE Rooms SET RoomNumber=@Num, RoomType=@Type, Floor=@Floor,
                            Capacity=@Cap, PricePerNight=@Price, Status=@Status,
                            Description=@Desc, Amenities=@Amen WHERE RoomId=@Id";
                // FIX: Changed SQLiteCommand to SqliteCommand
                using (var cmd = new SqliteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Num", r.RoomNumber);
                    cmd.Parameters.AddWithValue("@Type", r.RoomType);
                    cmd.Parameters.AddWithValue("@Floor", r.Floor);
                    cmd.Parameters.AddWithValue("@Cap", r.Capacity);
                    cmd.Parameters.AddWithValue("@Price", r.PricePerNight);
                    cmd.Parameters.AddWithValue("@Status", r.Status);
                    cmd.Parameters.AddWithValue("@Desc", r.Description ?? "");
                    cmd.Parameters.AddWithValue("@Amen", r.Amenities ?? "");
                    cmd.Parameters.AddWithValue("@Id", r.RoomId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static void UpdateRoomStatus(int roomId, string status)
        {
            using (var conn = new SqliteConnection(ConnectionString))
            {
                conn.Open();
                // FIX: Changed SQLiteCommand to SqliteCommand
                using (var cmd = new SqliteCommand("UPDATE Rooms SET Status=@Status WHERE RoomId=@Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Status", status);
                    cmd.Parameters.AddWithValue("@Id", roomId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static void DeleteRoom(int roomId)
        {
            using (var conn = new SqliteConnection(ConnectionString))
            {
                conn.Open();
                // FIX: Changed SQLiteCommand to SqliteCommand
                using (var cmd = new SqliteCommand("DELETE FROM Rooms WHERE RoomId=@Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", roomId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // ═══════════════════════════════════════
        //              GUESTS CRUD
        // ═══════════════════════════════════════

        public static List<Guest> GetAllGuests()
        {
            var list = new List<Guest>();
            using (var conn = new SqliteConnection(ConnectionString))
            {
                conn.Open();
                // FIX: Changed SQLiteCommand to SqliteCommand
                using (var cmd = new SqliteCommand("SELECT * FROM Guests ORDER BY LastName, FirstName", conn))
                using (var reader = cmd.ExecuteReader())
                    while (reader.Read())
                        list.Add(MapGuest(reader));
            }
            return list;
        }

        public static int AddGuest(Guest g)
        {
            using (var conn = new SqliteConnection(ConnectionString))
            {
                conn.Open();
                var sql = @"INSERT INTO Guests (FirstName, LastName, Email, Phone, Nationality, IdType, IdNumber, Address, CreatedAt)
                            VALUES (@Fn, @Ln, @Em, @Ph, @Nat, @IdT, @IdN, @Addr, @Cr);
                            SELECT last_insert_rowid();";
                // FIX: Changed SQLiteCommand to SqliteCommand
                using (var cmd = new SqliteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Fn", g.FirstName);
                    cmd.Parameters.AddWithValue("@Ln", g.LastName);
                    cmd.Parameters.AddWithValue("@Em", g.Email ?? "");
                    cmd.Parameters.AddWithValue("@Ph", g.Phone ?? "");
                    cmd.Parameters.AddWithValue("@Nat", g.Nationality ?? "");
                    cmd.Parameters.AddWithValue("@IdT", g.IdType ?? "");
                    cmd.Parameters.AddWithValue("@IdN", g.IdNumber ?? "");
                    cmd.Parameters.AddWithValue("@Addr", g.Address ?? "");
                    cmd.Parameters.AddWithValue("@Cr", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }

        public static void UpdateGuest(Guest g)
        {
            using (var conn = new SqliteConnection(ConnectionString))
            {
                conn.Open();
                var sql = @"UPDATE Guests SET FirstName=@Fn, LastName=@Ln, Email=@Em, Phone=@Ph,
                            Nationality=@Nat, IdType=@IdT, IdNumber=@IdN, Address=@Addr
                            WHERE GuestId=@Id";
                // FIX: Changed SQLiteCommand to SqliteCommand
                using (var cmd = new SqliteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Fn", g.FirstName);
                    cmd.Parameters.AddWithValue("@Ln", g.LastName);
                    cmd.Parameters.AddWithValue("@Em", g.Email ?? "");
                    cmd.Parameters.AddWithValue("@Ph", g.Phone ?? "");
                    cmd.Parameters.AddWithValue("@Nat", g.Nationality ?? "");
                    cmd.Parameters.AddWithValue("@IdT", g.IdType ?? "");
                    cmd.Parameters.AddWithValue("@IdN", g.IdNumber ?? "");
                    cmd.Parameters.AddWithValue("@Addr", g.Address ?? "");
                    cmd.Parameters.AddWithValue("@Id", g.GuestId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static void DeleteGuest(int guestId)
        {
            using (var conn = new SqliteConnection(ConnectionString))
            {
                conn.Open();
                // FIX: Changed SQLiteCommand to SqliteCommand
                using (var cmd = new SqliteCommand("DELETE FROM Guests WHERE GuestId=@Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", guestId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // ═══════════════════════════════════════
        //           RESERVATIONS CRUD
        // ═══════════════════════════════════════

        public static List<Reservation> GetAllReservations()
        {
            EnsureInitialized();
            var list = new List<Reservation>();
            using (var conn = new SqliteConnection(ConnectionString))
            {
                conn.Open();
                var sql = @"
                    SELECT r.*, g.FirstName || ' ' || g.LastName AS GuestName,
                           rm.RoomNumber, rm.RoomType, rm.PricePerNight
                    FROM Reservations r
                    JOIN Guests g ON r.GuestId = g.GuestId
                    JOIN Rooms rm ON r.RoomId = rm.RoomId
                    ORDER BY r.CheckIn DESC";
                // FIX: Changed SQLiteCommand to SqliteCommand
                using (var cmd = new SqliteCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                    while (reader.Read())
                        list.Add(MapReservation(reader));
            }
            return list;
        }

        public static List<Reservation> GetReservationsForDate(DateTime date)
        {
            var list = new List<Reservation>();
            using (var conn = new SqliteConnection(ConnectionString))
            {
                conn.Open();
                var sql = @"
                    SELECT r.*, g.FirstName || ' ' || g.LastName AS GuestName,
                           rm.RoomNumber, rm.RoomType, rm.PricePerNight
                    FROM Reservations r
                    JOIN Guests g ON r.GuestId = g.GuestId
                    JOIN Rooms rm ON r.RoomId = rm.RoomId
                    WHERE r.Status NOT IN ('Cancelled')
                    AND date(r.CheckIn) <= @Date AND date(r.CheckOut) > @Date
                    ORDER BY rm.RoomNumber";
                // FIX: Changed SQLiteCommand to SqliteCommand
                using (var cmd = new SqliteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Date", date.ToString("yyyy-MM-dd"));
                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                            list.Add(MapReservation(reader));
                }
            }
            return list;
        }

        public static List<Reservation> GetReservationsForMonth(int year, int month)
        {
            var list = new List<Reservation>();
            using (var conn = new SqliteConnection(ConnectionString))
            {
                conn.Open();
                var start = new DateTime(year, month, 1).ToString("yyyy-MM-dd");
                var end = new DateTime(year, month, 1).AddMonths(1).ToString("yyyy-MM-dd");
                var sql = @"
                    SELECT r.*, g.FirstName || ' ' || g.LastName AS GuestName,
                           rm.RoomNumber, rm.RoomType, rm.PricePerNight
                    FROM Reservations r
                    JOIN Guests g ON r.GuestId = g.GuestId
                    JOIN Rooms rm ON r.RoomId = rm.RoomId
                    WHERE r.Status NOT IN ('Cancelled')
                    AND NOT (date(r.CheckOut) <= @Start OR date(r.CheckIn) >= @End)
                    ORDER BY r.CheckIn";
                // FIX: Changed SQLiteCommand to SqliteCommand
                using (var cmd = new SqliteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Start", start);
                    cmd.Parameters.AddWithValue("@End", end);
                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                            list.Add(MapReservation(reader));
                }
            }
            return list;
        }

        public static bool HasConflict(int roomId, DateTime checkIn, DateTime checkOut, int excludeReservationId = 0)
        {
            using (var conn = new SqliteConnection(ConnectionString))
            {
                conn.Open();
                var sql = @"SELECT COUNT(*) FROM Reservations
                            WHERE RoomId = @RoomId
                            AND ReservationId != @ExcludeId
                            AND Status NOT IN ('Cancelled', 'CheckedOut')
                            AND NOT (CheckOut <= @CheckIn OR CheckIn >= @CheckOut)";
                // FIX: Changed SQLiteCommand to SqliteCommand
                using (var cmd = new SqliteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@RoomId", roomId);
                    cmd.Parameters.AddWithValue("@ExcludeId", excludeReservationId);
                    cmd.Parameters.AddWithValue("@CheckIn", checkIn.ToString("yyyy-MM-dd"));
                    cmd.Parameters.AddWithValue("@CheckOut", checkOut.ToString("yyyy-MM-dd"));
                    return (long)cmd.ExecuteScalar() > 0;
                }
            }
        }

        public static int AddReservation(Reservation res)
        {
            using (var conn = new SqliteConnection(ConnectionString))
            {
                conn.Open();
                var sql = @"INSERT INTO Reservations (GuestId, RoomId, CheckIn, CheckOut, GuestCount, Status, TotalAmount, SpecialRequests, CreatedAt)
                            VALUES (@Gid, @Rid, @Ci, @Co, @Gc, @St, @Ta, @Sr, @Cr);
                            SELECT last_insert_rowid();";
                // FIX: Changed SQLiteCommand to SqliteCommand
                using (var cmd = new SqliteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Gid", res.GuestId);
                    cmd.Parameters.AddWithValue("@Rid", res.RoomId);
                    cmd.Parameters.AddWithValue("@Ci", res.CheckIn.ToString("yyyy-MM-dd"));
                    cmd.Parameters.AddWithValue("@Co", res.CheckOut.ToString("yyyy-MM-dd"));
                    cmd.Parameters.AddWithValue("@Gc", res.GuestCount);
                    cmd.Parameters.AddWithValue("@St", res.Status ?? "Confirmed");
                    cmd.Parameters.AddWithValue("@Ta", res.TotalAmount);
                    cmd.Parameters.AddWithValue("@Sr", res.SpecialRequests ?? "");
                    cmd.Parameters.AddWithValue("@Cr", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }

        public static void UpdateReservation(Reservation res)
        {
            using (var conn = new SqliteConnection(ConnectionString))
            {
                conn.Open();
                var sql = @"UPDATE Reservations SET GuestId=@Gid, RoomId=@Rid, CheckIn=@Ci, CheckOut=@Co,
                            GuestCount=@Gc, Status=@St, TotalAmount=@Ta, SpecialRequests=@Sr
                            WHERE ReservationId=@Id";
                // FIX: Changed SQLiteCommand to SqliteCommand
                using (var cmd = new SqliteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Gid", res.GuestId);
                    cmd.Parameters.AddWithValue("@Rid", res.RoomId);
                    cmd.Parameters.AddWithValue("@Ci", res.CheckIn.ToString("yyyy-MM-dd"));
                    cmd.Parameters.AddWithValue("@Co", res.CheckOut.ToString("yyyy-MM-dd"));
                    cmd.Parameters.AddWithValue("@Gc", res.GuestCount);
                    cmd.Parameters.AddWithValue("@St", res.Status);
                    cmd.Parameters.AddWithValue("@Ta", res.TotalAmount);
                    cmd.Parameters.AddWithValue("@Sr", res.SpecialRequests ?? "");
                    cmd.Parameters.AddWithValue("@Id", res.ReservationId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static void UpdateReservationStatus(int reservationId, string status)
        {
            using (var conn = new SqliteConnection(ConnectionString))
            {
                conn.Open();
                // FIX: Changed SQLiteCommand to SqliteCommand
                using (var cmd = new SqliteCommand("UPDATE Reservations SET Status=@St WHERE ReservationId=@Id", conn))
                {
                    cmd.Parameters.AddWithValue("@St", status);
                    cmd.Parameters.AddWithValue("@Id", reservationId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static void DeleteReservation(int reservationId)
        {
            using (var conn = new SqliteConnection(ConnectionString))
            {
                conn.Open();
                // FIX: Changed SQLiteCommand to SqliteCommand
                using (var cmd = new SqliteCommand("DELETE FROM Reservations WHERE ReservationId=@Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", reservationId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // ═══════════════════════════════════════
        //             PAYMENTS CRUD
        // ═══════════════════════════════════════

        public static List<Payment> GetAllPayments()
        {
            var list = new List<Payment>();
            using (var conn = new SqliteConnection(ConnectionString))
            {
                conn.Open();
                var sql = @"
                    SELECT p.*, g.FirstName || ' ' || g.LastName AS GuestName, rm.RoomNumber
                    FROM Payments p
                    JOIN Reservations r ON p.ReservationId = r.ReservationId
                    JOIN Guests g ON r.GuestId = g.GuestId
                    JOIN Rooms rm ON r.RoomId = rm.RoomId
                    ORDER BY p.PaidAt DESC";
                // FIX: Changed SQLiteCommand to SqliteCommand
                using (var cmd = new SqliteCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                    while (reader.Read())
                        list.Add(MapPayment(reader));
            }
            return list;
        }

        public static List<Payment> GetPaymentsForReservation(int reservationId)
        {
            var list = new List<Payment>();
            using (var conn = new SqliteConnection(ConnectionString))
            {
                conn.Open();
                // FIX: Changed SQLiteCommand to SqliteCommand
                using (var cmd = new SqliteCommand("SELECT * FROM Payments WHERE ReservationId=@Id ORDER BY PaidAt DESC", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", reservationId);
                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                            list.Add(MapPayment(reader));
                }
            }
            return list;
        }

        public static void AddPayment(Payment p)
        {
            using (var conn = new SqliteConnection(ConnectionString))
            {
                conn.Open();
                var sql = @"INSERT INTO Payments (ReservationId, Amount, Method, Status, PaidAt, Notes)
                            VALUES (@Rid, @Am, @Met, @St, @Pa, @No)";
                // FIX: Changed SQLiteCommand to SqliteCommand
                using (var cmd = new SqliteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Rid", p.ReservationId);
                    cmd.Parameters.AddWithValue("@Am", p.Amount);
                    cmd.Parameters.AddWithValue("@Met", p.Method);
                    cmd.Parameters.AddWithValue("@St", p.Status);
                    cmd.Parameters.AddWithValue("@Pa", p.PaidAt.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("@No", p.Notes ?? "");
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // ═══════════════════════════════════════
        //          HOUSEKEEPING CRUD
        // ═══════════════════════════════════════

        public static List<HousekeepingTask> GetAllTasks()
        {
            var list = new List<HousekeepingTask>();
            using (var conn = new SqliteConnection(ConnectionString))
            {
                conn.Open();
                var sql = @"
                    SELECT h.*, r.RoomNumber FROM HousekeepingTasks h
                    JOIN Rooms r ON h.RoomId = r.RoomId
                    ORDER BY h.DueDate, h.Priority DESC";
                // FIX: Changed SQLiteCommand to SqliteCommand
                using (var cmd = new SqliteCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                    while (reader.Read())
                        list.Add(MapTask(reader));
            }
            return list;
        }

        public static void AddTask(HousekeepingTask t)
        {
            using (var conn = new SqliteConnection(ConnectionString))
            {
                conn.Open();
                var sql = @"INSERT INTO HousekeepingTasks (RoomId, AssignedTo, TaskType, Priority, Status, DueDate, Notes)
                            VALUES (@Rid, @Asgn, @Type, @Pri, @St, @Due, @No)";
                // FIX: Changed SQLiteCommand to SqliteCommand
                using (var cmd = new SqliteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Rid", t.RoomId);
                    cmd.Parameters.AddWithValue("@Asgn", t.AssignedTo ?? "");
                    cmd.Parameters.AddWithValue("@Type", t.TaskType);
                    cmd.Parameters.AddWithValue("@Pri", t.Priority);
                    cmd.Parameters.AddWithValue("@St", t.Status);
                    cmd.Parameters.AddWithValue("@Due", t.DueDate.ToString("yyyy-MM-dd"));
                    cmd.Parameters.AddWithValue("@No", t.Notes ?? "");
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static void UpdateTaskStatus(int taskId, string status)
        {
            using (var conn = new SqliteConnection(ConnectionString))
            {
                conn.Open();
                var completedAt = status == "Done" ? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") : null;
                // FIX: Changed SQLiteCommand to SqliteCommand
                using (var cmd = new SqliteCommand("UPDATE HousekeepingTasks SET Status=@St, CompletedAt=@Ca WHERE TaskId=@Id", conn))
                {
                    cmd.Parameters.AddWithValue("@St", status);
                    cmd.Parameters.AddWithValue("@Ca", (object)completedAt ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Id", taskId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static void DeleteTask(int taskId)
        {
            using (var conn = new SqliteConnection(ConnectionString))
            {
                conn.Open();
                // FIX: Changed SQLiteCommand to SqliteCommand
                using (var cmd = new SqliteCommand("DELETE FROM HousekeepingTasks WHERE TaskId=@Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", taskId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // ═══════════════════════════════════════
        //               REPORTS DATA
        // ═══════════════════════════════════════

        public static decimal GetTotalRevenueForMonth(int year, int month)
        {
            using (var conn = new SqliteConnection(ConnectionString))
            {
                conn.Open();
                var start = new DateTime(year, month, 1).ToString("yyyy-MM-dd");
                var end = new DateTime(year, month, 1).AddMonths(1).ToString("yyyy-MM-dd");
                // FIX: Changed SQLiteCommand to SqliteCommand
                using (var cmd = new SqliteCommand(
                    "SELECT COALESCE(SUM(Amount),0) FROM Payments WHERE date(PaidAt) >= @St AND date(PaidAt) < @En", conn))
                {
                    cmd.Parameters.AddWithValue("@St", start);
                    cmd.Parameters.AddWithValue("@En", end);
                    return Convert.ToDecimal(cmd.ExecuteScalar());
                }
            }
        }

        public static int GetOccupiedRoomsCount()
        {
            using (var conn = new SqliteConnection(ConnectionString))
            {
                conn.Open();
                // FIX: Changed SQLiteCommand to SqliteCommand
                using (var cmd = new SqliteCommand(
                    "SELECT COUNT(*) FROM Rooms WHERE Status IN ('Occupied', 'Reserved')", conn))
                    return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        // FIX: Completed the trailing truncated method block
        public static int GetTotalRoomsCount()
        {
            using (var conn = new SqliteConnection(ConnectionString))
            {
                conn.Open();
                using (var cmd = new SqliteCommand("SELECT COUNT(*) FROM Rooms", conn))
                    return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        // ═══════════════════════════════════════
        //           MAPPING HELPER METHODS
        // ═══════════════════════════════════════

        private static Room MapRoom(SqliteDataReader r) => new Room
        {
            RoomId = Convert.ToInt32(r["RoomId"]),
            RoomNumber = r["RoomNumber"].ToString(),
            RoomType = r["RoomType"].ToString(),
            Floor = Convert.ToInt32(r["Floor"]),
            Capacity = Convert.ToInt32(r["Capacity"]),
            PricePerNight = Convert.ToDecimal(r["PricePerNight"]),
            Status = r["Status"].ToString(),
            Description = r["Description"]?.ToString(),
            Amenities = r["Amenities"]?.ToString()
        };

        private static Guest MapGuest(SqliteDataReader r) => new Guest
        {
            GuestId = Convert.ToInt32(r["GuestId"]),
            FirstName = r["FirstName"].ToString(),
            LastName = r["LastName"].ToString(),
            Email = r["Email"]?.ToString(),
            Phone = r["Phone"]?.ToString(),
            Nationality = r["Nationality"]?.ToString(),
            IdType = r["IdType"]?.ToString(),
            IdNumber = r["IdNumber"]?.ToString(),
            Address = r["Address"]?.ToString()
        };

        private static Reservation MapReservation(SqliteDataReader r) => new Reservation
        {
            ReservationId = Convert.ToInt32(r["ReservationId"]),
            GuestId = Convert.ToInt32(r["GuestId"]),
            RoomId = Convert.ToInt32(r["RoomId"]),
            CheckIn = DateTime.Parse(r["CheckIn"].ToString()),
            CheckOut = DateTime.Parse(r["CheckOut"].ToString()),
            GuestCount = Convert.ToInt32(r["GuestCount"]),
            Status = r["Status"].ToString(),
            TotalAmount = Convert.ToDecimal(r["TotalAmount"]),
            SpecialRequests = r["SpecialRequests"]?.ToString(),
            GuestName = r.HasColumn("GuestName") ? r["GuestName"].ToString() : "",
            RoomNumber = r.HasColumn("RoomNumber") ? r["RoomNumber"].ToString() : ""
        };

        private static Payment MapPayment(SqliteDataReader r) => new Payment
        {
            PaymentId = Convert.ToInt32(r["PaymentId"]),
            ReservationId = Convert.ToInt32(r["ReservationId"]),
            Amount = Convert.ToDecimal(r["Amount"]),
            Method = r["Method"].ToString(),
            Status = r["Status"].ToString(),
            PaidAt = DateTime.Parse(r["PaidAt"].ToString()),
            Notes = r["Notes"]?.ToString(),
            GuestName = r.HasColumn("GuestName") ? r["GuestName"].ToString() : "",
            RoomNumber = r.HasColumn("RoomNumber") ? r["RoomNumber"].ToString() : ""
        };

        private static HousekeepingTask MapTask(SqliteDataReader r) => new HousekeepingTask
        {
            TaskId = Convert.ToInt32(r["TaskId"]),
            RoomId = Convert.ToInt32(r["RoomId"]),
            AssignedTo = r["AssignedTo"]?.ToString(),
            TaskType = r["TaskType"].ToString(),
            Priority = r["Priority"].ToString(),
            Status = r["Status"].ToString(),
            DueDate = DateTime.Parse(r["DueDate"].ToString()),
            CompletedAt = r["CompletedAt"] != DBNull.Value ? DateTime.Parse(r["CompletedAt"].ToString()) : (DateTime?)null,
            Notes = r["Notes"]?.ToString(),
            RoomNumber = r.HasColumn("RoomNumber") ? r["RoomNumber"].ToString() : ""
        };
    }

    // Small reader extension helper to safely parse custom JOIN properties
    public static class SqliteDataReaderExtensions
    {
        public static bool HasColumn(this SqliteDataReader reader, string columnName)
        {
            for (int i = 0; i < reader.FieldCount; i++)
            {
                if (reader.GetName(i).Equals(columnName, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }
    }
}
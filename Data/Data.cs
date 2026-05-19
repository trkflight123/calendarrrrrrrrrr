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
                EnsureDayOverrideTable(conn);
            }
            _initialized = true;
        }

        private static void EnsureDayOverrideTable(SqliteConnection conn)
        {
            using var cmd = new SqliteCommand(
                @"CREATE TABLE IF NOT EXISTS DayAvailabilityOverrides (
                    OverrideId INTEGER PRIMARY KEY AUTOINCREMENT,
                    OverrideDate TEXT NOT NULL,
                    RoomTypeFilter TEXT NOT NULL DEFAULT 'All',
                    AvailableCount INTEGER NOT NULL,
                    OccupiedCount INTEGER,
                    UNIQUE(OverrideDate, RoomTypeFilter));", conn);
            cmd.ExecuteNonQuery();
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
                    Amenities TEXT,
                    PhotoPath TEXT);",
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
                @"CREATE TABLE IF NOT EXISTS FoundItems (
                    FoundItemId INTEGER PRIMARY KEY AUTOINCREMENT,
                    RoomNumber TEXT NOT NULL,
                    GuestName TEXT NOT NULL,
                    ItemName TEXT NOT NULL,
                    Status TEXT NOT NULL DEFAULT 'Unclaimed',
                    CreatedAt TEXT NOT NULL);",
                @"CREATE TABLE IF NOT EXISTS HotelEvents (
                    EventId INTEGER PRIMARY KEY AUTOINCREMENT,
                    EventDate TEXT NOT NULL,
                    EventTime TEXT NOT NULL,
                    EventName TEXT NOT NULL,
                    Location TEXT NOT NULL DEFAULT 'Hotel Yncierto',
                    CreatedAt TEXT NOT NULL);",
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
                    CreatedAt TEXT NOT NULL);",
                @"CREATE TABLE IF NOT EXISTS DayAvailabilityOverrides (
                    OverrideId INTEGER PRIMARY KEY AUTOINCREMENT,
                    OverrideDate TEXT NOT NULL,
                    RoomTypeFilter TEXT NOT NULL DEFAULT 'All',
                    AvailableCount INTEGER NOT NULL,
                    OccupiedCount INTEGER,
                    UNIQUE(OverrideDate, RoomTypeFilter));"
            };

            foreach (var sql in statements)
            {
                using var cmd = new SqliteCommand(sql, conn);
                cmd.ExecuteNonQuery();
            }
        }

        private static void SeedData(SqliteConnection conn)
        {
            
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

                var sql = @"INSERT INTO Rooms 
                    (RoomNumber, RoomType, Floor, Capacity, PricePerNight, Status, Description, Amenities, PhotoPath)
                    VALUES 
                    (@Num, @Type, @Floor, @Cap, @Price, @Status, @Desc, @Amen, @Photo)";

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
                    cmd.Parameters.AddWithValue("@Photo", r.PhotoPath ?? "");
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

        public static bool TryDeleteRoom(int roomId, out string errorMessage)
        {
            EnsureInitialized();
            errorMessage = string.Empty;

            try
            {
                using (var conn = new SqliteConnection(ConnectionString))
                {
                    conn.Open();
                    using var transaction = conn.BeginTransaction();

                    using (var activeCmd = new SqliteCommand(
                        @"SELECT COUNT(*) FROM Reservations
                          WHERE RoomId = @Id AND Status NOT IN ('Cancelled', 'CheckedOut')
                            AND date(CheckOut) > date('now')", conn, transaction))
                    {
                        activeCmd.Parameters.AddWithValue("@Id", roomId);
                        var activeCount = Convert.ToInt32(activeCmd.ExecuteScalar());
                        if (activeCount > 0)
                        {
                            errorMessage = "This room still has active or upcoming bookings. Clear or cancel them first.";
                            return false;
                        }
                    }

                    string[] cleanupStatements =
                    {
                        @"DELETE FROM Payments WHERE ReservationId IN
                          (SELECT ReservationId FROM Reservations WHERE RoomId = @Id)",
                        "DELETE FROM Reservations WHERE RoomId = @Id",
                        "DELETE FROM HousekeepingTasks WHERE RoomId = @Id",
                        "DELETE FROM Rooms WHERE RoomId = @Id"
                    };

                    foreach (var sql in cleanupStatements)
                    {
                        using var cmd = new SqliteCommand(sql, conn, transaction);
                        cmd.Parameters.AddWithValue("@Id", roomId);
                        cmd.ExecuteNonQuery();
                    }

                    transaction.Commit();
                }

                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
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
                            SELECT r.*, 
                                   g.FirstName || ' ' || g.LastName AS GuestName,
                                   g.Phone,
                                   g.Email,
                                   g.Address,
                                   rm.RoomNumber
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
        //        DAY AVAILABILITY OVERRIDES
        // ═══════════════════════════════════════

        public static Dictionary<DateTime, DayAvailabilityOverride> GetDayOverridesForMonth(
            int year, int month, string roomTypeFilter)
        {
            EnsureInitialized();
            var result = new Dictionary<DateTime, DayAvailabilityOverride>();
            var start = new DateTime(year, month, 1);
            var end = start.AddMonths(1);

            using (var conn = new SqliteConnection(ConnectionString))
            {
                conn.Open();
                var sql = @"SELECT * FROM DayAvailabilityOverrides
                            WHERE RoomTypeFilter = @Filter
                              AND date(OverrideDate) >= date(@Start)
                              AND date(OverrideDate) < date(@End)";
                using var cmd = new SqliteCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Filter", roomTypeFilter);
                cmd.Parameters.AddWithValue("@Start", start.ToString("yyyy-MM-dd"));
                cmd.Parameters.AddWithValue("@End", end.ToString("yyyy-MM-dd"));
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var ovr = MapDayOverride(reader);
                    result[ovr.Date.Date] = ovr;
                }
            }

            return result;
        }

        public static DayAvailabilityOverride GetDayOverride(DateTime date, string roomTypeFilter)
        {
            EnsureInitialized();
            using (var conn = new SqliteConnection(ConnectionString))
            {
                conn.Open();
                var sql = @"SELECT * FROM DayAvailabilityOverrides
                            WHERE date(OverrideDate) = date(@Date) AND RoomTypeFilter = @Filter";
                using var cmd = new SqliteCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Date", date.ToString("yyyy-MM-dd"));
                cmd.Parameters.AddWithValue("@Filter", roomTypeFilter);
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                    return MapDayOverride(reader);
            }

            return null;
        }

        public static void SetDayOverride(DateTime date, string roomTypeFilter, int availableCount, int? occupiedCount)
        {
            EnsureInitialized();
            using (var conn = new SqliteConnection(ConnectionString))
            {
                conn.Open();
                var sql = @"INSERT INTO DayAvailabilityOverrides (OverrideDate, RoomTypeFilter, AvailableCount, OccupiedCount)
                            VALUES (@Date, @Filter, @Avail, @Occ)
                            ON CONFLICT(OverrideDate, RoomTypeFilter) DO UPDATE SET
                                AvailableCount = @Avail,
                                OccupiedCount = @Occ";
                using var cmd = new SqliteCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Date", date.ToString("yyyy-MM-dd"));
                cmd.Parameters.AddWithValue("@Filter", roomTypeFilter);
                cmd.Parameters.AddWithValue("@Avail", availableCount);
                cmd.Parameters.AddWithValue("@Occ", occupiedCount.HasValue ? (object)occupiedCount.Value : DBNull.Value);
                cmd.ExecuteNonQuery();
            }
        }

        public static void ClearDayOverride(DateTime date, string roomTypeFilter)
        {
            EnsureInitialized();
            using (var conn = new SqliteConnection(ConnectionString))
            {
                conn.Open();
                using var cmd = new SqliteCommand(
                    "DELETE FROM DayAvailabilityOverrides WHERE date(OverrideDate) = date(@Date) AND RoomTypeFilter = @Filter",
                    conn);
                cmd.Parameters.AddWithValue("@Date", date.ToString("yyyy-MM-dd"));
                cmd.Parameters.AddWithValue("@Filter", roomTypeFilter);
                cmd.ExecuteNonQuery();
            }
        }

        public static List<FoundItem> GetAllFoundItems()
        {
            EnsureInitialized();

            var list = new List<FoundItem>();

            using (var conn = new SqliteConnection(ConnectionString))
            {
                conn.Open();

                using (var cmd = new SqliteCommand("SELECT * FROM FoundItems WHERE Status = 'Unclaimed' ORDER BY CreatedAt DESC", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new FoundItem
                        {
                            FoundItemId = Convert.ToInt32(reader["FoundItemId"]),
                            RoomNumber = reader["RoomNumber"].ToString(),
                            GuestName = reader["GuestName"].ToString(),
                            ItemName = reader["ItemName"].ToString(),
                            Status = reader["Status"].ToString(),
                            CreatedAt = DateTime.Parse(reader["CreatedAt"].ToString())
                        });
                    }
                }
            }

            return list;
        }

        // ═══════════════════════════════════════
        //               ADD FOUND ITEM
        // ═══════════════════════════════════════
        public static void AddFoundItem(FoundItem item)
        {
            EnsureInitialized();

            using (var conn = new SqliteConnection(ConnectionString))
            {
                conn.Open();

                var sql = @"INSERT INTO FoundItems
                    (RoomNumber, GuestName, ItemName, Status, CreatedAt)
                    VALUES
                    (@Room, @Guest, @Item, @Status, @CreatedAt)";

                using (var cmd = new SqliteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Room", item.RoomNumber);
                    cmd.Parameters.AddWithValue("@Guest", item.GuestName);
                    cmd.Parameters.AddWithValue("@Item", item.ItemName);
                    cmd.Parameters.AddWithValue("@Status", item.Status ?? "Unclaimed");
                    cmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static void MarkFoundItemClaimed(int foundItemId)
        {
            EnsureInitialized();

            using (var conn = new SqliteConnection(ConnectionString))
            {
                conn.Open();

                using (var cmd = new SqliteCommand(
                    "UPDATE FoundItems SET Status='Claimed' WHERE FoundItemId=@Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", foundItemId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // ═══════════════════════════════════════
        //               ADD EVENT
        // ═══════════════════════════════════════
        public static void AddEvent(HotelEvent hotelEvent)
        {
            EnsureInitialized();

            using (var conn = new SqliteConnection(ConnectionString))
            {
                conn.Open();

                string sql = @"INSERT INTO HotelEvents
                       (EventDate, EventTime, EventName, Location, CreatedAt)
                       VALUES
                       (@Date, @Time, @Name, @Location, @CreatedAt)";

                using (var cmd = new SqliteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Date",
                        hotelEvent.EventDate.ToString("yyyy-MM-dd"));

                    cmd.Parameters.AddWithValue("@Time",
                        hotelEvent.EventTime);

                    cmd.Parameters.AddWithValue("@Name",
                        hotelEvent.EventName);

                    cmd.Parameters.AddWithValue("@Location",
                        hotelEvent.Location);

                    cmd.Parameters.AddWithValue("@CreatedAt",
                        DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static List<HotelEvent> GetAllEvents()
        {
            EnsureInitialized();

            var list = new List<HotelEvent>();

            using (var conn = new SqliteConnection(ConnectionString))
            {
                conn.Open();

                using (var cmd = new SqliteCommand(
                    "SELECT * FROM HotelEvents ORDER BY EventDate DESC", conn))

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new HotelEvent
                        {
                            EventId = Convert.ToInt32(reader["EventId"]),

                            EventDate = DateTime.Parse(
                                reader["EventDate"].ToString()),

                            EventTime = reader["EventTime"].ToString(),

                            EventName = reader["EventName"].ToString(),

                            Location = reader["Location"].ToString(),

                            CreatedAt = DateTime.Parse(
                                reader["CreatedAt"].ToString())
                        });
                    }
                }
            }

            return list;
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
            Amenities = r["Amenities"]?.ToString(),
            PhotoPath = r["PhotoPath"]?.ToString()
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
            RoomNumber = r.HasColumn("RoomNumber") ? r["RoomNumber"].ToString() : "",

            Phone = r.HasColumn("Phone") ? r["Phone"].ToString() : "",
            Email = r.HasColumn("Email") ? r["Email"].ToString() : "",
            Address = r.HasColumn("Address") ? r["Address"].ToString() : ""
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

        private static DayAvailabilityOverride MapDayOverride(SqliteDataReader r) => new DayAvailabilityOverride
        {
            OverrideId = Convert.ToInt32(r["OverrideId"]),
            Date = DateTime.Parse(r["OverrideDate"].ToString()),
            RoomTypeFilter = r["RoomTypeFilter"].ToString(),
            AvailableCount = Convert.ToInt32(r["AvailableCount"]),
            OccupiedCount = r["OccupiedCount"] != DBNull.Value
                ? Convert.ToInt32(r["OccupiedCount"])
                : (int?)null
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
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using calendarrrrrrrrrr.Models;

namespace calendarrrrrrrrrr
{
    public static class CalendarRoomHelper
    {
        public static string FormatDaySummary(int openCount, int bookedCount) =>
            $"{openCount} open · {bookedCount} booked";

        public static (int available, int occupied) CountRoomStatuses(
            ObservableCollection<RoomStatusViewModel> dayRooms) =>
            (
                dayRooms.Count(r => r.Status == "Available"),
                dayRooms.Count(r => r.Status == "Occupied" || r.Status == "Reserved")
            );

        public static int CountUnavailable(ObservableCollection<RoomStatusViewModel> dayRooms) =>
            dayRooms.Count(r => r.Status is "Maintenance" or "Cleaning" or "Unavailable");

        public static ObservableCollection<RoomStatusViewModel> GetRoomStatusForDate(
            DateTime date,
            List<Room> rooms,
            List<Reservation> reservations)
        {
            var roomStatuses = new ObservableCollection<RoomStatusViewModel>();

            foreach (var room in rooms)
            {
                var reservation = reservations.FirstOrDefault(r =>
                    r.RoomId == room.RoomId &&
                    r.CheckIn.Date <= date &&
                    r.CheckOut.Date > date &&
                    r.Status != "Cancelled");

                string status = reservation != null ? "Occupied" : room.Status;

                roomStatuses.Add(new RoomStatusViewModel
                {
                    RoomId = room.RoomId,
                    RoomNumber = room.RoomNumber,
                    RoomType = room.RoomType,
                    Status = status,
                    StatusColor = GetStatusColor(status),
                    Reservation = reservation
                });
            }

            return roomStatuses;
        }

        public static void ApplyDayVisualStyle(CalendarDayViewModel day, int available, int occupied)
        {
            if (!day.IsCurrentMonth)
            {
                day.DayBackground = GetBrush("SurfaceBrush");
                day.StatusBorderBrush = GetBrush("DividerBrush");
                day.StatusBorderThickness = new Thickness(1);
                day.StatusStripBrush = Brushes.Transparent;
                return;
            }

            if (occupied == 0 && available > 0)
            {
                day.DayBackground = new SolidColorBrush(Color.FromArgb(52, 100, 156, 109));
                day.StatusBorderBrush = GetBrush("StatusAvailableBrush");
                day.StatusStripBrush = GetBrush("StatusAvailableBrush");
            }
            else if (available == 0 && occupied > 0)
            {
                day.DayBackground = new SolidColorBrush(Color.FromArgb(58, 217, 107, 59));
                day.StatusBorderBrush = GetBrush("StatusOccupiedBrush");
                day.StatusStripBrush = GetBrush("StatusOccupiedBrush");
            }
            else if (occupied > 0 && available > 0)
            {
                day.DayBackground = new SolidColorBrush(Color.FromArgb(48, 231, 197, 151));
                day.StatusBorderBrush = GetBrush("StatusPendingBrush");
                day.StatusStripBrush = GetBrush("StatusPendingBrush");
            }
            else
            {
                day.DayBackground = GetBrush("SurfaceElevatedBrush");
                day.StatusBorderBrush = GetBrush("DividerBrush");
                day.StatusStripBrush = GetBrush("StatusUnavailableBrush");
            }

            day.StatusBorderThickness = new Thickness(5, 1, 1, 1);
        }

        public static Brush GetStatusColor(string status) =>
            status switch
            {
                "Available" => GetBrush("StatusAvailableBrush"),
                "Occupied" => GetBrush("StatusOccupiedBrush"),
                "Reserved" => GetBrush("StatusOccupiedBrush"),
                "Cleaning" => GetBrush("WarningBrush"),
                "Maintenance" => GetBrush("ErrorBrush"),
                _ => GetBrush("StatusUnavailableBrush")
            };

        public static Brush GetBrush(string resourceKey)
        {
            try
            {
                if (Application.Current.Resources[resourceKey] is Brush brush)
                    return brush;
            }
            catch
            {
            }

            return new SolidColorBrush(Colors.Gray);
        }
    }
}

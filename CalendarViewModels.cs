using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using calendarrrrrrrrrr.Models;

namespace calendarrrrrrrrrr
{
    public class CalendarDayViewModel
    {
        public DateTime Date { get; set; }
        public string DayNumber { get; set; } = string.Empty;
        public Brush DayBackground { get; set; } = Brushes.Transparent;
        public Brush DayNumberColor { get; set; } = Brushes.Black;
        public Brush StatusBorderBrush { get; set; } = Brushes.LightGray;
        public Thickness StatusBorderThickness { get; set; } = new Thickness(1);
        public Brush StatusStripBrush { get; set; } = Brushes.Transparent;
        public bool IsCurrentMonth { get; set; }
        public ObservableCollection<RoomStatusViewModel> Rooms { get; set; } = new();
        public int AvailableCount { get; set; }
        public int OccupiedCount { get; set; }

        public bool ShowDayStatus => IsCurrentMonth;
        public Cursor DayCursor => IsCurrentMonth ? Cursors.Hand : Cursors.Arrow;

        public string DateDisplay => Date.ToString("ddd, MMM dd");

        public string DayStatusLine => ShowDayStatus
            ? $"{AvailableCount} open · {OccupiedCount} booked"
            : string.Empty;

        public string StatusSummary => DayStatusLine;
    }

    public class RoomStatusViewModel
    {
        public int RoomId { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public string RoomType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public Brush StatusColor { get; set; } = Brushes.Gray;
        public Reservation? Reservation { get; set; }

        public string DisplayTitle => $"Room {RoomNumber}";
        public string DetailLine => $"{RoomType} · {Status}";

        public bool CanBook => Status == "Available";
        public bool CanRelease => Status is "Occupied" or "Reserved";
        public bool CanMarkAvailable => Status is "Maintenance" or "Cleaning" or "Unavailable";
    }

    public class ManageRoomItemViewModel
    {
        public int RoomId { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public string RoomType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public Brush StatusBrush { get; set; } = Brushes.Gray;

        public string Title => $"Room {RoomNumber}";
        public string DetailLine => $"{RoomType} · {Status}";
    }
}

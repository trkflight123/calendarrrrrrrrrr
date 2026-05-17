using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using calendarrrrrrrrrr.Data;
using calendarrrrrrrrrr.Models;
using HotelYnCierto;

namespace calendarrrrrrrrrr
{
    public partial class CalendarWindow : Window    
    {
        private DateTime currentMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        private string selectedRoomType = "All";
        private bool calendarReady;
        private List<Room> allRooms = new();
        private List<Reservation> allReservations = new();

        public CalendarWindow()
        {
            InitializeComponent();

            DatabaseService.Initialize();
            InitializeCalendar();
        }

        private void InitializeCalendar()
        {
            currentMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            LoadData();
            calendarReady = true;
            RefreshCalendar();
        }

        private void LoadData()
        {
            try
            {
                allRooms = DatabaseService.GetAllRooms();
                allReservations = DatabaseService.GetAllReservations();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}",
                    "Database Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                allRooms = new List<Room>();
                allReservations = new List<Reservation>();
            }
        }

        private void RefreshCalendar()
        {
            if (!calendarReady)
                return;

            try
            {
                LoadData();

                var calendarDays = new ObservableCollection<CalendarDayViewModel>();

                var firstDay = new DateTime(currentMonth.Year, currentMonth.Month, 1);
                int daysInMonth = DateTime.DaysInMonth(currentMonth.Year, currentMonth.Month);
                int startDayOfWeek = (int)firstDay.DayOfWeek;

                var previousMonth = firstDay.AddMonths(-1);
                int daysInPreviousMonth = DateTime.DaysInMonth(previousMonth.Year, previousMonth.Month);

                for (int i = startDayOfWeek - 1; i >= 0; i--)
                {
                    int dayNumber = daysInPreviousMonth - i;
                    var date = new DateTime(previousMonth.Year, previousMonth.Month, dayNumber);
                    calendarDays.Add(CreatePaddingDay(date));
                }

                var filteredRooms = selectedRoomType == "All" || selectedRoomType == "All Rooms"
                ? allRooms
                : allRooms.Where(r =>
                    r.RoomType.Contains(selectedRoomType)
                  ).ToList();

                for (int day = 1; day <= daysInMonth; day++)
                {
                    var date = new DateTime(currentMonth.Year, currentMonth.Month, day);
                    var dayRooms = GetRoomStatusForDate(date, filteredRooms);

                    calendarDays.Add(new CalendarDayViewModel
                    {
                        Date = date,
                        DayNumber = day.ToString(),
                        DayBackground = GetBackgroundForDay(dayRooms),
                        DayNumberColor = GetBrush("TextPrimaryBrush"),
                        IsCurrentMonth = true,
                        Rooms = dayRooms
                    });
                }

                var nextMonth = firstDay.AddMonths(1);
                int trailingDays = (7 - (calendarDays.Count % 7)) % 7;

                for (int day = 1; day <= trailingDays; day++)
                {
                    var date = new DateTime(nextMonth.Year, nextMonth.Month, day);
                    calendarDays.Add(CreatePaddingDay(date));
                }

                CalendarDaysControl.ItemsSource = calendarDays;
                TxtCurrentMonth.Text = currentMonth.ToString("MMMM yyyy");

                UpdateStatistics(filteredRooms);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error refreshing calendar: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private ObservableCollection<RoomStatusViewModel> GetRoomStatusForDate(DateTime date, List<Room> rooms)
        {
            var roomStatuses = new ObservableCollection<RoomStatusViewModel>();

            foreach (var room in rooms)
            {
                var reservation = allReservations.FirstOrDefault(r =>
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

        private Brush GetBackgroundForDay(ObservableCollection<RoomStatusViewModel> roomStatuses)
        {
            if (roomStatuses.Count == 0)
                return GetBrush("SurfaceElevatedBrush");

            int available = roomStatuses.Count(r => r.Status == "Available");
            int occupied = roomStatuses.Count(r => r.Status == "Occupied");

            if (occupied == 0)
                return GetBrush("SurfaceElevatedBrush");

            if (available == 0)
                return GetBrush("SurfaceBrush");

            return GetBrush("SurfaceElevatedBrush");
        }

        private Brush GetStatusColor(string status)
        {
            return status switch
            {
                "Available" => GetBrush("StatusAvailableBrush"),
                "Occupied" => GetBrush("StatusOccupiedBrush"),
                "Reserved" => GetBrush("StatusOccupiedBrush"),
                "Cleaning" => GetBrush("WarningBrush"),
                "Maintenance" => GetBrush("ErrorBrush"),
                _ => GetBrush("StatusUnavailableBrush")
            };
        }

        private Brush GetBrush(string resourceKey)
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

        private void UpdateStatistics(List<Room> filteredRooms)
        {
            var firstDay = new DateTime(currentMonth.Year, currentMonth.Month, 1);
            var lastDay = firstDay.AddMonths(1).AddDays(-1);

            int availableCount = 0;
            int occupiedCount = 0;
            int unavailableCount = 0;

            foreach (var room in filteredRooms)
            {
                bool hasReservation = allReservations.Any(r =>
                    r.RoomId == room.RoomId &&
                    r.CheckIn.Date <= lastDay &&
                    r.CheckOut.Date >= firstDay &&
                    r.Status != "Cancelled");

                if (hasReservation || room.Status == "Occupied" || room.Status == "Reserved")
                    occupiedCount++;
                else if (room.Status == "Maintenance" || room.Status == "Cleaning" || room.Status == "Unavailable")
                    unavailableCount++;
                else
                    availableCount++;
            }

            TxtAvailCount.Text = availableCount.ToString();
            TxtOccupiedCount.Text = occupiedCount.ToString();
            TxtUnavailCount.Text = unavailableCount.ToString();
        }

        private CalendarDayViewModel CreatePaddingDay(DateTime date)
        {
            return new CalendarDayViewModel
            {
                Date = date,
                DayNumber = "",
                DayBackground = GetBrush("SurfaceBrush"),
                DayNumberColor = GetBrush("TextTertiaryBrush"),
                IsCurrentMonth = false,
                Rooms = new ObservableCollection<RoomStatusViewModel>()
            };
        }

        private void BtnPrevMonth_Click(object sender, RoutedEventArgs e)
        {
            currentMonth = currentMonth.AddMonths(-1);
            RefreshCalendar();
        }

        private void BtnNextMonth_Click(object sender, RoutedEventArgs e)
        {
            currentMonth = currentMonth.AddMonths(1);
            RefreshCalendar();
        }

        private void BtnToday_Click(object sender, RoutedEventArgs e)
        {
            currentMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            RefreshCalendar();
        }

        private void CmbRoomType_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (!calendarReady)
                return;

            if (CmbRoomType.SelectedItem is System.Windows.Controls.ComboBoxItem item)
            {
                selectedRoomType = item.Content?.ToString() ?? "All";
                RefreshCalendar();
            }
        }

        private void BtnNewBooking_Click(object sender, RoutedEventArgs e)
        {
            ReservationWindow reservationWindow = new ReservationWindow();
            reservationWindow.ShowDialog();

            RefreshCalendar();
        }

        private void BtnAddRoom_Click(object sender, RoutedEventArgs e)
        {
            AddRoomWindow addRoomWindow = new AddRoomWindow();
            addRoomWindow.ShowDialog();

            RefreshCalendar();
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            Dashboard dashboard = new Dashboard();
            dashboard.Show();

            Close();
        }
    }

    public class CalendarDayViewModel
    {
        public DateTime Date { get; set; }
        public string DayNumber { get; set; } = string.Empty;
        public Brush DayBackground { get; set; } = Brushes.Transparent;
        public Brush DayNumberColor { get; set; } = Brushes.Black;
        public bool IsCurrentMonth { get; set; }
        public ObservableCollection<RoomStatusViewModel> Rooms { get; set; } = new();

        public string DateDisplay => Date.ToString("ddd, MMM dd");

        public string StatusSummary =>
            $"{Rooms?.Count(r => r.Status == "Available") ?? 0} available, {Rooms?.Count(r => r.Status == "Occupied") ?? 0} occupied";
    }

    public class RoomStatusViewModel
    {
        public int RoomId { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public string RoomType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public Brush StatusColor { get; set; } = Brushes.Gray;
        public Reservation? Reservation { get; set; }
    }
}
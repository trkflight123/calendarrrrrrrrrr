using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

        private List<Room> GetFilteredRooms() =>
            selectedRoomType == "All" || selectedRoomType == "All Rooms"
                ? allRooms
                : allRooms.Where(r => r.RoomType.Contains(selectedRoomType)).ToList();

        private void SyncCalendarDayCell(DateTime date)
        {
            if (CalendarDaysControl.ItemsSource is not ObservableCollection<CalendarDayViewModel> days)
            {
                RefreshCalendar();
                return;
            }

            var cell = days.FirstOrDefault(d => d.Date.Date == date.Date && d.IsCurrentMonth);
            if (cell == null)
            {
                RefreshCalendar();
                return;
            }

            var filteredRooms = GetFilteredRooms();
            var dayRooms = CalendarRoomHelper.GetRoomStatusForDate(date, filteredRooms, allReservations);
            var counts = CalendarRoomHelper.CountRoomStatuses(dayRooms);

            cell.Rooms = dayRooms;
            cell.AvailableCount = counts.available;
            cell.OccupiedCount = counts.occupied;
            CalendarRoomHelper.ApplyDayVisualStyle(cell, counts.available, counts.occupied);
            UpdateStatistics(filteredRooms);
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

                var filteredRooms = GetFilteredRooms();

                for (int day = 1; day <= daysInMonth; day++)
                {
                    var date = new DateTime(currentMonth.Year, currentMonth.Month, day);
                    var dayRooms = CalendarRoomHelper.GetRoomStatusForDate(date, filteredRooms, allReservations);
                    var (available, occupied) = CalendarRoomHelper.CountRoomStatuses(dayRooms);

                    var dayVm = new CalendarDayViewModel
                    {
                        Date = date,
                        DayNumber = day.ToString(),
                        DayNumberColor = CalendarRoomHelper.GetBrush("TextPrimaryBrush"),
                        IsCurrentMonth = true,
                        Rooms = dayRooms,
                        AvailableCount = available,
                        OccupiedCount = occupied
                    };
                    CalendarRoomHelper.ApplyDayVisualStyle(dayVm, available, occupied);
                    calendarDays.Add(dayVm);
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

        private void UpdateStatistics(List<Room> filteredRooms)
        {
            var todayRooms = CalendarRoomHelper.GetRoomStatusForDate(DateTime.Today, filteredRooms, allReservations);
            var (availableCount, occupiedCount) = CalendarRoomHelper.CountRoomStatuses(todayRooms);
            int unavailableCount = CalendarRoomHelper.CountUnavailable(todayRooms);

            TxtAvailCount.Text = availableCount.ToString();
            TxtOccupiedCount.Text = occupiedCount.ToString();
            TxtUnavailCount.Text = unavailableCount.ToString();
        }

        private CalendarDayViewModel CreatePaddingDay(DateTime date)
        {
            var day = new CalendarDayViewModel
            {
                Date = date,
                DayNumber = "",
                DayNumberColor = CalendarRoomHelper.GetBrush("TextTertiaryBrush"),
                IsCurrentMonth = false,
                Rooms = new ObservableCollection<RoomStatusViewModel>(),
                AvailableCount = 0,
                OccupiedCount = 0
            };
            CalendarRoomHelper.ApplyDayVisualStyle(day, 0, 0);
            return day;
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

        private void BtnManageRooms_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ManageRoomsWindow { Owner = this };
            dialog.ShowDialog();
            if (dialog.RoomsChanged)
                RefreshCalendar();
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            Dashboard dashboard = new Dashboard();
            dashboard.Show();

            this.Hide();
        }

        private void DayCell_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is not FrameworkElement element ||
                element.DataContext is not CalendarDayViewModel day ||
                !day.IsCurrentMonth)
                return;

            var dialog = new DayRoomsDialog(day, selectedRoomType, () => SyncCalendarDayCell(day.Date))
            {
                Owner = this
            };
            dialog.ShowDialog();
            if (dialog.CalendarChanged)
                RefreshCalendar();

            e.Handled = true;
        }
    }
}
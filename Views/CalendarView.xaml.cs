using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using calendarrrrrrrrrr.Data;
using calendarrrrrrrrrr.Models;

namespace calendarrrrrrrrrr.Views
{
    public class CalendarDayViewModel
    {
        public int DayNumber { get; set; }
        public DateTime Date { get; set; }
        public bool IsCurrentMonth { get; set; }
        public Brush DayBackground { get; set; } = Brushes.Transparent;
        public Brush DayNumberColor { get; set; } = Brushes.Black;
        public ObservableCollection<RoomStatusViewModel> Rooms { get; set; } = new();
    }

    public class RoomStatusViewModel
    {
        public string RoomNumber { get; set; } = string.Empty;
        public Brush StatusColor { get; set; } = Brushes.Gray;
        public string ToolTip { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public partial class CalendarView : Page
    {
        private DateTime currentMonth;
        private ObservableCollection<CalendarDayViewModel> calendarDays;
        private List<Room> allRooms = new List<Room>();
        private List<Reservation> allReservations = new List<Reservation>();

        public CalendarView()
        {
            InitializeComponent();
            currentMonth = DateTime.Now;
            calendarDays = new ObservableCollection<CalendarDayViewModel>();

            try
            {
                CalendarDaysControl.ItemsSource = calendarDays;
                LoadData();
                LoadCalendar();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CalendarView init error: {ex}");
                MessageBox.Show($"Error initializing calendar: {ex.Message}\n\n{ex.InnerException?.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadData()
        {
            try
            {
                allRooms = new List<Room>(DatabaseService.GetAllRooms() ?? new List<Room>());
                allReservations = new List<Reservation>(DatabaseService.GetAllReservations() ?? new List<Reservation>());
                System.Diagnostics.Debug.WriteLine($"Loaded {allRooms.Count} rooms and {allReservations.Count} reservations");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading data: {ex}");
                allRooms = new List<Room>();
                allReservations = new List<Reservation>();
            }
        }

        private void LoadCalendar()
        {
            try
            {
                calendarDays.Clear();

                if (TxtCurrentMonth != null)
                {
                    TxtCurrentMonth.Text = currentMonth.ToString("MMMM yyyy");
                }

                var firstDay = new DateTime(currentMonth.Year, currentMonth.Month, 1);
                int firstDayOfWeek = (int)firstDay.DayOfWeek;
                if (firstDayOfWeek == 0) firstDayOfWeek = 7; // Convert Sunday to 7
                firstDayOfWeek--; // Convert to 0-indexed (Mon=0)

                int daysInMonth = DateTime.DaysInMonth(currentMonth.Year, currentMonth.Month);

                // Add previous month's days
                var previousMonth = currentMonth.AddMonths(-1);
                var daysInPreviousMonth = DateTime.DaysInMonth(previousMonth.Year, previousMonth.Month);
                for (int i = firstDayOfWeek - 1; i >= 0; i--)
                {
                    int dayNumber = daysInPreviousMonth - i;
                    var date = new DateTime(previousMonth.Year, previousMonth.Month, dayNumber);
                    calendarDays.Add(CreateCalendarDay(date, false));
                }

                // Add current month's days
                for (int day = 1; day <= daysInMonth; day++)
                {
                    var date = new DateTime(currentMonth.Year, currentMonth.Month, day);
                    calendarDays.Add(CreateCalendarDay(date, true));
                }

                // Add next month's days to fill grid
                var nextMonth = currentMonth.AddMonths(1);
                int totalCells = 42; // 6 rows * 7 days
                int remainingDays = totalCells - calendarDays.Count;
                for (int day = 1; day <= remainingDays; day++)
                {
                    var date = new DateTime(nextMonth.Year, nextMonth.Month, day);
                    calendarDays.Add(CreateCalendarDay(date, false));
                }

                System.Diagnostics.Debug.WriteLine($"Calendar loaded with {calendarDays.Count} days");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in LoadCalendar: {ex}");
                MessageBox.Show($"Error loading calendar: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private CalendarDayViewModel CreateCalendarDay(DateTime date, bool isCurrentMonth)
        {
            try
            {
                var roomStatuses = new ObservableCollection<RoomStatusViewModel>();

                // Get reservations for this date
                if (allReservations != null && allReservations.Count > 0)
                {
                    var reservationsForDate = allReservations
                        .Where(r => r != null && r.CheckIn.Date <= date && r.CheckOut.Date > date)
                        .ToList();

                    // Add up to 3 room statuses per day
                    foreach (var room in allRooms.Take(3))
                    {
                        try
                        {
                            if (room == null) continue;

                            var reservation = reservationsForDate.FirstOrDefault(r => r.RoomId == room.RoomId);

                            string status = "Available";
                            Brush statusColor = GetBrush("StatusAvailableBrush") ?? GetDefaultBrush(Colors.Green);
                            string tooltip = $"{room.RoomNumber}: Available";

                            if (reservation != null)
                            {
                                status = "Occupied";
                                statusColor = GetBrush("StatusOccupiedBrush") ?? GetDefaultBrush(Colors.Red);
                                tooltip = $"{room.RoomNumber}: Occupied";
                            }

                            roomStatuses.Add(new RoomStatusViewModel
                            {
                                RoomNumber = room.RoomNumber ?? "?",
                                StatusColor = statusColor,
                                Status = status,
                                ToolTip = tooltip
                            });
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error processing room: {ex.Message}");
                        }
                    }
                }

                var dayBackground = isCurrentMonth
                    ? (GetBrush("NavyElevatedBrush") ?? GetDefaultBrush(Colors.WhiteSmoke))
                    : (GetBrush("NavySurfaceBrush") ?? GetDefaultBrush(Colors.LightGray));

                var dayNumberColor = isCurrentMonth
                    ? (GetBrush("TextPrimaryBrush") ?? GetDefaultBrush(Colors.Black))
                    : (GetBrush("TextMutedBrush") ?? GetDefaultBrush(Colors.Gray));

                bool isToday = isCurrentMonth && date.Date == DateTime.Now.Date;
                if (isToday)
                {
                    dayBackground = GetBrush("GoldGradientBrush") ?? GetDefaultBrush(new Color { R = 183, G = 131, B = 84, A = 255 });
                    dayNumberColor = GetBrush("SurfaceWhiteBrush") ?? GetDefaultBrush(Colors.White);
                }

                return new CalendarDayViewModel
                {
                    DayNumber = date.Day,
                    Date = date,
                    IsCurrentMonth = isCurrentMonth,
                    DayBackground = dayBackground,
                    DayNumberColor = dayNumberColor,
                    Rooms = roomStatuses
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating calendar day: {ex.Message}");
                // Return a safe default day
                return new CalendarDayViewModel
                {
                    DayNumber = date.Day,
                    Date = date,
                    IsCurrentMonth = isCurrentMonth,
                    DayBackground = GetDefaultBrush(Colors.White),
                    DayNumberColor = GetDefaultBrush(Colors.Black),
                    Rooms = new ObservableCollection<RoomStatusViewModel>()
                };
            }
        }

        private Brush GetBrush(string resourceKey)
        {
            try
            {
                if (Application.Current?.Resources == null) return Brushes.Gray;

                var resource = Application.Current.Resources[resourceKey];
                if (resource is Brush brush)
                {
                    return brush;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting brush {resourceKey}: {ex.Message}");
            }
            return Brushes.Gray;
        }

        private Brush GetDefaultBrush(Color color)
        {
            return new SolidColorBrush(color);
        }

        private void BtnPrevMonth_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                currentMonth = currentMonth.AddMonths(-1);
                LoadCalendar();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnNextMonth_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                currentMonth = currentMonth.AddMonths(1);
                LoadCalendar();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnToday_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                currentMonth = DateTime.Now;
                LoadCalendar();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnNewBooking_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MessageBox.Show("New Booking dialog coming soon...", "Not Yet Implemented", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

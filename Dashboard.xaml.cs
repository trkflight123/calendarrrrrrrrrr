using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;
using calendarrrrrrrrrr.Data;
using calendarrrrrrrrrr.Models;

namespace calendarrrrrrrrrr
{
    public partial class Dashboard : Window, INotifyPropertyChanged
    {
        private DispatcherTimer greetingTimer;

        private string _greeting;
        private string _todayDate;

        private int _totalRooms;
        private int _availableRooms;
        private int _occupiedRooms;

        private string _revenueThisMonth;
        private string _totalSalesOverall;

        private string _occupancyRate;
        private int _occupancyValue;
        private string _occupancyDescription;

        private int _pendingBookings;
        private int _confirmedBookings;
        private int _completedBookings;
        private int _cancelledBookings;

        public event PropertyChangedEventHandler PropertyChanged;

        public string Greeting
        {
            get { return _greeting; }
            set
            {
                _greeting = value;
                OnPropertyChanged(nameof(Greeting));
            }
        }

        public string TodayDate
        {
            get { return _todayDate; }
            set
            {
                _todayDate = value;
                OnPropertyChanged(nameof(TodayDate));
            }
        }

        public int TotalRooms
        {
            get { return _totalRooms; }
            set
            {
                _totalRooms = value;
                OnPropertyChanged(nameof(TotalRooms));
            }
        }

        public int AvailableRooms
        {
            get { return _availableRooms; }
            set
            {
                _availableRooms = value;
                OnPropertyChanged(nameof(AvailableRooms));
            }
        }

        public int OccupiedRooms
        {
            get { return _occupiedRooms; }
            set
            {
                _occupiedRooms = value;
                OnPropertyChanged(nameof(OccupiedRooms));
            }
        }

        public string RevenueThisMonth
        {
            get { return _revenueThisMonth; }
            set
            {
                _revenueThisMonth = value;
                OnPropertyChanged(nameof(RevenueThisMonth));
            }
        }

        public string TotalSalesOverall
        {
            get { return _totalSalesOverall; }
            set
            {
                _totalSalesOverall = value;
                OnPropertyChanged(nameof(TotalSalesOverall));
            }
        }

        public string OccupancyRate
        {
            get { return _occupancyRate; }
            set
            {
                _occupancyRate = value;
                OnPropertyChanged(nameof(OccupancyRate));
            }
        }

        public int OccupancyValue
        {
            get { return _occupancyValue; }
            set
            {
                _occupancyValue = value;
                OnPropertyChanged(nameof(OccupancyValue));
            }
        }

        public string OccupancyDescription
        {
            get { return _occupancyDescription; }
            set
            {
                _occupancyDescription = value;
                OnPropertyChanged(nameof(OccupancyDescription));
            }
        }

        public int PendingBookings
        {
            get { return _pendingBookings; }
            set
            {
                _pendingBookings = value;
                OnPropertyChanged(nameof(PendingBookings));
            }
        }

        public int ConfirmedBookings
        {
            get { return _confirmedBookings; }
            set
            {
                _confirmedBookings = value;
                OnPropertyChanged(nameof(ConfirmedBookings));
            }
        }

        public int CompletedBookings
        {
            get { return _completedBookings; }
            set
            {
                _completedBookings = value;
                OnPropertyChanged(nameof(CompletedBookings));
            }
        }

        public int CancelledBookings
        {
            get { return _cancelledBookings; }
            set
            {
                _cancelledBookings = value;
                OnPropertyChanged(nameof(CancelledBookings));
            }
        }

        public ObservableCollection<MonthlySale> MonthlySales { get; set; }
        public ObservableCollection<RecentBooking> RecentBookings { get; set; }
        public ObservableCollection<RoomAlert> RoomAlerts { get; set; }

        public Dashboard()
        {
            InitializeComponent();

            DatabaseService.Initialize();

            MonthlySales = new ObservableCollection<MonthlySale>();
            RecentBookings = new ObservableCollection<RecentBooking>();
            RoomAlerts = new ObservableCollection<RoomAlert>();

            DataContext = this;

            LoadDashboardData();
            UpdateGreeting();

            greetingTimer = new DispatcherTimer();
            greetingTimer.Interval = TimeSpan.FromSeconds(5);
            greetingTimer.Tick += (s, e) => UpdateGreeting();
            greetingTimer.Start();
        }

        private void LoadDashboardData()
        {
            TodayDate = DateTime.Now.ToString("dddd, MMMM dd, yyyy");

            TotalRooms = DatabaseService.GetTotalRoomsCount();
            AvailableRooms = DatabaseService.GetAvailableRoomsCount();
            OccupiedRooms = DatabaseService.GetOccupiedRoomsCount();

            decimal monthlyRevenue = DatabaseService.GetMonthlySales(DateTime.Now.Year, DateTime.Now.Month);
            decimal totalSales = DatabaseService.GetTotalSalesOverall();

            RevenueThisMonth = "₱" + monthlyRevenue.ToString("N2");
            TotalSalesOverall = "₱" + totalSales.ToString("N2");

            PendingBookings = DatabaseService.GetReservationStatusCount("Pending");
            ConfirmedBookings = DatabaseService.GetReservationStatusCount("Confirmed");
            CompletedBookings = DatabaseService.GetReservationStatusCount("Completed");
            CancelledBookings = DatabaseService.GetReservationStatusCount("Cancelled");

            if (TotalRooms > 0)
            {
                OccupancyValue = (int)Math.Round((double)OccupiedRooms / TotalRooms * 100);
            }
            else
            {
                OccupancyValue = 0;
            }

            OccupancyRate = OccupancyValue + "%";
            OccupancyDescription = OccupiedRooms + " out of " + TotalRooms + " rooms are currently occupied.";

            LoadMonthlySales();
            LoadRecentBookings();
            LoadRoomAlerts();
        }

        private void LoadMonthlySales()
        {
            MonthlySales.Clear();

            decimal maxSales = 1;

            for (int month = 1; month <= 12; month++)
            {
                decimal sales = DatabaseService.GetMonthlySales(DateTime.Now.Year, month);

                if (sales > maxSales)
                    maxSales = sales;
            }

            for (int month = 1; month <= 12; month++)
            {
                decimal sales = DatabaseService.GetMonthlySales(DateTime.Now.Year, month);

                MonthlySales.Add(new MonthlySale
                {
                    Month = new DateTime(DateTime.Now.Year, month, 1).ToString("MMM"),
                    Sales = sales,
                    MaxSales = maxSales
                });
            }
        }

        private void LoadRecentBookings()
        {
            RecentBookings.Clear();

            var bookings = DatabaseService.GetRecentBookings(5);

            foreach (var booking in bookings)
            {
                RecentBookings.Add(new RecentBooking
                {
                    GuestName = booking.GuestName,
                    RoomName = "Room " + booking.RoomNumber,
                    CheckIn = booking.CheckIn.ToString("MMM dd"),
                    CheckOut = booking.CheckOut.ToString("MMM dd"),
                    Status = booking.Status,
                    Amount = "₱" + booking.TotalAmount.ToString("N2")
                });
            }
        }

        private void LoadRoomAlerts()
        {
            RoomAlerts.Clear();

            var alerts = DatabaseService.GetDashboardRoomAlerts(5);

            foreach (var alert in alerts)
            {
                RoomAlerts.Add(new RoomAlert
                {
                    AlertTitle = "Room " + alert.RoomNumber + " - " + alert.TaskType,
                    AlertDescription = alert.Priority + " priority | " + alert.Status
                });
            }
        }

        private void UpdateGreeting()
        {
            Greeting = CalculateGreeting();
        }

        private string CalculateGreeting()
        {
            int hour = DateTime.Now.Hour;

            if (hour >= 0 && hour < 12)
                return "🌞 Good Morning!";

            if (hour >= 12 && hour < 17)
                return "☀️ Good Afternoon!";

            if (hour >= 17 && hour < 21)
                return "🌆 Good Evening!";

            return "🌙 Good Night!";
        }

        private void Dashboard_Click(object sender, RoutedEventArgs e)
        {
            LoadDashboardData();
        }

        private void Calendar_Click(object sender, RoutedEventArgs e)
        {
            CalendarWindow calendarWindow = new CalendarWindow();
            calendarWindow.Show();
            Hide();
        }

        private void Guest_Click(object sender, RoutedEventArgs e)
        {
            GuestWindow guestWindow = new GuestWindow();
            guestWindow.Show();
            Hide();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            FoundItems foundItemsWindow = new FoundItems();
            foundItemsWindow.Show();
            Hide();
        }

        private void Events_Click(object sender, RoutedEventArgs e)
        {
            Events eventsWindow = new Events();
            eventsWindow.Show();
            Hide();
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            LogIn login = new LogIn();
            login.Show();
            Close();
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class MonthlySale
    {
        public string Month { get; set; }
        public decimal Sales { get; set; }
        public decimal MaxSales { get; set; }

        public double BarHeight
        {
            get
            {
                double maxHeight = 260;

                if (MaxSales <= 0)
                    return 0;

                return Convert.ToDouble(Sales / MaxSales) * maxHeight;
            }
        }

        public string SalesText
        {
            get
            {
                return "₱" + Sales.ToString("N0");
            }
        }
    }

    public class RecentBooking
    {
        public string GuestName { get; set; }
        public string RoomName { get; set; }
        public string CheckIn { get; set; }
        public string CheckOut { get; set; }
        public string Status { get; set; }
        public string Amount { get; set; }
    }

    public class RoomAlert
    {
        public string AlertTitle { get; set; }
        public string AlertDescription { get; set; }
    }
}
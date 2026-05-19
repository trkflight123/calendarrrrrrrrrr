using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using calendarrrrrrrrrr.Data;
using System.Threading.Tasks;

namespace calendarrrrrrrrrr
{
    public partial class Dashboard : Window
    {
        private DispatcherTimer roomTimer;
        private List<RoomCard> availableRooms = new();
        private int currentRoomIndex = 0;

        public Dashboard()
        {
            InitializeComponent();

            DatabaseService.Initialize();
            LoadRooms();

            roomTimer = new DispatcherTimer();
            roomTimer.Interval = TimeSpan.FromSeconds(5);
            roomTimer.Tick += RoomTimer_Tick;
            roomTimer.Start();
        }

        private void LoadRooms()
        {
            var rooms = DatabaseService.GetAllRooms();

            availableRooms = rooms
                .Where(r => r.Status == "Available")
                .Select(r => new RoomCard
                {
                    RoomName = $"Room {r.RoomNumber}",
                    RoomType = r.RoomType,
                    Capacity = $"{r.Capacity} pax",
                    Status = r.Status,
                    Price = $"₱{r.PricePerNight:N2} / night",
                    ImagePath = r.PhotoPath
                })
                .ToList();

            currentRoomIndex = 0;
            ShowCurrentRoom();
                
            RoomStatusGrid.ItemsSource = availableRooms;
        }

        private void ShowCurrentRoom()
        {
            if (availableRooms.Count == 0)
            {
                RoomsList.ItemsSource = null;
                return;
            }

            RoomsList.ItemsSource = new List<RoomCard>
            {
                availableRooms[currentRoomIndex]
            };
        }

        private void RoomTimer_Tick(object sender, EventArgs e)
        {
            if (availableRooms.Count == 0)
                return;

            currentRoomIndex++;

            if (currentRoomIndex >= availableRooms.Count)
                currentRoomIndex = 0;

            ShowCurrentRoom();
        }

        private void Dashboard_Click(object sender, RoutedEventArgs e)
        {
            LoadRooms();
        }

        private void Calendar_Click(object sender, RoutedEventArgs e)
        {
            CalendarWindow calendarWindow = new CalendarWindow();

            calendarWindow.Show();

            this.Hide();
        }

        private void Guest_Click(object sender, RoutedEventArgs e)
        {
            GuestWindow guestWindow = new GuestWindow();
            guestWindow.Show();

            this.Hide();
        }

        

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            FoundItems foundItemsWindow = new FoundItems();
            foundItemsWindow.Show();
            this.Hide();
        }
        private void Events_Click(object sender, RoutedEventArgs e)
        {
            Events eventsWindow = new Events();
            eventsWindow.Show();

            this.Hide();
        }
    }

    public class RoomCard
    {
        public string RoomName { get; set; }
        public string RoomType { get; set; }
        public string Capacity { get; set; }
        public string Status { get; set; }
        public string Price { get; set; }
        public string ImagePath { get; set; }
    }
}
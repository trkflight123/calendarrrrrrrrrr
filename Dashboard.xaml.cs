using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using calendarrrrrrrrrr.Data;

namespace calendarrrrrrrrrr
{
    public partial class Dashboard : Window
    {
        private DispatcherTimer roomTimer;

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

            RoomsList.ItemsSource = rooms.Select(r => new RoomCard
            {
                RoomName = $"Room {r.RoomNumber} - {r.RoomType}",
                Capacity = $"Capacity: {r.Capacity} person(s)",
                Status = $"Status: {r.Status}",
                Price = $"₱{r.PricePerNight:N2} / night",
                ImagePath = r.PhotoPath
            }).ToList();
        }

        private BitmapImage LoadImage(string path)
        {
            MessageBox.Show(
                $"PhotoPath: {path}\nExists: {File.Exists(path)}",
                "Image Debug");

            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return null;

            BitmapImage image = new BitmapImage();
            image.BeginInit();
            image.UriSource = new Uri(path, UriKind.Absolute);
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.EndInit();

            return image;
        }

        private void RoomTimer_Tick(object sender, EventArgs e)
        {
            LoadRooms();
        }

        private void Dashboard_Click(object sender, RoutedEventArgs e)
        {
            LoadRooms();
        }

        private void Calendar_Click(object sender, RoutedEventArgs e)
        {
            CalendarWindow calendarWindow = new CalendarWindow();
            calendarWindow.Show();
            Close();
        }

        private void Guest_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    public class RoomCard
    {
        public string RoomName { get; set; }
        public string Capacity { get; set; }
        public string Status { get; set; }
        public string Price { get; set; }
        public string ImagePath { get; set; }
    }
}
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace calendarrrrrrrrrr
{
    public partial class Events : Window
    {
        private List<EventItem> events = new();

        public Events()
        {
            InitializeComponent();

            LoadDefaultEvents();
            LoadEvents();
        }

        private void LoadDefaultEvents()
        {
            events.Add(new EventItem
            {
                EventDateText = "February 14, 2022",
                EventName = "Food Bazaar",
                EventTimeText = "◷  5:00PM - 11:00PM"
            });

            events.Add(new EventItem
            {
                EventDateText = "February 22, 2022",
                EventName = "General Cleaning",
                EventTimeText = "◷  6:00AM Onwards"
            });

            Day14.Background = new SolidColorBrush(Color.FromRgb(209, 250, 229));
            Day22.Background = new SolidColorBrush(Color.FromRgb(209, 250, 229));
        }

        private void LoadEvents()
        {
            EventsList.ItemsSource = null;
            EventsList.ItemsSource = events;
        }

        private void AddEvent_Click(object sender, RoutedEventArgs e)
        {
            if (!dpEventDate.SelectedDate.HasValue ||
                string.IsNullOrWhiteSpace(txtEventTime.Text) ||
                string.IsNullOrWhiteSpace(txtEventName.Text))
            {
                MessageBox.Show("Please complete all event fields.");
                return;
            }

            string eventDateText =
                dpEventDate.SelectedDate.Value.ToString("MMMM dd, yyyy");

            events.Add(new EventItem
            {
                EventDateText = eventDateText,
                EventName = txtEventName.Text.Trim(),
                EventTimeText = "◷  " + txtEventTime.Text.Trim()
            });

            HighlightCalendarDay(dpEventDate.SelectedDate.Value.Day);

            LoadEvents();

            dpEventDate.SelectedDate = null;
            txtEventTime.Clear();
            txtEventName.Clear();

            MessageBox.Show("Event added successfully!");
        }

        private void HighlightCalendarDay(int day)
        {
            if (day == 14)
                Day14.Background = new SolidColorBrush(Color.FromRgb(209, 250, 229));

            if (day == 22)
                Day22.Background = new SolidColorBrush(Color.FromRgb(209, 250, 229));
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            Dashboard dashboard = new Dashboard();
            dashboard.Show();

            Close();
        }
    }

    public class EventItem
    {
        public string EventDateText { get; set; } = "";
        public string EventName { get; set; } = "";
        public string EventTimeText { get; set; } = "";
    }
}
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using calendarrrrrrrrrr.Data;
using calendarrrrrrrrrr.Models;

namespace calendarrrrrrrrrr
{
    public partial class Events : Window
    {
        private DateTime currentMonth = DateTime.Now;

        public Events()
        {
            InitializeComponent();

            DatabaseService.Initialize();
            LoadEvents();
        }

        private void LoadEvents()
        {
            var events = DatabaseService.GetAllEvents();

            EventsList.ItemsSource = null;
            EventsList.ItemsSource = events;

            LoadCalendar();
        }

        private void LoadCalendar()
        {
            CalendarDaysPanel.Children.Clear();

            txtMonthTitle.Text = currentMonth.ToString("MMMM yyyy");

            DateTime firstDay =
                new DateTime(currentMonth.Year, currentMonth.Month, 1);

            int daysInMonth =
                DateTime.DaysInMonth(currentMonth.Year, currentMonth.Month);

            int startOffset =
                ((int)firstDay.DayOfWeek + 6) % 7;

            string[] weekDays =
            {
        "Mon","Tue","Wed","Thu","Fri","Sat","Sun"
    };

            // WEEKDAY HEADERS
            foreach (string dayName in weekDays)
            {
                TextBlock header = new TextBlock
                {
                    Text = dayName,
                    FontWeight = FontWeights.Bold,
                    FontSize = 15,
                    Foreground = new SolidColorBrush(Color.FromRgb(91, 91, 234)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 15)
                };

                CalendarDaysPanel.Children.Add(header);
            }

            // EMPTY SPACES
            for (int i = 0; i < startOffset; i++)
            {
                CalendarDaysPanel.Children.Add(new Border());
            }

            var events = DatabaseService.GetAllEvents();

            // DAYS
            for (int day = 1; day <= daysInMonth; day++)
            {
                DateTime date =
                    new DateTime(currentMonth.Year, currentMonth.Month, day);

                bool hasEvent =
                    events.Exists(e => e.EventDate.Date == date.Date);

                Border border = new Border
                {
                    Width = 55,
                    Height = 55,
                    CornerRadius = new CornerRadius(12),
                    Background = hasEvent
                        ? new SolidColorBrush(Color.FromRgb(209, 250, 229))
                        : Brushes.Transparent,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                StackPanel stack = new StackPanel
                {
                    VerticalAlignment = VerticalAlignment.Center
                };

                TextBlock txtDay = new TextBlock
                {
                    Text = day.ToString(),
                    FontSize = 15,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = Brushes.Black,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextAlignment = TextAlignment.Center
                };

                stack.Children.Add(txtDay);

                if (hasEvent)
                {
                    stack.Children.Add(new TextBlock
                    {
                        Text = "•",
                        FontSize = 18,
                        FontWeight = FontWeights.Bold,
                        Foreground =
                            new SolidColorBrush(Color.FromRgb(74, 129, 80)),
                        HorizontalAlignment = HorizontalAlignment.Center
                    });
                }

                border.Child = stack;

                CalendarDaysPanel.Children.Add(border);
            }
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

            HotelEvent hotelEvent = new HotelEvent
            {
                EventDate = dpEventDate.SelectedDate.Value,
                EventTime = txtEventTime.Text.Trim(),
                EventName = txtEventName.Text.Trim(),
                Location = "Hotel Yncierto"
            };

            DatabaseService.AddEvent(hotelEvent);

            currentMonth = hotelEvent.EventDate;

            LoadEvents();

            dpEventDate.SelectedDate = null;
            txtEventTime.Clear();
            txtEventName.Clear();

            MessageBox.Show("Event added successfully!");
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            Dashboard dashboard = new Dashboard();
            dashboard.Show();
            Close();
        }

        private void PrevMonth_Click(object sender, RoutedEventArgs e)
        {
            currentMonth = currentMonth.AddMonths(-1);
            LoadCalendar();
        }

        private void NextMonth_Click(object sender, RoutedEventArgs e)
        {
            currentMonth = currentMonth.AddMonths(1);
            LoadCalendar();
        }
    }
}
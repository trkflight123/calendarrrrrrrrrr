using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using calendarrrrrrrrrr.Data;
using calendarrrrrrrrrr.Models;
using HotelYnCierto;
using MahApps.Metro.IconPacks;

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

        private static string FormatDaySummary(int openCount, int bookedCount) =>
            $"{openCount} open · {bookedCount} booked";

        private static void SetDialogIcon(Window window)
        {
            try
            {
                window.Icon = new BitmapImage(new Uri("pack://application:,,,/Assets/logo.png", UriKind.Absolute));
            }
            catch
            {
            }
        }

        private static StackPanel IconText(PackIconMaterialKind kind, string text, double iconSize = 15)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal };
            panel.Children.Add(new PackIconMaterial
            {
                Kind = kind,
                Width = iconSize,
                Height = iconSize,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 6, 0)
            });
            panel.Children.Add(new TextBlock
            {
                Text = text,
                VerticalAlignment = VerticalAlignment.Center
            });
            return panel;
        }

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
            var dayRooms = GetRoomStatusForDate(date, filteredRooms);
            var counts = CountRoomStatuses(dayRooms);

            cell.Rooms = dayRooms;
            cell.AvailableCount = counts.available;
            cell.OccupiedCount = counts.occupied;
            ApplyDayVisualStyle(cell, counts.available, counts.occupied);
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
                    var dayRooms = GetRoomStatusForDate(date, filteredRooms);
                    var (available, occupied) = CountRoomStatuses(dayRooms);

                    var dayVm = new CalendarDayViewModel
                    {
                        Date = date,
                        DayNumber = day.ToString(),
                        DayNumberColor = GetBrush("TextPrimaryBrush"),
                        IsCurrentMonth = true,
                        Rooms = dayRooms,
                        AvailableCount = available,
                        OccupiedCount = occupied
                    };
                    ApplyDayVisualStyle(dayVm, available, occupied);
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

        private static (int available, int occupied) CountRoomStatuses(
            ObservableCollection<RoomStatusViewModel> dayRooms) =>
            (
                dayRooms.Count(r => r.Status == "Available"),
                dayRooms.Count(r => r.Status == "Occupied" || r.Status == "Reserved")
            );

        private static int CountUnavailable(ObservableCollection<RoomStatusViewModel> dayRooms) =>
            dayRooms.Count(r => r.Status is "Maintenance" or "Cleaning" or "Unavailable");

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

        private void ApplyDayVisualStyle(CalendarDayViewModel day, int available, int occupied)
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
            var todayRooms = GetRoomStatusForDate(DateTime.Today, filteredRooms);
            var (availableCount, occupiedCount) = CountRoomStatuses(todayRooms);
            int unavailableCount = CountUnavailable(todayRooms);

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
                DayNumberColor = GetBrush("TextTertiaryBrush"),
                IsCurrentMonth = false,
                Rooms = new ObservableCollection<RoomStatusViewModel>(),
                AvailableCount = 0,
                OccupiedCount = 0
            };
            ApplyDayVisualStyle(day, 0, 0);
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
            ShowManageRoomsDialog();
        }

        private void ShowManageRoomsDialog()
        {
            var dialog = new Window
            {
                Title = "Manage Rooms",
                Width = 480,
                Height = 520,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                Background = GetBrush("SurfaceElevatedBrush"),
                ResizeMode = ResizeMode.CanResizeWithGrip
            };
            SetDialogIcon(dialog);

            var root = new Grid { Margin = new Thickness(20) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var intro = new TextBlock
            {
                Text = "Remove rooms that are demolished or no longer in use. Active bookings must be cleared first.",
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 12)
            };
            Grid.SetRow(intro, 0);
            root.Children.Add(intro);

            var list = new ListBox
            {
                BorderThickness = new Thickness(1),
                BorderBrush = GetBrush("DividerBrush")
            };
            Grid.SetRow(list, 1);

            void BindRooms()
            {
                list.Items.Clear();
                foreach (var room in allRooms.OrderBy(r => r.RoomNumber))
                {
                    var row = new Grid { Margin = new Thickness(4, 6, 4, 6) };
                    row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                    var info = new StackPanel();
                    info.Children.Add(new TextBlock
                    {
                        Text = $"Room {room.RoomNumber}",
                        FontWeight = FontWeights.SemiBold
                    });
                    info.Children.Add(new TextBlock
                    {
                        Text = $"{room.RoomType} · {room.Status}",
                        FontSize = 12,
                        Foreground = GetStatusColor(room.Status),
                        Margin = new Thickness(0, 2, 0, 0)
                    });
                    Grid.SetColumn(info, 0);
                    row.Children.Add(info);

                    var removeBtn = new Button
                    {
                        Content = IconText(PackIconMaterialKind.DeleteOutline, "Remove"),
                        Padding = new Thickness(12, 4, 12, 4),
                        Style = Application.Current.TryFindResource("SecondaryButtonStyle") as Style
                    };
                    removeBtn.Click += (_, _) =>
                    {
                        var confirm = MessageBox.Show(
                            $"Remove Room {room.RoomNumber} permanently?\n\nThis cannot be undone.",
                            "Remove room",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Warning);

                        if (confirm != MessageBoxResult.Yes)
                            return;

                        if (!DatabaseService.TryDeleteRoom(room.RoomId, out string error))
                        {
                            MessageBox.Show(error, "Cannot remove room",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        LoadData();
                        BindRooms();
                    };

                    Grid.SetColumn(removeBtn, 1);
                    row.Children.Add(removeBtn);
                    list.Items.Add(row);
                }
            }

            BindRooms();
            root.Children.Add(list);

            var closeBtn = new Button
            {
                Content = IconText(PackIconMaterialKind.Close, "Close"),
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 12, 0, 0),
                Padding = new Thickness(20, 8, 20, 8),
                Style = Application.Current.TryFindResource("PrimaryButtonStyle") as Style
            };
            closeBtn.Click += (_, _) => dialog.Close();
            Grid.SetRow(closeBtn, 2);
            root.Children.Add(closeBtn);

            dialog.Content = root;
            dialog.ShowDialog();
            RefreshCalendar();
        }

        private bool ConfirmRemoveRoom(RoomStatusViewModel room)
        {
            var confirm = MessageBox.Show(
                $"Remove Room {room.RoomNumber} permanently?\n\nUse this when the room is demolished or no longer exists.",
                "Remove room",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes)
                return false;

            if (!DatabaseService.TryDeleteRoom(room.RoomId, out string error))
            {
                MessageBox.Show(error, "Cannot remove room",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private void OpenBookingForRoom(CalendarDayViewModel day, RoomStatusViewModel room, Window dayDialog)
        {
            var booking = new ReservationWindow(day.Date, room.RoomNumber)
            {
                Owner = this
            };

            if (booking.ShowDialog() == true)
            {
                RefreshCalendar();
                dayDialog.Close();
            }
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

            ShowDayManagementDialog(day);
            e.Handled = true;
        }

        private void ShowDayManagementDialog(CalendarDayViewModel day)
        {
            var dialog = new Window
            {
                Title = day.Date.ToString("dddd, MMMM d, yyyy"),
                Width = 520,
                Height = 500,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                Background = GetBrush("SurfaceElevatedBrush"),
                ResizeMode = ResizeMode.NoResize
            };
            SetDialogIcon(dialog);

            var root = new Grid { Margin = new Thickness(20) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var titleRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };
            titleRow.Children.Add(new PackIconMaterial
            {
                Kind = PackIconMaterialKind.CalendarToday,
                Width = 22,
                Height = 22,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0),
                Foreground = GetBrush("GreenPrimaryBrush")
            });

            var summary = new TextBlock
            {
                Text = FormatDaySummary(day.AvailableCount, day.OccupiedCount),
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            };
            titleRow.Children.Add(summary);

            var hint = new TextBlock
            {
                Text = "Book an open room, clear a booking, or mark a room ready for guests.",
                TextWrapping = TextWrapping.Wrap,
                FontSize = 12,
                Foreground = Brushes.Gray,
                Margin = new Thickness(0, 0, 0, 12)
            };

            var headerPanel = new StackPanel();
            headerPanel.Children.Add(titleRow);
            headerPanel.Children.Add(hint);
            root.Children.Add(headerPanel);

            var list = new ListBox
            {
                BorderThickness = new Thickness(1),
                BorderBrush = GetBrush("DividerBrush"),
                Margin = new Thickness(0, 0, 0, 0)
            };
            Grid.SetRow(list, 1);

            void BindList()
            {
                LoadData();
                var filteredRooms = GetFilteredRooms();
                var dayRooms = GetRoomStatusForDate(day.Date, filteredRooms);
                var counts = CountRoomStatuses(dayRooms);

                day.Rooms = dayRooms;
                day.AvailableCount = counts.available;
                day.OccupiedCount = counts.occupied;
                summary.Text = FormatDaySummary(counts.available, counts.occupied);
                ApplyDayVisualStyle(day, counts.available, counts.occupied);
                SyncCalendarDayCell(day.Date);

                list.Items.Clear();
                foreach (var room in day.Rooms.OrderBy(r => r.RoomNumber))
                {
                    var row = new Grid { Margin = new Thickness(4, 8, 4, 8) };
                    row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                    var info = new StackPanel { Margin = new Thickness(0, 0, 8, 0) };
                    info.Children.Add(new TextBlock
                    {
                        Text = $"Room {room.RoomNumber}",
                        FontWeight = FontWeights.SemiBold
                    });
                    info.Children.Add(new TextBlock
                    {
                        Text = $"{room.RoomType} · {room.Status}",
                        FontSize = 12,
                        Foreground = room.StatusColor,
                        Margin = new Thickness(0, 2, 0, 0)
                    });
                    Grid.SetColumn(info, 0);
                    row.Children.Add(info);

                    bool canBook = room.Status == "Available";
                    bool canRelease = room.Status is "Occupied" or "Reserved";
                    bool canMarkAvailable = room.Status is "Maintenance" or "Cleaning" or "Unavailable";

                    var actions = new WrapPanel
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        MaxWidth = 280
                    };

                    if (canBook)
                    {
                        var bookBtn = new Button
                        {
                            Content = IconText(PackIconMaterialKind.BookPlus, "Book"),
                            Padding = new Thickness(10, 4, 10, 4),
                            Margin = new Thickness(0, 0, 6, 6),
                            Style = Application.Current.TryFindResource("PrimaryButtonStyle") as Style
                        };
                        bookBtn.Click += (_, _) => OpenBookingForRoom(day, room, dialog);
                        actions.Children.Add(bookBtn);
                    }

                    if (canRelease || canMarkAvailable)
                    {
                        if (canRelease)
                        {
                            var releaseBtn = new Button
                            {
                                Content = IconText(PackIconMaterialKind.CalendarRemove, "Clear booking"),
                                Padding = new Thickness(10, 4, 10, 4),
                                Margin = new Thickness(0, 0, 6, 6),
                                Tag = room,
                                Style = Application.Current.TryFindResource("SecondaryButtonStyle") as Style
                            };
                            releaseBtn.Click += (_, _) =>
                            {
                                if (ReleaseOccupancyForDate(room, day.Date))
                                    BindList();
                            };
                            actions.Children.Add(releaseBtn);
                        }

                        if (canMarkAvailable)
                        {
                            var availableBtn = new Button
                            {
                                Content = IconText(PackIconMaterialKind.BedOutline, "Ready for guests"),
                                Padding = new Thickness(10, 4, 10, 4),
                                Margin = new Thickness(0, 0, 6, 6),
                                Tag = room,
                                Style = Application.Current.TryFindResource("SecondaryButtonStyle") as Style
                            };
                            availableBtn.Click += (_, _) =>
                            {
                                if (SetRoomAvailable(room))
                                    BindList();
                            };
                            actions.Children.Add(availableBtn);
                        }
                    }

                    var removeBtn = new Button
                    {
                        Content = IconText(PackIconMaterialKind.DeleteOutline, "Remove"),
                        Padding = new Thickness(8, 4, 8, 4),
                        Margin = new Thickness(0, 0, 0, 6),
                        Style = Application.Current.TryFindResource("SecondaryButtonStyle") as Style
                    };
                    removeBtn.Click += (_, _) =>
                    {
                        if (ConfirmRemoveRoom(room))
                        {
                            BindList();
                            if (allRooms.Count == 0)
                                dialog.Close();
                        }
                    };
                    actions.Children.Add(removeBtn);

                    Grid.SetColumn(actions, 1);
                    row.Children.Add(actions);

                    list.Items.Add(row);
                }
            }

            BindList();
            root.Children.Add(list);

            var closeBtn = new Button
            {
                Content = IconText(PackIconMaterialKind.Close, "Close"),
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 12, 0, 0),
                Padding = new Thickness(20, 8, 20, 8),
                Style = Application.Current.TryFindResource("PrimaryButtonStyle") as Style
            };
            closeBtn.Click += (_, _) => dialog.Close();
            Grid.SetRow(closeBtn, 2);
            root.Children.Add(closeBtn);

            dialog.Content = root;
            dialog.ShowDialog();
        }

        private bool SetRoomAvailable(RoomStatusViewModel roomStatus)
        {
            if (roomStatus.Status is not ("Maintenance" or "Cleaning" or "Unavailable"))
                return false;

            try
            {
                DatabaseService.UpdateRoomStatus(roomStatus.RoomId, "Available");
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not update room: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return false;
            }
        }

        private bool ReleaseOccupancyForDate(RoomStatusViewModel roomStatus, DateTime date)
        {
            try
            {
                if (roomStatus.Reservation != null)
                {
                    var res = roomStatus.Reservation;
                    var checkIn = res.CheckIn.Date;
                    var checkOut = res.CheckOut.Date;

                    if (checkOut <= checkIn)
                    {
                        DatabaseService.UpdateReservationStatus(res.ReservationId, "Cancelled");
                    }
                    else if (checkIn == date && checkOut == date.AddDays(1))
                    {
                        DatabaseService.UpdateReservationStatus(res.ReservationId, "Cancelled");
                    }
                    else if (checkIn == date)
                    {
                        res.CheckIn = date.AddDays(1);
                        DatabaseService.UpdateReservation(res);
                    }
                    else if (checkOut == date.AddDays(1))
                    {
                        res.CheckOut = date;
                        DatabaseService.UpdateReservation(res);
                    }
                    else
                    {
                        var confirm = MessageBox.Show(
                            $"Room {roomStatus.RoomNumber} is booked {checkIn:d} – {checkOut:d}.\n\n" +
                            $"Releasing {date:d} will cancel the entire reservation. Continue?",
                            "Release room",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);

                        if (confirm != MessageBoxResult.Yes)
                            return false;

                        DatabaseService.UpdateReservationStatus(res.ReservationId, "Cancelled");
                    }
                }
                else if (roomStatus.Status is "Occupied" or "Reserved")
                {
                    DatabaseService.UpdateRoomStatus(roomStatus.RoomId, "Available");
                }
                else
                {
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not release room: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return false;
            }
        }
    }

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
    }
}
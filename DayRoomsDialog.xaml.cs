using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using calendarrrrrrrrrr.Data;
using calendarrrrrrrrrr.Models;
using HotelYnCierto;

namespace calendarrrrrrrrrr
{
    public partial class DayRoomsDialog : Window
    {
        private readonly CalendarDayViewModel _day;
        private readonly string _roomTypeFilter;
        private readonly Action _syncCalendarDay;
        private readonly ObservableCollection<RoomStatusViewModel> _rooms = new();

        private List<Room> _allRooms = new();
        private List<Reservation> _allReservations = new();

        public bool CalendarChanged { get; private set; }

        public DayRoomsDialog(CalendarDayViewModel day, string roomTypeFilter, Action syncCalendarDay)
        {
            _day = day;
            _roomTypeFilter = roomTypeFilter;
            _syncCalendarDay = syncCalendarDay;

            InitializeComponent();
            Title = day.Date.ToString("dddd, MMMM d, yyyy");
            RoomsList.ItemsSource = _rooms;
            RefreshList();
        }

        private void LoadData()
        {
            _allRooms = DatabaseService.GetAllRooms();
            _allReservations = DatabaseService.GetAllReservations();
        }

        private List<Room> GetFilteredRooms() =>
            _roomTypeFilter is "All" or "All Rooms"
                ? _allRooms
                : _allRooms.Where(r => r.RoomType.Contains(_roomTypeFilter)).ToList();

        private void RefreshList()
        {
            LoadData();
            var filteredRooms = GetFilteredRooms();
            var dayRooms = CalendarRoomHelper.GetRoomStatusForDate(_day.Date, filteredRooms, _allReservations);
            var counts = CalendarRoomHelper.CountRoomStatuses(dayRooms);

            _day.Rooms = dayRooms;
            _day.AvailableCount = counts.available;
            _day.OccupiedCount = counts.occupied;
            TxtSummary.Text = CalendarRoomHelper.FormatDaySummary(counts.available, counts.occupied);
            CalendarRoomHelper.ApplyDayVisualStyle(_day, counts.available, counts.occupied);

            _rooms.Clear();
            foreach (var room in dayRooms.OrderBy(r => r.RoomNumber))
                _rooms.Add(room);

            _syncCalendarDay();
        }

        private void Book_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement { Tag: RoomStatusViewModel room })
                return;

            var booking = new ReservationWindow(_day.Date, room.RoomNumber) { Owner = Owner };
            if (booking.ShowDialog() == true)
            {
                CalendarChanged = true;
                RefreshList();
                Close();
            }
        }

        private void ClearBooking_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement { Tag: RoomStatusViewModel room })
                return;

            if (ReleaseOccupancyForDate(room, _day.Date))
            {
                CalendarChanged = true;
                RefreshList();
            }
        }

        private void ReadyForGuests_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement { Tag: RoomStatusViewModel room })
                return;

            if (room.Status is not ("Maintenance" or "Cleaning" or "Unavailable"))
                return;

            try
            {
                DatabaseService.UpdateRoomStatus(room.RoomId, "Available");
                CalendarChanged = true;
                RefreshList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not update room: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RemoveRoom_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement { Tag: RoomStatusViewModel room })
                return;

            var confirm = MessageBox.Show(
                $"Remove Room {room.RoomNumber} permanently?\n\nUse this when the room is demolished or no longer exists.",
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

            CalendarChanged = true;
            RefreshList();

            if (_allRooms.Count == 0)
                Close();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();

        private static bool ReleaseOccupancyForDate(RoomStatusViewModel roomStatus, DateTime date)
        {
            try
            {
                if (roomStatus.Reservation != null)
                {
                    var res = roomStatus.Reservation;
                    var checkIn = res.CheckIn.Date;
                    var checkOut = res.CheckOut.Date;

                    if (checkOut <= checkIn || (checkIn == date && checkOut == date.AddDays(1)))
                        DatabaseService.UpdateReservationStatus(res.ReservationId, "Cancelled");
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
                MessageBox.Show($"Could not release room: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
    }
}

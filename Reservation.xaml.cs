using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using calendarrrrrrrrrr.Data;
using calendarrrrrrrrrr.Models;
using System.Linq;

namespace HotelYnCierto
{
    public partial class ReservationWindow : Window
    {
        private List<Room> availableRooms = new();

        public ReservationWindow()
        {
            InitializeComponent();

            DatabaseService.Initialize();
            LoadAvailableRooms();
        }

        private void LoadAvailableRooms()
        {
            if (dpCheckIn.SelectedDate.HasValue && dpCheckOut.SelectedDate.HasValue)
            {
                availableRooms = DatabaseService.GetAvailableRooms(
                    dpCheckIn.SelectedDate.Value,
                    dpCheckOut.SelectedDate.Value);
            }
            else
            {
                availableRooms = DatabaseService.GetAllRooms();
            }

            if (cmbRoomType.SelectedItem is ComboBoxItem selectedType)
            {
                string roomType = selectedType.Content.ToString();

                availableRooms = availableRooms
             .Where(r => r.RoomType.Contains(roomType))
             .ToList();
            }

            cmbAvailable.ItemsSource = availableRooms;
            cmbAvailable.SelectedIndex = -1;
            txtRoomRate.Clear();
        }

        private void DatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadAvailableRooms();
        }

        private void cmbRoomType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadAvailableRooms();
        }

        private void cmbAvailable_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbAvailable.SelectedItem is Room selectedRoom)
            {
                txtRoomRate.Text = selectedRoom.PricePerNight.ToString("₱#,##0.00");
            }
        }

        private void BtnBook_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtFirstName.Text) ||
                    string.IsNullOrWhiteSpace(txtLastName.Text) ||
                    string.IsNullOrWhiteSpace(txtPhone.Text) ||
                    !dpCheckIn.SelectedDate.HasValue ||
                    !dpCheckOut.SelectedDate.HasValue ||
                    cmbRoomType.SelectedItem == null ||
                    cmbAvailable.SelectedItem == null)
                {
                    MessageBox.Show("Please complete all required fields.");
                    return;
                }

                if (dpCheckOut.SelectedDate.Value <= dpCheckIn.SelectedDate.Value)
                {
                    MessageBox.Show("Check-out date must be after check-in date.");
                    return;
                }

                Room selectedRoom = (Room)cmbAvailable.SelectedItem;

                bool hasConflict = DatabaseService.HasConflict(
                    selectedRoom.RoomId,
                    dpCheckIn.SelectedDate.Value,
                    dpCheckOut.SelectedDate.Value);

                if (hasConflict)
                {
                    MessageBox.Show("This room is already booked for the selected dates.");
                    LoadAvailableRooms();
                    return;
                }

                Guest guest = new Guest
                {
                    FirstName = txtFirstName.Text.Trim(),
                    LastName = txtLastName.Text.Trim(),
                    Phone = txtPhone.Text.Trim(),
                    CreatedAt = DateTime.Now
                };

                int guestId = DatabaseService.AddGuest(guest);

                int nights = (dpCheckOut.SelectedDate.Value - dpCheckIn.SelectedDate.Value).Days;

                Reservation reservation = new Reservation
                {
                    GuestId = guestId,
                    RoomId = selectedRoom.RoomId,
                    CheckIn = dpCheckIn.SelectedDate.Value,
                    CheckOut = dpCheckOut.SelectedDate.Value,
                    GuestCount = 1,
                    Status = "Confirmed",
                    TotalAmount = selectedRoom.PricePerNight * nights,
                    CreatedAt = DateTime.Now
                };

                DatabaseService.AddReservation(reservation);

                MessageBox.Show("Reservation saved successfully!");

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Save Error");
            }
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            ClearInputs(this);
            LoadAvailableRooms();
        }

        private void ClearInputs(DependencyObject parent)
        {
            foreach (var child in LogicalTreeHelper.GetChildren(parent))
            {
                if (child is TextBox textBox)
                    textBox.Clear();

                else if (child is ComboBox comboBox)
                    comboBox.SelectedIndex = -1;

                else if (child is DatePicker datePicker)
                    datePicker.SelectedDate = null;

                else if (child is DependencyObject depObj)
                    ClearInputs(depObj);
            }
        }
    }
}
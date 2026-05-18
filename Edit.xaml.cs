using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using calendarrrrrrrrrr.Data;
using calendarrrrrrrrrr.Models;

namespace calendarrrrrrrrrr
{
    public partial class Edit : Window
    {
        private Reservation reservation;

        public Edit(Reservation selectedReservation)
        {
            InitializeComponent();

            reservation = selectedReservation;

            DatabaseService.Initialize();
            LoadData();
        }

        private void LoadData()
        {
            var rooms = DatabaseService.GetAllRooms();
            cmbRoom.ItemsSource = rooms;
            cmbRoom.SelectedItem = rooms.FirstOrDefault(r => r.RoomId == reservation.RoomId);

            string[] names = reservation.GuestName.Split(' ');

            txtFirstName.Text = names.Length > 0 ? names[0] : "";
            txtLastName.Text = names.Length > 1 ? string.Join(" ", names.Skip(1)) : "";
            txtPhone.Text = reservation.Phone;
            txtEmail.Text = reservation.Email;
            txtAddress.Text = reservation.Address;

            dpCheckIn.SelectedDate = reservation.CheckIn;
            dpCheckOut.SelectedDate = reservation.CheckOut;

            foreach (ComboBoxItem item in cmbStatus.Items)
            {
                if (item.Content.ToString() == reservation.Status)
                {
                    cmbStatus.SelectedItem = item;
                    break;
                }
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!dpCheckIn.SelectedDate.HasValue ||
                    !dpCheckOut.SelectedDate.HasValue ||
                    cmbRoom.SelectedItem == null ||
                    cmbStatus.SelectedItem == null)
                {
                    MessageBox.Show("Please complete all fields.");
                    return;
                }

                if (dpCheckOut.SelectedDate.Value <= dpCheckIn.SelectedDate.Value)
                {
                    MessageBox.Show("Check-out date must be after check-in date.");
                    return;
                }

                Room selectedRoom = (Room)cmbRoom.SelectedItem;

                bool hasConflict = DatabaseService.HasConflict(
                    selectedRoom.RoomId,
                    dpCheckIn.SelectedDate.Value,
                    dpCheckOut.SelectedDate.Value,
                    reservation.ReservationId);

                if (hasConflict)
                {
                    MessageBox.Show("This room is already booked for the selected dates.");
                    return;
                }

                Guest guest = new Guest
                {
                    GuestId = reservation.GuestId,
                    FirstName = txtFirstName.Text.Trim(),
                    LastName = txtLastName.Text.Trim(),
                    Phone = txtPhone.Text.Trim(),
                    Email = txtEmail.Text.Trim(),
                    Address = txtAddress.Text.Trim()
                };

                DatabaseService.UpdateGuest(guest);

                int nights = (dpCheckOut.SelectedDate.Value - dpCheckIn.SelectedDate.Value).Days;

                reservation.RoomId = selectedRoom.RoomId;
                reservation.CheckIn = dpCheckIn.SelectedDate.Value;
                reservation.CheckOut = dpCheckOut.SelectedDate.Value;
                reservation.Status = (cmbStatus.SelectedItem as ComboBoxItem)?.Content.ToString();
                reservation.TotalAmount = selectedRoom.PricePerNight * nights;

                DatabaseService.UpdateReservation(reservation);

                MessageBox.Show("Reservation updated successfully!");

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Update Error");
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
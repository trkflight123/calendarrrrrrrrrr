using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using calendarrrrrrrrrr.Data;
using calendarrrrrrrrrr.Models;

namespace calendarrrrrrrrrr
{
    public partial class GuestWindow : Window
    {
        private List<Reservation> reservations = new();

        public GuestWindow()
        {
            InitializeComponent();

            DatabaseService.Initialize();
            LoadGuests();
        }

        private void LoadGuests()
        {
            reservations = DatabaseService.GetAllReservations();
            GuestGrid.ItemsSource = reservations;
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            string keyword = txtSearch.Text.ToLower();

            GuestGrid.ItemsSource = reservations
                .Where(r =>
                    r.GuestName.ToLower().Contains(keyword) ||
                    r.Phone.ToLower().Contains(keyword) ||
                    r.Email.ToLower().Contains(keyword) ||
                    r.Address.ToLower().Contains(keyword) ||
                    r.RoomNumber.ToLower().Contains(keyword) ||
                    r.Status.ToLower().Contains(keyword))
                .ToList();
        }

        private void CheckIn_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is Reservation reservation)
            {
                DatabaseService.UpdateReservationStatus(reservation.ReservationId, "Checked In");
                LoadGuests();
            }
        }

        private void CheckOut_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is Reservation reservation)
            {
                DatabaseService.UpdateReservationStatus(reservation.ReservationId, "Checked Out");
                LoadGuests();
            }
        }
            
        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is Reservation reservation)
            {
                Edit editWindow = new Edit(reservation);
                editWindow.ShowDialog();

                LoadGuests();
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is Reservation reservation)
            {
                MessageBoxResult result = MessageBox.Show(
                    "Are you sure you want to cancel this reservation?",
                    "Cancel Reservation",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    DatabaseService.UpdateReservationStatus(reservation.ReservationId, "Cancelled");
                    LoadGuests();
                }
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            Dashboard dashboard = new Dashboard();
            dashboard.Show();

            this.Hide();
        }

        private void GuestGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (GuestGrid.SelectedItem is Reservation reservation)
            {
                MessageBox.Show(
                    $"Guest Name: {reservation.GuestName}\n\n" +
                    $"Phone Number: {reservation.Phone}\n" +
                    $"Email: {reservation.Email}\n" +
                    $"Address: {reservation.Address}\n\n" +
                    $"Room Number: {reservation.RoomNumber}\n" +
                    $"Room Type: {reservation.RoomType}\n\n" +
                    $"Check In: {reservation.CheckIn:MMMM dd, yyyy}\n" +
                    $"Check Out: {reservation.CheckOut:MMMM dd, yyyy}\n" +
                    $"Status: {reservation.Status}\n" +
                    $"Total Amount: ₱{reservation.TotalAmount:N2}",
                    "Reservation Details",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
        }
    }
}
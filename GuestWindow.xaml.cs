using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Windows;
using System.Windows.Controls;
using calendarrrrrrrrrr.Data;
using calendarrrrrrrrrr.Models;

namespace calendarrrrrrrrrr
{
    public partial class GuestWindow : Window
    {
        private List<Reservation> reservations = new();

        private Reservation selectedReservationForCancellation;

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
                    (r.GuestName ?? "").ToLower().Contains(keyword) ||
                    (r.Phone ?? "").ToLower().Contains(keyword) ||
                    (r.Email ?? "").ToLower().Contains(keyword) ||
                    (r.Address ?? "").ToLower().Contains(keyword) ||
                    (r.RoomNumber ?? "").ToLower().Contains(keyword) ||
                    (r.Status ?? "").ToLower().Contains(keyword))
                .ToList();
        }

        private void CheckIn_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is Reservation reservation)
            {
                DateTime today = DateTime.Today;

                if (reservation.CheckIn.Date != today)
                {
                    MessageBox.Show(
                        $"This guest cannot check in today.\n\n" +
                        $"Booked check-in date: {reservation.CheckIn:MMMM dd, yyyy}\n" +
                        $"Today: {today:MMMM dd, yyyy}",
                        "Check-in Not Allowed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                if (reservation.Status == "Checked In")
                {
                    MessageBox.Show(
                        "This guest is already checked in.",
                        "Already Checked In",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    return;
                }

                if (reservation.Status == "Cancelled")
                {
                    MessageBox.Show(
                        "Cancelled reservations cannot be checked in.",
                        "Check-in Not Allowed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                DatabaseService.UpdateReservationStatus(reservation.ReservationId, "Checked In");
                DatabaseService.UpdateRoomStatus(reservation.RoomId, "Occupied");

                LoadGuests();
            }
        }

        private void CheckOut_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is Reservation reservation)
            {
                DateTime today = DateTime.Today;

                if (reservation.CheckOut.Date != today)
                {
                    MessageBox.Show(
                        $"This guest cannot check out today.\n\n" +
                        $"Booked check-out date: {reservation.CheckOut:MMMM dd, yyyy}\n" +
                        $"Today: {today:MMMM dd, yyyy}",
                        "Check-out Not Allowed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                if (reservation.Status != "Checked In")
                {
                    MessageBox.Show(
                        "Only checked-in guests can be checked out.",
                        "Check-out Not Allowed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                DatabaseService.UpdateReservationStatus(reservation.ReservationId, "Checked Out");
                DatabaseService.UpdateRoomStatus(reservation.RoomId, "Available");

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
                if (reservation.Status == "Cancelled")
                {
                    MessageBox.Show(
                        "This reservation is already cancelled.",
                        "Already Cancelled",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    return;
                }

                if (reservation.Status == "Checked In")
                {
                    MessageBox.Show(
                        "Checked-in guests cannot be cancelled. Please check out the guest first.",
                        "Cancellation Not Allowed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                if (reservation.Status == "Checked Out")
                {
                    MessageBox.Show(
                        "Checked-out reservations cannot be cancelled.",
                        "Cancellation Not Allowed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                selectedReservationForCancellation = reservation;
                txtCancellationReason.Clear();
                CancellationOverlay.Visibility = Visibility.Visible;
            }
        }

        private void CloseCancellationOverlay_Click(object sender, RoutedEventArgs e)
        {
            txtCancellationReason.Clear();
            selectedReservationForCancellation = null;
            CancellationOverlay.Visibility = Visibility.Collapsed;
        }

        private void ConfirmCancellation_Click(object sender, RoutedEventArgs e)
        {
            if (selectedReservationForCancellation == null)
            {
                MessageBox.Show(
                    "No reservation selected.",
                    "Cancellation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            string reason = txtCancellationReason.Text.Trim();

            if (string.IsNullOrWhiteSpace(reason))
            {
                MessageBox.Show(
                    "Please enter the reason for cancellation.",
                    "Reason Required",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            MessageBoxResult result = MessageBox.Show(
                "Are you sure you want to cancel this reservation?",
                "Confirm Cancellation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                Reservation reservation = selectedReservationForCancellation;

                DatabaseService.UpdateReservationStatus(reservation.ReservationId, "Cancelled");
                DatabaseService.UpdateRoomStatus(reservation.RoomId, "Available");

                try
                {
                    SendCancellationEmail(
                        reservation.Email,
                        reservation.GuestName,
                        reservation.RoomNumber,
                        reservation.RoomType,
                        reservation.CheckIn,
                        reservation.CheckOut,
                        reason
                    );

                    MessageBox.Show(
                        "Reservation cancelled and cancellation email sent!",
                        "Cancellation Successful",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                catch (Exception emailEx)
                {
                    MessageBox.Show(
                        "Reservation cancelled, but cancellation email was not sent.\n\n" +
                        "Email error: " + emailEx.Message,
                        "Email Failed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }

                txtCancellationReason.Clear();
                selectedReservationForCancellation = null;
                CancellationOverlay.Visibility = Visibility.Collapsed;

                LoadGuests();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Cancellation failed: " + ex.Message,
                    "Cancellation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void SendCancellationEmail(
            string customerEmail,
            string customerName,
            string roomNumber,
            string roomType,
            DateTime checkIn,
            DateTime checkOut,
            string reason)
        {
            string senderEmail = "k.karldonayre05@gmail.com";

            //here, you need to generate an app password for your Gmail account and use it instead of your regular password.
            string appPassword = "qczs hacb idho rfru";

            MailMessage mail = new MailMessage();
            mail.From = new MailAddress(senderEmail, "Hotel Yncierto");
            mail.To.Add(customerEmail);
            mail.Subject = "Hotel Yncierto Booking Cancellation Notice";

            mail.Body =
$@"Dear {customerName},

Greetings from Hotel Yncierto.

We would like to inform you that your room reservation has been cancelled.

Below are the details of the cancelled booking:

Room Type: {roomType}
Room Number: {roomNumber}
Check-In Date: {checkIn:MMMM dd, yyyy} at 3:00 PM
Check-Out Date: {checkOut:MMMM dd, yyyy} at 10:00 AM

Reason for Cancellation:
{reason}

We sincerely apologize for any inconvenience this may have caused. If you have any questions or concerns, please feel free to contact Hotel Yncierto for further assistance.

Thank you for your understanding.

Best regards,

Hotel Yncierto
Reservations Team";

            SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587);
            smtp.Credentials = new NetworkCredential(senderEmail, appPassword);
            smtp.EnableSsl = true;

            smtp.Send(mail);
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
using System.Windows;
using System.Windows.Controls;

namespace calendarrrrrrrrrr.Views
{
    public partial class DashboardView : Page
    {
        public DashboardView()
        {
            InitializeComponent();
        }

        private void BtnNewBooking_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Open new booking dialog
            MessageBox.Show("New Booking dialog coming soon!", "New Booking",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}

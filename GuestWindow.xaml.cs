using System.Windows;
using calendarrrrrrrrrr.Data;

namespace calendarrrrrrrrrr
{
    public partial class GuestWindow : Window
    {
        public GuestWindow()
        {
            InitializeComponent();

            DatabaseService.Initialize();
            LoadGuests();
        }

        private void LoadGuests()
        {
            GuestGrid.ItemsSource = DatabaseService.GetAllGuests();
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            Dashboard dashboard = new Dashboard();
            dashboard.Show();

            Close();

        }
    }
}
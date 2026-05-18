using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using calendarrrrrrrrrr.Data;
using calendarrrrrrrrrr.Models;

namespace calendarrrrrrrrrr
{
    public partial class FoundItems : Window
    {
        private List<FoundItem> foundItems = new();

        public FoundItems()
        {
            InitializeComponent();

            DatabaseService.Initialize();

            LoadFoundItems();
        }

        private void LoadFoundItems()
        {
            foundItems = DatabaseService.GetAllFoundItems();

            FoundItemsGrid.ItemsSource = null;
            FoundItemsGrid.ItemsSource = foundItems;
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            Dashboard dashboard = new Dashboard();
            dashboard.Show();

            this.Hide();
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            txtRoom.Clear();
            txtLastName.Clear();
            txtMI.Clear();
            txtFirstName.Clear();
            txtPhone.Clear();
            txtItemsFound.Clear();

            dpCheckIn.SelectedDate = null;
            dpCheckOut.SelectedDate = null;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtRoom.Text) ||
                string.IsNullOrWhiteSpace(txtLastName.Text) ||
                string.IsNullOrWhiteSpace(txtFirstName.Text) ||
                string.IsNullOrWhiteSpace(txtPhone.Text) ||
                string.IsNullOrWhiteSpace(txtItemsFound.Text) ||
                !dpCheckIn.SelectedDate.HasValue ||
                !dpCheckOut.SelectedDate.HasValue)
            {
                MessageBox.Show(
                    "Please complete all required fields.",
                    "Missing Information",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            string guestName =
                $"{txtFirstName.Text} {txtLastName.Text}".Trim();

            FoundItem item = new FoundItem
            {
                RoomNumber = txtRoom.Text.Trim(),
                GuestName = guestName,
                ItemName = txtItemsFound.Text.Trim(),
                Status = "Unclaimed"
            };

            DatabaseService.AddFoundItem(item);

            LoadFoundItems();

            MessageBox.Show(
                "Found item saved successfully!",
                "Success",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            Clear_Click(null, null);
        }

        private void Claimed_Click(object sender, RoutedEventArgs e)
        {
            if (FoundItemsGrid.SelectedItem is FoundItem selectedItem)
            {
                DatabaseService.MarkFoundItemClaimed(selectedItem.FoundItemId);

                MessageBox.Show(
                    "Item claimed.",
                    "Claimed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                LoadFoundItems();
            }
            else
            {
                MessageBox.Show(
                    "Please select an item first.",
                    "No Item Selected",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void FoundItemsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FoundItemsGrid.SelectedItem is FoundItem selectedItem)
            {
                txtRoom.Text = selectedItem.RoomNumber;

                string[] names = selectedItem.GuestName.Split(' ');

                if (names.Length > 0)
                    txtFirstName.Text = names[0];

                if (names.Length > 1)
                    txtLastName.Text = names[1];

                txtItemsFound.Text = selectedItem.ItemName;
            }
        }
    }
}
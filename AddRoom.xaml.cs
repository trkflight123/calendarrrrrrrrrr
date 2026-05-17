using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using calendarrrrrrrrrr.Data;
using calendarrrrrrrrrr.Models;
using Microsoft.Win32;

namespace HotelYnCierto
{
    public partial class AddRoomWindow : Window
    {
        private string selectedPhotoPath = "";

        public AddRoomWindow()
        {
            InitializeComponent();
        }

        private void BtnUploadPhoto_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            openFileDialog.Filter =
                "Image Files|*.jpg;*.jpeg;*.png;*.bmp";

            if (openFileDialog.ShowDialog() == true)
            {
                string assetsFolder =
                    System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RoomImages");

                Directory.CreateDirectory(assetsFolder);

                string fileName = Guid.NewGuid().ToString() +
                                  System.IO.Path.GetExtension(openFileDialog.FileName);

                string destinationPath =
                    System.IO.Path.Combine(assetsFolder, fileName);

                File.Copy(openFileDialog.FileName, destinationPath, true);

                selectedPhotoPath = destinationPath;

                imgRoomPhoto.Source =
                    new BitmapImage(new Uri(destinationPath));
            }
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            ClearInputs(this);
            selectedPhotoPath = "";
        }

        private void ClearInputs(DependencyObject parent)
        {
            foreach (var child in LogicalTreeHelper.GetChildren(parent))
            {
                if (child is TextBox textBox)
                {
                    textBox.Clear();
                }
                else if (child is ComboBox comboBox)
                {
                    comboBox.SelectedIndex = -1;
                }
                else if (child is Image image)
                {
                    image.Source = null;
                }
                else if (child is DependencyObject depObj)
                {
                    ClearInputs(depObj);
                }
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Room room = new Room()
                {
                    RoomNumber = txtRoomNumber.Text,
                    RoomType = (cmbRoomType.SelectedItem as ComboBoxItem)?.Content.ToString(),
                    PricePerNight = decimal.Parse(txtRoomRate.Text),
                    Capacity = cmbCapacity.SelectedIndex + 1,
                    Status = (cmbStatus.SelectedItem as ComboBoxItem)?.Content.ToString(),
                    Floor = 1,
                    Description = "",
                    Amenities = "",
                    PhotoPath = selectedPhotoPath
                };

                DatabaseService.AddRoom(room);

                MessageBox.Show("Room added successfully!",
                                "Success",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }


    }
}
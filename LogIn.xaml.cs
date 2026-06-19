using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace calendarrrrrrrrrr
{
    public partial class LogIn : Window
    {
        public LogIn()
        {
            InitializeComponent();
        }

        private void TopBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            string username = txtUsername.Text;
            string password = isPasswordVisible ? txtPasswordVisible.Text : txtPassword.Password;

            if (username == "admin" && password == "admin123")
            {
                Dashboard dashboard = new Dashboard();
                dashboard.Show();
                Close();
            }
            else
            {
                MessageBox.Show("Invalid username or password",
                                "Login Failed",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        }

        private void txtUsername_GotFocus(object sender, RoutedEventArgs e)
        {
            if (txtUsername.Text == "Username")
            {
                txtUsername.Text = "";
                txtUsername.Foreground = Brushes.White;
            }
        }

        private void txtUsername_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtUsername.Text))
            {
                txtUsername.Text = "Username";
                txtUsername.Foreground = Brushes.Gray;
            }
        }

        private bool isPasswordVisible = false;

        private void btnShowPassword_Click(object sender, RoutedEventArgs e)
        {
            if (!isPasswordVisible)
            {
                txtPasswordVisible.Text = txtPassword.Password;
                txtPasswordVisible.Visibility = Visibility.Visible;
                txtPassword.Visibility = Visibility.Collapsed;
                btnShowPassword.Content = "🙈";
                isPasswordVisible = true;
            }
            else
            {
                txtPassword.Password = txtPasswordVisible.Text;
                txtPassword.Visibility = Visibility.Visible;
                txtPasswordVisible.Visibility = Visibility.Collapsed;
                btnShowPassword.Content = "👁";
                isPasswordVisible = false;
            }
        }

    }
}
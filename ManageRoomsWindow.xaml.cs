using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using calendarrrrrrrrrr.Data;

namespace calendarrrrrrrrrr
{
    public partial class ManageRoomsWindow : Window
    {
        private readonly ObservableCollection<ManageRoomItemViewModel> _rooms = new();

        public bool RoomsChanged { get; private set; }

        public ManageRoomsWindow()
        {
            InitializeComponent();
            RoomsList.ItemsSource = _rooms;
            LoadRooms();
        }

        private void LoadRooms()
        {
            _rooms.Clear();
            foreach (var room in DatabaseService.GetAllRooms().OrderBy(r => r.RoomNumber))
            {
                _rooms.Add(new ManageRoomItemViewModel
                {
                    RoomId = room.RoomId,
                    RoomNumber = room.RoomNumber,
                    RoomType = room.RoomType,
                    Status = room.Status,
                    StatusBrush = CalendarRoomHelper.GetStatusColor(room.Status)
                });
            }
        }

        private void RemoveRoom_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement { Tag: ManageRoomItemViewModel room })
                return;

            var confirm = MessageBox.Show(
                $"Remove Room {room.RoomNumber} permanently?\n\nThis cannot be undone.",
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

            RoomsChanged = true;
            LoadRooms();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
    }
}

using System.Data;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MySqlConnector;

namespace WpfApp1
{
    public partial class MainWindow : Window
    {
        private MySqlConnection _connection;

        public MainWindow()
        {
            InitializeComponent();
            _connection = new MySqlConnection("Server=localhost;User ID=root;Password=student;Database=hotel");
            _connection.Open();
            LoadRoomComboBoxes(); // Load rooms dynamically
        }

        private void LoadRoomComboBoxes()
        {
            try
            {
                var command = _connection.CreateCommand();
                command.CommandText = @"SELECT r.id, r.number, c.name 
                                      FROM rooms r 
                                      JOIN categories c ON r.category = c.id";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var item = new ComboBoxItem
                        {
                            Content = $"Номер {reader.GetInt32("number")} - {reader.GetString("name")}",
                            Tag = reader.GetInt32("id") // Store as int
                        };
                        roomComboBox.Items.Add(item);
                        adminRoomComboBox.Items.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки номеров: {ex.Message}", "Ошибка");
            }
        }

        // Guest Authorization
        private void GuestAuthButton_Click(object sender, RoutedEventArgs e)
        {
            string login = guestLoginTextBox.Text.Trim();
            string password = guestPasswordTextBox.Password.Trim();

            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Введите логин и пароль!", "Ошибка");
                return;
            }

            try
            {
                var command = _connection.CreateCommand();
                command.CommandText = "SELECT id FROM users WHERE login = @login AND password = @password AND access_level = 1";
                command.Parameters.AddWithValue("@login", login);
                command.Parameters.AddWithValue("@password", password);

                var result = command.ExecuteScalar();

                if (result != null)
                {
                    guestPanel.Visibility = Visibility.Visible;
                    authPanel.Visibility = Visibility.Collapsed;
                    MessageBox.Show("Успешная авторизация гостя!");
                }
                else
                {
                    MessageBox.Show("Неверный логин или пароль гостя");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка авторизации: {ex.Message}", "Ошибка");
            }
        }

        // Admin Authorization
        private void AdminAuthButton_Click(object sender, RoutedEventArgs e)
        {
            string login = adminLoginTextBox.Text.Trim();
            string password = adminPasswordTextBox.Password.Trim();

            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Введите логин и пароль!", "Ошибка");
                return;
            }

            try
            {
                var command = _connection.CreateCommand();
                command.CommandText = "SELECT id FROM users WHERE login = @login AND password = @password AND access_level IN (3,4)";
                command.Parameters.AddWithValue("@login", login);
                command.Parameters.AddWithValue("@password", password);

                var result = command.ExecuteScalar();

                if (result != null)
                {
                    adminPanel.Visibility = Visibility.Visible;
                    authPanel.Visibility = Visibility.Collapsed;
                    MessageBox.Show("Успешная авторизация администратора!");
                }
                else
                {
                    MessageBox.Show("Неверный логин или пароль администратора");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка авторизации: {ex.Message}", "Ошибка");
            }
        }

        // Guest Room Booking
        private void BookRoomButton_Click(object sender, RoutedEventArgs e)
        {
            if (roomComboBox.SelectedItem == null || !DateTime.TryParse(startDateTextBox.Text, out DateTime startDate) ||
                !DateTime.TryParse(endDateTextBox.Text, out DateTime endDate))
            {
                MessageBox.Show("Выберите номер и укажите даты!", "Ошибка");
                return;
            }

            try
            {
                var selectedItem = (ComboBoxItem)roomComboBox.SelectedItem;
                if (!int.TryParse(selectedItem.Tag.ToString(), out int roomId))
                {
                    MessageBox.Show("Неверный идентификатор номера!", "Ошибка");
                    return;
                }
                var guestId = GetGuestId(guestLoginTextBox.Text.Trim());

                var command = _connection.CreateCommand();
                command.CommandText = @"INSERT INTO reservations (guest, room, payment_status, start, end) 
                                      VALUES (@guest, @room, 1, @start, @end)";
                command.Parameters.AddWithValue("@guest", guestId);
                command.Parameters.AddWithValue("@room", roomId);
                command.Parameters.AddWithValue("@start", startDate);
                command.Parameters.AddWithValue("@end", endDate);

                command.ExecuteNonQuery();
                MessageBox.Show("Номер успешно забронирован!");
                UpdateAvailableRooms();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка бронирования: {ex.Message}", "Ошибка");
            }
        }

        // Admin Room Booking
        private void AdminBookRoomButton_Click(object sender, RoutedEventArgs e)
        {
            if (adminRoomComboBox.SelectedItem == null || string.IsNullOrWhiteSpace(adminGuestTextBox.Text) ||
                !DateTime.TryParse(adminStartDateTextBox.Text, out DateTime startDate) ||
                !DateTime.TryParse(adminEndDateTextBox.Text, out DateTime endDate))
            {
                MessageBox.Show("Заполните все поля!", "Ошибка");
                return;
            }

            try
            {
                var selectedItem = (ComboBoxItem)adminRoomComboBox.SelectedItem;
                if (!int.TryParse(selectedItem.Tag.ToString(), out int roomId))
                {
                    MessageBox.Show("Неверный идентификатор номера!", "Ошибка");
                    return;
                }
                var guestId = CreateOrGetGuest(adminGuestTextBox.Text.Trim());

                var command = _connection.CreateCommand();
                command.CommandText = @"INSERT INTO reservations (guest, room, payment_status, start, end) 
                                      VALUES (@guest, @room, 1, @start, @end)";
                command.Parameters.AddWithValue("@guest", guestId);
                command.Parameters.AddWithValue("@room", roomId);
                command.Parameters.AddWithValue("@start", startDate);
                command.Parameters.AddWithValue("@end", endDate);

                command.ExecuteNonQuery();
                MessageBox.Show("Номер успешно забронирован для гостя!");
                UpdateAvailableRooms();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка бронирования: {ex.Message}", "Ошибка");
            }
        }

        // View Available Rooms
        private void ViewAvailableRoomsButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateAvailableRooms();
        }

        private void UpdateAvailableRooms()
        {
            try
            {
                var command = _connection.CreateCommand();
                command.CommandText = @"SELECT r.id, r.number, c.name 
                                      FROM rooms r 
                                      JOIN categories c ON r.category = c.id 
                                      WHERE r.id NOT IN (
                                          SELECT room FROM reservations 
                                          WHERE CURDATE() BETWEEN start AND end
                                          OR start >= CURDATE()
                                      )";

                using (var adapter = new MySqlDataAdapter(command))
                {
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);
                    availableRoomsDataGrid.ItemsSource = dataTable.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при получении списка номеров: {ex.Message}", "Ошибка");
            }
        }

        private int GetGuestId(string login)
        {
            var command = _connection.CreateCommand();
            command.CommandText = "SELECT g.id FROM guests g JOIN users u ON u.id = g.id WHERE u.login = @login";
            command.Parameters.AddWithValue("@login", login);
            var result = command.ExecuteScalar();
            if (result == null)
                throw new Exception("Гость не найден");
            return Convert.ToInt32(result);
        }

        private int CreateOrGetGuest(string name)
        {
            var command = _connection.CreateCommand();
            command.CommandText = "SELECT id FROM guests WHERE name = @name";
            command.Parameters.AddWithValue("@name", name);
            var result = command.ExecuteScalar();

            if (result != null)
                return Convert.ToInt32(result);

            command.CommandText = "INSERT INTO guests (name) VALUES (@name); SELECT LAST_INSERT_ID();";
            return Convert.ToInt32(command.ExecuteScalar());
        }

        protected override void OnClosed(EventArgs e)
        {
            _connection?.Close();
            base.OnClosed(e);
        }
    }
}

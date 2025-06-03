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
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MySqlConnection _connection;

        public MainWindow()
        {
            InitializeComponent();

            _connection = new MySqlConnection("Server=localhost;User ID=root;Password=student;Database=hotel");
            _connection.Open();

        }

        public void AuthButton_Click(Object sender, RoutedEventArgs e)
        {
            string Login = loginTextBox.Text.Trim();
            string Password = passwordTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(Login)
                || string.IsNullOrWhiteSpace(Password))
            {
                MessageBox.Show("Введите логин и пароль!", "Ошибка");
                return;
            }

            try
            {
                var command = _connection.CreateCommand();
                command.CommandText = "SELECT COUNT(*) FROM users WHERE login = @login AND password = @password";
                command.Parameters.AddWithValue("login", Login);
                command.Parameters.AddWithValue("password", Password);

                var result = (long)command.ExecuteScalar();

                if (result > 0)

                    MessageBox.Show("Успешная авторизация!");
                else
                    MessageBox.Show("Неверный логин или пароль");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось подключиться к базе данных!" + ex.Message, "База данных");
            }
        }

        public void RefillButton_Click(Object sender, RoutedEventArgs e)
        {
            try
            {
                //var deleteCommand = _connection.CreateCommand();
                //deleteCommand.CommandText = "DELETE from users";
                //deleteCommand.ExecuteNonQuery();

                var command = _connection.CreateCommand();
                command.CommandText = @"INSERT INTO users (login, password) VALUES
                    ('admin','super'),
                    ('manager','owner');
                ";
                int rows = command.ExecuteNonQuery();

                MessageBox.Show($"База перезаполнена. Добавлено пользователей: {rows}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при заполнении базы: {ex.Message}");
            }
        }

        public void ExecuteQueryButton_Click(object sender, RoutedEventArgs e)
        {
            string query = sqlQueryTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(query))
            {
                MessageBox.Show("Введите SQL запрос!", "Ошибка");
                return;
            }

            try
            {
                var command = _connection.CreateCommand();
                command.CommandText = query;

                // Use DataTable to store query results
                using (var adapter = new MySqlDataAdapter(command))
                {
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);

                    // Bind the DataTable to the DataGrid
                    resultDataGrid.ItemsSource = dataTable.DefaultView;

                    MessageBox.Show($"Запрос выполнен успешно. Найдено строк: {dataTable.Rows.Count}", "Успех");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при выполнении запроса: {ex.Message}", "Ошибка");
            }
        }
    }
}

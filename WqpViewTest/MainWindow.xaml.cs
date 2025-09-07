using System.Data;
using System.Data.SQLite;
using System.IO;
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

namespace WqpViewTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly string DbPath = "mydb.db";
        private static readonly string ConnStr = $"Data Source={DbPath};Version=3;";

        public MainWindow()
        {
            InitializeComponent();
            DatabeseHelper dbHelper = new DatabeseHelper();
            dbHelper.ConnectToDatabase();
            LoadDepartments();      // 左の表を読み込む
            EmployeesGrid.ItemsSource = null; // 右は最初は空でもOK

        }

        private void LoadDepartments()
        {
            try
            {
                if (!File.Exists(DbPath))
                {
                    MessageBox.Show($"DBが見つかりません:\n{DbPath}");
                    return;
                }

                using var cn = new SQLiteConnection(ConnStr);
                cn.Open();

                using var da = new SQLiteDataAdapter(
                    "SELECT DepartmentId, DepartmentName FROM Departments ORDER BY DepartmentId;", cn);
                var dt = new DataTable();
                da.Fill(dt);
                DepartmentsGrid.ItemsSource = dt.DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Departments 読み込みエラー:\n" + ex.Message);
            }
        }

        private void LoadEmployeesByDepartment(int departmentId)
        {
            try
            {
                using var cn = new SQLiteConnection(ConnStr);
                cn.Open();

                using var cmd = new SQLiteCommand(
                    @"SELECT EmployeeId, Name, Age, DepartmentId
                      FROM Employees
                      WHERE DepartmentId = @id
                      ORDER BY EmployeeId;", cn);
                cmd.Parameters.AddWithValue("@id", departmentId);

                using var da = new SQLiteDataAdapter(cmd);
                var dt = new DataTable();
                da.Fill(dt);
                EmployeesGrid.ItemsSource = dt.DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Employees 読み込みエラー:\n" + ex.Message);
            }
        }

        // 左の選択が変わったら右を絞り込み表示
        private void DepartmentsGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // DataGridの行は DataRowView で来る
            if (DepartmentsGrid.SelectedItem is DataRowView row)
            {
                if (row.Row.Table.Columns.Contains("DepartmentId") &&
                    int.TryParse(row["DepartmentId"].ToString(), out int depId))
                {
                    LoadEmployeesByDepartment(depId);
                    return;
                }
            }

            // 選択が外れた／取得失敗時は右をクリア
            EmployeesGrid.ItemsSource = null;
        }
    }
}
using Microsoft.VisualBasic;
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
        // DBへの接続文字列
        private static readonly string DbPath = "mydb.db";
        private static readonly string ConnStr = $"Data Source={DbPath};Version=3;";

        // ★ 空でもヘッダーを出すためにスキーマを持ったテーブルを保持
        private readonly DataTable _employeesTable = new DataTable();


        /// <summary>
        /// コンストラクタ
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            DatabeseHelper dbHelper = new DatabeseHelper();
            dbHelper.ConnectToDatabase();

            InitEmployeesTableSchema();

            LoadDepartments();      // 左の表を読み込む
            EmployeesGrid.ItemsSource = _employeesTable.DefaultView;


        }


        /// <summary>
        /// デパートテーブルをロードする
        /// </summary>
        private void LoadDepartments()
        {
            try
            {
                // もしDBへのパスが無ければ
                if (!File.Exists(DbPath))
                {
                    // メッセージ表示
                    MessageBox.Show($"DBが見つかりません:\n{DbPath}");
                    return;
                }

                // DBに接続する
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

        private void InitEmployeesTableSchema()
        {
            _employeesTable.Columns.Add("EmployeeId", typeof(int));
            _employeesTable.Columns.Add("Name", typeof(string));
            _employeesTable.Columns.Add("Age", typeof(int));
            _employeesTable.Columns.Add("DepartmentId", typeof(int));
            _employeesTable.Columns.Add("IsActive", typeof(int)); // 0/1想定
        }



        private void LoadEmployeesByDepartment(int departmentId)
        {
            try
            {
                using var cn = new SQLiteConnection(ConnStr);
                cn.Open();

                using var cmd = new SQLiteCommand(
                    @"SELECT EmployeeId, Name, Age, DepartmentId, IsActive
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
            if (DepartmentsGrid.SelectedItem is DataRowView row &&
                      row.Row.Table.Columns.Contains("DepartmentId") &&
                      int.TryParse(row["DepartmentId"]?.ToString(), out int depId))
            {
                LoadEmployeesByDepartment(depId);
            }
            else
            {
                _employeesTable.Clear(); // ← nullにしない
            }
        }

        private void DepartmentsGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (DepartmentsGrid.SelectedItem is not DataRowView row) return;

            // 既存値の取得
            if (!int.TryParse(row["DepartmentId"].ToString(), out int depId)) return;
            string oldName = row["DepartmentName"]?.ToString() ?? "";

            // 入力ダイアログ
            string? newName = Interaction.InputBox(
                "部署名を入力してください：", "部署名の編集", oldName);

            // Cancel or 変更なし
            if (newName is null) return;
            newName = newName.Trim();
            if (newName.Length == 0 || newName == oldName) return;

            try
            {
                using var cn = new SQLiteConnection(ConnStr);
                cn.Open();
                using var tx = cn.BeginTransaction();

                using (var cmd = new SQLiteCommand(
                    "UPDATE Departments SET DepartmentName = @name WHERE DepartmentId = @id;", cn, tx))
                {
                    cmd.Parameters.AddWithValue("@name", newName);
                    cmd.Parameters.AddWithValue("@id", depId);
                    cmd.ExecuteNonQuery();
                }

                tx.Commit();
                LoadDepartments(); // 再読込

                // 変更した部署の従業員を右側も更新（選択を維持していれば自動でもOKだが明示的に）
                LoadEmployeesByDepartment(depId);
            }
            catch (Exception ex)
            {
                MessageBox.Show("部署名更新エラー:\n" + ex.Message);
            }
        }

        private void EmployeesGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            // チェックボックス列以外は何もしない
            if (e.Column is not DataGridCheckBoxColumn) return;
            if (e.Row.Item is not DataRowView rowView) return;

            // 主キーを取得
            if (rowView["EmployeeId"] is DBNull) return;
            int employeeId = Convert.ToInt32(rowView["EmployeeId"]);

            // 編集後のチェック状態を取得
            if (e.EditingElement is CheckBox chk)
            {
                int newValue = (chk.IsChecked == true) ? 1 : 0;

                try
                {
                    using var cn = new SQLiteConnection(ConnStr);
                    cn.Open();

                    using var cmd = new SQLiteCommand(
                        "UPDATE Employees SET IsActive = @v WHERE EmployeeId = @id;", cn);
                    cmd.Parameters.AddWithValue("@v", newValue);
                    cmd.Parameters.AddWithValue("@id", employeeId);
                    cmd.ExecuteNonQuery();

                    // DataTable側も即反映（画面のズレ防止）
                    rowView["IsActive"] = newValue;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("フラグ更新エラー:\n" + ex.Message);
                }
            }
        }
    }
}
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

        // 最後に赤くした行の主キー（DepartmentId）を保持
        private int? _lastHighlightedId;

        // 既定の行背景を戻す時に使う（必要に応じてテーマ色に合わせて変更）
        private static readonly Brush DefaultRowBackground = Brushes.White;
        private static readonly Brush DefaultRowForeground = Brushes.Black;

        // 赤ハイライト用
        private static readonly Brush HighlightBackground = Brushes.LightCoral;
        private static readonly Brush HighlightForeground = Brushes.White;


        /// <summary>
        /// コンストラクタ
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            DatabeseHelper dbHelper = new DatabeseHelper();
            dbHelper.ConnectToDatabase();

           // InitEmployeesTableSchema();

            LoadDepartments();      // 左の表を読み込む
            EmployeesGrid.ItemsSource = _employeesTable.DefaultView;
        }


        /// <summary>
        /// デパートテーブルをロード
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

                // DBに接続
                using var cn = new SQLiteConnection(ConnStr);
                cn.Open();


                // SQL文を作成し実行
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


        /// <summary>
        /// 従業員一覧を一時保存するメソッド
        /// </summary>
        //private void InitEmployeesTableSchema()
        //{
        //    _employeesTable.Columns.Add("EmployeeId", typeof(int));
        //    _employeesTable.Columns.Add("Name", typeof(string));
        //    _employeesTable.Columns.Add("Age", typeof(int));
        //    _employeesTable.Columns.Add("DepartmentId", typeof(int));
        //    _employeesTable.Columns.Add("IsActive", typeof(int)); // 0/1想定
        //}


        /// <summary>
        /// 従業員テーブルをロード
        /// </summary>
        /// <param name="departmentId"></param>
        private void LoadEmployeesByDepartment(int departmentId)
        {
            try
            {
                // データベースに接続
                using var cn = new SQLiteConnection(ConnStr);
                cn.Open();


                // SQL文を作成し実行
                using var cmd = new SQLiteCommand(
                    @"SELECT EmployeeId, Name, Age, DepartmentId, IsActive
              FROM Employees
              WHERE DepartmentId = @id
              ORDER BY EmployeeId;", cn);
                cmd.Parameters.AddWithValue("@id", departmentId);


                // データをDataTableに保存
                using var da = new SQLiteDataAdapter(cmd);
                var dt = new DataTable();
                da.Fill(dt);

                // DataGridに表示
                EmployeesGrid.ItemsSource = dt.DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Employees 読み込みエラー:\n" + ex.Message);
            }
        }


        /// <summary>
        /// 部署での絞り込み
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DepartmentsGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (DepartmentsGrid.SelectedItem is DataRowView row &&
                row.Row.Table.Columns.Contains("DepartmentId") &&
                int.TryParse(row["DepartmentId"]?.ToString(), out int depId))
            {
                // 有効な DepartmentId → 右側を読み込み
                LoadEmployeesByDepartment(depId);
            }
            else
            {
                // 右側を空に（列ヘッダーは残したいなら ItemsSource を切らずに Clear のみ）
                _employeesTable.Clear();
                EmployeesGrid.ItemsSource = _employeesTable.DefaultView; // ← これを維持すると列が消えません
            }

        }

        /// <summary>
        /// 余白クリックで選択解除
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DepartmentsGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var grid = (DataGrid)sender;
            var dep = e.OriginalSource as DependencyObject;
            var row = ItemsControl.ContainerFromElement(grid, dep) as DataGridRow;

            if (row == null)
            {
                // 行の外（余白）をクリック → 選択解除
                grid.UnselectAll();
                return;
            }

        }
    }
}
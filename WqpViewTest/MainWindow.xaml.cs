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
            ResetDetailsPane();
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
                var departmentName = row.Row.Table.Columns.Contains("DepartmentName")
                    ? row["DepartmentName"]?.ToString()
                    : null;
                UpdateDepartmentDetails(depId, departmentName);
            }
            else
            {
                // 右側を空に（列ヘッダーは残したいなら ItemsSource を切らずに Clear のみ）
                _employeesTable.Clear();
                EmployeesGrid.ItemsSource = _employeesTable.DefaultView;
                ResetDetailsPane();
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

        /// <summary>
        /// 右側の詳細テーブルに部署ごとの情報を組み立てて表示する
        /// </summary>
        /// <param name="departmentId">部署ID</param>
        /// <param name="departmentName">部署名</param>
        private void UpdateDepartmentDetails(int departmentId, string? departmentName)
        {
            var detailTable = new DataTable();
            detailTable.Columns.Add("項目");
            detailTable.Columns.Add("内容");

            var displayName = string.IsNullOrWhiteSpace(departmentName)
                ? $"部署 {departmentId}"
                : departmentName;

            string[] locations = { "東京本社", "大阪支社", "名古屋オフィス", "福岡サテライト", "札幌テックラボ" };
            string[] managers = { "佐藤マネージャー", "田中リーダー", "鈴木主任", "高橋ディレクター", "伊藤マネージャー" };
            string[] focuses = { "顧客対応強化", "新製品開発", "業務効率化", "品質改善", "社内研修" };

            var location = locations[departmentId % locations.Length];
            var manager = managers[departmentId % managers.Length];
            var focus = focuses[departmentId % focuses.Length];
            var monthlyGoal = 60 + (departmentId * 11 % 40);
            var reviewDate = System.DateTime.Today.AddDays((departmentId * 3) % 20 + 7).ToString("yyyy/MM/dd");

            detailTable.Rows.Add("部署名", displayName);
            detailTable.Rows.Add("部署コード", $"DEP-{departmentId:000}");
            detailTable.Rows.Add("拠点", location);
            detailTable.Rows.Add("責任者", manager);
            detailTable.Rows.Add("注力テーマ", focus);
            detailTable.Rows.Add("月次目標", $"{monthlyGoal} 件の成果報告");
            detailTable.Rows.Add("次回レビュー", reviewDate);
            detailTable.Rows.Add("共有メモ", $"{displayName} のチームでは{focus}を中心に取り組んでいます。");

            DetailsGrid.ItemsSource = detailTable.DefaultView;
        }

        /// <summary>
        /// 右側テーブルの初期メッセージを設定
        /// </summary>
        private void ResetDetailsPane()
        {
            var detailTable = new DataTable();
            detailTable.Columns.Add("項目");
            detailTable.Columns.Add("内容");
            detailTable.Rows.Add("ガイド", "左側の部署を選択すると詳細が表示されます。");

            DetailsGrid.ItemsSource = detailTable.DefaultView;
        }
    }
}

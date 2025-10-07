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

            InitEmployeesTableSchema();

            LoadDepartments();      // 左の表を読み込む
            EmployeesGrid.ItemsSource = _employeesTable.DefaultView;
        }


        //// マウス左クリックで行を特定してハイライトIDを更新
        //private void DepartmentsGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        //{
        //    var dep = (DependencyObject)e.OriginalSource;
        //    var row = ItemsControl.ContainerFromElement(DepartmentsGrid, dep) as DataGridRow;
        //    if (row == null) return;

        //    if (row.Item is DataRowView drv && drv.Row.Table.Columns.Contains("DepartmentId"))
        //    {
        //        int id = Convert.ToInt32(drv["DepartmentId"]);
        //        UpdateHighlight(id);
        //    }
        //}

        // 行が生成（再利用）されるたびに、現在のハイライトIDに応じて色を再適用
        private void DepartmentsGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            if (e.Row.Item is DataRowView drv && drv.Row.Table.Columns.Contains("DepartmentId"))
            {
                int id = Convert.ToInt32(drv["DepartmentId"]);
                if (_lastHighlightedId.HasValue && id == _lastHighlightedId.Value)
                {
                    ApplyHighlight(e.Row);
                }
                else
                {
                    ResetHighlight(e.Row);
                }
            }
            else
            {
                ResetHighlight(e.Row);
            }
        }



        // ====== 内部ヘルパ ======

        private void UpdateHighlight(int newId)
        {
            // 以前の行を消す
            if (_lastHighlightedId.HasValue && _lastHighlightedId.Value != newId)
            {
                var prevItem = FindItemById(_lastHighlightedId.Value);
                if (prevItem != null)
                {
                    var prevRow = (DataGridRow)DepartmentsGrid.ItemContainerGenerator.ContainerFromItem(prevItem);
                    if (prevRow != null) ResetHighlight(prevRow);
                }
            }

            _lastHighlightedId = newId;

            // 新しい行に適用
            var item = FindItemById(newId);
            if (item != null)
            {
                var row = (DataGridRow)DepartmentsGrid.ItemContainerGenerator.ContainerFromItem(item);
                if (row != null) ApplyHighlight(row);
            }
            // ※ 行がまだ未生成（画面外など）の場合でも問題なし。
            //    その行が可視化された時（LoadingRow）に自動で赤が当たります。
        }

        private object? FindItemById(int id)
        {
            // ItemsSource が DataView / DataTable想定（ご提示コードに合わせています）
            foreach (var obj in DepartmentsGrid.Items)
            {
                if (obj is DataRowView drv && drv.Row.Table.Columns.Contains("DepartmentId"))
                {
                    if (Convert.ToInt32(drv["DepartmentId"]) == id)
                        return obj;
                }
            }
            return null;
        }

        private static void ApplyHighlight(DataGridRow row)
        {
            row.Background = HighlightBackground;   // ローカル値はスタイルトリガより優先される
            row.Foreground = HighlightForeground;
        }

        private static void ResetHighlight(DataGridRow row)
        {
            // 既定へ戻す（ClearValueでもOK）
            row.Background = DefaultRowBackground;
            row.Foreground = DefaultRowForeground;
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
        private void InitEmployeesTableSchema()
        {
            _employeesTable.Columns.Add("EmployeeId", typeof(int));
            _employeesTable.Columns.Add("Name", typeof(string));
            _employeesTable.Columns.Add("Age", typeof(int));
            _employeesTable.Columns.Add("DepartmentId", typeof(int));
            _employeesTable.Columns.Add("IsActive", typeof(int)); // 0/1想定
        }


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
        /// 行ダブルクリックで部署をインライン編集
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DepartmentsGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (DepartmentsGrid.SelectedItem is not DataRowView row)
            {
                // 選択行を取得できない場合

                // 何もせず終了
                return;
            }

            // 既存値の取得
            if (!int.TryParse(row["DepartmentId"].ToString(), out int depId))
            {
                return;
            }

            string oldName = row["DepartmentName"]?.ToString() ?? "";

            // 入力ダイアログ
            string? newName = Interaction.InputBox(
                "部署名を入力してください：", "部署名の編集", oldName);

            // Cancel or 変更なし
            if (newName is null)
            {
                // 空欄が入力された場合プログラムを終了
                return;
            }

            newName = newName.Trim();
            if (newName.Length == 0 || newName == oldName)
            {
                // もし新しい名前が０文字または前と同じ名前の場合
                // プログラムを就労
                return;
            }

            try
            {
                // DBに接続
                using var cn = new SQLiteConnection(ConnStr);
                cn.Open();

                // トランザクションで開始
                using var tx = cn.BeginTransaction();

                // 部署名を変更するSQLを実行
                using (var cmd = new SQLiteCommand(
                    "UPDATE Departments SET DepartmentName = @name WHERE DepartmentId = @id;", cn, tx))
                {
                    cmd.Parameters.AddWithValue("@name", newName);
                    cmd.Parameters.AddWithValue("@id", depId);

                    // SQLを実行
                    cmd.ExecuteNonQuery();
                }

                // コミットで確定
                tx.Commit();

                // 部署一覧を再読み込み
                LoadDepartments(); // 再読込

                // 変更した部署の従業員を右側も更新（選択を維持していれば自動でもOKだが明示的に）
                LoadEmployeesByDepartment(depId);
            }
            catch (Exception ex)
            {
                // エラー内容を表示
                MessageBox.Show("部署名更新エラー:\n" + ex.Message);
            }
        }


        /// <summary>
        /// 従業員一覧（EmployeesGrid）のセル編集終了イベント
        /// チェックボックス列が編集されたら DB の IsActive を更新する
        /// </summary>
        private void EmployeesGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.Column is not DataGridCheckBoxColumn)
            {
                // もしチェックボックス以外の列の場合
                // メソッドを終了
                return;
            }

            if (e.Row.Item is not DataRowView rowView)
            {
                // 行のデータを取得できない場合
                // メソッドを終了
                return;
            }

            // 主キーを取得
            if (rowView["EmployeeId"] is DBNull)
            {
                // 主キーがnullの場合
                // メソッドを終了
                return;
            }

            // 主キーを取得
            int employeeId = Convert.ToInt32(rowView["EmployeeId"]);

            // 編集後のチェック状態を取得
            if (e.EditingElement is CheckBox chk)
            {
                int newValue = -1;
                if (chk.IsChecked == true)
                {
                    newValue = 1;
                }
                else
                {
                    newValue = 0;
                }

                try
                {
                    // DBに接続
                    using var cn = new SQLiteConnection(ConnStr);
                    cn.Open();

                    // SQL文を作成
                    using var cmd = new SQLiteCommand(
                        "UPDATE Employees SET IsActive = @v WHERE EmployeeId = @id;", cn);
                    cmd.Parameters.AddWithValue("@v", newValue);
                    cmd.Parameters.AddWithValue("@id", employeeId);

                    // SQL文を実行
                    cmd.ExecuteNonQuery();

                    // DataTable側も即反映（画面のズレ防止）
                    rowView["IsActive"] = newValue;
                }
                catch (Exception ex)
                {
                    // エラー内容を表示
                    MessageBox.Show("フラグ更新エラー:\n" + ex.Message);
                }
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

            // 行をクリック → 最後にクリックした行を赤ハイライト
            if (row.Item is DataRowView drv && drv.Row.Table.Columns.Contains("DepartmentId"))
            {
                int id = Convert.ToInt32(drv["DepartmentId"]);
                UpdateHighlight(id);
            }

        }
    }
}
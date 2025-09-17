using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Data.SQLite;

namespace WqpViewTest
{
    public class DatabeseHelper
    {
        // SQLiteの接続文字列
        private const string ConnectonString = "Data Source=mydb.db;Version=3;";


        /// <summary>
        /// データベースへの接続
        /// </summary>
        public void ConnectToDatabase()
        {
            try
            {
                // SQLiteクラスのインスタンスを生成
                using (var connection = new SQLiteConnection(ConnectonString))
                {
                    // データベースファイルを開く
                    connection.Open();
                    System.Diagnostics.Debug.WriteLine("SQLiteに接続成功しました。");
                }

                // テーブル作成
                CreateTable(ConnectonString);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("SQLiteへの接続に失敗しました: " + ex.Message);
            }
        }


        /// <summary>
        /// テーブル作成メソッド
        /// </summary>
        /// <param name="ConectionString"></param>
        private void CreateTable(String ConectionString)
        {
            // SQLiteクラスのインスタンスを生成
            using (var connection = new SQLiteConnection(ConectionString))
            {
                // データベースファイルを開く
                connection.Open();

                // 外部キーをオンにする
                using (var pragmaCmd = new SQLiteCommand("PRAGMA foreign_keys = ON;", connection))
                {
                    pragmaCmd.ExecuteNonQuery();
                }

                // SQL文を作成
                string sql = @"
                    -- 親テーブル: 部署
                    CREATE TABLE IF NOT EXISTS Departments (
                        DepartmentId   INTEGER PRIMARY KEY AUTOINCREMENT,
                        DepartmentName TEXT NOT NULL
                    );

                    -- 子テーブル: 従業員（部署IDを参照）
                    CREATE TABLE IF NOT EXISTS Employees (
                        EmployeeId   INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name         TEXT NOT NULL,
                        Age          INTEGER,
                        DepartmentId INTEGER NOT NULL,
                        IsActive     INTEGER NOT NULL DEFAULT 1, -- Bool相当 (1=true, 0=false)
                        FOREIGN KEY (DepartmentId) REFERENCES Departments(DepartmentId)
                            ON DELETE CASCADE
                            ON UPDATE CASCADE
                    );
                    ";

                // 作成したSQL文を実行する
                using var cmd = new SQLiteCommand(sql, connection);
                cmd.ExecuteNonQuery();
            }
        }
    }
}

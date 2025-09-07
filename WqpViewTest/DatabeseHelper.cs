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
        private const string ConnectonString = "Data Source=mydb.db;Version=3;";

        public void ConnectToDatabase()
        {
            try
            {
                using (var connection = new SQLiteConnection(ConnectonString))
                {
                    connection.Open();
                    System.Diagnostics.Debug.WriteLine("SQLiteに接続成功しました。");
                }

                CreateTable(ConnectonString);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("SQLiteへの接続に失敗しました: " + ex.Message);
            }
        }

        private void CreateTable(String ConectionString)
        {
            using (var connection = new SQLiteConnection(ConectionString))
            {
                connection.Open();
                using (var pragmaCmd = new SQLiteCommand("PRAGMA foreign_keys = ON;", connection))
                {
                    pragmaCmd.ExecuteNonQuery();
                }

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
                        FOREIGN KEY (DepartmentId) REFERENCES Departments(DepartmentId)
                            ON DELETE CASCADE
                            ON UPDATE CASCADE
                            );
                       ";

                using var cmd = new SQLiteCommand(sql, connection);
                cmd.ExecuteNonQuery();
            }
        }
    }
}

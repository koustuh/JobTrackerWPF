using Microsoft.Data.Sqlite;
using System;
using System.IO;

namespace JobTrackerWPF.Data
{
    public static class Database
    {
        private static readonly string DbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "JobTracker", "jobtracker.db");

        public static SqliteConnection GetConnection()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DbPath)!);
            return new SqliteConnection($"Data Source={DbPath}");
        }

        public static void Initialize()
        {
            using var conn = GetConnection();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS Interviews (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    CompanyName TEXT NOT NULL,
                    RoleName TEXT NOT NULL,
                    PanelLane TEXT,
                    HRName TEXT,
                    InterviewDate TEXT,
                    Status TEXT NOT NULL DEFAULT 'Scheduled',
                    Notes TEXT,
                    CreatedAt TEXT DEFAULT (datetime('now'))
                );
                CREATE TABLE IF NOT EXISTS InterviewQuestions (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    InterviewId INTEGER NOT NULL,
                    Question TEXT NOT NULL,
                    Answer TEXT,
                    Round TEXT,
                    FOREIGN KEY (InterviewId) REFERENCES Interviews(Id) ON DELETE CASCADE
                );";
            cmd.ExecuteNonQuery();
        }
    }
}

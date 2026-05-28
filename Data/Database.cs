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
            // Ensure foreign keys are enforced
            using (var fkCmd = conn.CreateCommand()) { fkCmd.CommandText = "PRAGMA foreign_keys = ON;"; fkCmd.ExecuteNonQuery(); }

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
                    InterviewId INTEGER,
                    Question TEXT NOT NULL,
                    Answer TEXT,
                    Round TEXT,
                    FOREIGN KEY (InterviewId) REFERENCES Interviews(Id) ON DELETE CASCADE
                );";
            cmd.ExecuteNonQuery();

            // Create Companies and QuestionCompanies tables for normalized many-to-many
            using var createCompanies = conn.CreateCommand();
            createCompanies.CommandText = @"
                CREATE TABLE IF NOT EXISTS Companies (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL UNIQUE
                );
                CREATE TABLE IF NOT EXISTS QuestionCompanies (
                    QuestionId INTEGER NOT NULL,
                    CompanyId INTEGER NOT NULL,
                    PRIMARY KEY (QuestionId, CompanyId),
                    FOREIGN KEY (QuestionId) REFERENCES InterviewQuestions(Id) ON DELETE CASCADE,
                    FOREIGN KEY (CompanyId) REFERENCES Companies(Id) ON DELETE CASCADE
                );";
            createCompanies.ExecuteNonQuery();

            // Migrate existing comma-separated Companies column into normalized tables
            using var checkCols = conn.CreateCommand();
            checkCols.CommandText = "PRAGMA table_info('InterviewQuestions');";
            var existingCols = new System.Collections.Generic.HashSet<string>();
            using (var rdr = checkCols.ExecuteReader())
            {
                while (rdr.Read()) existingCols.Add(rdr.GetString(1));
            }
            if (existingCols.Contains("Companies"))
            {
                using var sel = conn.CreateCommand();
                sel.CommandText = "SELECT Id, Companies FROM InterviewQuestions WHERE Companies IS NOT NULL AND Companies != '';";
                using var reader2 = sel.ExecuteReader();
                while (reader2.Read())
                {
                    var qid = reader2.GetInt32(0);
                    var comps = reader2.IsDBNull(1) ? "" : reader2.GetString(1);
                    foreach (var name in comps.Split(','))
                    {
                        var cname = name.Trim();
                        if (string.IsNullOrEmpty(cname)) continue;
                        // insert or ignore company
                        using var insC = conn.CreateCommand();
                        insC.CommandText = "INSERT OR IGNORE INTO Companies (Name) VALUES (@n);";
                        insC.Parameters.AddWithValue("@n", cname);
                        insC.ExecuteNonQuery();
                        // get company id
                        using var getC = conn.CreateCommand();
                        getC.CommandText = "SELECT Id FROM Companies WHERE Name=@n LIMIT 1;";
                        getC.Parameters.AddWithValue("@n", cname);
                        var cid = Convert.ToInt32(getC.ExecuteScalar());
                        // link
                        using var link = conn.CreateCommand();
                        link.CommandText = "INSERT OR IGNORE INTO QuestionCompanies (QuestionId, CompanyId) VALUES (@q,@c);";
                        link.Parameters.AddWithValue("@q", qid);
                        link.Parameters.AddWithValue("@c", cid);
                        link.ExecuteNonQuery();
                    }
                }
            }

            // Ensure columns for question bank exist (Companies, Rating)
            using var colsCmd = conn.CreateCommand();
            colsCmd.CommandText = "PRAGMA table_info('InterviewQuestions');";
            var cols = new System.Collections.Generic.HashSet<string>();
            using (var rdr = colsCmd.ExecuteReader())
            {
                while (rdr.Read()) cols.Add(rdr.GetString(1));
            }
            if (!cols.Contains("Companies"))
            {
                using var addC = conn.CreateCommand();
                addC.CommandText = "ALTER TABLE InterviewQuestions ADD COLUMN Companies TEXT;";
                addC.ExecuteNonQuery();
            }
            if (!cols.Contains("Rating"))
            {
                using var addR = conn.CreateCommand();
                addR.CommandText = "ALTER TABLE InterviewQuestions ADD COLUMN Rating INTEGER DEFAULT 0;";
                addR.ExecuteNonQuery();
            }

            // If the existing InterviewQuestions table had InterviewId NOT NULL, migrate to allow NULLs
            using var checkCmd = conn.CreateCommand();
            checkCmd.CommandText = "PRAGMA table_info('InterviewQuestions');";
            bool interviewIdNotNull = false;
            using (var reader = checkCmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var colName = reader.GetString(1);
                    var notNull = reader.GetInt32(3);
                    if (colName == "InterviewId" && notNull == 1) { interviewIdNotNull = true; break; }
                }
            }
            if (interviewIdNotNull)
            {
                // disable FK enforcement during migration
                using (var fkOff = conn.CreateCommand()) { fkOff.CommandText = "PRAGMA foreign_keys = OFF;"; fkOff.ExecuteNonQuery(); }
                using var tran = conn.BeginTransaction();
                using var createNew = conn.CreateCommand();
                createNew.CommandText = @"
                    CREATE TABLE InterviewQuestions_new (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        InterviewId INTEGER,
                        Question TEXT NOT NULL,
                        Answer TEXT,
                        Round TEXT,
                        FOREIGN KEY (InterviewId) REFERENCES Interviews(Id) ON DELETE CASCADE
                    );";
                createNew.ExecuteNonQuery();
                using var copy = conn.CreateCommand();
                copy.CommandText = "INSERT INTO InterviewQuestions_new(Id,InterviewId,Question,Answer,Round) SELECT Id,InterviewId,Question,Answer,Round FROM InterviewQuestions;";
                copy.ExecuteNonQuery();
                using var drop = conn.CreateCommand();
                drop.CommandText = "DROP TABLE InterviewQuestions;";
                drop.ExecuteNonQuery();
                using var rename = conn.CreateCommand();
                rename.CommandText = "ALTER TABLE InterviewQuestions_new RENAME TO InterviewQuestions;";
                rename.ExecuteNonQuery();
                tran.Commit();
                using (var fkOn = conn.CreateCommand()) { fkOn.CommandText = "PRAGMA foreign_keys = ON;"; fkOn.ExecuteNonQuery(); }
            }
        }
    }
}

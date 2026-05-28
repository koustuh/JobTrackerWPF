using System;
using System.Collections.Generic;
using JobTrackerWPF.Models;
using Microsoft.Data.Sqlite;

namespace JobTrackerWPF.Data
{
    public static class InterviewRepository
    {
        public static List<Interview> GetAll(string? statusFilter = null, string? search = null)
        {
            var list = new List<Interview>();
            using var conn = Database.GetConnection();
            conn.Open();
            using var cmd = conn.CreateCommand();
            var where = new List<string>();
            if (!string.IsNullOrEmpty(statusFilter)) where.Add("Status = @status");
            if (!string.IsNullOrEmpty(search)) where.Add("(CompanyName LIKE @search OR RoleName LIKE @search OR HRName LIKE @search)");
            cmd.CommandText = "SELECT * FROM Interviews" + (where.Count > 0 ? " WHERE " + string.Join(" AND ", where) : "") + " ORDER BY Id DESC";
            if (!string.IsNullOrEmpty(statusFilter)) cmd.Parameters.AddWithValue("@status", statusFilter);
            if (!string.IsNullOrEmpty(search)) cmd.Parameters.AddWithValue("@search", $"%{search}%");
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new Interview
                {
                    Id = reader.GetInt32(0),
                    CompanyName = reader.GetString(1),
                    RoleName = reader.GetString(2),
                    PanelLane = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    HRName = reader.IsDBNull(4) ? "" : reader.GetString(4),
                    InterviewDate = reader.IsDBNull(5) ? "" : reader.GetString(5),
                    Status = reader.GetString(6),
                    Notes = reader.IsDBNull(7) ? "" : reader.GetString(7),
                    CreatedAt = reader.IsDBNull(8) ? "" : reader.GetString(8),
                });
            }
            return list;
        }

        public static List<InterviewQuestion> GetQuestionsByCompany(string company)
        {
            var list = new List<InterviewQuestion>();
            using var conn = Database.GetConnection();
            conn.Open();
            using var cmd = conn.CreateCommand();
            // Use normalized tables to find questions linked to the given company name
            cmd.CommandText = @"SELECT iq.* FROM InterviewQuestions iq
                JOIN QuestionCompanies qc ON qc.QuestionId = iq.Id
                JOIN Companies c ON c.Id = qc.CompanyId
                WHERE c.Name = @name
                ORDER BY iq.Rating DESC, iq.Id";
            cmd.Parameters.AddWithValue("@name", company);
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new InterviewQuestion
                {
                    Id = r.GetInt32(0),
                    InterviewId = r.IsDBNull(1) ? 0 : r.GetInt32(1),
                    Question = r.GetString(2),
                    Answer = r.IsDBNull(3) ? "" : r.GetString(3),
                    Round = r.IsDBNull(4) ? "" : r.GetString(4),
                    Companies = r.FieldCount > 5 && !r.IsDBNull(5) ? r.GetString(5) : "",
                    Rating = r.FieldCount > 6 && !r.IsDBNull(6) ? r.GetInt32(6) : 0
                });
            }
            return list;
        }

        public static List<InterviewQuestion> GetAllQuestions()
        {
            var list = new List<InterviewQuestion>();
            using var conn = Database.GetConnection();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM InterviewQuestions ORDER BY Id";
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new InterviewQuestion
                {
                    Id = r.GetInt32(0),
                    InterviewId = r.IsDBNull(1) ? 0 : r.GetInt32(1),
                    Question = r.GetString(2),
                    Answer = r.IsDBNull(3) ? "" : r.GetString(3),
                    Round = r.IsDBNull(4) ? "" : r.GetString(4),
                    Companies = r.FieldCount > 5 && !r.IsDBNull(5) ? r.GetString(5) : "",
                    Rating = r.FieldCount > 6 && !r.IsDBNull(6) ? r.GetInt32(6) : 0
                });
            }
            return list;
        }

        public static int Save(Interview iv)
        {
            using var conn = Database.GetConnection();
            conn.Open();
            using var cmd = conn.CreateCommand();
            if (iv.Id == 0)
            {
                cmd.CommandText = @"INSERT INTO Interviews (CompanyName,RoleName,PanelLane,HRName,InterviewDate,Status,Notes)
                    VALUES (@c,@r,@p,@h,@d,@s,@n); SELECT last_insert_rowid();";
            }
            else
            {
                cmd.CommandText = @"UPDATE Interviews SET CompanyName=@c,RoleName=@r,PanelLane=@p,HRName=@h,
                    InterviewDate=@d,Status=@s,Notes=@n WHERE Id=@id; SELECT @id;";
                cmd.Parameters.AddWithValue("@id", iv.Id);
            }
            cmd.Parameters.AddWithValue("@c", iv.CompanyName);
            cmd.Parameters.AddWithValue("@r", iv.RoleName);
            cmd.Parameters.AddWithValue("@p", iv.PanelLane);
            cmd.Parameters.AddWithValue("@h", iv.HRName);
            cmd.Parameters.AddWithValue("@d", iv.InterviewDate);
            cmd.Parameters.AddWithValue("@s", iv.Status);
            cmd.Parameters.AddWithValue("@n", iv.Notes);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public static void Delete(int id)
        {
            using var conn = Database.GetConnection();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Interviews WHERE Id=@id; DELETE FROM InterviewQuestions WHERE InterviewId=@id";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        public static List<InterviewQuestion> GetQuestions(int interviewId)
        {
            var list = new List<InterviewQuestion>();
            using var conn = Database.GetConnection();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM InterviewQuestions WHERE InterviewId=@id ORDER BY Id";
            cmd.Parameters.AddWithValue("@id", interviewId);
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new InterviewQuestion
                {
                    Id = r.GetInt32(0),
                    InterviewId = r.IsDBNull(1) ? 0 : r.GetInt32(1),
                    Question = r.GetString(2),
                    Answer = r.IsDBNull(3) ? "" : r.GetString(3),
                    Round = r.IsDBNull(4) ? "" : r.GetString(4),
                    Companies = r.FieldCount > 5 && !r.IsDBNull(5) ? r.GetString(5) : "",
                    Rating = r.FieldCount > 6 && !r.IsDBNull(6) ? r.GetInt32(6) : 0
                });
            }
            return list;
        }

        public static void SaveQuestion(InterviewQuestion q)
        {
            using var conn = Database.GetConnection();
            conn.Open();
            using var cmd = conn.CreateCommand();
            if (q.Id == 0)
            {
                cmd.CommandText = "INSERT INTO InterviewQuestions (InterviewId,Question,Answer,Round,Companies,Rating) VALUES (@iv,@q,@a,@r,@c,@rt)";
            }
            else
            {
                cmd.CommandText = "UPDATE InterviewQuestions SET Question=@q,Answer=@a,Round=@r,Companies=@c,Rating=@rt WHERE Id=@id";
                cmd.Parameters.AddWithValue("@id", q.Id);
            }
            if (q.InterviewId == 0)
                cmd.Parameters.AddWithValue("@iv", DBNull.Value);
            else
                cmd.Parameters.AddWithValue("@iv", q.InterviewId);
            cmd.Parameters.AddWithValue("@c", q.Companies);
            cmd.Parameters.AddWithValue("@rt", q.Rating);
            cmd.Parameters.AddWithValue("@q", q.Question);
            cmd.Parameters.AddWithValue("@a", q.Answer);
            cmd.Parameters.AddWithValue("@r", q.Round);
            cmd.ExecuteNonQuery();

            // If inserted, get last insert id
            if (q.Id == 0)
            {
                using var last = conn.CreateCommand();
                last.CommandText = "SELECT last_insert_rowid();";
                q.Id = Convert.ToInt32(last.ExecuteScalar());
            }

            // Update normalized QuestionCompanies links based on comma-separated Companies string
            UpdateQuestionCompanyLinks(conn, q.Id, q.Companies);
        }

        private static void UpdateQuestionCompanyLinks(SqliteConnection conn, int questionId, string companiesCsv)
        {
            // delete existing links
            using var del = conn.CreateCommand();
            del.CommandText = "DELETE FROM QuestionCompanies WHERE QuestionId=@q";
            del.Parameters.AddWithValue("@q", questionId);
            del.ExecuteNonQuery();

            if (string.IsNullOrWhiteSpace(companiesCsv)) return;
            var parts = companiesCsv.Split(',');
            foreach (var p in parts)
            {
                var name = p.Trim();
                if (string.IsNullOrEmpty(name)) continue;
                // insert or ignore into Companies
                using var ins = conn.CreateCommand();
                ins.CommandText = "INSERT OR IGNORE INTO Companies (Name) VALUES (@n)";
                ins.Parameters.AddWithValue("@n", name);
                ins.ExecuteNonQuery();
                // get id
                using var get = conn.CreateCommand();
                get.CommandText = "SELECT Id FROM Companies WHERE Name=@n LIMIT 1";
                get.Parameters.AddWithValue("@n", name);
                var cid = Convert.ToInt32(get.ExecuteScalar());
                // insert link
                using var link = conn.CreateCommand();
                link.CommandText = "INSERT OR IGNORE INTO QuestionCompanies (QuestionId, CompanyId) VALUES (@q,@c)";
                link.Parameters.AddWithValue("@q", questionId);
                link.Parameters.AddWithValue("@c", cid);
                link.ExecuteNonQuery();
            }
        }

        public static List<Company> GetCompanies()
        {
            var list = new List<Company>();
            using var conn = Database.GetConnection();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Name FROM Companies ORDER BY Name";
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(new Company { Id = r.GetInt32(0), Name = r.GetString(1) });
            return list;
        }

        public static List<Company> GetCompaniesForQuestion(int questionId)
        {
            var list = new List<Company>();
            using var conn = Database.GetConnection();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT c.Id, c.Name FROM Companies c
                JOIN QuestionCompanies qc ON qc.CompanyId = c.Id
                WHERE qc.QuestionId = @q ORDER BY c.Name";
            cmd.Parameters.AddWithValue("@q", questionId);
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(new Company { Id = r.GetInt32(0), Name = r.GetString(1) });
            return list;
        }

        public static void DeleteQuestion(int id)
        {
            using var conn = Database.GetConnection();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM InterviewQuestions WHERE Id=@id";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        public static Dictionary<string, int> GetStats()
        {
            using var conn = Database.GetConnection();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Status, COUNT(*) FROM Interviews GROUP BY Status";
            var d = new Dictionary<string, int>
            {
                { "Total", 0 }, { "Scheduled", 0 }, { "Interviewed", 0 },
                { "Offer", 0 }, { "Rejected", 0 }, { "Next Round", 0 }
            };
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                var k = r.GetString(0);
                var v = r.GetInt32(1);
                if (d.ContainsKey(k)) d[k] = v;
                d["Total"] += v;
            }
            return d;
        }
    }
}

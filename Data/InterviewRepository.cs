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
                    InterviewId = r.GetInt32(1),
                    Question = r.GetString(2),
                    Answer = r.IsDBNull(3) ? "" : r.GetString(3),
                    Round = r.IsDBNull(4) ? "" : r.GetString(4)
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
                cmd.CommandText = "INSERT INTO InterviewQuestions (InterviewId,Question,Answer,Round) VALUES (@iv,@q,@a,@r)";
            }
            else
            {
                cmd.CommandText = "UPDATE InterviewQuestions SET Question=@q,Answer=@a,Round=@r WHERE Id=@id";
                cmd.Parameters.AddWithValue("@id", q.Id);
            }
            cmd.Parameters.AddWithValue("@iv", q.InterviewId);
            cmd.Parameters.AddWithValue("@q", q.Question);
            cmd.Parameters.AddWithValue("@a", q.Answer);
            cmd.Parameters.AddWithValue("@r", q.Round);
            cmd.ExecuteNonQuery();
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

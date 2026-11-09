using System.Collections.Generic;
using System.Data.SQLite; // NuGet: System.Data.SQLite.Core

namespace Bai05
{
    /// <summary>1 mon an (dong thoi la DTO dung chung cho server va client).</summary>
    public class FoodItem
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Owner { get; set; }
    }

    /// <summary>Truy cap co so du lieu SQLite cua Server.</summary>
    public class FoodRepository
    {
        private readonly string connectionString;
        private readonly object dbLock = new object(); // tranh loi "database is locked"

        public FoodRepository(string dbPath)
        {
            connectionString = "Data Source=" + dbPath + ";Version=3;";
            Initialize();
        }

        private void Initialize()
        {
            lock (dbLock)
            {
                using (SQLiteConnection conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();
                    using (SQLiteCommand cmd = new SQLiteCommand(
                        "CREATE TABLE IF NOT EXISTS MonAn (" +
                        "Id INTEGER PRIMARY KEY AUTOINCREMENT, " +
                        "TenMon TEXT NOT NULL, " +
                        "NguoiDung TEXT NOT NULL)", conn))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        public void Add(string foodName, string owner)
        {
            lock (dbLock)
            {
                using (SQLiteConnection conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();
                    using (SQLiteCommand cmd = new SQLiteCommand(
                        "INSERT INTO MonAn (TenMon, NguoiDung) VALUES (@name, @owner)", conn))
                    {
                        cmd.Parameters.AddWithValue("@name", foodName);
                        cmd.Parameters.AddWithValue("@owner", owner);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        // Chi xoa duoc mon cua chinh minh
        public bool Delete(long id, string owner)
        {
            lock (dbLock)
            {
                using (SQLiteConnection conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();
                    using (SQLiteCommand cmd = new SQLiteCommand(
                        "DELETE FROM MonAn WHERE Id = @id AND NguoiDung = @owner", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.Parameters.AddWithValue("@owner", owner);
                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
        }

        // owner = null: lay mon cua ca cong dong
        public List<FoodItem> GetAll(string owner)
        {
            return Query(owner, false);
        }

        // Chon ngau nhien 1 mon (null neu chua co mon nao)
        public FoodItem GetRandom(string owner)
        {
            List<FoodItem> result = Query(owner, true);
            return result.Count > 0 ? result[0] : null;
        }

        private List<FoodItem> Query(string owner, bool randomOne)
        {
            List<FoodItem> items = new List<FoodItem>();
            string sql = "SELECT Id, TenMon, NguoiDung FROM MonAn";
            if (owner != null) sql += " WHERE NguoiDung = @owner";
            sql += randomOne ? " ORDER BY RANDOM() LIMIT 1" : " ORDER BY Id";

            lock (dbLock)
            {
                using (SQLiteConnection conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();
                    using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
                    {
                        if (owner != null) cmd.Parameters.AddWithValue("@owner", owner);
                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                items.Add(new FoodItem
                                {
                                    Id = reader.GetInt64(0),
                                    Name = reader.GetString(1),
                                    Owner = reader.GetString(2)
                                });
                            }
                        }
                    }
                }
            }
            return items;
        }
    }
}


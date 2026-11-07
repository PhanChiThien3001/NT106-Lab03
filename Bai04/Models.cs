using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Bai04
{
    /// <summary>Một bộ phim: tên, giá vé chuẩn, danh sách phòng chiếu.</summary>
    public class Movie
    {
        public string Name { get; set; } = "";
        public long BasePrice { get; set; }
        public List<int> Rooms { get; set; } = new List<int>();
    }

    /// <summary>Thông tin ghế + quy tắc tính giá (kế thừa Lab1 - Bài 04).</summary>
    public static class SeatCatalog
    {
        // Mỗi phòng có 15 ghế: A1-5, B1-5, C1-5
        public static readonly string[] AllSeats =
        {
            "A1", "A2", "A3", "A4", "A5",
            "B1", "B2", "B3", "B4", "B5",
            "C1", "C2", "C3", "C4", "C5"
        };

        private static readonly HashSet<string> DiscountSeats = new HashSet<string> { "A1", "A5", "C1", "C5" }; // vé vớt = 1/4
        private static readonly HashSet<string> VipSeats = new HashSet<string> { "B2", "B3", "B4" };            // vé VIP = x2
                                                                                                                // Còn lại (A2-A4, C2-C4, và B1, B5) là vé thường = x1

        public static bool IsValid(string seat)
        {
            return Array.IndexOf(AllSeats, seat) >= 0;
        }

        public static string GetCategory(string seat)
        {
            if (DiscountSeats.Contains(seat)) return "Vé vớt";
            if (VipSeats.Contains(seat)) return "Vé VIP";
            return "Vé thường";
        }

        public static long GetPrice(long basePrice, string seat)
        {
            if (DiscountSeats.Contains(seat)) return basePrice / 4;
            if (VipSeats.Contains(seat)) return basePrice * 2;
            return basePrice;
        }

        public static string FormatMoney(long value)
        {
            return value.ToString("N0", new CultureInfo("vi-VN")) + "đ";
        }
    }

    /// <summary>
    /// Đọc / tạo file input5.txt. Mỗi phim gồm 3 dòng:
    ///   &lt;tên phim&gt;
    ///   &lt;giá vé chuẩn&gt;
    ///   &lt;các phòng chiếu, cách nhau dấu phẩy&gt;
    /// </summary>
    public static class MovieFile
    {
        public static List<Movie> Load(string path)
        {
            var lines = File.ReadAllLines(path, Encoding.UTF8)
                            .Select(l => l.Trim())
                            .Where(l => l.Length > 0)
                            .ToList();

            if (lines.Count % 3 != 0)
                throw new FormatException("File phim sai định dạng: mỗi phim phải có đủ 3 dòng (tên, giá vé, phòng chiếu).");

            var movies = new List<Movie>();
            for (int i = 0; i < lines.Count; i += 3)
            {
                if (!long.TryParse(lines[i + 1], out long price) || price <= 0)
                    throw new FormatException("Giá vé không hợp lệ ở phim: " + lines[i]);

                var rooms = new List<int>();
                foreach (string part in lines[i + 2].Split(','))
                {
                    if (!int.TryParse(part.Trim(), out int room))
                        throw new FormatException("Phòng chiếu không hợp lệ ở phim: " + lines[i]);
                    rooms.Add(room);
                }

                movies.Add(new Movie
                {
                    Name = lines[i].Replace('|', ' '), // '|' là ký tự phân tách của giao thức
                    BasePrice = price,
                    Rooms = rooms
                });
            }
            return movies;
        }

        /// <summary>Tạo file mẫu theo dữ liệu của Lab1 - Bài 04.</summary>
        public static void CreateSample(string path)
        {
            string[] sample =
            {
                "Đào, phở và piano", "45000", "1,2,3",
                "Mai", "100000", "2,3",
                "Gặp lại chị bầu", "70000", "1",
                "Tarot", "90000", "3"
            };
            File.WriteAllLines(path, sample, new UTF8Encoding(false));
        }
    }
}
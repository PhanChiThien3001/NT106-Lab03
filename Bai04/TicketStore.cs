using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Bai04
{
    public class BookingResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public string Customer { get; set; } = "";
        public long Total { get; set; }
        public List<string> Seats { get; set; } = new List<string>();
    }

    public class MovieStat
    {
        public string Name { get; set; } = "";
        public int Sold { get; set; }
        public int Remaining { get; set; }
        public double Rate { get; set; }   // % vé bán ra
        public long Revenue { get; set; }
        public int Rank { get; set; }
    }

    /// <summary>
    /// Nơi lưu trữ thống nhất dữ liệu vé tại Server.
    /// Mọi thao tác đặt vé đều đi qua lock nên 2 quầy không thể đặt trùng 1 ghế.
    /// </summary>
    public class TicketStore
    {
        private readonly object _sync = new object();
        private readonly string _soldFilePath;
        private readonly Dictionary<(string Movie, int Room), HashSet<string>> _sold =
            new Dictionary<(string Movie, int Room), HashSet<string>>();

        public List<Movie> Movies { get; }

        public TicketStore(List<Movie> movies, string soldFilePath)
        {
            Movies = movies;
            _soldFilePath = soldFilePath;
            LoadSold();
        }

        public BookingResult TryBook(string customer, string movieName, int room, IEnumerable<string> seats)
        {
            customer = (customer ?? "").Replace('|', ' ').Trim();
            if (customer.Length == 0)
                return Fail("Vui lòng nhập họ tên khách hàng.");

            Movie movie = Movies.FirstOrDefault(m => m.Name == movieName);
            if (movie == null)
                return Fail("Phim không tồn tại.");
            if (!movie.Rooms.Contains(room))
                return Fail("Phim \"" + movie.Name + "\" không chiếu ở phòng " + room + ".");

            List<string> seatList = (seats ?? new List<string>())
                .Select(s => s.Trim().ToUpperInvariant())
                .Where(s => s.Length > 0)
                .Distinct()
                .ToList();
            if (seatList.Count == 0)
                return Fail("Vui lòng chọn ít nhất 1 ghế.");

            string invalidSeat = seatList.FirstOrDefault(s => !SeatCatalog.IsValid(s));
            if (invalidSeat != null)
                return Fail("Ghế không hợp lệ: " + invalidSeat);

            lock (_sync)
            {
                var key = (movie.Name, room);
                if (!_sold.TryGetValue(key, out HashSet<string> soldSet))
                {
                    soldSet = new HashSet<string>();
                    _sold[key] = soldSet;
                }

                List<string> taken = seatList.Where(soldSet.Contains).ToList();
                if (taken.Count > 0)
                    return Fail("Ghế đã có người đặt: " + string.Join(", ", taken));

                // Tất cả ghế còn trống -> đặt toàn bộ (all-or-nothing)
                long total = 0;
                foreach (string seat in seatList)
                {
                    soldSet.Add(seat);
                    total += SeatCatalog.GetPrice(movie.BasePrice, seat);
                }
                SaveSold();

                return new BookingResult
                {
                    Success = true,
                    Customer = customer,
                    Total = total,
                    Seats = seatList,
                    Message = "Đặt vé thành công."
                };
            }
        }

        /// <summary>Bản sao danh sách ghế đã bán, dùng để gửi cho Client mới kết nối.</summary>
        public List<(string Movie, int Room, List<string> Seats)> SnapshotSold()
        {
            lock (_sync)
            {
                return _sold
                    .Where(kv => kv.Value.Count > 0)
                    .Select(kv => (kv.Key.Movie, kv.Key.Room, kv.Value.ToList()))
                    .ToList();
            }
        }

        /// <summary>Thống kê theo phim: vé bán, vé tồn, tỉ lệ, doanh thu, xếp hạng (Lab2 - Bài 05).</summary>
        public List<MovieStat> BuildStats()
        {
            lock (_sync)
            {
                var stats = new List<MovieStat>();
                foreach (Movie movie in Movies)
                {
                    int totalSeats = movie.Rooms.Count * SeatCatalog.AllSeats.Length;
                    int sold = 0;
                    long revenue = 0;
                    foreach (int room in movie.Rooms)
                    {
                        if (_sold.TryGetValue((movie.Name, room), out HashSet<string> set))
                        {
                            sold += set.Count;
                            revenue += set.Sum(s => SeatCatalog.GetPrice(movie.BasePrice, s));
                        }
                    }
                    stats.Add(new MovieStat
                    {
                        Name = movie.Name,
                        Sold = sold,
                        Remaining = totalSeats - sold,
                        Rate = totalSeats == 0 ? 0 : (double)sold / totalSeats * 100,
                        Revenue = revenue
                    });
                }

                foreach (MovieStat s in stats)
                    s.Rank = stats.Count(x => x.Revenue > s.Revenue) + 1;

                return stats.OrderBy(s => s.Rank).ToList();
            }
        }

        private static BookingResult Fail(string message)
        {
            return new BookingResult { Success = false, Message = message };
        }

        // ---- Lưu / nạp vé đã bán (để khởi động lại Server vẫn giữ trạng thái) ----

        private void LoadSold()
        {
            if (!File.Exists(_soldFilePath)) return;

            foreach (string line in File.ReadAllLines(_soldFilePath, Encoding.UTF8))
            {
                string[] p = line.Split('|');
                if (p.Length != 3 || !int.TryParse(p[1], out int room)) continue;

                Movie movie = Movies.FirstOrDefault(m => m.Name == p[0]);
                if (movie == null || !movie.Rooms.Contains(room) || !SeatCatalog.IsValid(p[2])) continue;

                var key = (movie.Name, room);
                if (!_sold.TryGetValue(key, out HashSet<string> set))
                {
                    set = new HashSet<string>();
                    _sold[key] = set;
                }
                set.Add(p[2]);
            }
        }

        private void SaveSold()
        {
            try
            {
                var lines = new List<string>();
                foreach (var kv in _sold)
                    foreach (string seat in kv.Value)
                        lines.Add(kv.Key.Movie + "|" + kv.Key.Room + "|" + seat);
                File.WriteAllLines(_soldFilePath, lines, new UTF8Encoding(false));
            }
            catch (IOException)
            {
                // Không lưu được file thì vẫn giữ dữ liệu trong bộ nhớ
            }
        }
    }
}

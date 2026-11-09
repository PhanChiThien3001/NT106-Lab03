using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Bai04
{
    public partial class ClientForm : Form
    {
        private TcpClient _tcp;
        private StreamReader _reader;
        private StreamWriter _writer;
        private readonly object _writeLock = new object();
        private volatile bool _connected;

        private readonly List<Movie> _movies = new List<Movie>();
        private readonly Dictionary<(string Movie, int Room), HashSet<string>> _sold =
            new Dictionary<(string Movie, int Room), HashSet<string>>();
        private bool _isRefreshing;

        public ClientForm()
        {
            InitializeComponent();
        }

        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            Disconnect(true);
        }

        private void cboMovie_SelectedIndexChanged(object sender, EventArgs e)
        {
            FillRooms();
        }

        private void cboRoom_SelectedIndexChanged(object sender, EventArgs e)
        {
            RefreshSeats(false);
        }

        private void ClientForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Disconnect(false);
        }

        // =============== KẾT NỐI ===============

        private void btnConnect_Click(object sender, EventArgs e)
        {
            if (!int.TryParse(txtPort.Text.Trim(), out int port) || port < 1 || port > 65535)
            {
                MessageBox.Show("Port phải là số từ 1 đến 65535.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                _tcp = new TcpClient();
                _tcp.Connect(txtHost.Text.Trim(), port);

                NetworkStream stream = _tcp.GetStream();
                var utf8 = new UTF8Encoding(false);
                _reader = new StreamReader(stream, utf8);
                _writer = new StreamWriter(stream, utf8) { AutoFlush = true, NewLine = "\n" };
                _connected = true;

                new Thread(ReceiveLoop) { IsBackground = true }.Start();

                btnConnect.Enabled = false;
                txtHost.Enabled = false;
                txtPort.Enabled = false;
                btnDisconnect.Enabled = true;
                AppendLog("Đã kết nối tới server " + txtHost.Text.Trim() + ":" + port);

                SendLine("GET_DATA");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể kết nối: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Disconnect(false);
            }
        }

        private void Disconnect(bool logIt)
        {
            bool wasConnected = _connected;
            _connected = false;

            try { _writer?.Close(); } catch { }
            try { _reader?.Close(); } catch { }
            try { _tcp?.Close(); } catch { }
            _writer = null; _reader = null; _tcp = null;

            if (IsDisposed) return;

            _movies.Clear();
            _sold.Clear();
            cboMovie.Items.Clear();
            cboRoom.Items.Clear();
            clbSeats.Items.Clear();
            UpdateTotal();

            btnConnect.Enabled = true;
            txtHost.Enabled = true;
            txtPort.Enabled = true;
            btnDisconnect.Enabled = false;
            btnBook.Enabled = false;

            if (logIt && wasConnected) AppendLog("Đã ngắt kết nối.");
        }

        private void SendLine(string line)
        {
            lock (_writeLock)
            {
                try { _writer?.WriteLine(line); }
                catch (IOException) { }
                catch (ObjectDisposedException) { }
            }
        }

        // =============== NHẬN DỮ LIỆU (thread nền) ===============

        private void ReceiveLoop()
        {
            try
            {
                string line;
                while (_connected && (line = _reader.ReadLine()) != null)
                {
                    string captured = line;
                    SafeInvoke(() => HandleServerMessage(captured));
                }
            }
            catch (IOException) { }
            catch (ObjectDisposedException) { }
            finally
            {
                if (_connected) // server đóng kết nối chứ không phải người dùng bấm Disconnect
                {
                    SafeInvoke(() =>
                    {
                        AppendLog("Mất kết nối tới server.");
                        Disconnect(false);
                    });
                }
            }
        }

        private void SafeInvoke(Action action)
        {
            if (IsDisposed || !IsHandleCreated) return;
            try { BeginInvoke(action); }
            catch (InvalidOperationException) { }
        }

        // =============== XỬ LÝ THÔNG ĐIỆP TỪ SERVER (UI thread) ===============

        private void HandleServerMessage(string line)
        {
            string[] p = line.Split('|');
            switch (p[0])
            {
                case "DATA_BEGIN":
                    _movies.Clear();
                    _sold.Clear();
                    break;

                case "MOVIE":
                    if (p.Length == 4 && long.TryParse(p[2], out long price))
                    {
                        var movie = new Movie { Name = p[1], BasePrice = price };
                        foreach (string r in p[3].Split(','))
                            if (int.TryParse(r, out int room)) movie.Rooms.Add(room);
                        _movies.Add(movie);
                    }
                    break;

                case "SOLD":
                case "SOLD_UPDATE":
                    if (p.Length == 4 && int.TryParse(p[2], out int soldRoom))
                    {
                        MergeSold(p[1], soldRoom, p[3]);
                        if (p[0] == "SOLD_UPDATE") RefreshSeats(true);
                    }
                    break;

                case "DATA_END":
                    FillMovies();
                    btnBook.Enabled = true;
                    break;

                case "BOOK_OK":
                    if (p.Length == 6) ShowReceipt(p);
                    btnBook.Enabled = true;
                    break;

                case "BOOK_FAIL":
                    AppendLog("❌ Đặt vé thất bại: " + (p.Length > 1 ? p[1] : ""));
                    MessageBox.Show(p.Length > 1 ? p[1] : "Đặt vé thất bại.", "Không đặt được vé",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    btnBook.Enabled = true;
                    break;

                case "ERROR":
                    AppendLog("Lỗi từ server: " + (p.Length > 1 ? p[1] : ""));
                    break;
            }
        }

        private void MergeSold(string movie, int room, string seatsCsv)
        {
            var key = (movie, room);
            if (!_sold.TryGetValue(key, out HashSet<string> set))
            {
                set = new HashSet<string>();
                _sold[key] = set;
            }
            foreach (string seat in seatsCsv.Split(','))
                if (seat.Length > 0) set.Add(seat);
        }

        private void ShowReceipt(string[] p)
        {
            // BOOK_OK|<họ tên>|<phim>|<phòng>|<ghế,...>|<tổng>
            Movie movie = _movies.FirstOrDefault(m => m.Name == p[2]);
            long.TryParse(p[5], out long total);

            var sb = new StringBuilder();
            sb.AppendLine("===== ĐẶT VÉ THÀNH CÔNG =====");
            sb.AppendLine("Khách hàng: " + p[1]);
            sb.AppendLine("Phim: " + p[2]);
            sb.AppendLine("Phòng chiếu: " + p[3]);
            foreach (string seat in p[4].Split(','))
            {
                string detail = movie == null ? "" :
                    " (" + SeatCatalog.GetCategory(seat) + "): " + SeatCatalog.FormatMoney(SeatCatalog.GetPrice(movie.BasePrice, seat));
                sb.AppendLine("  - Ghế " + seat + detail);
            }
            sb.AppendLine("TỔNG TIỀN: " + SeatCatalog.FormatMoney(total));
            AppendLog(sb.ToString());
            txtCustomer.Clear();
        }

        // =============== GIAO DIỆN CHỌN PHIM / PHÒNG / GHẾ ===============

        private void FillMovies()
        {
            cboMovie.Items.Clear();
            foreach (Movie m in _movies) cboMovie.Items.Add(m.Name);
            if (cboMovie.Items.Count > 0) cboMovie.SelectedIndex = 0; // kéo theo FillRooms -> RefreshSeats
        }

        private void FillRooms()
        {
            cboRoom.Items.Clear();
            Movie movie = CurrentMovie();
            if (movie != null)
            {
                foreach (int room in movie.Rooms) cboRoom.Items.Add(room);
                if (cboRoom.Items.Count > 0) cboRoom.SelectedIndex = 0;
            }
            RefreshSeats(false);
        }

        private Movie CurrentMovie()
        {
            return cboMovie.SelectedIndex < 0 ? null : _movies.FirstOrDefault(m => m.Name == (string)cboMovie.SelectedItem);
        }

        private int? CurrentRoom()
        {
            return cboRoom.SelectedIndex < 0 ? (int?)null : (int)cboRoom.SelectedItem;
        }

        private HashSet<string> SoldSeatsOfCurrent()
        {
            Movie movie = CurrentMovie();
            int? room = CurrentRoom();
            if (movie != null && room.HasValue && _sold.TryGetValue((movie.Name, room.Value), out HashSet<string> set))
                return set;
            return new HashSet<string>();
        }

        /// <summary>Vẽ lại danh sách ghế. Ghế đã bán hiển thị "ĐÃ BÁN" và không thể tick.</summary>
        private void RefreshSeats(bool keepChecked)
        {
            Movie movie = CurrentMovie();
            int? room = CurrentRoom();
            HashSet<string> previouslyChecked = keepChecked ? new HashSet<string>(GetCheckedSeats()) : new HashSet<string>();
            HashSet<string> sold = SoldSeatsOfCurrent();

            _isRefreshing = true;
            clbSeats.Items.Clear();
            if (movie != null && room.HasValue)
            {
                foreach (string seat in SeatCatalog.AllSeats)
                {
                    bool isSold = sold.Contains(seat);
                    string text = isSold
                        ? seat + " - ĐÃ BÁN"
                        : seat + " - " + SeatCatalog.GetCategory(seat) + " - " + SeatCatalog.FormatMoney(SeatCatalog.GetPrice(movie.BasePrice, seat));
                    clbSeats.Items.Add(text, !isSold && previouslyChecked.Contains(seat));

                    if (isSold && previouslyChecked.Contains(seat))
                        AppendLog("⚠ Ghế " + seat + " vừa được quầy khác bán, đã bỏ chọn.");
                }
            }
            _isRefreshing = false;
            UpdateTotal();
        }

        private List<string> GetCheckedSeats()
        {
            // Thứ tự item trong clbSeats trùng thứ tự SeatCatalog.AllSeats
            return clbSeats.CheckedIndices.Cast<int>().Select(i => SeatCatalog.AllSeats[i]).ToList();
        }

        private void clbSeats_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (_isRefreshing) return;

            // Chặn tick vào ghế đã bán
            if (e.NewValue == CheckState.Checked && SoldSeatsOfCurrent().Contains(SeatCatalog.AllSeats[e.Index]))
            {
                e.NewValue = CheckState.Unchecked;
                return;
            }
            BeginInvoke(new Action(UpdateTotal)); // chờ trạng thái tick cập nhật xong mới tính
        }

        private void UpdateTotal()
        {
            Movie movie = CurrentMovie();
            long total = 0;
            if (movie != null)
                total = GetCheckedSeats().Sum(s => SeatCatalog.GetPrice(movie.BasePrice, s));
            lblTotal.Text = "Tạm tính: " + SeatCatalog.FormatMoney(total);
        }

        // =============== ĐẶT VÉ ===============

        private void btnBook_Click(object sender, EventArgs e)
        {
            if (!_connected)
            {
                MessageBox.Show("Chưa kết nối tới server.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string customer = txtCustomer.Text.Trim();
            Movie movie = CurrentMovie();
            int? room = CurrentRoom();
            List<string> seats = GetCheckedSeats();

            if (customer.Length == 0)
            {
                MessageBox.Show("Vui lòng nhập họ và tên khách hàng.", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtCustomer.Focus();
                return;
            }
            if (movie == null || !room.HasValue)
            {
                MessageBox.Show("Vui lòng chọn phim và phòng chiếu.", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (seats.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn ít nhất 1 ghế.", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnBook.Enabled = false; // chờ server trả lời BOOK_OK / BOOK_FAIL
            SendLine("BOOK|" + customer.Replace('|', ' ') + "|" + movie.Name + "|" + room.Value + "|" + string.Join(",", seats));
        }

        private void AppendLog(string message)
        {
            rtbLog.AppendText("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + message + Environment.NewLine);
            rtbLog.ScrollToCaret();
        }
    }
}

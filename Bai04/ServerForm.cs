using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Bai04
{
    public partial class ServerForm : Form
    {
        private TicketServer _server;
        private TicketStore _store;

        public ServerForm()
        {
            InitializeComponent();
        }

        private void btnListen_Click(object sender, EventArgs e)
        {
            if (!int.TryParse(txtPort.Text.Trim(), out int port) || port < 1 || port > 65535)
            {
                MessageBox.Show("Port phải là số từ 1 đến 65535.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                string inputPath = Path.Combine(AppContext.BaseDirectory, "input5.txt");
                if (!File.Exists(inputPath))
                    MovieFile.CreateSample(inputPath); // chưa có file thì tạo file mẫu (dữ liệu Lab1 - Bài 04)

                _store = new TicketStore(MovieFile.Load(inputPath), Path.Combine(AppContext.BaseDirectory, "sold5.txt"));
                _server = new TicketServer(_store);
                _server.LogMessage += AppendLog;
                _server.Start(port);

                btnListen.Enabled = false;
                txtPort.Enabled = false;
                btnExport.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể khởi động server: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void btnExport_Click(object sender, EventArgs e)
        {
            btnExport.Enabled = false;
            try
            {
                var stats = _store.BuildStats();
                string outputPath = Path.Combine(AppContext.BaseDirectory, "output5.txt");

                progressBar.Minimum = 0;
                progressBar.Maximum = Math.Max(stats.Count, 1);
                progressBar.Value = 0;

                using (var writer = new StreamWriter(outputPath, false, new UTF8Encoding(false)))
                {
                    foreach (MovieStat s in stats)
                    {
                        await writer.WriteLineAsync("Tên phim: " + s.Name);
                        await writer.WriteLineAsync("Số lượng vé bán ra: " + s.Sold);
                        await writer.WriteLineAsync("Số lượng vé tồn: " + s.Remaining);
                        await writer.WriteLineAsync("Tỉ lệ vé bán ra: " + s.Rate.ToString("0.##") + "%");
                        await writer.WriteLineAsync("Doanh thu: " + SeatCatalog.FormatMoney(s.Revenue));
                        await writer.WriteLineAsync("Xếp hạng doanh thu: " + s.Rank);
                        await writer.WriteLineAsync();

                        progressBar.Value++;
                        await Task.Delay(300); // làm chậm nhẹ để thấy ProgressBar chạy
                    }
                }
                AppendLog("Đã xuất thống kê ra " + outputPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Xuất file thất bại: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnExport.Enabled = true;
            }
        }

        private void ServerForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _server?.Stop();
        }

        /// <summary>Được gọi từ nhiều thread -> phải Invoke về UI thread (thread-safe call).</summary>
        private void AppendLog(string message)
        {
            if (IsDisposed || !IsHandleCreated) return;

            if (InvokeRequired)
            {
                try { BeginInvoke(new Action<string>(AppendLog), message); }
                catch (InvalidOperationException) { }
                return;
            }

            lstLog.Items.Add("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + message);
            lstLog.TopIndex = lstLog.Items.Count - 1;
        }
    }
}

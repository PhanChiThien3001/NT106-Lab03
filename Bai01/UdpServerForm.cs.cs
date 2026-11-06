
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Bai01
{
    public partial class UdpServerForm : Form
    {
        private UdpClient server;
        private bool isListening = false;

        public UdpServerForm()
        {
            InitializeComponent();

            txtPort.Text = "8080";
            btnStop.Enabled = false;
        }

        // Bắt đầu lắng nghe
        private async void btnListen_Click(object sender, EventArgs e)
        {
            if (!int.TryParse(txtPort.Text, out int port) ||
                port < 1 || port > 65535)
            {
                MessageBox.Show("Vui lòng nhập Port hợp lệ!");
                return;
            }

            try
            {
                server = new UdpClient(port);
                isListening = true;

                btnListen.Enabled = false;
                btnStop.Enabled = true;
                txtPort.Enabled = false;

                lstMessages.Items.Add(
                    "Server đang lắng nghe tại Port: " + port);

                await ReceiveMessagesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi Server: " + ex.Message);
            }
            finally
            {
                server?.Close();
                server = null;

                isListening = false;

                btnListen.Enabled = true;
                btnStop.Enabled = false;
                txtPort.Enabled = true;
            }
        }

        // Nhận tin nhắn từ Client
        private async Task ReceiveMessagesAsync()
        {
            while (isListening && server != null)
            {
                try
                {
                    UdpReceiveResult result =
                        await server.ReceiveAsync();

                    string message =
                        Encoding.UTF8.GetString(result.Buffer);

                    string clientIP =
                        result.RemoteEndPoint.Address.ToString();

                    int clientPort =
                        result.RemoteEndPoint.Port;

                    lstMessages.Items.Add(
                        $"[{clientIP}:{clientPort}] {message}");
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (SocketException)
                {
                    if (!isListening)
                        break;

                    throw;
                }
            }
        }

        // Dừng Server
        private void btnStop_Click(object sender, EventArgs e)
        {
            isListening = false;

            server?.Close();
        }

        // Đóng Server khi đóng Form
        protected override void OnFormClosing(
            FormClosingEventArgs e)
        {
            isListening = false;
            server?.Close();

            base.OnFormClosing(e);
        }
    }
}

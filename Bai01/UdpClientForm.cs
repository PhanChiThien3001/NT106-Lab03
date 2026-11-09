using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;

namespace Bai01
{
    public partial class UdpClientForm : Form
    {
        private UdpClient client;

        public UdpClientForm()
        {
            InitializeComponent();

            this.Text = "UDP Client";
            this.StartPosition = FormStartPosition.CenterScreen;

            txtIP.Text = "127.0.0.1";
            txtPort.Text = "8080";

            btnSend.Click += btnSend_Click;
        }

        // Gửi tin nhắn đến Server
        private async void btnSend_Click(object sender, EventArgs e)
        {
            if (!IPAddress.TryParse(txtIP.Text.Trim(), out IPAddress ip))
            {
                MessageBox.Show("IP không hợp lệ!");
                return;
            }

            if (!int.TryParse(txtPort.Text, out int port) ||
                port < 1 || port > 65535)
            {
                MessageBox.Show("Port không hợp lệ!");
                return;
            }

            if (string.IsNullOrWhiteSpace(txtMessage.Text))
            {
                MessageBox.Show("Vui lòng nhập tin nhắn!");
                return;
            }

            try
            {
                if (client == null)
                    client = new UdpClient();

                byte[] data = Encoding.UTF8.GetBytes(txtMessage.Text);

                await client.SendAsync(data, data.Length,
                    new IPEndPoint(ip, port));

                txtMessage.Clear();
                txtMessage.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi gửi tin nhắn: " + ex.Message);
            }
        }

        // Đóng UDP Client khi đóng Form
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            client?.Close();
            client = null;

            base.OnFormClosing(e);
        }
    }
}
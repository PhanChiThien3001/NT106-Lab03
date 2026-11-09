using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;

namespace Bai03
{
    public partial class FormClient : Form
    {
        private TcpClient tcpClient;
        private NetworkStream ns;

        public FormClient()
        {
            InitializeComponent();
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            try
            {
                tcpClient = new TcpClient();
                IPAddress ipAddress = IPAddress.Parse(tbIP.Text);
                IPEndPoint ipEndPoint = new IPEndPoint(ipAddress, int.Parse(tbPort.Text));

                tcpClient.Connect(ipEndPoint);
                ns = tcpClient.GetStream();

                MessageBox.Show("Kết nối tới Server thành công!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi kết nối: " + ex.Message);
            }
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            if (tcpClient != null && tcpClient.Connected)
            {
                byte[] data = Encoding.UTF8.GetBytes(tbMessage.Text + "\n");
                ns.Write(data, 0, data.Length);
                tbMessage.Clear();
            }
            else
            {
                MessageBox.Show("Chưa kết nối tới Server!");
            }
        }

        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            if (tcpClient != null && tcpClient.Connected)
            {
                byte[] data = Encoding.UTF8.GetBytes("quit\n");
                ns.Write(data, 0, data.Length);

                ns.Close();
                tcpClient.Close();
                MessageBox.Show("Đã ngắt kết nối!");
            }
        }
    }
}

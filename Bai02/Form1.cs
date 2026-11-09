using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Bai02
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void btnListen_Click(object sender, EventArgs e)
        {
            // Tránh lỗi xung đột thread khi cập nhật giao diện
            CheckForIllegalCrossThreadCalls = false;

            // Tạo luồng mới lắng nghe kết nối
            Thread serverThread = new Thread(new ThreadStart(StartUnsafeThread));
            serverThread.Start();

            btnListen.Enabled = false; // Khóa nút Listen sau khi đã kích hoạt
        }

        void StartUnsafeThread()
        {
            int bytesReceived = 0;
            byte[] recv = new byte[1];

            // Khởi tạo Socket lắng nghe TCP/IP IPv4
            Socket listenerSocket = new Socket(
                AddressFamily.InterNetwork,
                SocketType.Stream,
                ProtocolType.Tcp
            );

            // Gán IP Loopback (127.0.0.1) và Port 8080
            IPEndPoint ipepServer = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8080);

            // Bind socket và bắt đầu lắng nghe
            listenerSocket.Bind(ipepServer);
            listenerSocket.Listen(-1);

            // Hiển thị trạng thái khởi chạy
            listViewCommand.Items.Add(new ListViewItem("Telnet running on 127.0.0.1:8080"));

            // Đợi kết nối từ Telnet Client
            Socket clientSocket = listenerSocket.Accept();
            listViewCommand.Items.Add(new ListViewItem("New client connected"));

            // Vòng lặp nhận dữ liệu từ Client
            while (clientSocket.Connected)
            {
                string text = "";
                do
                {
                    bytesReceived = clientSocket.Receive(recv);
                    if (bytesReceived == 0) break;

                    text += Encoding.ASCII.GetString(recv, 0, bytesReceived);
                }
                while (text.Length > 0 && text[text.Length - 1] != '\n');

                if (bytesReceived == 0) break;

                // Thêm chuỗi nhận được vào ListView
                listViewCommand.Items.Add(new ListViewItem(text.TrimEnd('\r', '\n')));
            }

            // Đóng socket khi ngắt kết nối
            clientSocket.Close();
            listenerSocket.Close();
        }
    }
}
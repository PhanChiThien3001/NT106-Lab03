using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Bai03
{
    public partial class FormServer : Form
    {
        public FormServer()
        {
            InitializeComponent();
        }

        private void btnListen_Click(object sender, EventArgs e)
        {
            CheckForIllegalCrossThreadCalls = false;
            Thread serverThread = new Thread(new ThreadStart(StartServer));
            serverThread.Start();
            btnListen.Enabled = false;
        }

        void StartServer()
        {
            TcpListener listener = new TcpListener(IPAddress.Parse("127.0.0.1"), 8080);
            listener.Start();
            listViewCommand.Items.Add(new ListViewItem("Server started!"));

            TcpClient client = listener.AcceptTcpClient();
            listViewCommand.Items.Add(new ListViewItem("Connection accepted from " + client.Client.RemoteEndPoint.ToString()));

            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];
            int bytesRead;

            while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                if (message.Trim() == "quit") break;

                listViewCommand.Items.Add(new ListViewItem("From client: " + message.TrimEnd('\r', '\n')));
            }

            stream.Close();
            client.Close();
            listener.Stop();
            listViewCommand.Items.Add(new ListViewItem("Client disconnected. Server stopped!"));
        }
    }
}

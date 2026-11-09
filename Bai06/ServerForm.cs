using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;

namespace Bai06
{
    public partial class ServerForm : Form
    {
        public ServerForm()
        {
            InitializeComponent();
        }
        private ChatServer server;

        private void label1_Click(object sender, EventArgs e)
        {
        }

        private void btnListen_Click(object sender, EventArgs e)
        {
            int port;
            if (!int.TryParse(txtPort.Text, out port))
            {
                MessageBox.Show("Port không hợp lệ");
                return;
            }

            server = new ChatServer();
            server.LogReceived += AddLog;   // mỗi khi server có log thì gọi AddLog
            server.Start(port);
            btnListen.Enabled = false;
        }
        private void AddLog(string text)
        {
            if (lstLog.InvokeRequired)
            {
                lstLog.Invoke(new Action<string>(AddLog), text);
                return;
            }
            lstLog.Items.Add(DateTime.Now.ToString("HH:mm:ss") + "  " + text);
        }

        // Đóng form thì dừng server
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            server?.Stop();
            base.OnFormClosing(e);
        }
    }
}

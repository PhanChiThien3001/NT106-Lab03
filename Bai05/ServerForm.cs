using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;

namespace Bai05
{
    // Giu nguyen file ServerForm.Designer.cs (rong). Giao dien duoc dung bang code trong BuildUi().
    public partial class ServerForm : Form
    {
        private readonly TextBox txtPort = new TextBox();
        private readonly Button btnListen = new Button();
        private readonly ListBox lstLog = new ListBox();

        private FoodServer server;

        public ServerForm()
        {
            InitializeComponent();
            BuildUi();
        }

        private void BuildUi()
        {
            Text = "Hôm nay ăn gì? - Server";
            ClientSize = new Size(560, 420);

            Label lblPort = new Label { Text = "Port:", Location = new Point(12, 17), AutoSize = true };
            txtPort.Location = new Point(55, 14);
            txtPort.Size = new Size(70, 23);
            txtPort.Text = "8080";

            btnListen.Text = "Listen";
            btnListen.Location = new Point(135, 13);
            btnListen.Size = new Size(80, 25);
            btnListen.Click += BtnListen_Click;

            lstLog.Location = new Point(12, 50);
            lstLog.Size = new Size(536, 358);
            lstLog.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            lstLog.HorizontalScrollbar = true;

            Controls.AddRange(new Control[] { lblPort, txtPort, btnListen, lstLog });
        }

        private void BtnListen_Click(object sender, EventArgs e)
        {
            int port;
            if (!int.TryParse(txtPort.Text, out port) || port < 1 || port > 65535)
            {
                MessageBox.Show("Port không hợp lệ");
                return;
            }

            try
            {
                // File CSDL SQLite nam canh file .exe
                string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "monan.db");
                server = new FoodServer(new FoodRepository(dbPath));
                server.LogReceived += AddLog;
                server.Start(port);
                AddLog("Database: " + dbPath);
            }
            catch (SocketException ex)
            {
                MessageBox.Show("Không mở được port " + port + ": " + ex.Message);
                return;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khởi động server (kiểm tra đã cài System.Data.SQLite.Core chưa): " + ex.Message);
                return;
            }

            btnListen.Enabled = false;
            txtPort.Enabled = false;
            AddLog("IP của máy này (cho client nhập): " + GetLocalIps());
        }

        // AddLog duoc goi tu thread khac => phai Invoke (thread-safe call)
        private void AddLog(string text)
        {
            if (IsDisposed) return;
            if (lstLog.InvokeRequired)
            {
                try { lstLog.Invoke(new Action<string>(AddLog), text); }
                catch (ObjectDisposedException) { }
                catch (InvalidOperationException) { }
                return;
            }
            lstLog.Items.Add(DateTime.Now.ToString("HH:mm:ss") + "  " + text);
            lstLog.TopIndex = lstLog.Items.Count - 1;
        }

        private static string GetLocalIps()
        {
            List<string> list = new List<string>();
            foreach (IPAddress ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork) list.Add(ip.ToString());
            }
            return string.Join(", ", list);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (server != null) server.Stop();
            base.OnFormClosing(e);
        }
    }
}

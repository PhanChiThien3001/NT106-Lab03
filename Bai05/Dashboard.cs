using System;
using System.Drawing;
using System.Windows.Forms;

namespace Bai05
{
    public partial class Dashboard : Form
    {
        public Dashboard()
        {
            InitializeComponent();

            Text = "Bài 5 - Hôm nay ăn gì?";
            ClientSize = new Size(300, 130);
            StartPosition = FormStartPosition.CenterScreen;

            Button btnServer = new Button();
            btnServer.Text = "Mở Server";
            btnServer.Location = new Point(30, 30);
            btnServer.Size = new Size(110, 60);
            btnServer.Click += (s, e) => new ServerForm().Show();

            Button btnClient = new Button();
            btnClient.Text = "Mở Client";
            btnClient.Location = new Point(160, 30);
            btnClient.Size = new Size(110, 60);
            btnClient.Click += (s, e) => new ClientForm().Show();

            Controls.Add(btnServer);
            Controls.Add(btnClient);
        }
    }
}
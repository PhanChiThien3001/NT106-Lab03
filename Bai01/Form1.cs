
using NT106_Lab03;
using System;
using System.Windows.Forms;

namespace Bai01
{
    public partial class Form1 : Form
    {
        private UdpServerForm serverForm;
        private UdpClientForm clientForm;

        public Form1()
        {
            InitializeComponent();
        }

        // Mở giao diện UDP Server
        private void btnOpenServer_Click(object sender, EventArgs e)
        {
            if (serverForm == null || serverForm.IsDisposed)
            {
                serverForm = new UdpServerForm();
            }

            serverForm.Show();
            serverForm.BringToFront();
        }

        // Mở giao diện UDP Client
        private void btnOpenClient_Click(object sender, EventArgs e)
        {
            if (clientForm == null || clientForm.IsDisposed)
            {
                clientForm = new UdpClientForm();
            }

            clientForm.Show();
            clientForm.BringToFront();
        }
    }
}
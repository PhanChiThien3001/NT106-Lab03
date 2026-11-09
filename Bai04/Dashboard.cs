using System;
using System.Windows.Forms;

namespace Bai04
{
    /// <summary>Form điều hướng: mở Server hoặc mở thêm nhiều Client (mỗi Client là 1 quầy vé).</summary>
    public partial class Dashboard : Form
    {
        public Dashboard()
        {
            InitializeComponent();
        }

        private void btnServer_Click(object sender, EventArgs e)
        {
            btnServer.Enabled = false; // chỉ mở 1 server
            new ServerForm().Show();
        }

        private void btnClient_Click(object sender, EventArgs e)
        {
            new ClientForm().Show();
        }
    }
}

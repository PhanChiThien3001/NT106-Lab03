using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Bai03
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void btnOpenClient_Click(object sender, EventArgs e)
        {
            FormClient clientForm = new FormClient();
            clientForm.Show();
        }

        private void btnOpenServer_Click(object sender, EventArgs e)
        {
            FormServer serverForm = new FormServer();
            serverForm.Show();
        }

    }
}


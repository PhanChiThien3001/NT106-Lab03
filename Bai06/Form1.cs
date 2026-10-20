using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Bai06
{
    public partial class Lab03 : Form
    {
        public Lab03()
        {
            InitializeComponent();
        }

        private void btnClient_Click(object sender, EventArgs e)
        {
            new ServerForm().Show();
        }

        private void btnServer_Click(object sender, EventArgs e)
        {
            new ClientForm().Show();
        }
    }
}

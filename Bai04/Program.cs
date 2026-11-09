using System;
using System.Windows.Forms;

namespace Bai04
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            // .NET 6+: ApplicationConfiguration.Initialize();
            // .NET Framework: dùng 2 dòng dưới
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Dashboard());
        }
    }
}
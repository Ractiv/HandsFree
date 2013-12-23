using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;

namespace HandsFree
{
    class Program
    {
        static void Main(string[] args)
        {
            FileVersionInfo myFileVersionInfo = null;
            bool airNotInstalled = false;

            if (File.Exists("C:\\Program Files (x86)\\Common Files\\Adobe AIR\\Versions\\1.0\\Adobe AIR.dll"))
                myFileVersionInfo = FileVersionInfo.GetVersionInfo(@"C:\\Program Files (x86)\\Common Files\\Adobe AIR\\Versions\\1.0\\Adobe AIR.dll");
            else if (File.Exists("C:\\Program Files\\Common Files\\Adobe AIR\\Versions\\1.0\\Adobe AIR.dll"))
                myFileVersionInfo = FileVersionInfo.GetVersionInfo(@"C:\\Program Files\\Common Files\\Adobe AIR\\Versions\\1.0\\Adobe AIR.dll");
            else
                airNotInstalled = true;

            if (airNotInstalled || int.Parse(myFileVersionInfo.FileVersion.Replace(".", "")) < 3901030)
            {
                MessageBox.Show("Please install the latest Adobe Air");
                Process.Start("http://get.adobe.com/air/");
                Environment.Exit(0);
            }

            Main main = new Main(false);

            if (HandsFree.Properties.Settings.Default.FirstRun)
            {
                Process[] proc = Process.GetProcessesByName("HFGraphics");

                if (proc.Length > 0)
                    proc[0].Kill();

                Process.Start(new ProcessStartInfo("GUI\\HFGraphics.exe", "panel3"));
                proc = Process.GetProcessesByName("HFGraphics");

                while (proc.Length > 0)
                {
                    proc = Process.GetProcessesByName("HFGraphics");
                    Thread.Sleep(500);
                }

                HandsFree.Properties.Settings.Default.FirstRun = false;
                HandsFree.Properties.Settings.Default.Save();
            }

            main.Enabled = true;
            System.Windows.Forms.Application.Run();
        }
    }
}

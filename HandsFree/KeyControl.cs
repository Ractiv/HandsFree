using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace HandsFree
{
    public partial class KeyControl : Form
    {
        private const int APPCOMMAND_VOLUME_MUTE = 0x80000;
        private const int APPCOMMAND_VOLUME_UP = 0xA0000;
        private const int APPCOMMAND_VOLUME_DOWN = 0x90000;
        private const int WM_APPCOMMAND = 0x319;
        private const int APPCOMMAND_MEDIA_PLAY_PAUSE = 0xE0000;
        private const int APPCOMMAND_MEDIA_NEXTTRACK = 0xB0000;
        private const int APPCOMMAND_MEDIA_PREVIOUSTRACK = 0xC0000;

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessageW(IntPtr hWnd, int Msg,
            IntPtr wParam, IntPtr lParam);

        private NotifyIcon m_notifyicon;
        private ContextMenu m_menu = new ContextMenu();  

        public KeyControl()
        {
            InitializeComponent();
            m_notifyicon = new NotifyIcon();
            m_notifyicon.Text = "HandsFree"; 
            m_notifyicon.Visible = true;
            m_notifyicon.Icon = new Icon(GetType(), "icon_016.ico");
            m_menu.MenuItems.Add(0, new MenuItem("Exit", new System.EventHandler(Exit_Click)));
            m_menu.MenuItems.Add(1, new MenuItem("Show/Hide Visualizers", new System.EventHandler(Show_Hide)));
            m_notifyicon.ContextMenu = m_menu;
        }

        protected void Exit_Click(Object sender, EventArgs e)
        {
            System.Environment.Exit(0);
        }

        protected void Show_Hide(Object sender, EventArgs e)
        {
            if (OmniViewer.Suppressed) OmniViewer.Suppressed = false;
            else                      OmniViewer.CloseAll();
        }

        public void PausePlay()
        {
            SendMessageW(this.Handle, WM_APPCOMMAND, this.Handle,
                (IntPtr)APPCOMMAND_MEDIA_PLAY_PAUSE);

            try
            {
                Process[] proc = Process.GetProcessesByName("HFGraphics");
                proc[0].Kill();
            }
            catch (Exception) { }

            string[] args = { "panel0", "0", "\"aPlay\"" };
            Process.Start("GUI\\HFGraphics.exe", String.Join(" ", args));
        }

        public void Mute(object sender, EventArgs e)
        {
            SendMessageW(this.Handle, WM_APPCOMMAND, this.Handle,
                (IntPtr)APPCOMMAND_VOLUME_MUTE);
        }

        public void VolumeDown(object sender, EventArgs e)
        {
            SendMessageW(this.Handle, WM_APPCOMMAND, this.Handle,
                (IntPtr)APPCOMMAND_VOLUME_DOWN);
        }

        public void VolumeUp(object sender, EventArgs e)
        {
            SendMessageW(this.Handle, WM_APPCOMMAND, this.Handle,
                (IntPtr)APPCOMMAND_VOLUME_UP);
        }

        public void NextTrack()
        {
            SendMessageW(this.Handle, WM_APPCOMMAND, this.Handle,
                (IntPtr)APPCOMMAND_MEDIA_NEXTTRACK);

            try
            {
                Process[] proc = Process.GetProcessesByName("HFGraphics");
                proc[0].Kill();
            }
            catch (Exception) { }

            string[] args = { "panel0", "2", "\"aNext\"" };
            Process.Start("GUI\\HFGraphics.exe", String.Join(" ", args));
        }

        public void PrevTrack()
        {
            SendMessageW(this.Handle, WM_APPCOMMAND, this.Handle,
                (IntPtr)APPCOMMAND_MEDIA_PREVIOUSTRACK);

            try
            {
                Process[] proc = Process.GetProcessesByName("HFGraphics");
                proc[0].Kill();
            }
            catch (Exception) { }

            string[] args = { "panel0", "1", "\"aPrev\"" };
            Process.Start("GUI\\HFGraphics.exe", String.Join(" ", args));
        }

        public void Start()
        {
            try
            {
                Process[] proc = Process.GetProcessesByName("HFGraphics");
                proc[0].Kill();
            }
            catch (Exception) { }

            string[] args = { "panel0", "5", "\"aStart\"" };
            Process.Start("GUI\\HFGraphics.exe", String.Join(" ", args));
            SendKeys.Send("^{ESC}");
        }
    }
}

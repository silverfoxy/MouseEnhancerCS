using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Management;
using System.Threading;

namespace MouseEnhancerCS
{
    public partial class Form1 : Form
    {
        bool acceleration;
        string applicationName = "MouseEnhancerCS";
        bool menuIsOpen = false;
        int deviceCount;

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SystemParametersInfo(int uiAction, int uiParam, int[] pvParam, uint fWinIni);

        public Form1()
        {
            InitializeComponent();
            WindowState = FormWindowState.Minimized;
            //label_UsbMouseConnected.Text = "USB Mouse Connection state: Unknown";
            RegisterHidNotification();
            InitialAcceleration();

            /*deviceCount = 0;
            Thread.Sleep(5000);
            int numRetries = 10;
            do
            {
                try { deviceCount = NewDeviceCount(); break; }
                catch
                {
                    if (numRetries <= 0) break;  // improved to avoid silent failure
                    else Thread.Sleep(2000);
                }
            } while (numRetries-- > 0);
            */
            this.Hide();
            RegistryKey rkApp = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            if (rkApp.GetValue(applicationName) == null)
            {
                // The value doesn't exist, the application is not set to run at startup
                Startup.Checked = false;
            }
            else
            {
                // The value exists, the application is set to run at startup
                Startup.Checked = true;
            }
        }

        void InitialAcceleration()
        {
            int SPI_GETMOUSE = 0x0003;

            int[] mouseParams = new int[3];

            // Get the current values.
            SystemParametersInfo(SPI_GETMOUSE, 0, mouseParams, 0);

            if (mouseParams[2] == 0)
            {
                //label_EnhancePointerPrecision.Text = "Enhance pointer precision: Off";
                Enhancepointerprecision.Checked = false;
            }
            else
            {
                //label_EnhancePointerPrecision.Text = "Enhance pointer precision: On";
                Enhancepointerprecision.Checked = true;
            }
        }

        void SetAcceleration(int acceleration)
        {
            int SPI_GETMOUSE = 0x0003;
            int SPI_SETMOUSE = 0x0004;
            int SPI_GETWHEELSCROLLLINES = 0x0068;
            int SPI_SETWHEELSCROLLLINES = 0x0069;
            int scroll_lines = 0;
            int[] mouseParams = new int[3];

            // Get the current values.
            SystemParametersInfo(SPI_GETMOUSE, 0, mouseParams, 0);

            // Modify the acceleration value as directed.
            mouseParams[2] = acceleration;
            SystemParametersInfo(SPI_GETWHEELSCROLLLINES, scroll_lines, mouseParams, 0);
            if (acceleration == 0)
            {
                scroll_lines = 8;
                mouseParams[0] = 8;
                SystemParametersInfo(SPI_SETWHEELSCROLLLINES, scroll_lines, mouseParams, 0);
                // Update the system setting.
                SystemParametersInfo(SPI_SETMOUSE, 0, mouseParams, 0);
                //Enhancepointerprecision.Checked = false;
                //label_EnhancePointerPrecision.Text = "Enhance pointer precision: Off";
            }
            else
            {
                scroll_lines = 30;
                mouseParams[0] = 30;
                SystemParametersInfo(SPI_SETWHEELSCROLLLINES, scroll_lines, mouseParams, 0);
                // Update the system setting.
                SystemParametersInfo(SPI_SETMOUSE, 0, mouseParams, 0);
                Enhancepointerprecision.Checked = true;
                //label_EnhancePointerPrecision.Text = "Enhance pointer precision: On";
            }
        }


        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case Win32.WM_DEVICECHANGE: OnDeviceChange(ref m); break;
            }
            base.WndProc(ref m);
        }

        void OnDeviceChange(ref Message msg)
        {
            Menu.Close();
            int wParam = (int)msg.WParam;

            if (wParam == Win32.DBT_DEVICEARRIVAL)
            {
                int newDeviceCount = deviceCount;
                Thread.Sleep(5000);
                int numRetries = 10;
                do
                {
                    try { newDeviceCount = NewDeviceCount(); break; }
                    catch
                    {
                        if (numRetries <= 0) break;  // improved to avoid silent failure
                        else Thread.Sleep(2000);
                    }
                } while (numRetries-- > 0);

                if (newDeviceCount > deviceCount)
                {
                    deviceCount = newDeviceCount;
                    //label_UsbMouseConnected.Text = "USB Mouse Connected";
                    //usbMouseConnected = true;
                    SetAcceleration(0);
                    notifyIcon1.BalloonTipText = "USB Mouse Connected";
                    notifyIcon1.ShowBalloonTip(2000);
                }
            }
            else if (wParam == Win32.DBT_DEVICEREMOVECOMPLETE)
            {
                int newDeviceCount = deviceCount;
                Thread.Sleep(5000);
                int numRetries = 10;
                do
                {
                    try { newDeviceCount = NewDeviceCount(); break; }
                    catch
                    {
                        if (numRetries <= 0) break;  // improved to avoid silent failure
                        else Thread.Sleep(2000);
                    }
                } while (numRetries-- > 0);

                if (newDeviceCount < deviceCount)
                {
                    deviceCount = newDeviceCount;
                    //label_UsbMouseConnected.Text = "USB Mouse Disconnected";
                    //usbMouseConnected = false;
                    SetAcceleration(1);
                    notifyIcon1.BalloonTipText = "USB Mouse Disconnected";
                    notifyIcon1.ShowBalloonTip(2000);
                }
            }
        }

        int NewDeviceCount()
        {
            ManagementScope scope = new ManagementScope("\\\\.\\ROOT\\cimv2");
            ObjectQuery query = new ObjectQuery("SELECT * FROM Win32_PointingDevice");
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, query);
            ManagementObjectCollection queryCollection = searcher.Get();
            return queryCollection.Count;
        }

        void RegisterHidNotification()
        {
            Win32.DEV_BROADCAST_DEVICEINTERFACE dbi = new
            Win32.DEV_BROADCAST_DEVICEINTERFACE();
            int size = Marshal.SizeOf(dbi);
            dbi.dbcc_size = size;
            dbi.dbcc_devicetype = Win32.DBT_DEVTYP_DEVICEINTERFACE;
            dbi.dbcc_reserved = 0;
            dbi.dbcc_classguid = Win32.GUID_DEVINTERFACE_HID;
            dbi.dbcc_name = 0;
            IntPtr buffer = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(dbi, buffer, true);
            IntPtr r = Win32.RegisterDeviceNotification(Handle, buffer, Win32.DEVICE_NOTIFY_WINDOW_HANDLE);
            if (r == IntPtr.Zero)
                MessageBox.Show(Win32.GetLastError().ToString(), "Error occured!", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                //label_UsbMouseConnected.Text = Win32.GetLastError().ToString();
        }

        private void notifyIcon1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                Menu.Show(Cursor.Position.X, Cursor.Position.Y);
                menuIsOpen = true;
            }
            else
                if (menuIsOpen)
                {
                    Menu.Hide();
                    menuIsOpen = false;
                }
        }

        private void Exit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void togglePointerPrecisionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (acceleration)
            {
                acceleration = false;
                SetAcceleration(0);
            }
            else
            {
                acceleration = true;
                SetAcceleration(1);
            }
        }

        private void runAtStartUpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                RegistryKey rkApp = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                if (rkApp.GetValue(applicationName) == null)
                {
                    rkApp.SetValue("MouseEnhancerCS", Application.ExecutablePath.ToString());
                    Startup.Checked = true;
                }
                else
                {
                    rkApp.DeleteValue("MouseEnhancerCS", true);
                    Startup.Checked = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error occured!", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
            }

        }

        private void Menu_MouseLeave(object sender, EventArgs e)
        {
            Menu.Hide();
            menuIsOpen = false;
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (acceleration)
            {
                acceleration = false;
                SetAcceleration(0);
            }
            else
            {
                acceleration = true;
                SetAcceleration(1);
            }
        }
    }
}


    
